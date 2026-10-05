using System;

namespace Sitrac.Repository.Models
{
    public class AnciIncidentePlazoSla
    {
        public long IdIncidente { get; set; }
        public long IdTipoPlazoSla { get; set; }
        public DateTime FechaTomaConocimiento { get; set; }
        public DateTime FechaLimiteAvisoTemprano { get; set; }
        public DateTime FechaLimiteInformePreliminar { get; set; }
        public DateTime FechaLimiteInformeFinal { get; set; }
    }
}