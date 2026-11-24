namespace Sitrac.Servicio.Configuration
{
    public class ConfiguracionSeguridad
    {
        public const string SectionName = "Seguridad";
        public string PrefijoSistema { get; set; } = "SITR";
        public bool HabilitarAutorizacionPorTransaccion { get; set; } = true;
        public bool HabilitarBootstrap { get; set; } = true;
    }
}
