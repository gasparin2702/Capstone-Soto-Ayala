using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Sitrac.Service.Configuration;
using Sitrac.Service.Dtos;
using Sitrac.Service.Helpers;
using Sitrac.Service.Interfaces;
using Sitrac.Repository.Models;

namespace Sitrac.Service
{
    public class NotificacionService : INotificacionService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly TeamsSettings _teamsSettings;
        private readonly FrontendSettings _frontendSettings;
        private readonly ILogger<NotificacionService> _logger;

        private static readonly SemaphoreSlim _sendLock = new(1, 1);
        private static DateTime _nextAllowedSendTime = DateTime.MinValue;
        private static readonly HttpClient _httpClient = new HttpClient();

        public NotificacionService(
            IOptions<SmtpSettings> smtpOptions,
            IOptions<TeamsSettings> teamsOptions,
            IOptions<FrontendSettings> frontendOptions,
            ILogger<NotificacionService> logger)
        {
            _smtpSettings = smtpOptions.Value;
            _teamsSettings = teamsOptions.Value;
            _frontendSettings = frontendOptions.Value;
            _logger = logger;
        }

        public async Task<ResultadoEnvio> EnviarAlertaNuevoIncidenteAsync(
            long idIncidente, string destinatario, string ticketId, string sistema, int horasSla, CancellationToken cancellationToken = default)
        {
            if (!_smtpSettings.EstaConfigurado())
            {
                var motivo = "SMTP no configurado (falta Host/UserName/Password).";
                _logger.LogWarning("[Notificacion][Email] Evento: ConfiguracionInvalida Ticket: {Ticket} Motivo: {Motivo}", ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            if (!EsEmailValido(destinatario))
            {
                var motivo = $"Destinatario inválido: {destinatario}";
                _logger.LogWarning("[Notificacion][Email] Evento: DestinatarioInvalido Ticket: {Ticket} Motivo: {Motivo}", ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            var asunto = $"[URGENTE] Nuevo Incidente Registrado - Ley N° 21.663 ({ticketId})";
            var cuerpoHtml = ConstruirCuerpoEmail(idIncidente, ticketId, sistema, horasSla);

            return await EnviarEmailConReintentosAsync(destinatario, asunto, cuerpoHtml, ticketId, cancellationToken);
        }

        public async Task<ResultadoEnvio> EnviarAlertaTeamsAsync(
            long idIncidente, string ticketId, string sistema, int gravedad, string clasificacion, string estado, double prioridadScore, string hitoPendiente, string? razon, CancellationToken cancellationToken = default)
        {
            if (!_teamsSettings.EstaConfigurado())
            {
                var motivo = "Teams no configurado (falta Habilitado o WebhookUrl).";
                _logger.LogInformation("[Notificacion][Teams] Evento: Omitido Ticket: {Ticket} Motivo: {Motivo}", ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            if (prioridadScore < _teamsSettings.UmbralPrioridadMinima)
            {
                var motivo = $"Prioridad {prioridadScore:0.00} bajo umbral {_teamsSettings.UmbralPrioridadMinima}.";
                _logger.LogInformation("[Notificacion][Teams] Evento: Omitido Ticket: {Ticket} Motivo: {Motivo}", ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            try
            {
                var payload = ConstruirPayloadTeamsAdaptiveCard(idIncidente, ticketId, sistema, gravedad, clasificacion, estado, prioridadScore, hitoPendiente, razon);

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_teamsSettings.TimeoutMs);

                var response = await _httpClient.PostAsync(_teamsSettings.WebhookUrl, content, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    var error = $"HTTP {(int)response.StatusCode}: {body}";
                    _logger.LogWarning("[Notificacion][Teams] Evento: FalloEnvio Ticket: {Ticket} Score: {Score} Error: {Error}", ticketId, prioridadScore, error);
                    return ResultadoEnvio.Fallo(error);
                }

                _logger.LogInformation("[Notificacion][Teams] Evento: Enviado Ticket: {Ticket} Score: {Score}", ticketId, prioridadScore);
                return ResultadoEnvio.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notificacion][Teams] Evento: Error Ticket: {Ticket} Score: {Score}", ticketId, prioridadScore);
                return ResultadoEnvio.Fallo(ex.Message);
            }
        }

        public async Task<ResultadoEnvio> EnviarResumenConsolidadoAsync(List<NotificacionLog> pendientes, CancellationToken cancellationToken = default)
        {
            if (pendientes == null || pendientes.Count == 0) return ResultadoEnvio.Ok();

            var correos = pendientes.Where(p => p.Canal == "Email").ToList();
            var teams = pendientes.Where(p => p.Canal == "Teams").ToList();

            var resultadoGeneral = ResultadoEnvio.Ok();

            // 1. Procesar Correos
            if (correos.Any())
            {
                var destinatario = _smtpSettings.DestinatarioDemo;
                var asunto = correos.Count == 1 
                    ? $"[URGENTE] Nuevo Incidente Registrado - Ley N° 21.663"
                    : $"[RESUMEN] {correos.Count} Nuevos Incidentes requieren atención - Ley N° 21.663";
                
                var cuerpo = ConstruirCuerpoEmailConsolidado(correos);
                var res = await EnviarEmailConReintentosAsync(destinatario, asunto, cuerpo, "CONSOLIDADO", cancellationToken);
                if (!res.Exito) resultadoGeneral = res;
            }

            // 2. Procesar Teams
            if (teams.Any() && _teamsSettings.EstaConfigurado())
            {
                try
                {
                    var payload = ConstruirPayloadTeamsConsolidado(teams);
                    var json = JsonSerializer.Serialize(payload);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_teamsSettings.TimeoutMs);

                    var response = await _httpClient.PostAsync(_teamsSettings.WebhookUrl, content, cts.Token);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("[Notificacion][Teams] Evento: FalloEnvioConsolidado Error: HTTP {Status} - {Body}", (int)response.StatusCode, body);
                        resultadoGeneral = ResultadoEnvio.Fallo($"HTTP {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Notificacion][Teams] Evento: ErrorConsolidado");
                    resultadoGeneral = ResultadoEnvio.Fallo(ex.Message);
                }
            }

            return resultadoGeneral;
        }

        private async Task<ResultadoEnvio> EnviarEmailConReintentosAsync(string destinatario, string asunto, string cuerpoHtml, string ticketId, CancellationToken cancellationToken)
        {
            int intento = 0;
            string ultimoError = string.Empty;
            Random random = new Random();

            while (intento <= _smtpSettings.MaxReintentos)
            {
                intento++;
                try
                {
                    await EspaciarEnvioAsync(cancellationToken);
                    await EnviarEmailAsync(destinatario, asunto, cuerpoHtml, cancellationToken);
                    _logger.LogInformation("[Notificacion][Email] Evento: Enviado Destinatario: {Destinatario} Ticket: {Ticket} Intento: {Intento}", destinatario, ticketId, intento);
                    return ResultadoEnvio.Ok();
                }
                catch (Exception ex)
                {
                    ultimoError = ex.Message;
                    _logger.LogWarning(ex, "[Notificacion][Email] Evento: FalloIntento Destinatario: {Destinatario} Ticket: {Ticket} Intento: {Intento}", destinatario, ticketId, intento);
                    if (intento <= _smtpSettings.MaxReintentos)
                    {
                        int baseDelayMs = 100;
                        int maxDelayMs = 10000;
                        int delayMs = Math.Min(baseDelayMs * (1 << (intento - 1)), maxDelayMs);
                        int jitterMs = random.Next(0, 100);
                        await Task.Delay(delayMs + jitterMs, cancellationToken);
                    }
                }
            }

            _logger.LogError("[Notificacion][Email] Evento: FalloDefinitivo Destinatario: {Destinatario} Ticket: {Ticket} Error: {Error}", destinatario, ticketId, ultimoError);
            return ResultadoEnvio.Fallo(ultimoError);
        }

        private async Task EspaciarEnvioAsync(CancellationToken ct)
        {
            await _sendLock.WaitAsync(ct);
            try
            {
                var ahora = DateTime.UtcNow;
                if (ahora < _nextAllowedSendTime)
                {
                    var espera = (int)(_nextAllowedSendTime - ahora).TotalMilliseconds;
                    await Task.Delay(espera, ct);
                }
                _nextAllowedSendTime = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(_smtpSettings.IntervaloEntreEnviosMs));
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task EnviarEmailAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken ct)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.NombreRemitente, _smtpSettings.UserName));
            message.To.Add(MailboxAddress.Parse(destinatario));
            message.Subject = asunto;

            var bodyBuilder = new BodyBuilder { HtmlBody = cuerpoHtml };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = _smtpSettings.TimeoutMs;

            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, _smtpSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
            await client.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }

        private static bool EsEmailValido(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try { return MailboxAddress.Parse(email).Address == email; }
            catch { return false; }
        }

        private class TeamsFact
        {
            public string title { get; set; }
            public string value { get; set; }

            public TeamsFact(string title, string value)
            {
                this.title = title;
                this.value = value;
            }
        }

        private object ConstruirPayloadTeamsAdaptiveCard(
            long idIncidente, string ticketId, string sistema, int gravedad, string clasificacion, string estado, double prioridadScore, string hitoPendiente, string? razon)
        {
            var etiqueta = PrioridadCalculator.Etiqueta(prioridadScore);
            var emojiGravedad = PrioridadCalculator.EmojiGravedad(gravedad);
            var nombreGravedad = PrioridadCalculator.NombreGravedad(gravedad);
            var urlDirecta = $"{_frontendSettings.BaseUrl}/index.html?incidente={idIncidente}";

            var facts = new TeamsFact[]
            {
                new TeamsFact("Ticket:", ticketId),
                new TeamsFact("Prioridad:", $"{etiqueta} ({prioridadScore:0.00})"),
                new TeamsFact("Gravedad:", $"{emojiGravedad} {nombreGravedad}"),
                new TeamsFact("Clasificación:", clasificacion),
                new TeamsFact("Estado actual:", estado),
                new TeamsFact("Próximo hito:", hitoPendiente),
                new TeamsFact("Sistema:", sistema)
            };

            return new
            {
                type = "message",
                attachments = new object[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        contentUrl = (string?)null,
                        content = new
                        {
                            type = "AdaptiveCard",
                            version = "1.4",
                            body = new object[]
                            {
                                new
                                {
                                    type = "Container",
                                    style = "emphasis",
                                    items = new object[]
                                    {
                                        new { type = "TextBlock", text = "ALERTA SITRAC-ANCI", weight = "Bolder", size = "Large", color = "Attention" },
                                        new { type = "TextBlock", text = "Ley N° 21.663 · Marco de Ciberseguridad", isSubtle = true, size = "Small", spacing = "None", wrap = true },
                                        new { type = "TextBlock", text = " ", spacing = "None" },
                                        new { type = "FactSet", facts = facts }
                                    }
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = $"⚠️ {razon ?? "Nuevo incidente registrado"}",
                                    wrap = true,
                                    color = "Attention",
                                    weight = "Bolder",
                                    spacing = "Medium"
                                }
                            },
                            actions = new object[]
                            {
                                new
                                {
                                    type = "Action.OpenUrl",
                                    title = "🔗 Revisar Incidente en SITRAC",
                                    url = urlDirecta
                                }
                            }
                        }
                    }
                }
            };
        }

        private object ConstruirPayloadTeamsConsolidado(List<NotificacionLog> pendientes)
        {
            var urlDirecta = $"{_frontendSettings.BaseUrl}/index.html";
            
            // Agrupación por Gravedad
            var criticos = pendientes.Where(p => p.CodTipoGravedad == 4).ToList();
            var altos = pendientes.Where(p => p.CodTipoGravedad == 3).ToList();
            var normales = pendientes.Where(p => p.CodTipoGravedad <= 2).ToList();

            var listaItems = new List<object>();

            if (criticos.Any())
            {
                listaItems.Add(new { type = "TextBlock", text = $"🔴 **CRÍTICOS ({criticos.Count})**", color = "Attention", weight = "Bolder", spacing = "Medium" });
                foreach (var item in criticos) listaItems.Add(new { type = "TextBlock", text = $"• [{item.Destinatario}] {item.SistemaAfectado}", wrap = true, spacing = "None" });
            }
            if (altos.Any())
            {
                listaItems.Add(new { type = "TextBlock", text = $"🟠 **ALTOS ({altos.Count})**", color = "Warning", weight = "Bolder", spacing = "Medium" });
                foreach (var item in altos) listaItems.Add(new { type = "TextBlock", text = $"• [{item.Destinatario}] {item.SistemaAfectado}", wrap = true, spacing = "None" });
            }
            if (normales.Any())
            {
                listaItems.Add(new { type = "TextBlock", text = $"🟡 **MEDIOS/BAJOS ({normales.Count})**", color = "Default", weight = "Bolder", spacing = "Medium" });
                foreach (var item in normales) listaItems.Add(new { type = "TextBlock", text = $"• [{item.Destinatario}] {item.SistemaAfectado}", wrap = true, spacing = "None" });
            }

            return new
            {
                type = "message",
                attachments = new object[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        contentUrl = (string?)null,
                        content = new
                        {
                            type = "AdaptiveCard",
                            version = "1.4",
                            body = new object[]
                            {
                                new
                                {
                                    type = "Container",
                                    style = "attention",
                                    items = new object[]
                                    {
                                        new { type = "TextBlock", text = $"🚨 {pendientes.Count} Incidentes requieren atención", weight = "Bolder", size = "Large", color = "Attention" },
                                        new { type = "TextBlock", text = "Resumen consolidado de los últimos minutos", isSubtle = true, size = "Small", wrap = true },
                                    }
                                },
                                new
                                {
                                    type = "Container",
                                    items = listaItems.ToArray()
                                }
                            },
                            actions = new object[]
                            {
                                new
                                {
                                    type = "Action.OpenUrl",
                                    title = "🔗 Ir a la Consola SITRAC",
                                    url = urlDirecta
                                }
                            }
                        }
                    }
                }
            };
        }

