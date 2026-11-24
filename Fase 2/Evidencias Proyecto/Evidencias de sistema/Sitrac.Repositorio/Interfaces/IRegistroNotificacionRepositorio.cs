using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repositorio.Models;

namespace Sitrac.Repositorio.Interfaces
{
    public interface IRegistroNotificacionRepositorio
    {
        Task InsertarLoteAsync(IEnumerable<NotificacionLog> logs);
        Task InsertarAsync(NotificacionLog log);
        Task<IEnumerable<NotificacionLog>> ObtenerPendientesParaEnvioAsync(int maximo, int maximoIntentos);
        Task MarcarEnviadaAsync(
            long idNotificacion,
            System.DateTime fechaEnvio,
            string? destinatarioCorreo = null,
            string? idMensajeProveedor = null,
            string? referenciaTicket = null);
        Task MarcarFallidaAsync(long idNotificacion, string mensajeError, int maximoIntentos, int retrasoBaseSegundos = 30);
        Task MarcarCanceladaAsync(long idNotificacion, string motivo);
        Task CancelarPendientesPorHitoAsync(long idHito);
    }
}
