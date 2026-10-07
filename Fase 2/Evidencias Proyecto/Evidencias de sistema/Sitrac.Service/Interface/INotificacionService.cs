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
    }
}