        private string ConstruirCuerpoEmail(long idIncidente, string ticketId, string sistema, int horasSla)
        {
            var ticketSafe = System.Net.WebUtility.HtmlEncode(ticketId);
            var sistemaSafe = System.Net.WebUtility.HtmlEncode(sistema);
            var horasSafe = System.Net.WebUtility.HtmlEncode(horasSla.ToString());
            var urlDirecta = $"{_frontendSettings.BaseUrl}/index.html?incidente={idIncidente}";

            var sb = new StringBuilder();
            sb.Append("<div style='font-family: Arial, sans-serif; max-width: 640px;'>");
            sb.Append("<div style='background:#0f1b2a;color:#fff;padding:16px 20px;border-radius:8px 8px 0 0'>");
            sb.Append("<h2 style='margin:0'>SITRAC · ANCI — Alerta de Seguridad</h2>");
            sb.Append("<p style='margin:4px 0 0;font-size:12px;color:#a9b6c6'>Ley N° 21.663</p>");
            sb.Append("</div>");
            sb.Append("<div style='border:1px solid #d5dbdb;border-top:none;padding:20px;border-radius:0 0 8px 8px'>");
            sb.Append("<p>Se ha registrado un nuevo incidente que requiere evaluación inmediata.</p>");
            sb.Append("<table style='border-collapse:collapse;width:100%;margin-top:15px'>");
            sb.Append("<tr><td style='padding:10px;border:1px solid #e9ebed;background:#f2f3f3;font-weight:bold'>Ticket:</td>");
            sb.Append("<td style='padding:10px;border:1px solid #e9ebed'>").Append(ticketSafe).Append("</td></tr>");
            sb.Append("<tr><td style='padding:10px;border:1px solid #e9ebed;background:#f2f3f3;font-weight:bold'>Sistema:</td>");
            sb.Append("<td style='padding:10px;border:1px solid #e9ebed'>").Append(sistemaSafe).Append("</td></tr>");
            sb.Append("<tr><td style='padding:10px;border:1px solid #e9ebed;background:#f2f3f3;font-weight:bold'>Aviso Temprano:</td>");
            sb.Append("<td style='padding:10px;border:1px solid #e9ebed;color:#c4161c;font-weight:bold'>").Append(horasSafe).Append(" horas</td></tr>");
            sb.Append("</table>");
            
            sb.Append("<div style='text-align: center; margin-top: 25px;'>");
            sb.AppendFormat("<a href='{0}' style='background-color: #0972d3; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Revisar Incidente en SITRAC</a>", urlDirecta);
            sb.Append("</div>");

            sb.Append("<p style='margin-top:20px; font-size:12px; color:#6b7785;'>Ingrese a la consola SITRAC para revisar el caso y confirmar acciones.</p>");
            sb.Append("</div>");
            sb.Append("</div>");

            return sb.ToString();
        }

