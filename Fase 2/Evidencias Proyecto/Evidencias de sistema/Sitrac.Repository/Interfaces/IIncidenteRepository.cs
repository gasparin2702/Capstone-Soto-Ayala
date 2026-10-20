using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repository.Models;

namespace Sitrac.Repository.Interfaces
{
    public interface IIncidenteRepository
    {
        Task<SitracIncidente?> ObtenerPorIdAsync(long idIncidente);
        Task<long> InsertarAsync(SitracIncidente incidente);
        Task<IEnumerable<IncidenteSlaInfo>> ObtenerIncidentesParaEvaluacionSlaAsync();
        Task AvanzarEstadoConHistorialAsync(long idIncidente, long nuevoEstado, SitracEventoHistorial historial);
        Task<IEnumerable<IncidenteResumen>> ObtenerActivosResumenAsync();
        Task<IncidenteDetalle?> ObtenerDetallePorIdAsync(long idIncidente);
        Task<IEnumerable<EventoHistorialResumen>> ObtenerHistorialAsync(long idIncidente);

        // Nuevos métodos transaccionales (Bloque B)
        Task ConfirmarAvisoTransaccionalAsync(long idIncidente, long nuevoEstado, SitracEventoHistorial historial, Hito? hitoACerrar, Hito? hitoACrear, System.DateTime ahora);
        Task RegistrarInformeTransaccionalAsync(long idIncidente, long? nuevoEstado, SitracEventoHistorial historial, Hito hitoACerrar, Hito? hitoACrear, System.DateTime ahora);
        Task CerrarIncidenteTransaccionalAsync(long idIncidente, SitracEventoHistorial historial, System.DateTime ahora);
    }
}