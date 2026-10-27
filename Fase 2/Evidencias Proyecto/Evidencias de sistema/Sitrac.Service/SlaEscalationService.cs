using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitrac.Repository.Interfaces;

namespace Sitrac.Service
{
    public class SlaEscalationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SlaEscalationService> _logger;

        public SlaEscalationService(IServiceProvider serviceProvider, ILogger<SlaEscalationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Revisa cada 1 minuto
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var hitoRepo = scope.ServiceProvider.GetRequiredService<IHitoRepository>();
                    var reglaRepo = scope.ServiceProvider.GetRequiredService<IReglaEscalamientoRepository>();
                    var incidenteRepo = scope.ServiceProvider.GetRequiredService<IIncidenteRepository>();
                    var notifService = scope.ServiceProvider.GetRequiredService<Interfaces.INotificacionService>();

                    var hitos = (await hitoRepo.ObtenerPendientesParaEvaluarAsync()).ToList();
                    if (!hitos.Any()) continue;

                    var ahora = DateTime.Now;
                    var ahoraUtc = DateTime.UtcNow;

                    // Agrupamos reglas por tipo para no consultar BD por cada hito
                    var reglasCache = new System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<Repository.Models.ReglaEscalamiento>>();

                    foreach (var hito in hitos)
                    {
                        var remainingMins = (hito.FechaLimite - ahora).TotalMinutes;

                        if (!reglasCache.ContainsKey(hito.CodTipoHito))
                        {
                            var reglas = await reglaRepo.ObtenerPorTipoHitoAsync(hito.CodTipoHito);
                            reglasCache[hito.CodTipoHito] = reglas.ToList();
                        }

                        var reglasHito = reglasCache[hito.CodTipoHito];
                        
                        // Buscar la regla más urgente que ya hayamos cruzado
                        // Ej: Si faltan 55 mins y hay reglas [120, 60, 30], el firstOrDefault devolverá 60.
                        var reglaAplicable = reglasHito
                            .OrderBy(r => r.TiempoRestanteMinutos) 
                            .FirstOrDefault(r => remainingMins <= r.TiempoRestanteMinutos);

                        if (reglaAplicable != null)
                        {
                            // Intentar reservar (evita envíos duplicados)
                            // Si la regla no tiene RepetirCadaMinutos (es un umbral único), la próxima notificación será nula
                            DateTime proximaNotificacion = reglaAplicable.RepetirCadaMinutos.HasValue 
                                ? ahoraUtc.AddMinutes(reglaAplicable.RepetirCadaMinutos.Value)
                                : DateTime.MaxValue;

                            bool reservado = await hitoRepo.ReservarNotificacionAsync(hito.IdHito, reglaAplicable.IdRegla, proximaNotificacion, ahoraUtc);

                            if (reservado)
                            {
                                var incidente = await incidenteRepo.ObtenerPorIdAsync(hito.IdIncidente);
                                if (incidente != null)
                                {
                                    var ticketStr = incidente.CodigoTicketInvgate ?? $"INC-{incidente.IdIncidente}";
                                    var clasificacion = incidente.CodTipoClasificacionEmpresa == 2 ? "OIV" : "Esencial";
                                    var hitoNombre = hito.CodTipoHito switch { 1 => "Aviso Temprano", 2 => "Informe Preliminar", 3 => "Informe Final", _ => "Hito" };

                                    await notifService.EnviarAlertaEscalamientoAsync(
                                        incidente.IdIncidente,
                                        ticketStr,
                                        incidente.DescripcionSistemaAfectado ?? "No especificado",
                                        (int)incidente.CodTipoGravedad,
                                        clasificacion,
                                        hitoNombre,
                                        reglaAplicable.Descripcion,
                                        hito.FechaLimite,
                                        remainingMins);

                                    _logger.LogInformation("[SlaEscalation] Alerta Enviada Ticket: {Ticket} Regla: {Regla} Faltan: {Mins} min", ticketStr, reglaAplicable.IdRegla, Math.Round(remainingMins));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el motor de escalamiento de SLA");
                }
            }
        }
    }
}