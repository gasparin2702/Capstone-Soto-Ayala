using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Sitrac.Service.Configuration;
using Sitrac.Service.Dtos;
using Sitrac.Service.Interfaces;

namespace Sitrac.Service
{
    public class NotificacionService : INotificacionService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<NotificacionService> _logger;

        public NotificacionService(IOptions<SmtpSettings> options, ILogger<NotificacionService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<ResultadoEnvio> EnviarAlertaNuevoIncidenteAsync(
            string destinatario,
            string ticketId,
            string sistema,
            int horasSla,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.EstaConfigurado())
            {
                var motivo = "SMTP no configurado (falta Host/UserName/Password).";
                _logger.LogWarning("[Notificacion] {Motivo}", motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            if (!EsEmailValido(destinatario))
            {
                var motivo = $"Destinatario inválido: {destinatario}";
                _logger.LogWarning("[Notificacion] {Motivo}", motivo);
                return ResultadoEnvio.Fallo(motivo);
            }

            var asunto = $"[URGENTE] Nuevo Incidente Registrado - Ley N° 21.663 ({ticketId})";
            var cuerpoHtml = ConstruirCuerpoHtml(ticketId, sistema, horasSla);

            int intento = 0;
            string ultimoError = string.Empty;
            while (intento <= _settings.MaxReintentos)
            {
                intento++;
                try
                {
                    await EnviarAsync(destinatario, asunto, cuerpoHtml, cancellationToken);
                    _logger.LogInformation(
                        "[Notificacion] Correo enviado a {Destinatario} para ticket {Ticket} (intento {Intento})",
                        destinatario, ticketId, intento);
                    return ResultadoEnvio.Ok();
                }
                catch (Exception ex)
                {
                    ultimoError = ex.Message;
                    _logger.LogWarning(ex, "[Notificacion] Fallo intento {Intento} para {Ticket}",
                        intento, ticketId);
                    if (intento <= _settings.MaxReintentos)
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }
            return ResultadoEnvio.Fallo(ultimoError);
        }

        private async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken ct)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.NombreRemitente, _settings.UserName));
            message.To.Add(MailboxAddress.Parse(destinatario));
            message.Subject = asunto;

            var bodyBuilder = new BodyBuilder { HtmlBody = cuerpoHtml };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = _settings.TimeoutMs;

            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                ct);

            await client.AuthenticateAsync(_settings.UserName, _settings.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }

        private static bool EsEmailValido(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try { return MailboxAddress.Parse(email).Address == email; }
            catch { return false; }
        }

        private static string ConstruirCuerpoHtml(string ticketId, string sistema, int horasSla)
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