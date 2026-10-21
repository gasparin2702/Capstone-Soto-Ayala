using System;

namespace Sitrac.Repository.Models
{
    public class InformePreliminar
    {
        public long IdInformePreliminar { get; set; }
        public long IdIncidente { get; set; }
        public long CodTipoGravedad { get; set; }
        public DateTime? FechaEstimadaInicio { get; set; }
        public string? IndiciosOcurrencia { get; set; }
        public string? ActivosAfectados { get; set; }
        public string? EvidenciaDisponible { get; set; }
        public string? EvidenciaPosibleDelito { get; set; }
        public string? RepercusionesOtrasInstituciones { get; set; }
        public string? OtraInformacionRelevante { get; set; }
        public string? EvaluacionGravedadImpacto { get; set; }
        public string? EstadoActualIncidente { get; set; }
        public string? DescripcionIocs { get; set; }
        public string? DescripcionMedidasMitigacion { get; set; }
        
        // DPO
        public string? HayDatosPersonales { get; set; }
        public DateTime? FechaNotificacionDpo { get; set; }
        public string? ResponsableDpo { get; set; }

        public DateTime? FechaEnvio { get; set; }
    }
}