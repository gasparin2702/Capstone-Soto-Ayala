using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Sitrac.Servicio.Interfaces;

namespace Sitrac.Servicio
{
    public class AvanzadorCicloVidaServicio : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AvanzadorCicloVidaServicio> _logger;

        public AvanzadorCicloVidaServicio(IServiceProvider serviceProvider, ILogger<AvanzadorCicloVidaServicio> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try 
                {
                    using var scope = _serviceProvider.CreateScope();
                    var incidenteService = scope.ServiceProvider.GetRequiredService<IIncidenteServicio>();
                    await incidenteService.ProcesarCicloDeVidaAsync();
                    
                    _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Motor de ciclo de vida evaluó incidentes correctamente.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error en el motor de ciclo de vida: {ex.Message}");
                }
            }
        }
    }
}