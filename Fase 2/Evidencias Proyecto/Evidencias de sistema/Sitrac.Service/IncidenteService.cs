using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitrac.Repository.Interfaces;
using Sitrac.Repository.Models;
using Sitrac.Service.Configuration;
using Sitrac.Service.Dtos;
using Sitrac.Service.Helpers;
using Sitrac.Service.Interfaces;

namespace Sitrac.Service
{
    public class IncidenteService : IIncidenteService
    {
        private readonly IIncidenteRepository _incidenteRepository;
        private readonly ISlaRepository _slaRepository;
        private readonly INotificacionService _notificacionService;
        private readonly SmtpSettings _smtpSettings;
        private readonly TeamsSettings _teamsSettings;
        private readonly ILogger<IncidenteService> _logger;

        public IncidenteService(
            IIncidenteRepository incidenteRepository,
            ISlaRepository slaRepository,
            INotificacionService notificacionService,
            IOptions<SmtpSettings> smtpSettings,
            IOptions<TeamsSettings> teamsSettings,
            ILogger<IncidenteService> logger)
        {
            _incidenteRepository = incidenteRepository;
            _slaRepository = slaRepository;
            _notificacionService = notificacionService;
            _smtpSettings = smtpSettings.Value;
            _teamsSettings = teamsSettings.Value;
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

            AnciIncidentePlazoSla? plazos = null;
            if (parametrosSla != null)
            {
                plazos = new AnciIncidentePlazoSla
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

            var horasSla = parametrosSla?.HorasAvisoTemprano ?? 3;

            await IntentarNotificarEmailAsync(idGenerado, request, horasSla);
            await IntentarNotificarTeamsAsync(idGenerado, request, plazos, horasSla, esCreacion: true);

            return idGenerado;
        }

        private async Task IntentarNotificarEmailAsync(long idIncidente, CrearIncidenteRequest request, int horasSla)
        {
            var destinatario = _smtpSettings.DestinatarioDemo;
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                _logger.LogInformation(
                    "[IncidenteService][Email] Evento: Omitido Ticket: {Ticket} Motivo: SinDestinatarioConfigurado",
                    request.CodigoTicketInvgate);
                return;
            }

            var resultado = await _notificacionService.EnviarAlertaNuevoIncidenteAsync(
                destinatario,
                request.CodigoTicketInvgate ?? $"INC-{idIncidente}",
                request.DescripcionSistemaAfectado ?? "No especificado",
                horasSla);

            if (!resultado.Exito)
                _logger.LogWarning(
                    "[IncidenteService][Email] Evento: Fallo Ticket: {Ticket} Error: {Error}",
                    request.CodigoTicketInvgate, resultado.MensajeError);
        }

        private async Task IntentarNotificarTeamsAsync(
            long idIncidente,
            CrearIncidenteRequest request,
            AnciIncidentePlazoSla? plazos,
            int horasSla,
            bool esCreacion)
        {
            if (!_teamsSettings.EstaConfigurado())
            {
                _logger.LogInformation(
                    "[IncidenteService][Teams] Evento: Omitido Ticket: {Ticket} Motivo: TeamsNoConfigurado",
                    request.CodigoTicketInvgate);
                return;
            }

            var ahora = DateTime.Now;
            var fechaLimite = plazos?.FechaLimiteAvisoTemprano
                ?? request.FechaTomaConocimiento.AddHours(horasSla);

            var clasificacion = request.CodTipoClasificacionEmpresa == 2 ? "OIV" : "Esencial";
            var score = PrioridadCalculator.Calcular(
                gravedad: (int)request.CodTipoGravedad,
                clasificacion: clasificacion,
                hitoCodigo: 1,
                fechaLimite: fechaLimite,
                fechaVencimiento: plazos?.FechaTomaConocimiento ?? request.FechaTomaConocimiento,
                ahora: ahora);

            if (!esCreacion && score < _teamsSettings.UmbralPrioridadMinima)
            {
                _logger.LogInformation(
                    "[IncidenteService][Teams] Evento: Omitido Ticket: {Ticket} Score: {Score} Umbral: {Umbral}",
                    request.CodigoTicketInvgate, score, _teamsSettings.UmbralPrioridadMinima);
                return;
            }

            var razon = esCreacion
                ? "Nuevo incidente registrado"
                : "Cambio de estado relevante";

            var resultado = await _notificacionService.EnviarAlertaTeamsAsync(
                ticketId: request.CodigoTicketInvgate ?? $"INC-{idIncidente}",
                sistema: request.DescripcionSistemaAfectado ?? "No especificado",
                gravedad: (int)request.CodTipoGravedad,
                clasificacion: clasificacion,
                estado: "En triage",
                prioridadScore: score,
                hitoPendiente: "Aviso Temprano",
                razon: razon);

            if (!resultado.Exito)
                _logger.LogWarning(
                    "[IncidenteService][Teams] Evento: Fallo Ticket: {Ticket} Score: {Score} Error: {Error}",
                    request.CodigoTicketInvgate, score, resultado.MensajeError);
        }

