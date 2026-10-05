using System;

namespace Sitrac.Repository.Models
{
    public class IncidenteResumen
    {
        public long IdIncidente { get; set; }
        public string CodigoTicketInvgate { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public string Clasificacion { get; set; } = string.Empty;
        public int Gravedad { get; set; }
        public string Estado { get; set; } = string.Empty;
        public long CodTipoEstadoIncidente { get; set; }   // ← NUEVO
        public DateTime FechaTomaConocimiento { get; set; }
        public DateTime? FechaLimiteAvisoTemprano { get; set; }
        public DateTime? FechaLimiteInformePreliminar { get; set; }
        public DateTime? FechaLimiteInformeFinal { get; set; }
    }
}