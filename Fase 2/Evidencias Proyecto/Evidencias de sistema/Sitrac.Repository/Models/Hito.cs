using System;

namespace Sitrac.Repository.Models
{
    public class Hito
    {
        public long IdHito { get; set; }
        public long IdIncidente { get; set; }
        public long CodTipoHito { get; set; }
        public int Secuencia { get; set; }
        public DateTime FechaBase { get; set; }
        public DateTime FechaLimite { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public DateTime? FechaCumplimiento { get; set; }
        public string? ReferenciaExterna { get; set; }
        public long? UltimaReglaNotificada { get; set; }
        public DateTime? ProximaNotificacionAt { get; set; }
        public int ConteoNotificaciones { get; set; }
        public DateTime? SilenciadoHasta { get; set; }
    }
}