        public async Task ProcesarCicloDeVidaAsync()
        {
            var incidentes = await _incidenteRepository.ObtenerIncidentesParaEvaluacionSlaAsync();
            var ahora = DateTime.Now;

            foreach (var inc in incidentes)
            {
                long nuevoEstado = 0;
                string comentarioCambio = string.Empty;
                string hitoPendiente = string.Empty;

                if (inc.CodTipoEstadoIncidente == 1 && ahora >= inc.FechaLimiteAvisoTemprano)
                {
                    nuevoEstado = 2;
                    comentarioCambio = "Avance automático a Aviso Enviado por vencimiento de SLA (3 horas).";
                    hitoPendiente = "Aviso Temprano";
                }
                else if (inc.CodTipoEstadoIncidente == 2 && ahora >= inc.FechaLimiteInformePreliminar)
                {
                    nuevoEstado = 3;
                    comentarioCambio = "Avance automático a Preliminar Enviado por vencimiento de SLA.";
                    hitoPendiente = "Informe Preliminar";
                }
                else if (inc.CodTipoEstadoIncidente == 3 && ahora >= inc.FechaLimiteInformeFinal)
                {
                    nuevoEstado = 4;
                    comentarioCambio = "Cierre automático de incidente por vencimiento de SLA (15 días).";
                    hitoPendiente = "Informe Final";
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

                    if (nuevoEstado < 4)
                    {
                        await NotificarTeamsPorAvanceMotorAsync(inc, nuevoEstado, hitoPendiente, ahora);
                    }
                }
            }
        }

        private async Task NotificarTeamsPorAvanceMotorAsync(
            IncidenteSlaInfo inc,
            long nuevoEstado,
            string hitoPendiente,
            DateTime ahora)
        {
            if (!_teamsSettings.EstaConfigurado())
                return;

            var gravedad = inc.CodTipoGravedad;
            var clasificacion = inc.CodTipoClasificacionEmpresa == 2 ? "OIV" : "Esencial";

            var fechaLimite = nuevoEstado switch
            {
                2 => inc.FechaLimiteInformePreliminar,
                3 => inc.FechaLimiteInformeFinal,
                _ => inc.FechaLimiteInformeFinal
            };

            var score = PrioridadCalculator.Calcular(
                gravedad: (int)gravedad,
                clasificacion: clasificacion,
                hitoCodigo: nuevoEstado,
                fechaLimite: fechaLimite,
                fechaVencimiento: inc.FechaLimiteAvisoTemprano,
                ahora: ahora);

            if (score < _teamsSettings.UmbralPrioridadMinima)
            {
                _logger.LogInformation(
                    "[IncidenteService][Teams] Evento: OmitidoMotor Ticket: {Ticket} Score: {Score} Umbral: {Umbral}",
                    inc.CodigoTicketInvgate, score, _teamsSettings.UmbralPrioridadMinima);
                return;
            }

            var estadoTexto = nuevoEstado switch
            {
                2 => "Aviso Temprano",
                3 => "Informe Preliminar",
                _ => "Desconocido"
            };

            var resultado = await _notificacionService.EnviarAlertaTeamsAsync(
                ticketId: inc.CodigoTicketInvgate ?? $"INC-{inc.IdIncidente}",
                sistema: "Sistema registrado",
                gravedad: (int)gravedad,
                clasificacion: clasificacion,
                estado: estadoTexto,
                prioridadScore: score,
                hitoPendiente: hitoPendiente,
                razon: "Plazo vencido: el motor avanzó el estado automáticamente");

            if (!resultado.Exito)
                _logger.LogWarning(
                    "[IncidenteService][Teams] Evento: FalloMotor Ticket: {Ticket} Score: {Score} Error: {Error}",
                    inc.CodigoTicketInvgate, score, resultado.MensajeError);
        }

        public async Task<IEnumerable<IncidenteResumen>> ObtenerIncidentesActivosAsync()
            => await _incidenteRepository.ObtenerActivosResumenAsync();

        public async Task<IncidenteDetalle?> ObtenerDetalleIncidenteAsync(long idIncidente)
            => await _incidenteRepository.ObtenerDetallePorIdAsync(idIncidente);

        public async Task<IEnumerable<EventoHistorialResumen>> ObtenerHistorialIncidenteAsync(long idIncidente)
            => await _incidenteRepository.ObtenerHistorialAsync(idIncidente);
    }
}