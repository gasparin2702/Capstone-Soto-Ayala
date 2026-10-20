using System;

namespace Sitrac.Repository.Models
{
    public class HitoResumen
    {
        public long IdHito { get; set; }
        public long CodTipoHito { get; set; }
        public string NombreTipoHito { get; set; } = string.Empty;
        public DateTime FechaBase { get; set; }
        public DateTime FechaLimite { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public DateTime? FechaCumplimiento { get; set; }
        public string? ReferenciaExterna { get; set; }
    }
}