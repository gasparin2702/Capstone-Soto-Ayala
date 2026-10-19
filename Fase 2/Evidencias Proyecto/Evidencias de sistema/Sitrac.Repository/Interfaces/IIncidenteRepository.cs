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
    }
}
