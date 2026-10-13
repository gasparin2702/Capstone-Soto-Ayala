using System.Threading;
using System.Threading.Tasks;
using Sitrac.Service.Dtos;

namespace Sitrac.Service.Interfaces
{
    public interface INotificacionService
    {
        Task<ResultadoEnvio> EnviarAlertaNuevoIncidenteAsync(
            string destinatario,
            string ticketId,
            string sistema,
            int horasSla,
            CancellationToken cancellationToken = default);

        Task<ResultadoEnvio> EnviarAlertaTeamsAsync(
            string ticketId,
            string sistema,
            int gravedad,
            string clasificacion,
            string estado,
            double prioridadScore,
            string hitoPendiente,
            string? razon,
            CancellationToken cancellationToken = default);
    }
}