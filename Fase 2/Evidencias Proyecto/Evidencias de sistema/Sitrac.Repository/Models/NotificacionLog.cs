using System;

namespace Sitrac.Repository.Models
{
    public class NotificacionLog
    {
        public long IdNotificacion { get; set; }
        public long IdHito { get; set; }
        public int NumeroNotificacion { get; set; }
        public string Destinatario { get; set; } = string.Empty;
        public string Canal { get; set; } = "Email";
        public long? IdRegla { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public int Intentos { get; set; }
        public DateTime? ProximoReintentoAt { get; set; }
        public DateTime FechaProgramada { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public string? MensajeError { get; set; }

        // ▼▼▼ NUEVOS CAMPOS REQUERIDOS PARA LA AGRUPACIÓN VISUAL ▼▼▼
        public int CodTipoGravedad { get; set; }
        public string SistemaAfectado { get; set; } = string.Empty;
        // ▲▲▲ FIN NUEVOS ▲▲▲
    }
}