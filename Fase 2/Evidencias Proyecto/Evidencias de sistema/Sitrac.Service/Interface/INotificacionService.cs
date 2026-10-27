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

        Task<ResultadoEnvio> EnviarResumenConsolidadoAsync(
            List<NotificacionLog> pendientes, 
            CancellationToken cancellationToken = default);

        // NUEVO: Para enviar alertas de umbral de SLA (antes de que venza)
        Task<ResultadoEnvio> EnviarAlertaEscalamientoAsync(
            long idIncidente, string ticketId, string sistema, int gravedad, string clasificacion, 
            string hitoNombre, string reglaDescripcion, DateTime fechaLimite, double minutosRestantes, 
            CancellationToken cancellationToken = default);
    }
}