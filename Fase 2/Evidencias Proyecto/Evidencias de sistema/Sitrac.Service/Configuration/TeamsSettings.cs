namespace Sitrac.Service.Configuration
{
    public class TeamsSettings
    {
        public const string SectionName = "TeamsConfig";

        public bool Habilitado { get; set; } = false;
        public string WebhookUrl { get; set; } = string.Empty;
        public int UmbralPrioridadMinima { get; set; } = 22;
        public int TimeoutMs { get; set; } = 10000;

        public bool EstaConfigurado() =>
            Habilitado &&
            !string.IsNullOrWhiteSpace(WebhookUrl);
    }
}