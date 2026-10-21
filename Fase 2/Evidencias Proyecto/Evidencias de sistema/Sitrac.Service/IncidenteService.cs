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
        private readonly INotificacionLogRepository _notificacionLogRepository;
        private readonly IHitoRepository _hitoRepository;
        private readonly SmtpSettings _smtpSettings;
        private readonly TeamsSettings _teamsSettings;
        private readonly ILogger<IncidenteService> _logger;

        public IncidenteService(
            IIncidenteRepository incidenteRepository,
            ISlaRepository slaRepository,
            INotificacionService notificacionService,
            INotificacionLogRepository notificacionLogRepository,
            IHitoRepository hitoRepository,
            IOptions<SmtpSettings> smtpSettings,
            IOptions<TeamsSettings> teamsSettings,
            ILogger<IncidenteService> logger)
        {
            _incidenteRepository = incidenteRepository;
            _slaRepository = slaRepository;
            _notificacionService = notificacionService;
            _notificacionLogRepository = notificacionLogRepository;
            _hitoRepository = hitoRepository;
            _smtpSettings = smtpSettings.Value;
            _teamsSettings = teamsSettings.Value;
            _logger = logger;
        }

        public async Task<long> RegistrarIncidenteInvgateAsync(CrearIncidenteRequest request)
        {
            var nuevoIncidente = new SitracIncidente
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

            SitracIncidentePlazoSla? plazos = null;
            if (parametrosSla != null)
            {
                plazos = new SitracIncidentePlazoSla
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

            var hitoAviso = new Hito
            {
                IdIncidente = idGenerado,
                CodTipoHito = 1,
                Secuencia = 1,
                FechaBase = request.FechaTomaConocimiento,
                FechaLimite = plazos?.FechaLimiteAvisoTemprano ?? request.FechaTomaConocimiento.AddHours(3),
                Estado = "Pendiente"
            };
            var idHitoAviso = await _hitoRepository.CrearAsync(hitoAviso);

            string ticketStr = request.CodigoTicketInvgate ?? $"INC-{idGenerado}";

            // Calcular Prioridad para enrutamiento inteligente
            var clasificacionStr = request.CodTipoClasificacionEmpresa == 2 ? "OIV" : "Esencial";
            var score = PrioridadCalculator.Calcular(
                gravedad: (int)request.CodTipoGravedad,
                clasificacion: clasificacionStr,
                hitoCodigo: 1,
                fechaLimite: plazos?.FechaLimiteAvisoTemprano ?? request.FechaTomaConocimiento.AddHours(3),
                fechaVencimiento: request.FechaTomaConocimiento,
                ahora: DateTime.Now);

            // ENRUTAMIENTO: Si es Crítico (>=30), enviamos inmediatamente. Si no, encolamos.
            if (score >= 30)
            {
                _logger.LogInformation("[IncidenteService][Router] Evento: EnvioInmediato Ticket: {Ticket} Score: {Score}", ticketStr, score);
                await _notificacionService.EnviarAlertaNuevoIncidenteAsync(idGenerado, _smtpSettings.DestinatarioDemo, ticketStr, request.DescripcionSistemaAfectado, 3);
                await _notificacionService.EnviarAlertaTeamsAsync(idGenerado, ticketStr, request.DescripcionSistemaAfectado, (int)request.CodTipoGravedad, clasificacionStr, "En triage", score, "Aviso Temprano", "Nuevo incidente crítico");
            }
            else
            {
                await EncolarNotificacionAsync(idHitoAviso, "Email", ticketStr, DateTime.UtcNow.AddSeconds(30));
                await EncolarNotificacionAsync(idHitoAviso, "Teams", ticketStr, DateTime.UtcNow.AddSeconds(30));
            }

            return idGenerado;
        }

        private async Task EncolarNotificacionAsync(long idHito, string canal, string ticketId, DateTime fechaProgramada)
        {
            var log = new NotificacionLog
            {
                IdHito = idHito,
                NumeroNotificacion = 1,
                Destinatario = ticketId,
                Canal = canal,
                Estado = "Pendiente",
                FechaProgramada = fechaProgramada
            };
            await _notificacionLogRepository.InsertarAsync(log);
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
                    var hitoFinal = await _hitoRepository.ObtenerHitoPendientePorIncidenteYTipoAsync(inc.IdIncidente, 3);
                    if (hitoFinal == null) continue;

                    nuevoEstado = 4;
                    comentarioCambio = "Cierre automático de incidente por vencimiento de SLA (15 días).";
                }

                if (nuevoEstado > 0)
                {
                    var historial = new SitracEventoHistorial
                    {
                        IdIncidente = inc.IdIncidente,
                        CodigoTicketInvgate = inc.CodigoTicketInvgate ?? $"INC-{inc.IdIncidente}",
                        IdTipoAmbitoCambio = 1,
                        IdTipoEstadoIncidenteAnterior = inc.CodTipoEstadoIncidente,
                        IdTipoEstadoIncidenteNuevo = nuevoEstado,
                        IdResponsableCambia = 1,
                        Comentario = comentarioCambio
                    };

                    Hito? hitoACerrar = null;
                    Hito? hitoACrear = null;

                    if (nuevoEstado == 2)
                    {
                        hitoACerrar = await _hitoRepository.ObtenerHitoPendientePorIncidenteYTipoAsync(inc.IdIncidente, 1);
                        hitoACrear = new Hito { IdIncidente = inc.IdIncidente, CodTipoHito = 2, Secuencia = 1, FechaBase = inc.FechaLimiteAvisoTemprano, FechaLimite = inc.FechaLimiteInformePreliminar, Estado = "Pendiente" };
                    }
                    else if (nuevoEstado == 3)
                    {
                        hitoACerrar = await _hitoRepository.ObtenerHitoPendientePorIncidenteYTipoAsync(inc.IdIncidente, 2);
                        hitoACrear = new Hito { IdIncidente = inc.IdIncidente, CodTipoHito = 3, Secuencia = 1, FechaBase = inc.FechaLimiteInformePreliminar, FechaLimite = inc.FechaLimiteInformeFinal, Estado = "Pendiente" };
                    }
                    else if (nuevoEstado == 4)
                    {
                        hitoACerrar = await _hitoRepository.ObtenerHitoPendientePorIncidenteYTipoAsync(inc.IdIncidente, 3);
                    }

                    await _incidenteRepository.AvanzarEstadoConHitosTransaccionalAsync(inc.IdIncidente, nuevoEstado, historial, hitoACerrar, hitoACrear, ahora);

                    if (nuevoEstado < 4 && hitoACrear != null)
                    {
                        var hitoId = await _hitoRepository.ObtenerHitoPendientePorIncidenteYTipoAsync(inc.IdIncidente, (long)nuevoEstado + 1);
                        if(hitoId != null)
                        {
                            string ticketStr = inc.CodigoTicketInvgate ?? $"INC-{inc.IdIncidente}";
                            await EncolarNotificacionAsync(hitoId.IdHito, "Email", ticketStr, DateTime.UtcNow.AddSeconds(30));
                            await EncolarNotificacionAsync(hitoId.IdHito, "Teams", ticketStr, DateTime.UtcNow.AddSeconds(30));
                        }
                    }
                }
            }
        }

        public async Task<IEnumerable<IncidenteResumen>> ObtenerIncidentesActivosAsync()
            => await _incidenteRepository.ObtenerActivosResumenAsync();

        public async Task<IncidenteDetalle?> ObtenerDetalleIncidenteAsync(long idIncidente)
            => await _incidenteRepository.ObtenerDetallePorIdAsync(idIncidente);

        public async Task<IEnumerable<EventoHistorialResumen>> ObtenerHistorialIncidenteAsync(long idIncidente)
            => await _incidenteRepository.ObtenerHistorialAsync(idIncidente);

        public async Task ConfirmarAvisoAsync(long idIncidente, ConfirmarAvisoRequest request)
        {
            var incidente = await _incidenteRepository.ObtenerPorIdAsync(idIncidente)
                ?? throw new KeyNotFoundException($"Incidente {idIncidente} no existe.");

            if (incidente.CodTipoEstadoIncidente != 1)
                throw new InvalidOperationException($"Incidente en estado {incidente.CodTipoEstadoIncidente}, se esperaba 1 (En triage).");

            var ahora = DateTime.Now;
            var hitoAviso = await _hitoRepository.ObtenerHitoPendientePorIncidenteYTipoAsync(idIncidente, 1)
                ?? throw new InvalidOperationException("No hay hito Aviso pendiente para este incidente.");

            hitoAviso.ReferenciaExterna = request.ReferenciaEnvio;

            var plazoSla = await _slaRepository.ObtenerParametrosPorEmpresaAsync(incidente.CodTipoClasificacionEmpresa);
            var horasPrelim = plazoSla?.HorasInformePreliminar ?? 72;

            var hitoPreliminar = new Hito
            {
                IdIncidente = idIncidente,
                CodTipoHito = 2,
                Secuencia = 1,
                FechaBase = incidente.FechaTomaConocimiento,
                FechaLimite = incidente.FechaTomaConocimiento.AddHours(horasPrelim),
                Estado = "Pendiente"
            };

            string ticketStr = incidente.CodigoTicketInvgate ?? $"INC-{idIncidente}";

            var historial = new SitracEventoHistorial
            {
                IdIncidente = idIncidente,
                CodigoTicketInvgate = ticketStr,
                IdTipoAmbitoCambio = 2,
                IdTipoEstadoIncidenteAnterior = 1,
                IdTipoEstadoIncidenteNuevo = 2,
                IdResponsableCambia = 1,
                Comentario = request.Comentario
            };

            await _incidenteRepository.ConfirmarAvisoTransaccionalAsync(idIncidente, 2, historial, hitoAviso, hitoPreliminar, ahora);
            
            var hitoCreado = await _hitoRepository.ObtenerHitoPendientePorIncidenteYTipoAsync(idIncidente, 2);
            if (hitoCreado != null)
            {
                await EncolarNotificacionAsync(hitoCreado.IdHito, "Teams", ticketStr, DateTime.UtcNow.AddSeconds(30));
            }
        }

        public async Task RegistrarInformeAsync(long idIncidente, RegistrarInformeRequest request)
        {
            var incidente = await _incidenteRepository.ObtenerPorIdAsync(idIncidente)
                ?? throw new KeyNotFoundException($"Incidente {idIncidente} no existe.");

            var codTipoHito = request.Tipo == "P" ? 2L : 3L;
            var hito = await _hitoRepository.ObtenerHitoPendientePorIncidenteYTipoAsync(idIncidente, codTipoHito)
                ?? throw new InvalidOperationException($"No hay hito {(request.Tipo == "P" ? "Preliminar" : "Final")} pendiente.");

            hito.ReferenciaExterna = request.NombreArchivo;

            long? nuevoEstado = null;
            Hito? hitoSiguiente = null;
            InformePreliminar? informeP = null;
            InformeCierre? informeC = null;

            string hayDp = "No";
            if (request.CodTipoDatosPersonales == 1) hayDp = "Sí";
            if (request.CodTipoDatosPersonales == 3) hayDp = "Desconocido";

            if (request.Tipo == "P")
            {
                if (incidente.CodTipoEstadoIncidente != 2)
                    throw new InvalidOperationException($"Incidente en estado {incidente.CodTipoEstadoIncidente}, se esperaba 2 (Aviso enviado).");

                nuevoEstado = 3;
                var plazoSla = await _slaRepository.ObtenerParametrosPorEmpresaAsync(incidente.CodTipoClasificacionEmpresa);
                var diasFinal = plazoSla?.DiasInformeFinal ?? 15;

                hitoSiguiente = new Hito
                {
                    IdIncidente = idIncidente,
                    CodTipoHito = 3,
                    Secuencia = 1,
                    FechaBase = incidente.FechaTomaConocimiento,
                    FechaLimite = incidente.FechaTomaConocimiento.AddDays(diasFinal),
                    Estado = "Pendiente"
                };

                informeP = new InformePreliminar
                {
                    IdIncidente = idIncidente,
                    FechaEstimadaInicio = request.FechaEstimadaInicio,
                    IndiciosOcurrencia = request.IndiciosOcurrencia,
                    ActivosAfectados = request.ActivosAfectados,
                    EvidenciaDisponible = request.EvidenciaDisponible,
                    EvidenciaPosibleDelito = request.EvidenciaPosibleDelito,
                    RepercusionesOtrasInstituciones = request.RepercusionesOtrasInstituciones,
                    OtraInformacionRelevante = request.OtraInformacionRelevante,
                    EvaluacionGravedadImpacto = request.EvaluacionGravedadImpacto,
                    EstadoActualIncidente = request.EstadoActualIncidente,
                    DescripcionIocs = request.DescripcionIocs,
                    DescripcionMedidasMitigacion = request.DescripcionMedidasMitigacion,
                    HayDatosPersonales = hayDp,
                    FechaNotificacionDpo = request.FechaNotificacionDpo,
                    ResponsableDpo = request.IdResponsableDpo?.ToString(),
                    FechaEnvio = DateTime.Now
                };
            }
            else
            {
                if (incidente.CodTipoEstadoIncidente != 3)
                    throw new InvalidOperationException($"Incidente en estado {incidente.CodTipoEstadoIncidente}, se esperaba 3 (Preliminar enviado).");

                informeC = new InformeCierre
                {
                    IdIncidente = idIncidente,
                    FechaEstimadaInicio = request.FechaEstimadaInicio,
                    IndiciosOcurrencia = request.IndiciosOcurrencia,
                    ActivosAfectados = request.ActivosAfectados,
                    EvidenciaPosibleDelito = request.EvidenciaPosibleDelito,
                    RepercusionesOtrasInstituciones = request.RepercusionesOtrasInstituciones,
                    OtraInformacionRelevante = request.OtraInformacionRelevante,
                    DescripcionIocs = request.DescripcionIocs,
                    DescripcionDetallada = request.DescripcionDetallada,
                    TipoAmenazaCausaPrincipal = request.TipoAmenazaCausaPrincipal,
                    RepercusionesTransfronterizas = request.RepercusionesTransfronterizas,
                    MedidasMitigacionAplicadas = request.MedidasMitigacionAplicadas,
                    VulnerabilidadesExplotadas = request.VulnerabilidadesExplotadas,
                    AplicacionesAbusivas = request.AplicacionesAbusivas,
                    ControlesTecnicosPrevistos = request.ControlesTecnicosPrevistos,
                    CausasAusenciaControles = request.CausasAusenciaControles,
                    MedidasFrenteAlertasCsirt = request.MedidasFrenteAlertasCsirt,
                    HayDatosPersonales = hayDp,
                    FechaNotificacionDpo = request.FechaNotificacionDpo,
                    ResponsableDpo = request.IdResponsableDpo?.ToString(),
                    FechaEnvio = DateTime.Now
                };
            }

            string ticketStr = incidente.CodigoTicketInvgate ?? $"INC-{idIncidente}";

            var historial = new SitracEventoHistorial
            {
                IdIncidente = idIncidente,
                CodigoTicketInvgate = ticketStr,
                IdTipoAmbitoCambio = 3,
                IdTipoEstadoIncidenteAnterior = incidente.CodTipoEstadoIncidente,
                IdTipoEstadoIncidenteNuevo = nuevoEstado ?? incidente.CodTipoEstadoIncidente,
                IdResponsableCambia = 1,
                Comentario = request.Comentario
            };

            await _incidenteRepository.RegistrarInformeTransaccionalAsync(idIncidente, nuevoEstado, historial, hito, hitoSiguiente, DateTime.Now, informeP, informeC, null);
        }

        public async Task CerrarIncidenteAsync(long idIncidente, CerrarIncidenteRequest request)
        {
            var incidente = await _incidenteRepository.ObtenerPorIdAsync(idIncidente)
                ?? throw new KeyNotFoundException($"Incidente {idIncidente} no existe.");

            if (incidente.CodTipoEstadoIncidente != 3)
                throw new InvalidOperationException($"No se puede cerrar: incidente en estado {incidente.CodTipoEstadoIncidente}, se esperaba 3.");

            var hitoFinal = await _hitoRepository.ObtenerHitoPendientePorIncidenteYTipoAsync(idIncidente, 3);
            if (hitoFinal != null)
                throw new InvalidOperationException("No se puede cerrar: el Informe Final aún no ha sido registrado.");

            string ticketStr = incidente.CodigoTicketInvgate ?? $"INC-{idIncidente}";

            var historial = new SitracEventoHistorial
            {
                IdIncidente = idIncidente,
                CodigoTicketInvgate = ticketStr,
                IdTipoAmbitoCambio = 4,
                IdTipoEstadoIncidenteAnterior = 3,
                IdTipoEstadoIncidenteNuevo = 4,
                IdResponsableCambia = 1,
                Comentario = request.Comentario
            };

            await _incidenteRepository.CerrarIncidenteTransaccionalAsync(idIncidente, historial, DateTime.Now);
        }
    }
}