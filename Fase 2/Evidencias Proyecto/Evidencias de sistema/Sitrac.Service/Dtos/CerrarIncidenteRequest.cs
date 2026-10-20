using System.ComponentModel.DataAnnotations;

namespace Sitrac.Service.Dtos
{
    public class CerrarIncidenteRequest
    {
        [Required, MinLength(15), MaxLength(500)]
        public string Comentario { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ReferenciaCierre { get; set; }
    }
}