using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repository.Models;

namespace Sitrac.Repository.Interfaces
{
    public interface INotificacionLogRepository
    {
        Task InsertarLoteAsync(IEnumerable<NotificacionLog> logs);
        Task<IEnumerable<NotificacionLog>> ObtenerPendientesParaEnvioAsync(int maximo);
        Task MarcarEnviadaAsync(long idNotificacion, System.DateTime fechaEnvio);
        Task MarcarFallidaAsync(long idNotificacion, string mensajeError, System.DateTime? proximoReintento);
        Task CancelarPendientesPorHitoAsync(long idHito);
    }
}
