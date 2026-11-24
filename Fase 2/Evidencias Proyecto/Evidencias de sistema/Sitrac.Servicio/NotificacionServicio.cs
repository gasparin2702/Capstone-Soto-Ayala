using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Sitrac.Servicio.Configuration;
using Sitrac.Servicio.Dtos;
using Sitrac.Servicio.Helpers;
using Sitrac.Servicio.Interfaces;
using Sitrac.Repositorio.Interfaces;
using Sitrac.Repositorio.Models;

namespace Sitrac.Servicio
{
    public class NotificacionServicio : INotificacionServicio
    {
        private readonly ConfiguracionGraph _graphSettings;
        private readonly ConfiguracionTeams _teamsSettings;
        private readonly ConfiguracionFrontend _frontendSettings;
        private readonly IResponsableRepositorio _responsableRepository;
        private readonly ILogger<NotificacionServicio> _logger;
        private readonly Lazy<GraphServiceClient?> _clienteGraph;

        private static readonly SemaphoreSlim _sendLock = new(1, 1);
        private static DateTime _nextAllowedSendTime = DateTime.MinValue;
        private static readonly HttpClient _httpClient = new HttpClient();

        public NotificacionServicio(
            IOptions<ConfiguracionGraph> graphOptions,
            IOptions<ConfiguracionTeams> teamsOptions,
            IOptions<ConfiguracionFrontend> frontendOptions,
            IResponsableRepositorio responsableRepository,
            ILogger<NotificacionServicio> logger)
        {
            _graphSettings = graphOptions.Value;
            _teamsSettings = teamsOptions.Value;
            _frontendSettings = frontendOptions.Value;
            _responsableRepository = responsableRepository;
            _logger = logger;
            _clienteGraph = new Lazy<GraphServiceClient?>(CrearClienteGraph, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private GraphServiceClient? CrearClienteGraph()
        {
            if (!_graphSettings.EstaConfigurado()) return null;

            var credencial = new ClientSecretCredential(
                _graphSettings.TenantId,
                _graphSettings.ClientId,
                _graphSettings.ClientSecret);

            return new GraphServiceClient(credencial, new[] { "https://graph.microsoft.com/.default" });
        }

        public async Task<ResultadoEnvio> EnviarAlertaNuevoIncidenteAsync(
            long idIncidente, string destinatario, string ticketId, string sistema, int horasSla, CancellationToken cancellationToken = default)
        {
            if (!_graphSettings.EstaConfigurado())
            {
                var motivo = "Microsoft Graph no configurado (faltan TenantId, ClientId, ClientSecret o RemitenteUpn).";
                _logger.LogWarning("[Notificacion][Email] Evento: ConfiguracionInvalida Ticket: {Ticket} Motivo: {Motivo}", ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            var destinatarios = await ResolverDestinatariosCorreoAsync(cancellationToken);
            if (destinatarios.Count == 0)
            {
                var motivo = "No hay destinatarios de correo configurados ni delegados de ciberseguridad activos.";
                _logger.LogWarning("[Notificacion][Email] Evento: DestinatariosNoDisponibles Ticket: {Ticket} Solicitado: {Solicitado} Motivo: {Motivo}", ticketId, destinatario, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            var asunto = $"[URGENTE] Nuevo Incidente Registrado - Ley N° 21.663 ({ticketId})";
            var cuerpoHtml = ConstruirCuerpoEmail(idIncidente, ticketId, sistema, horasSla);

            return await EnviarEmailConReintentosAsync(destinatarios, asunto, cuerpoHtml, ticketId, cancellationToken);
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

            if (correos.Any())
            {
                var destinatarios = await ResolverDestinatariosCorreoAsync(cancellationToken);
                if (destinatarios.Count == 0)
                {
                    resultadoGeneral = ResultadoEnvio.Fallo("No hay destinatarios de correo configurados ni delegados de ciberseguridad activos.");
                }
                else
                {
                    var asunto = correos.Count == 1
                        ? "[URGENTE] Nuevo Incidente Registrado - Ley N° 21.663"
                        : $"[RESUMEN] {correos.Count} Nuevos Incidentes requieren atención - Ley N° 21.663";

                    var cuerpo = ConstruirCuerpoEmailConsolidado(correos);
                    resultadoGeneral = await EnviarEmailConReintentosAsync(destinatarios, asunto, cuerpo, "CONSOLIDADO", cancellationToken);
                }
            }

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

        public async Task<ResultadoEnvio> EnviarAlertaEscalamientoAsync(
            long idIncidente, string ticketId, string sistema, int gravedad, string clasificacion, 
            string hitoNombre, string reglaDescripcion, DateTime fechaLimite, double minutosRestantes, 
            bool notificarEmail = true, bool notificarTeams = true, CancellationToken cancellationToken = default)
        {
            bool vencido = minutosRestantes <= 0;
            var asunto = $"[ESCALAMIENTO SLA] {ticketId} - {hitoNombre} - {(vencido ? "VENCIDO" : $"Faltan {Math.Round(minutosRestantes)} min")}";
            var fallos = new List<string>();
            var canalesRequeridos = 0;

            if (notificarEmail)
            {
                canalesRequeridos++;
                if (!_graphSettings.EstaConfigurado())
                {
                    fallos.Add("Microsoft Graph no configurado para envío de correo de escalamiento.");
                }
                else
                {
                    var destinatarios = await ResolverDestinatariosCorreoAsync(cancellationToken);
                    if (destinatarios.Count == 0)
                    {
                        fallos.Add("No hay destinatarios de correo disponibles para escalamiento.");
                    }
                    else
                    {
                        var cuerpo = ConstruirCuerpoEmailEscalamiento(idIncidente, ticketId, sistema, hitoNombre, reglaDescripcion, fechaLimite, minutosRestantes);
                        var resultadoEmail = await EnviarEmailConReintentosAsync(destinatarios, asunto, cuerpo, ticketId, cancellationToken);
                        if (!resultadoEmail.Exito)
                        {
                            fallos.Add(resultadoEmail.MensajeError ?? "Fallo desconocido al enviar correo de escalamiento.");
                        }
                    }
                }
            }

            if (notificarTeams)
            {
                if (_teamsSettings.EstaConfigurado())
                {
                    canalesRequeridos++;
                    try
                    {
                        var payload = ConstruirPayloadTeamsEscalamiento(idIncidente, ticketId, sistema, gravedad, clasificacion, hitoNombre, reglaDescripcion, fechaLimite, minutosRestantes);
                        var json = JsonSerializer.Serialize(payload);
                        using var content = new StringContent(json, Encoding.UTF8, "application/json");
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(_teamsSettings.TimeoutMs);
                        var response = await _httpClient.PostAsync(_teamsSettings.WebhookUrl, content, cts.Token);

                        if (!response.IsSuccessStatusCode)
                        {
                            var body = await response.Content.ReadAsStringAsync(cancellationToken);
                            _logger.LogWarning("[Notificacion][Teams] Evento: FalloEscalamiento Ticket: {Ticket} Error: {Error}", ticketId, body);
                            fallos.Add($"Teams HTTP {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Notificacion][Teams] Evento: ErrorEscalamiento Ticket: {Ticket}", ticketId);
                        fallos.Add(ex.Message);
                    }
                }
                else
                {
                    _logger.LogInformation("[Notificacion][Teams] Evento: Omitido Ticket: {Ticket} Motivo: Teams deshabilitado o sin WebhookUrl", ticketId);
                }
            }

            if (canalesRequeridos == 0)
            {
                return ResultadoEnvio.Fallo("La regla de escalamiento no tiene canales de notificación disponibles.");
            }

            if (fallos.Count > 0)
            {
                return ResultadoEnvio.Fallo(string.Join(" | ", fallos));
            }

            return ResultadoEnvio.Ok();
        }

        private async Task<ResultadoEnvio> EnviarEmailConReintentosAsync(
            IReadOnlyList<string> destinatarios, string asunto, string cuerpoHtml, string ticketId, CancellationToken cancellationToken)
        {
            if (!_graphSettings.EstaConfigurado())
            {
                var motivo = "Microsoft Graph no configurado (faltan credenciales o remitente).";
                _logger.LogWarning("[Notificacion][Email] Evento: ConfiguracionInvalida Ticket: {Ticket} Motivo: {Motivo}", ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            if (destinatarios == null || destinatarios.Count == 0)
            {
                var motivo = "No hay destinatarios válidos para el correo.";
                _logger.LogWarning("[Notificacion][Email] Evento: DestinatarioInvalido Ticket: {Ticket} Motivo: {Motivo}", ticketId, motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            var destinatariosTexto = string.Join(";", destinatarios);
            var intento = 0;
            var ultimoError = string.Empty;

            while (intento <= _graphSettings.MaxReintentos)
            {
                intento++;
                try
                {
                    await EspaciarEnvioAsync(cancellationToken);
                    await EnviarEmailAsync(destinatarios, asunto, cuerpoHtml, cancellationToken);
                    _logger.LogInformation("[Notificacion][Email] Evento: Enviado Destinatario: {Destinatario} Ticket: {Ticket} Intento: {Intento}", destinatariosTexto, ticketId, intento);
                    return ResultadoEnvio.Ok(destinatariosTexto);
                }
                catch (ServiceException ex)
                {
                    ultimoError = ConstruirDetalleErrorGraph(ex);
                    var codigo = ex.ResponseStatusCode;

                    if (EsErrorPermanente(codigo) || !EsErrorTransitorio(codigo))
                    {
                        _logger.LogError(ex, "[Notificacion][Email] Evento: FalloPermanente Destinatario: {Destinatario} Ticket: {Ticket} Codigo: {Codigo} Detalle: {Detalle}", destinatariosTexto, ticketId, codigo, ultimoError);
                        return ResultadoEnvio.Fallo(ultimoError);
                    }

                    _logger.LogWarning(ex, "[Notificacion][Email] Evento: FalloIntento Destinatario: {Destinatario} Ticket: {Ticket} Codigo: {Codigo} Intento: {Intento}", destinatariosTexto, ticketId, codigo, intento);
                    await EsperarAntesDeReintentarAsync(intento, ObtenerRetrasoGraph(ex, intento), cancellationToken);
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    ultimoError = "Tiempo de espera agotado al enviar por Microsoft Graph.";
                    _logger.LogWarning(ex, "[Notificacion][Email] Evento: FalloIntento Destinatario: {Destinatario} Ticket: {Ticket} Intento: {Intento}", destinatariosTexto, ticketId, intento);
                    await EsperarAntesDeReintentarAsync(intento, ObtenerRetrasoPorIntento(intento), cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    ultimoError = ex.Message;
                    _logger.LogWarning(ex, "[Notificacion][Email] Evento: FalloIntento Destinatario: {Destinatario} Ticket: {Ticket} Intento: {Intento}", destinatariosTexto, ticketId, intento);
                    await EsperarAntesDeReintentarAsync(intento, ObtenerRetrasoPorIntento(intento), cancellationToken);
                }
                catch (Exception ex)
                {
                    ultimoError = ex.Message;
                    _logger.LogError(ex, "[Notificacion][Email] Evento: FalloPermanente Destinatario: {Destinatario} Ticket: {Ticket} Detalle: {Detalle}", destinatariosTexto, ticketId, ultimoError);
                    return ResultadoEnvio.Fallo(ultimoError);
                }
            }

            _logger.LogError("[Notificacion][Email] Evento: FalloDefinitivo Destinatario: {Destinatario} Ticket: {Ticket} Error: {Error}", destinatariosTexto, ticketId, ultimoError);
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
                _nextAllowedSendTime = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(_graphSettings.IntervaloEntreEnviosMs));
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task EnviarEmailAsync(IReadOnlyList<string> destinatarios, string asunto, string cuerpoHtml, CancellationToken ct)
        {
            var cliente = _clienteGraph.Value
                ?? throw new InvalidOperationException("No se pudo crear el cliente de Microsoft Graph.");

            var mensaje = new Message
            {
                Subject = asunto,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = cuerpoHtml
                },
                ToRecipients = destinatarios.Select(correo => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = correo }
                }).ToList()
            };

            var solicitud = new SendMailPostRequestBody
            {
                Message = mensaje,
                SaveToSentItems = _graphSettings.GuardarEnEnviados
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_graphSettings.TimeoutMs);

            await cliente.Users[_graphSettings.RemitenteUpn]
                .SendMail
                .PostAsync(solicitud, cancellationToken: cts.Token);
        }

        private async Task<List<string>> ResolverDestinatariosCorreoAsync(CancellationToken cancellationToken)
        {
            if (_graphSettings.TieneDestinatarioConfigurado())
            {
                var destinatariosConfigurados = ObtenerDestinatarios(_graphSettings.DestinatarioCorreo);
                _logger.LogInformation("[Notificacion][Email] Evento: DestinatariosResueltos Origen: Configuracion Cantidad: {Cantidad}", destinatariosConfigurados.Count);
                return destinatariosConfigurados;
            }

            try
            {
                var correosDelegados = await _responsableRepository.ObtenerCorreosDelegadosActivosAsync();
                var destinatariosDelegados = correosDelegados
                    .Where(EsEmailValido)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _logger.LogInformation("[Notificacion][Email] Evento: DestinatariosResueltos Origen: TbSitracResponsable Cantidad: {Cantidad}", destinatariosDelegados.Count);
                return destinatariosDelegados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notificacion][Email] Evento: ErrorResolucionDestinatarios Origen: TbSitracResponsable");
                return new List<string>();
            }
        }

        private static List<string> ObtenerDestinatarios(string origen)
        {
            if (string.IsNullOrWhiteSpace(origen)) return new List<string>();

            return origen
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(EsEmailValido)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool EsEmailValido(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;

            try
            {
                var direccion = new MailAddress(correo);
                return string.Equals(direccion.Address, correo.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private async Task EsperarAntesDeReintentarAsync(int intento, TimeSpan retraso, CancellationToken ct)
        {
            if (intento > _graphSettings.MaxReintentos || retraso <= TimeSpan.Zero) return;
            await Task.Delay(retraso, ct);
        }

        private static TimeSpan ObtenerRetrasoGraph(ServiceException ex, int intento)
        {
            var cabeceraReintento = ex.ResponseHeaders?.RetryAfter;
            if (cabeceraReintento?.Delta is TimeSpan delta && delta > TimeSpan.Zero) return delta;
            if (cabeceraReintento?.Date is DateTimeOffset fecha && fecha > DateTimeOffset.UtcNow)
            {
                return fecha - DateTimeOffset.UtcNow;
            }

            return ObtenerRetrasoPorIntento(intento);
        }

        private static TimeSpan ObtenerRetrasoPorIntento(int intento)
        {
            var milisegundos = Math.Min(200 * Math.Pow(2, intento - 1), 10000);
            return TimeSpan.FromMilliseconds(milisegundos + Random.Shared.Next(0, 100));
        }

        private static bool EsErrorPermanente(int codigo)
        {
            return codigo == 400 || codigo == 401 || codigo == 403 || codigo == 404;
        }

        private static bool EsErrorTransitorio(int codigo)
        {
            return codigo == 408 || codigo == 429 || codigo == 500 ||
                   codigo == 502 || codigo == 503 || codigo == 504;
        }

        private static string ConstruirDetalleErrorGraph(ServiceException ex)
        {
            var cuerpo = string.IsNullOrWhiteSpace(ex.RawResponseBody)
                ? string.Empty
                : $" Cuerpo: {ex.RawResponseBody}";
            return $"Graph HTTP {ex.ResponseStatusCode}: {ex.Message}.{cuerpo}";
        }

        private string ConstruirUrlBase()
        {
            return (_frontendSettings.BaseUrl ?? string.Empty).TrimEnd('/');
        }

        private string ConstruirUrlIncidente(long idIncidente)
        {
            return $"{ConstruirUrlBase()}/index.html?idIncidente={idIncidente}";
        }

        private string ConstruirUrlConsola()
        {
            return $"{ConstruirUrlBase()}/index.html";
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
            var etiqueta = CalculadoraPrioridad.Etiqueta(prioridadScore);
            var emojiGravedad = CalculadoraPrioridad.EmojiGravedad(gravedad);
            var nombreGravedad = CalculadoraPrioridad.NombreGravedad(gravedad);
            var urlDirecta = ConstruirUrlIncidente(idIncidente);

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
            var urlDirecta = ConstruirUrlConsola();
            
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
            var urlDirecta = ConstruirUrlIncidente(idIncidente);

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
            var urlDirecta = ConstruirUrlConsola();
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

        private string ConstruirCuerpoEmailEscalamiento(long idIncidente, string ticketId, string sistema, string hitoNombre, string reglaDescripcion, DateTime fechaLimite, double minutosRestantes)
        {
            var ticketSafe = System.Net.WebUtility.HtmlEncode(ticketId);
            var sistemaSafe = System.Net.WebUtility.HtmlEncode(sistema);
            var hitoSafe = System.Net.WebUtility.HtmlEncode(hitoNombre);
            var reglaSafe = System.Net.WebUtility.HtmlEncode(reglaDescripcion);
            var fechaSafe = fechaLimite.ToString("dd-MM-yyyy HH:mm");
            
            var color = minutosRestantes <= 0 ? "#E4002B" : (minutosRestantes <= 60 ? "#DFC500" : "#F58220");
            var estadoStr = minutosRestantes <= 0 ? "VENCIDO" : $"Faltan {Math.Round(minutosRestantes)} minutos";

            var sb = new StringBuilder();
            sb.Append("<div style='font-family: Arial, sans-serif; max-width: 640px;'>");
            sb.Append($"<div style='background:{color};color:#fff;padding:16px 20px;border-radius:8px 8px 0 0'>");
            sb.Append($"<h2 style='margin:0'>⚠️ ALERTA DE ESCALAMIENTO SLA</h2>");
            sb.Append("<p style='margin:4px 0 0;font-size:12px'>Ley N° 21.663</p>");
            sb.Append("</div>");
            sb.Append("<div style='border:1px solid #d5dbdb;border-top:none;padding:20px;border-radius:0 0 8px 8px'>");
            sb.Append($"<h3 style='color:{color};margin-top:0'>{(minutosRestantes <= 0 ? "¡PLAZO VENCIDO!" : "Plazo por vencer")}</h3>");
            sb.Append($"<p>{reglaSafe}</p>");
            sb.Append("<table style='border-collapse:collapse;width:100%;margin-top:15px'>");
            sb.Append("<tr><td style='padding:10px;border:1px solid #e9ebed;background:#f2f3f3;font-weight:bold'>Ticket:</td>");
            sb.Append($"<td style='padding:10px;border:1px solid #e9ebed'>{ticketSafe}</td></tr>");
            sb.Append("<tr><td style='padding:10px;border:1px solid #e9ebed;background:#f2f3f3;font-weight:bold'>Hito:</td>");
            sb.Append($"<td style='padding:10px;border:1px solid #e9ebed'>{hitoSafe}</td></tr>");
            sb.Append("<tr><td style='padding:10px;border:1px solid #e9ebed;background:#f2f3f3;font-weight:bold'>Fecha Límite:</td>");
            sb.Append($"<td style='padding:10px;border:1px solid #e9ebed;font-weight:bold;color:{color}'>{fechaSafe}</td></tr>");
            sb.Append("<tr><td style='padding:10px;border:1px solid #e9ebed;background:#f2f3f3;font-weight:bold'>Estado:</td>");
            sb.Append($"<td style='padding:10px;border:1px solid #e9ebed;font-weight:bold;color:{color}'>{estadoStr}</td></tr>");
            sb.Append("</table>");
            
            var urlDirecta = ConstruirUrlIncidente(idIncidente);
            sb.Append("<div style='text-align: center; margin-top: 25px;'>");
            sb.AppendFormat("<a href='{0}' style='background-color: #0f1b2a; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Revisar Incidente Ahora</a>", urlDirecta);
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            return sb.ToString();
        }

        private object ConstruirPayloadTeamsEscalamiento(long idIncidente, string ticketId, string sistema, int gravedad, string clasificacion, string hitoNombre, string reglaDescripcion, DateTime fechaLimite, double minutosRestantes)
        {
            var emojiGravedad = CalculadoraPrioridad.EmojiGravedad(gravedad);
            var nombreGravedad = CalculadoraPrioridad.NombreGravedad(gravedad);
            var urlDirecta = ConstruirUrlIncidente(idIncidente);
            
            var color = minutosRestantes <= 0 ? "Attention" : (minutosRestantes <= 60 ? "Warning" : "Accent");
            var estadoStr = minutosRestantes <= 0 ? "VENCIDO" : $"Faltan {Math.Round(minutosRestantes)} minutos";
            var tituloCard = minutosRestantes <= 0 ? "¡PLAZO VENCIDO!" : "Plazo por vencer";

            var facts = new TeamsFact[]
            {
                new TeamsFact("Ticket:", ticketId),
                new TeamsFact("Sistema:", sistema),
                new TeamsFact("Gravedad:", $"{emojiGravedad} {nombreGravedad}"),
                new TeamsFact("Clasificación:", clasificacion),
                new TeamsFact("Hito:", hitoNombre),
                new TeamsFact("Vence:", fechaLimite.ToString("dd-MM-yyyy HH:mm")),
                new TeamsFact("Estado:", estadoStr)
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
                                        new { type = "TextBlock", text = $"⚠️ {tituloCard} - {hitoNombre}", weight = "Bolder", size = "Large", color = color },
                                        new { type = "TextBlock", text = reglaDescripcion, isSubtle = true, size = "Small", spacing = "None", wrap = true },
                                        new { type = "TextBlock", text = " ", spacing = "None" },
                                        new { type = "FactSet", facts = facts }
                                    }
                                }
                            },
                            actions = new object[]
                            {
                                new
                                {
                                    type = "Action.OpenUrl",
                                    title = "🔗 Revisar Incidente",
                                    url = urlDirecta
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}