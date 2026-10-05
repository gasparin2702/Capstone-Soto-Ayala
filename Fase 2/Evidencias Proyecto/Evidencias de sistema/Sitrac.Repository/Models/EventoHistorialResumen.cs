using System;

namespace Sitrac.Repository.Models
{
    public class EventoHistorialResumen
    {
        public long IdEventoHistorial { get; set; }
        public string AmbitoCambio { get; set; } = string.Empty;
        public string? EstadoAnterior { get; set; }
        public string? EstadoNuevo { get; set; }
        public int? GravedadAnterior { get; set; }
        public int? GravedadNueva { get; set; }
        public string Responsable { get; set; } = string.Empty;
        public bool EsAccionAutomatica { get; set; } 
        public DateTime FechaCambio { get; set; }
        public string? Comentario { get; set; }
    }
}