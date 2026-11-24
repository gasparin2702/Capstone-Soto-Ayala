using System;

namespace Sitrac.Servicio.Configuration
{
    public class ConfiguracionFrontend
    {
        public const string SectionName = "FrontendSettings";
        public string BaseUrl { get; set; } = "http://127.0.0.1:5500";
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    }
}