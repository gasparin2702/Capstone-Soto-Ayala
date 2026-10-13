using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repository.Models;

namespace Sitrac.Repository.Interfaces
{
    public interface IHitoRepository
    {
        Task<long> CrearAsync(Hito hito);
        Task<Hito?> ObtenerPorIdAsync(long idHito);
        Task<IEnumerable<Hito>> ObtenerPorIncidenteAsync(long idIncidente);
        Task<IEnumerable<Hito>> ObtenerPendientesParaEvaluarAsync();
        Task<Hito?> ObtenerHitoPendientePorIncidenteYTipoAsync(long idIncidente, long codTipoHito);
        Task<bool> ReservarNotificacionAsync(long idHito, long idRegla, System.DateTime proximaNotificacion, System.DateTime ahoraUtc);
        Task CerrarHitoAsync(long idHito, string referenciaANCI, System.DateTime fechaCumplimiento);
        Task CancelarHitosAbiertosAsync(long idIncidente);
    }
}
