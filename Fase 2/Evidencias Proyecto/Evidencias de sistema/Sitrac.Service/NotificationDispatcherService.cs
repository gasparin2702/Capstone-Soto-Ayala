using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitrac.Service.Interfaces;
using Sitrac.Repository.Interfaces;

namespace Sitrac.Service
{
    public class NotificationDispatcherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationDispatcherService> _logger;

        public NotificationDispatcherService(IServiceProvider serviceProvider, ILogger<NotificationDispatcherService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Ventana deslizante: Revisa la cola cada 15 segundos
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var logRepo = scope.ServiceProvider.GetRequiredService<INotificacionLogRepository>();
                    var notifService = scope.ServiceProvider.GetRequiredService<INotificacionService>();

                    // Buscar notificaciones pendientes (cuya FechaProgramada ya pasó)
                    var pendientes = (await logRepo.ObtenerPendientesParaEnvioAsync(50)).ToList();

                    if (pendientes.Any())
                    {
                        _logger.LogInformation("[Dispatcher] Enviando lote consolidado de {Count} notificaciones", pendientes.Count);

                        // Enviamos el resumen consolidado
                        var resultado = await notifService.EnviarResumenConsolidadoAsync(pendientes, stoppingToken);

                        if (resultado.Exito)
                        {
                            foreach (var item in pendientes)
                            {
                                await logRepo.MarcarEnviadaAsync(item.IdNotificacion, DateTime.UtcNow);
                            }
                            _logger.LogInformation("[Dispatcher] Lote de {Count} notificaciones enviado correctamente", pendientes.Count);
                        }
                        else
                        {
                            _logger.LogWarning("[Dispatcher] Fallo el envío del lote. Se reintentará en el próximo ciclo.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el motor de consolidación de notificaciones");
                }
            }
        }
    }
}