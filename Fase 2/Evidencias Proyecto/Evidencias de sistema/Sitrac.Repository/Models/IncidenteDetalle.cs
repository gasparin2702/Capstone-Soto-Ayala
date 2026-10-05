using System;

namespace Sitrac.Repository.Models
{
    public class IncidenteDetalle
    {
        public long IdIncidente { get; set; }
        public string CodigoTicketInvgate { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public string Clasificacion { get; set; } = string.Empty;
        public int Gravedad { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? SistemaAfectado { get; set; }
        public string? VectorAtaque { get; set; }
        public string? OrigenDeteccion { get; set; }
        public DateTime FechaTomaConocimiento { get; set; }
        public DateTime? FechaLimiteAvisoTemprano { get; set; }
        public DateTime? FechaLimiteInformePreliminar { get; set; }
        public DateTime? FechaLimiteInformeFinal { get; set; }
    }
}