namespace Sitrac.Service.Dtos
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}