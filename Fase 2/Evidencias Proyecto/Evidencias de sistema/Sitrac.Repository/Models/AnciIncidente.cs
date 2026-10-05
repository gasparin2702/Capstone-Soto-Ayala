using System;

namespace Sitrac.Repository.Models
{
    public class AnciIncidente
    {
        public long IdIncidente { get; set; }
        public DateTime FechaTomaConocimiento { get; set; }
        public DateTime FechaRegistroSistema { get; set; }
        public long CodTipoCanalEntrada { get; set; }
        public string? CodigoTicketInvgate { get; set; }
        public bool IndEsIncidenteReal { get; set; }
        public long CodTipoDatosPersonales { get; set; }
        public long CodTipoClasificacionEmpresa { get; set; }
        public long CodTipoGravedad { get; set; }
        public long CodTipoEstadoIncidente { get; set; }
        public string? DescripcionSistemaAfectado { get; set; }
    }
}