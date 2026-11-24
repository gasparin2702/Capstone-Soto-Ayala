using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitrac.Servicio.Configuration;
using Sitrac.Servicio.Interfaces;
using Sitrac.Repositorio.Interfaces;
using Sitrac.Repositorio.Models;

namespace Sitrac.Servicio
{
    public class DespachadorNotificacionServicio : BackgroundService
    {
        private const int RetrasoBaseReintentoSegundos = 30;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DespachadorNotificacionServicio> _logger;
        private readonly ConfiguracionGraph _graphSettings;
        private readonly ConfiguracionTeams _teamsSettings;

        public DespachadorNotificacionServicio(
            IServiceProvider serviceProvider,
            ILogger<DespachadorNotificacionServicio> logger,
            IOptions<ConfiguracionGraph> graphOptions,
            IOptions<ConfiguracionTeams> teamsOptions)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _graphSettings = graphOptions.Value;
            _teamsSettings = teamsOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var logRepo = scope.ServiceProvider.GetRequiredService<IRegistroNotificacionRepositorio>();
                    var notifService = scope.ServiceProvider.GetRequiredService<INotificacionServicio>();

                    var maximoIntentos = _graphSettings.MaxReintentos + 1;
                    var pendientes = (await logRepo.ObtenerPendientesParaEnvioAsync(50, maximoIntentos)).ToList();
                    if (pendientes.Count == 0) continue;

                    _logger.LogInformation("[Dispatcher] Lote recibido. Total: {Total} Email: {Correos} Teams: {Teams}",
                        pendientes.Count,
                        pendientes.Count(item => item.Canal == "Email"),
                        pendientes.Count(item => item.Canal == "Teams"));

                    var correos = pendientes.Where(item => item.Canal == "Email").ToList();
                    var teams = pendientes.Where(item => item.Canal == "Teams").ToList();

                    await ProcesarCorreosAsync(logRepo, notifService, correos, maximoIntentos, stoppingToken);
                    await ProcesarTeamsAsync(logRepo, notifService, teams, maximoIntentos, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el motor de consolidación de notificaciones");
                }
            }
        }

        private async Task ProcesarCorreosAsync(
            IRegistroNotificacionRepositorio logRepo,
            INotificacionServicio notifService,
            IReadOnlyList<NotificacionLog> correos,
            int maximoIntentos,
            CancellationToken cancellationToken)
        {
            if (correos.Count == 0) return;

            var resultado = await notifService.EnviarResumenConsolidadoAsync(correos.ToList(), cancellationToken);

            if (resultado.Exito)
            {
                foreach (var item in correos)
                {
                    await logRepo.MarcarEnviadaAsync(
                        item.IdNotificacion,
                        DateTime.UtcNow,
                        resultado.DestinatarioCorreo,
                        resultado.IdMensajeProveedor,
                        ObtenerReferenciaTicket(item));
                }

                _logger.LogInformation("[Dispatcher][Email] Lote enviado. Cantidad: {Cantidad} Destinatarios: {Destinatarios}",
                    correos.Count,
                    resultado.DestinatarioCorreo);
                return;
            }

            var detalle = resultado.MensajeError ?? "Fallo desconocido al enviar correo consolidado.";
            foreach (var item in correos)
            {
                await logRepo.MarcarFallidaAsync(item.IdNotificacion, detalle, maximoIntentos, RetrasoBaseReintentoSegundos);
            }

            _logger.LogWarning("[Dispatcher][Email] Fallo el envío del lote. Cantidad: {Cantidad} Detalle: {Detalle}", correos.Count, detalle);
        }

        private async Task ProcesarTeamsAsync(
            IRegistroNotificacionRepositorio logRepo,
            INotificacionServicio notifService,
            IReadOnlyList<NotificacionLog> teams,
            int maximoIntentos,
            CancellationToken cancellationToken)
        {
            if (teams.Count == 0) return;

            if (!_teamsSettings.EstaConfigurado())
            {
                foreach (var item in teams)
                {
                    await logRepo.MarcarCanceladaAsync(item.IdNotificacion, "Teams deshabilitado o sin WebhookUrl configurado.");
                }

                _logger.LogInformation("[Dispatcher][Teams] Filas canceladas por configuración. Cantidad: {Cantidad}", teams.Count);
                return;
            }

            var resultado = await notifService.EnviarResumenConsolidadoAsync(teams.ToList(), cancellationToken);

            if (resultado.Exito)
            {
                foreach (var item in teams)
                {
                    await logRepo.MarcarEnviadaAsync(item.IdNotificacion, DateTime.UtcNow);
                }

                _logger.LogInformation("[Dispatcher][Teams] Lote enviado. Cantidad: {Cantidad}", teams.Count);
                return;
            }

            var detalle = resultado.MensajeError ?? "Fallo desconocido al enviar notificación consolidada de Teams.";
            foreach (var item in teams)
            {
                await logRepo.MarcarFallidaAsync(item.IdNotificacion, detalle, maximoIntentos, RetrasoBaseReintentoSegundos);
            }

            _logger.LogWarning("[Dispatcher][Teams] Fallo el envío del lote. Cantidad: {Cantidad} Detalle: {Detalle}", teams.Count, detalle);
        }

        private static string? ObtenerReferenciaTicket(NotificacionLog item)
        {
            return string.IsNullOrWhiteSpace(item.ReferenciaTicket) ? item.Destinatario : item.ReferenciaTicket;
        }
    }
}
