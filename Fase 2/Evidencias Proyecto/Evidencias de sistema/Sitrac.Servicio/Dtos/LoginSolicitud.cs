using System.ComponentModel.DataAnnotations;

namespace Sitrac.Servicio.Dtos
{
    public class LoginSolicitud
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}