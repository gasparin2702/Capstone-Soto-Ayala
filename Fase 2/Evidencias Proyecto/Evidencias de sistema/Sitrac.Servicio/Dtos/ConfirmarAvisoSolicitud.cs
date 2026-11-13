using System.ComponentModel.DataAnnotations;

namespace Sitrac.Servicio.Dtos
{
    public class ConfirmarAvisoSolicitud
    {
        [Required, MinLength(15), MaxLength(500)]
        public string Comentario { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ReferenciaEnvio { get; set; }
    }
}