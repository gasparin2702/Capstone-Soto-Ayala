namespace Sitrac.Repository.Models
{
    public class Usuario
    {
        public long IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = "Analista";
    }
}