        private string ConstruirCuerpoEmailConsolidado(List<NotificacionLog> pendientes)
        {
            var urlDirecta = $"{_frontendSettings.BaseUrl}/index.html";
            var sb = new StringBuilder();
            sb.Append("<div style='font-family: Arial, sans-serif; max-width: 640px;'>");
            sb.Append("<div style='background:#0f1b2a;color:#fff;padding:16px 20px;border-radius:8px 8px 0 0'>");
            sb.Append("<h2 style='margin:0'>SITRAC · ANCI — Resumen de Alertas</h2>");
            sb.Append("<p style='margin:4px 0 0;font-size:12px;color:#a9b6c6'>Ley N° 21.663</p>");
            sb.Append("</div>");
            sb.Append("<div style='border:1px solid #d5dbdb;border-top:none;padding:20px;border-radius:0 0 8px 8px'>");
            sb.Append($"<p>Se han registrado <b>{pendientes.Count} incidentes</b> que requieren tu atención inmediata en los últimos minutos.</p>");
            
            // Agrupación por Gravedad
            var criticos = pendientes.Where(p => p.CodTipoGravedad == 4).ToList();
            var altos = pendientes.Where(p => p.CodTipoGravedad == 3).ToList();
            var normales = pendientes.Where(p => p.CodTipoGravedad <= 2).ToList();

            if (criticos.Any())
            {
                sb.Append("<h3 style='color:#c4161c;margin-top:20px;'>🔴 Críticos (").Append(criticos.Count).Append(")</h3><ul style='padding-left:20px'>");
                foreach(var p in criticos) sb.Append($"<li style='margin-bottom:5px'>Ticket: <b>{p.Destinatario}</b> - {p.SistemaAfectado}</li>");
                sb.Append("</ul>");
            }
            if (altos.Any())
            {
                sb.Append("<h3 style='color:#F58220;margin-top:20px;'>🟠 Altos (").Append(altos.Count).Append(")</h3><ul style='padding-left:20px'>");
                foreach(var p in altos) sb.Append($"<li style='margin-bottom:5px'>Ticket: <b>{p.Destinatario}</b> - {p.SistemaAfectado}</li>");
                sb.Append("</ul>");
            }
            if (normales.Any())
            {
                sb.Append("<h3 style='color:#8F7D00;margin-top:20px;'>🟡 Medios/Bajos (").Append(normales.Count).Append(")</h3><ul style='padding-left:20px'>");
                foreach(var p in normales) sb.Append($"<li style='margin-bottom:5px'>Ticket: <b>{p.Destinatario}</b> - {p.SistemaAfectado}</li>");
                sb.Append("</ul>");
            }

            sb.Append("<div style='text-align: center; margin-top: 25px;'>");
            sb.AppendFormat("<a href='{0}' style='background-color: #0972d3; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Ir a la Consola SITRAC</a>", urlDirecta);
            sb.Append("</div>");

            sb.Append("<p style='margin-top:20px; font-size:12px; color:#6b7785;'>Ingrese a la consola SITRAC para revisar los casos pendientes.</p>");
            sb.Append("</div>");
            sb.Append("</div>");

            return sb.ToString();
        }
    }
}