using System;

namespace Sitrac.Service.Dtos
{
    public class CrearIncidenteRequest
    {
        public string CodigoTicketInvgate { get; set; } = string.Empty;
        public DateTime FechaTomaConocimiento { get; set; }
        public long CodTipoClasificacionEmpresa { get; set; }
        public long CodTipoGravedad { get; set; }
        public string DescripcionSistemaAfectado { get; set; } = string.Empty;
    }
}