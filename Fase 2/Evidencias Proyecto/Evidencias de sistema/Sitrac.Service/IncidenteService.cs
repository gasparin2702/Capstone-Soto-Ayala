using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitrac.Repository.Interfaces;
using Sitrac.Repository.Models;
using Sitrac.Service.Configuration;
using Sitrac.Service.Dtos;
using Sitrac.Service.Interfaces;

namespace Sitrac.Service
{
    public class IncidenteService : IIncidenteService
    {
        private readonly IIncidenteRepository _incidenteRepository;
        private readonly ISlaRepository _slaRepository;
        private readonly INotificacionService _notificacionService;
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<IncidenteService> _logger;

        public IncidenteService(
            IIncidenteRepository incidenteRepository,
            ISlaRepository slaRepository,
            INotificacionService notificacionService,
            IOptions<SmtpSettings> smtpSettings,
            ILogger<IncidenteService> logger)
        {
            _incidenteRepository = incidenteRepository;
            _slaRepository = slaRepository;
            _notificacionService = notificacionService;
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task<long> RegistrarIncidenteInvgateAsync(CrearIncidenteRequest request)
        {
            var nuevoIncidente = new AnciIncidente
            {
                CodigoTicketInvgate = request.CodigoTicketInvgate,
                FechaTomaConocimiento = request.FechaTomaConocimiento,
                CodTipoClasificacionEmpresa = request.CodTipoClasificacionEmpresa,
                CodTipoGravedad = request.CodTipoGravedad,
                DescripcionSistemaAfectado = request.DescripcionSistemaAfectado,
                CodTipoCanalEntrada = 1,
                IndEsIncidenteReal = true,
                CodTipoDatosPersonales = 3,
                CodTipoEstadoIncidente = 1
            };

            var idGenerado = await _incidenteRepository.InsertarAsync(nuevoIncidente);
            var parametrosSla = await _slaRepository.ObtenerParametrosPorEmpresaAsync(request.CodTipoClasificacionEmpresa);

            if (parametrosSla != null)
            {
                var plazos = new AnciIncidentePlazoSla
                {
                    IdIncidente = idGenerado,
                    IdTipoPlazoSla = parametrosSla.IdTipoPlazoSla,
                    FechaTomaConocimiento = request.FechaTomaConocimiento,
                    FechaLimiteAvisoTemprano = request.FechaTomaConocimiento.AddHours(parametrosSla.HorasAvisoTemprano),
                    FechaLimiteInformePreliminar = request.FechaTomaConocimiento.AddHours(parametrosSla.HorasInformePreliminar),
                    FechaLimiteInformeFinal = request.FechaTomaConocimiento.AddDays(parametrosSla.DiasInformeFinal)
                };
                await _slaRepository.InsertarPlazosAsync(plazos);
            }

            await IntentarNotificarAsync(idGenerado, request, parametrosSla?.HorasAvisoTemprano ?? 3);

            return idGenerado;
        }

        private async Task IntentarNotificarAsync(long idIncidente, CrearIncidenteRequest request, int horasSla)
        {
            var destinatario = _smtpSettings.DestinatarioDemo;
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                _logger.LogInformation(
                    "[IncidenteService] Sin destinatario configurado. Omitiendo notificación para {Ticket}",
                    request.CodigoTicketInvgate);
                return;
            }

            try
            {
                var resultado = await _notificacionService.EnviarAlertaNuevoIncidenteAsync(
                    destinatario,
                    request.CodigoTicketInvgate ?? $"INC-{idIncidente}",
                    request.DescripcionSistemaAfectado ?? "No especificado",
                    horasSla);

                if (!resultado.Exito)
                    _logger.LogWarning("[IncidenteService] Alerta no enviada para {Ticket}: {Error}",
                        request.CodigoTicketInvgate, resultado.MensajeError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidenteService] Error notificando {Ticket}. Incidente guardado igual.",
                    request.CodigoTicketInvgate);
            }
        }

        public async Task ProcesarCicloDeVidaAsync()
        {
            var incidentes = await _incidenteRepository.ObtenerIncidentesParaEvaluacionSlaAsync();
            var ahora = DateTime.Now;

            foreach (var inc in incidentes)
            {
                long nuevoEstado = 0;
                string comentarioCambio = string.Empty;

                if (inc.CodTipoEstadoIncidente == 1 && ahora >= inc.FechaLimiteAvisoTemprano)
                {
                    nuevoEstado = 2;
                    comentarioCambio = "Avance automático a Aviso Enviado por vencimiento de SLA (3 horas).";
                }
                else if (inc.CodTipoEstadoIncidente == 2 && ahora >= inc.FechaLimiteInformePreliminar)
                {
                    nuevoEstado = 3;
                    comentarioCambio = "Avance automático a Preliminar Enviado por vencimiento de SLA.";
                }
                else if (inc.CodTipoEstadoIncidente == 3 && ahora >= inc.FechaLimiteInformeFinal)
                {
                    nuevoEstado = 4;
                    comentarioCambio = "Cierre automático de incidente por vencimiento de SLA (15 días).";
                }

                if (nuevoEstado > 0)
                {
                    var historial = new AnciEventoHistorial
                    {
                        IdIncidente = inc.IdIncidente,
                        CodigoTicketInvgate = inc.CodigoTicketInvgate,
                        IdTipoAmbitoCambio = 1,
                        IdTipoEstadoIncidenteAnterior = inc.CodTipoEstadoIncidente,
                        IdTipoEstadoIncidenteNuevo = nuevoEstado,
                        IdResponsableCambia = 1,
                        Comentario = comentarioCambio
                    };
                    await _incidenteRepository.AvanzarEstadoConHistorialAsync(inc.IdIncidente, nuevoEstado, historial);
                }
            }
        }

        public async Task<IEnumerable<IncidenteResumen>> ObtenerIncidentesActivosAsync()
            => await _incidenteRepository.ObtenerActivosResumenAsync();

        public async Task<IncidenteDetalle?> ObtenerDetalleIncidenteAsync(long idIncidente)
            => await _incidenteRepository.ObtenerDetallePorIdAsync(idIncidente);

        public async Task<IEnumerable<EventoHistorialResumen>> ObtenerHistorialIncidenteAsync(long idIncidente)
            => await _incidenteRepository.ObtenerHistorialAsync(idIncidente);
    }
}