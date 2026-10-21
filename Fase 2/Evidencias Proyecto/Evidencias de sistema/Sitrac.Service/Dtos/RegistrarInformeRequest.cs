using System;
using System.ComponentModel.DataAnnotations;

namespace Sitrac.Service.Dtos
{
    public class RegistrarInformeRequest
    {
        [Required]
        public string Comentario { get; set; } = string.Empty;

        [Required, RegularExpression("^[PF]$", ErrorMessage = "Tipo debe ser 'P' o 'F'")]
        public string Tipo { get; set; } = "P";

        [MaxLength(255)]
        public string? NombreArchivo { get; set; }

        // --- Campos Informe Preliminar y Final ---
        public DateTime? FechaEstimadaInicio { get; set; }
        public string? IndiciosOcurrencia { get; set; }
        public string? ActivosAfectados { get; set; }
        public string? EvidenciaDisponible { get; set; }
        public string? EvidenciaPosibleDelito { get; set; }
        public string? RepercusionesOtrasInstituciones { get; set; }
        public string? OtraInformacionRelevante { get; set; }
        public string? DescripcionIocs { get; set; }

        // --- Exclusivos Informe Preliminar ---
        public string? EvaluacionGravedadImpacto { get; set; }
        public string? EstadoActualIncidente { get; set; }
        public string? DescripcionMedidasMitigacion { get; set; }

        // --- Exclusivos Informe Final ---
        public string? DescripcionDetallada { get; set; }
        public string? TipoAmenazaCausaPrincipal { get; set; }
        public string? RepercusionesTransfronterizas { get; set; }
        public string? MedidasMitigacionAplicadas { get; set; }

        // --- Exclusivos OIV (Informe Final) ---
        public string? VulnerabilidadesExplotadas { get; set; }
        public string? AplicacionesAbusivas { get; set; }
        public string? ControlesTecnicosPrevistos { get; set; }
        public string? CausasAusenciaControles { get; set; }
        public string? MedidasFrenteAlertasCsirt { get; set; }

        // --- Datos Personales (DPO) ---
        // 1=Si, 2=No, 3=Desconocido
        public long CodTipoDatosPersonales { get; set; } = 3; 
        public DateTime? FechaNotificacionDpo { get; set; }
        public long? IdResponsableDpo { get; set; }
    }
}