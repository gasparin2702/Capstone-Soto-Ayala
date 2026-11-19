using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repositorio.Models;

namespace Sitrac.Repositorio.Interfaces
{
    public interface IIncidenteRepositorio
    {
        Task<SitracIncidente?> ObtenerPorIdAsync(long idIncidente);
        Task<long> InsertarAsync(SitracIncidente incidente);
        Task<IEnumerable<IncidenteSlaInfo>> ObtenerIncidentesParaEvaluacionSlaAsync();
        Task AvanzarEstadoConHistorialAsync(long idIncidente, long nuevoEstado, SitracEventoHistorial historial);
        Task AvanzarEstadoConHitosTransaccionalAsync(long idIncidente, long nuevoEstado, SitracEventoHistorial historial, Hito? hitoACerrar, Hito? hitoACrear, System.DateTime ahora);
        
        Task<IEnumerable<IncidenteResumen>> ObtenerActivosResumenAsync();
        Task<IncidenteDetalle?> ObtenerDetallePorIdAsync(long idIncidente);
        Task<IEnumerable<EventoHistorialResumen>> ObtenerHistorialAsync(long idIncidente);

        Task ConfirmarAvisoTransaccionalAsync(long idIncidente, long nuevoEstado, SitracEventoHistorial historial, Hito? hitoACerrar, Hito? hitoACrear, System.DateTime ahora);
        
        Task RegistrarInformeTransaccionalAsync(
            long idIncidente, long? nuevoEstado, SitracEventoHistorial historial, 
            Hito hitoACerrar, Hito? hitoACrear, System.DateTime ahora, 
            InformePreliminar? informeP, InformeCierre? informeC,
            RevisionDatosPersonales? revisionDp);
            
        Task CerrarIncidenteTransaccionalAsync(long idIncidente, SitracEventoHistorial historial, System.DateTime ahora);
    }
}