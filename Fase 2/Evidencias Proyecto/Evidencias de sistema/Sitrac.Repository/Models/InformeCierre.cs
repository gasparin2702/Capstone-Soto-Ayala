using System;

namespace Sitrac.Repository.Models
{
    public class InformeCierre
    {
        public long IdInformeCierre { get; set; }
        public long IdIncidente { get; set; }
        public long CodTipoInformeCierre { get; set; }
        public long IdResponsableConfirma { get; set; }
        public DateTime? FechaEstimadaInicio { get; set; }
        public string? IndiciosOcurrencia { get; set; }
        public string? ActivosAfectados { get; set; }
        public string? EvidenciaPosibleDelito { get; set; }
        public string? RepercusionesOtrasInstituciones { get; set; }
        public string? OtraInformacionRelevante { get; set; }
        public string? DescripcionIocs { get; set; }
        public string? DescripcionDetallada { get; set; }
        public string? TipoAmenazaCausaPrincipal { get; set; }
        public string? RepercusionesTransfronterizas { get; set; }
        public string? MedidasMitigacionAplicadas { get; set; }
        public string? VulnerabilidadesExplotadas { get; set; }
        public string? AplicacionesAbusivas { get; set; }
        public string? ControlesTecnicosPrevistos { get; set; }
        public string? CausasAusenciaControles { get; set; }
        public string? MedidasFrenteAlertasCsirt { get; set; }
        
        // DPO
        public string? HayDatosPersonales { get; set; }
        public DateTime? FechaNotificacionDpo { get; set; }
        public string? ResponsableDpo { get; set; }

        public DateTime? FechaEnvio { get; set; }
    }
}