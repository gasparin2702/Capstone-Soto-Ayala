using System;
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

namespace Sitrac.Service
{
    public class NotificacionService : INotificacionService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly TeamsSettings _teamsSettings;
        private readonly ILogger<NotificacionService> _logger;

        private static readonly SemaphoreSlim _sendLock = new(1, 1);
        private static DateTime _ultimoEnvio = DateTime.MinValue;

        private static readonly HttpClient _httpClient = new HttpClient();

        public NotificacionService(
            IOptions<SmtpSettings> smtpOptions,
            IOptions<TeamsSettings> teamsOptions,
            ILogger<NotificacionService> logger)
        {
            _smtpSettings = smtpOptions.Value;
            _teamsSettings = teamsOptions.Value;
            _logger = logger;
        }

        public async Task<ResultadoEnvio> EnviarAlertaNuevoIncidenteAsync(
            string destinatario,
            string ticketId,
            string sistema,
            int horasSla,
            CancellationToken cancellationToken = default)
        {
            if (!_smtpSettings.EstaConfigurado())
            {
                var motivo = "SMTP no configurado (falta Host/UserName/Password).";
                _logger.LogWarning("[Notificacion][Email] Evento: ConfiguracionInvalida Ticket: {Ticket} Motivo: {Motivo}",
                    ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            if (!EsEmailValido(destinatario))
            {
                var motivo = $"Destinatario inválido: {destinatario}";
                _logger.LogWarning("[Notificacion][Email] Evento: DestinatarioInvalido Ticket: {Ticket} Motivo: {Motivo}",
                    ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            var asunto = $"[URGENTE] Nuevo Incidente Registrado - Ley N° 21.663 ({ticketId})";
            var cuerpoHtml = ConstruirCuerpoEmailAsync(ticketId, sistema, horasSla);

            return await EnviarEmailConReintentosAsync(destinatario, asunto, cuerpoHtml, ticketId, cancellationToken);
        }

        public async Task<ResultadoEnvio> EnviarAlertaTeamsAsync(
            string ticketId,
            string sistema,
            int gravedad,
            string clasificacion,
            string estado,
            double prioridadScore,
            string hitoPendiente,
            string? razon,
            CancellationToken cancellationToken = default)
        {
            if (!_teamsSettings.EstaConfigurado())
            {
                var motivo = "Teams no configurado (falta Habilitado o WebhookUrl).";
                _logger.LogInformation("[Notificacion][Teams] Evento: Omitido Ticket: {Ticket} Motivo: {Motivo}",
                    ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            if (prioridadScore < _teamsSettings.UmbralPrioridadMinima)
            {
                var motivo = $"Prioridad {prioridadScore:0.00} bajo umbral {_teamsSettings.UmbralPrioridadMinima}.";
                _logger.LogInformation("[Notificacion][Teams] Evento: Omitido Ticket: {Ticket} Motivo: {Motivo}",
                    ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            try
            {
                var payload = ConstruirPayloadTeamsAdaptiveCard(
                    ticketId, sistema, gravedad, clasificacion, estado, prioridadScore, hitoPendiente, razon);

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_teamsSettings.TimeoutMs);

                var response = await _httpClient.PostAsync(_teamsSettings.WebhookUrl, content, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    var error = $"HTTP {(int)response.StatusCode}: {body}";
                    _logger.LogWarning(
                        "[Notificacion][Teams] Evento: FalloEnvio Ticket: {Ticket} Score: {Score} Error: {Error}",
                        ticketId, prioridadScore, error);
                    return ResultadoEnvio.Fallo(error);
                }

                _logger.LogInformation(
                    "[Notificacion][Teams] Evento: Enviado Ticket: {Ticket} Score: {Score}",
                    ticketId, prioridadScore);
                return ResultadoEnvio.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Notificacion][Teams] Evento: Error Ticket: {Ticket} Score: {Score}",
                    ticketId, prioridadScore);
                return ResultadoEnvio.Fallo(ex.Message);
            }
        }

        private async Task<ResultadoEnvio> EnviarEmailConReintentosAsync(
            string destinatario,
            string asunto,
            string cuerpoHtml,
            string ticketId,
            CancellationToken cancellationToken)
        {
            int intento = 0;
            string ultimoError = string.Empty;

            while (intento <= _smtpSettings.MaxReintentos)
            {
                intento++;
                try
                {
                    await EspaciarEnvioAsync(cancellationToken);
                    await EnviarEmailAsync(destinatario, asunto, cuerpoHtml, cancellationToken);
                    _logger.LogInformation(
                        "[Notificacion][Email] Evento: Enviado Destinatario: {Destinatario} Ticket: {Ticket} Intento: {Intento}",
                        destinatario, ticketId, intento);
                    return ResultadoEnvio.Ok();
                }
                catch (Exception ex)
                {
                    ultimoError = ex.Message;
                    _logger.LogWarning(ex,
                        "[Notificacion][Email] Evento: FalloIntento Destinatario: {Destinatario} Ticket: {Ticket} Intento: {Intento}",
                        destinatario, ticketId, intento);
                    if (intento <= _smtpSettings.MaxReintentos)
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }

            _logger.LogError(
                "[Notificacion][Email] Evento: FalloDefinitivo Destinatario: {Destinatario} Ticket: {Ticket} Error: {Error}",
                destinatario, ticketId, ultimoError);
            return ResultadoEnvio.Fallo(ultimoError);
        }

        private async Task EspaciarEnvioAsync(CancellationToken ct)
        {
            await _sendLock.WaitAsync(ct);
            try
            {
                var desdeUltimo = (DateTime.UtcNow - _ultimoEnvio).TotalMilliseconds;
                if (desdeUltimo < _smtpSettings.IntervaloEntreEnviosMs)
                {
                    var espera = (int)(_smtpSettings.IntervaloEntreEnviosMs - desdeUltimo);
                    await Task.Delay(espera, ct);
                }
                _ultimoEnvio = DateTime.UtcNow;
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

            await client.ConnectAsync(
                _smtpSettings.Host,
                _smtpSettings.Port,
                _smtpSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                ct);

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

        private static object ConstruirPayloadTeamsAdaptiveCard(
            string ticketId,
            string sistema,
            int gravedad,
            string clasificacion,
            string estado,
            double prioridadScore,
            string hitoPendiente,
            string? razon)
        {
            var etiqueta = PrioridadCalculator.Etiqueta(prioridadScore);
            var emojiGravedad = PrioridadCalculator.EmojiGravedad(gravedad);
            var nombreGravedad = PrioridadCalculator.NombreGravedad(gravedad);

            var facts = new object[]
            {
                new { title = "Ticket:",          value = ticketId },
                new { title = "Prioridad:",       value = $"{etiqueta} ({prioridadScore:0.00})" },
                new { title = "Gravedad:",        value = $"{emojiGravedad} {nombreGravedad}" },
                new { title = "Clasificación:",   value = clasificacion },
                new { title = "Estado actual:",   value = estado },
                new { title = "Próximo hito:",    value = hitoPendiente },
                new { title = "Sistema:",         value = sistema }
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
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = "Ingresar a la consola SITRAC para revisar el caso y confirmar acciones.",
                                    wrap = true,
                                    isSubtle = true,
                                    size = "Small",
                                    spacing = "Medium"
                                }
                            }
                        }
                    }
                }
            };
        }

        private static string ConstruirCuerpoEmailAsync(string ticketId, string sistema, int horasSla)
        {
            var ticketSafe = System.Net.WebUtility.HtmlEncode(ticketId);
            var sistemaSafe = System.Net.WebUtility.HtmlEncode(sistema);
            var horasSafe = System.Net.WebUtility.HtmlEncode(horasSla.ToString());

            return $@"
<div style='font-family: Arial, sans-serif; max-width: 640px;'>
  <div style='background:#0f1b2a;color:#fff;padding:16px 20px;border-radius:8px 8px 0 0'>
    <h2 style='margin:0'>SITRAC · ANCI — Alerta de Seguridad</h2>
    <p style='margin:4px 0 0;font-size:12px;color:#a9b6c6'>Ley N° 21.663</p>
  </div>
  <div style='border:1px solid #d5dbdb;border-top:none;padding:20px;border-radius:0 0 8px 8px'>
    <p>Se ha registrado un nuevo incidente que requiere evaluación inmediata.</p>
    <table style='border-collapse:collapse;width:100%;margin-top:15px'>
      <tr><td style='padding:10px;border:1px solid #e9ebed;background:#f2f3f3;font-weight:bold'>Ticket:</td>
          <td style='padding:10px;border:1px solid #e9ebed'>{ticketSafe}</td></tr>
      <tr><td style='padding:10px;border:1px solid #e9ebed;background:#f2f3f3;font-weight:bold'>Sistema:</td>
          <td style='padding:10px;border:1px solid #e9ebed'>{sistemaSafe}</td></tr>
      <tr><td style='padding:10px;border:1px solid #e9ebed;background:#f2f3f3;font-weight:bold'>Aviso Temprano:</td>
          <td style='padding:10px;border:1px solid #e9ebed;color:#c4161c;font-weight:bold'>{horasSafe} horas</td></tr>
    </table>
    <p style='margin-top:20px'>Ingrese a la consola SITRAC para revisar el caso.</p>
  </div>
</div>";
        }
    }
}