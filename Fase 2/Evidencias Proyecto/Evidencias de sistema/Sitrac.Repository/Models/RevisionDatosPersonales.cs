using System;

namespace Sitrac.Repository.Models
{
    public class RevisionDatosPersonales
    {
        public long IdRevisionDatosPersonales { get; set; }
        public long IdIncidente { get; set; }
        public long? CodTipoDatosPersonales { get; set; }
        public DateTime? FechaAuditoria { get; set; }
        public DateTime? FechaNotificaLegal { get; set; }
        public DateTime? FechaNotificaDpo { get; set; }
        public long? IdResponsableValida { get; set; }
        public long CodTipoEstadoRevisionDp { get; set; } // 1=Pendiente, 2=Completado
    }
}