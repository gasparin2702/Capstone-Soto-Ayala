namespace Sitrac.Repository.Models
{
    public class AnciEventoHistorial
    {
        public long IdIncidente { get; set; }
        public string CodigoTicketInvgate { get; set; } = string.Empty;
        public long IdTipoAmbitoCambio { get; set; }
        public long? IdTipoEstadoIncidenteAnterior { get; set; }
        public long? IdTipoEstadoIncidenteNuevo { get; set; }
        public long IdResponsableCambia { get; set; }
        public string? Comentario { get; set; }
    }
}