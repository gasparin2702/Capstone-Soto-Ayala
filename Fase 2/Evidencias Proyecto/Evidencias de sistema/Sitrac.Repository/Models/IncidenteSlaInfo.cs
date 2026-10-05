using System;

namespace Sitrac.Repository.Models
{
    public class IncidenteSlaInfo
    {
        public long IdIncidente { get; set; }
        public string CodigoTicketInvgate { get; set; } = string.Empty;
        public long CodTipoEstadoIncidente { get; set; }
        public DateTime FechaLimiteAvisoTemprano { get; set; }
        public DateTime FechaLimiteInformePreliminar { get; set; }
        public DateTime FechaLimiteInformeFinal { get; set; }
    }
}