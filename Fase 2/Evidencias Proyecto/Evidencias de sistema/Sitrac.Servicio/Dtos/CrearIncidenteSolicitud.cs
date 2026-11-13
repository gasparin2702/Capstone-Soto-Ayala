using System;

namespace Sitrac.Servicio.Dtos
{
    public class CrearIncidenteSolicitud
    {
        public string CodigoTicketInvgate { get; set; } = string.Empty;
        public DateTime FechaTomaConocimiento { get; set; }
        public long CodTipoClasificacionEmpresa { get; set; }
        public long CodTipoGravedad { get; set; }
        public string DescripcionSistemaAfectado { get; set; } = string.Empty;
    }
}