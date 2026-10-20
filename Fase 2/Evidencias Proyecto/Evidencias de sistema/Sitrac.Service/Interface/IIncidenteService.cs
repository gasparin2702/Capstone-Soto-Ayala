using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repository.Models;
using Sitrac.Service.Dtos;

namespace Sitrac.Service.Interfaces
{
    public interface IIncidenteService
    {
        Task<long> RegistrarIncidenteInvgateAsync(CrearIncidenteRequest request);
        Task ProcesarCicloDeVidaAsync();
        Task<IEnumerable<IncidenteResumen>> ObtenerIncidentesActivosAsync();
        Task<IncidenteDetalle?> ObtenerDetalleIncidenteAsync(long idIncidente);
        Task<IEnumerable<EventoHistorialResumen>> ObtenerHistorialIncidenteAsync(long idIncidente);

        // Nuevos (Bloque B)
        Task ConfirmarAvisoAsync(long idIncidente, ConfirmarAvisoRequest request);
        Task RegistrarInformeAsync(long idIncidente, RegistrarInformeRequest request);
        Task CerrarIncidenteAsync(long idIncidente, CerrarIncidenteRequest request);
    }
}