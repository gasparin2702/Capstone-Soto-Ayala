namespace Sitrac.Service.Configuration
{
    public class SmtpSettings
    {
        public const string SectionName = "SmtpConfig";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 2525;
        public bool EnableSsl { get; set; } = false;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DestinatarioDemo { get; set; } = string.Empty;
        public string NombreRemitente { get; set; } = "SITRAC-ANCI Alertas";
        public int TimeoutMs { get; set; } = 10000;
        public int MaxReintentos { get; set; } = 1;

        public bool EstaConfigurado() =>
            !string.IsNullOrWhiteSpace(Host) &&
            !string.IsNullOrWhiteSpace(UserName) &&
            !string.IsNullOrWhiteSpace(Password);
    }
}