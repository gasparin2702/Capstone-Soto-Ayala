using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repositorio.Models;
using Sitrac.Servicio.Dtos;

namespace Sitrac.Servicio.Interfaces
{
    public interface IIncidenteServicio
    {
        Task<long> RegistrarIncidenteInvgateAsync(CrearIncidenteSolicitud request);
        Task ProcesarCicloDeVidaAsync();
        Task<IEnumerable<IncidenteResumen>> ObtenerIncidentesActivosAsync();
        Task<IncidenteDetalle?> ObtenerDetalleIncidenteAsync(long idIncidente);
        Task<IEnumerable<EventoHistorialResumen>> ObtenerHistorialIncidenteAsync(long idIncidente);

        // Nuevos (Bloque B)
        Task ConfirmarAvisoAsync(long idIncidente, ConfirmarAvisoSolicitud request);
        Task RegistrarInformeAsync(long idIncidente, RegistrarInformeSolicitud request);
        Task CerrarIncidenteAsync(long idIncidente, CerrarIncidenteSolicitud request);
    }
}