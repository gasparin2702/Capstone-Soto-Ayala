using System;
using System.Linq;

namespace Sitrac.Servicio.Configuration
{
    public class ConfiguracionGraph
    {
        public const string SectionName = "GraphConfig";

        private static readonly string[] MarcadoresNoConfigurados =
        {
            "CAMBIAR",
            "CHANGE_ME",
            "PLACEHOLDER",
            "REEMPLAZAR",
            "TU_",
            "YOUR_",
            "XXX",
            "EXAMPLE.COM",
            "CONTOSO.COM",
            "LOCAL.TEST"
        };

        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RemitenteUpn { get; set; } = string.Empty;
        public string DestinatarioCorreo { get; set; } = string.Empty;
        public bool GuardarEnEnviados { get; set; } = true;
        public int TimeoutMs { get; set; } = 15000;
        public int MaxReintentos { get; set; } = 2;
        public int IntervaloEntreEnviosMs { get; set; } = 1200;

        public bool EstaConfigurado()
        {
            return EsValorValido(TenantId)
                && EsValorValido(ClientId)
                && EsValorValido(ClientSecret)
                && EsValorValido(RemitenteUpn);
        }

        public bool TieneDestinatarioConfigurado()
        {
            return EsValorValido(DestinatarioCorreo);
        }

        private static bool EsValorValido(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return false;

            var texto = valor.Trim();
            return !MarcadoresNoConfigurados.Any(marcador =>
                texto.Contains(marcador, StringComparison.OrdinalIgnoreCase));
        }
    }
}
