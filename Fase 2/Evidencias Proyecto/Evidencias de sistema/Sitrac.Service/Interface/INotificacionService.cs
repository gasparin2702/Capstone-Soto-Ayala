using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sitrac.Service.Dtos;
using Sitrac.Repository.Models;

namespace Sitrac.Service.Interfaces
{
    public interface INotificacionService
    {
        Task<ResultadoEnvio> EnviarAlertaNuevoIncidenteAsync(
            long idIncidente, string destinatario, string ticketId, string sistema, int horasSla, CancellationToken cancellationToken = default);

        Task<ResultadoEnvio> EnviarAlertaTeamsAsync(
            long idIncidente, string ticketId, string sistema, int gravedad, string clasificacion, string estado, double prioridadScore, string hitoPendiente, string? razon, CancellationToken cancellationToken = default);

        // NUEVO: Para enviar el resumen consolidado
        Task<ResultadoEnvio> EnviarResumenConsolidadoAsync(
            List<NotificacionLog> pendientes, 
            CancellationToken cancellationToken = default);
    }
}