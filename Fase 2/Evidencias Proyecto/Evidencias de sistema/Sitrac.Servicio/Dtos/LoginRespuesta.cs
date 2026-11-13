namespace Sitrac.Servicio.Dtos
{
    public class LoginRespuesta
    {
        public string Token { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}