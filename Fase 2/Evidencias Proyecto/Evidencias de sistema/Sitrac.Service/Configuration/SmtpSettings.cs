using System.ComponentModel.DataAnnotations;

namespace Sitrac.Service.Configuration
{
    public class SmtpSettings
    {
        public const string SectionName = "SmtpConfig";

        [Required]
        public string Host { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; } = 2525;

        public bool EnableSsl { get; set; } = false;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string DestinatarioDemo { get; set; } = string.Empty;
        public string NombreRemitente { get; set; } = "SITRAC-ANCI Alertas";
        public int TimeoutMs { get; set; } = 10000;
        public int MaxReintentos { get; set; } = 1;
        public int IntervaloEntreEnviosMs { get; set; } = 1200;

        public bool EstaConfigurado() =>
            !string.IsNullOrWhiteSpace(Host) &&
            !string.IsNullOrWhiteSpace(UserName) &&
            !string.IsNullOrWhiteSpace(Password);
    }
}