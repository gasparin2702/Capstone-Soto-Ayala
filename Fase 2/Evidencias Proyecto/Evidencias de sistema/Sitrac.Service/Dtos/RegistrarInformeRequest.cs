using System.ComponentModel.DataAnnotations;

namespace Sitrac.Service.Dtos
{
    public class RegistrarInformeRequest
    {
        [Required, MinLength(15), MaxLength(500)]
        public string Comentario { get; set; } = string.Empty;

        [Required, RegularExpression("^[PF]$", ErrorMessage = "Tipo debe ser 'P' o 'F'")]
        public string Tipo { get; set; } = "P";

        [MaxLength(255)]
        public string? NombreArchivo { get; set; }
    }
}