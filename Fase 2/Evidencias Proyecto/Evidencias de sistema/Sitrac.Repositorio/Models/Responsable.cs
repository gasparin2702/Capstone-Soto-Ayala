namespace Sitrac.Repositorio.Models
{
    public class Responsable
    {
        public long IdResponsable { get; set; }
        public string NombreResponsable { get; set; } = string.Empty;
        public string EmailResponsable { get; set; } = string.Empty;
        public string? TelefonoResponsable { get; set; }
        public string? DescripcionRolOrganizacional { get; set; }
        public bool IndEsDelegadoCiberseguridad { get; set; }
    }
}
