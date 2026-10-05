using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repository.Models;

namespace Sitrac.Repository.Interfaces
{
    public interface IIncidenteRepository
    {
        Task<AnciIncidente?> ObtenerPorIdAsync(long idIncidente);
        Task<long> InsertarAsync(AnciIncidente incidente);
        Task<IEnumerable<IncidenteSlaInfo>> ObtenerIncidentesParaEvaluacionSlaAsync();
        Task AvanzarEstadoConHistorialAsync(long idIncidente, long nuevoEstado, AnciEventoHistorial historial);
        Task<IEnumerable<IncidenteResumen>> ObtenerActivosResumenAsync();
        Task<IncidenteDetalle?> ObtenerDetallePorIdAsync(long idIncidente);
        Task<IEnumerable<EventoHistorialResumen>> ObtenerHistorialAsync(long idIncidente);
    }
}