using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repository.Interfaces;
using Sitrac.Repository.Models;
using Sitrac.Service.Dtos;
using Sitrac.Service.Interfaces;

namespace Sitrac.Service
{
    public class IncidenteService : IIncidenteService
    {
        private readonly IIncidenteRepository _incidenteRepository;
        private readonly ISlaRepository _slaRepository;

        public IncidenteService(IIncidenteRepository incidenteRepository, ISlaRepository slaRepository)
        {
            _incidenteRepository = incidenteRepository;
            _slaRepository = slaRepository;
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

            return idGenerado;
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
        {
            return await _incidenteRepository.ObtenerActivosResumenAsync();
        }

        public async Task<IncidenteDetalle?> ObtenerDetalleIncidenteAsync(long idIncidente)
        {
            return await _incidenteRepository.ObtenerDetallePorIdAsync(idIncidente);
        }

        public async Task<IEnumerable<EventoHistorialResumen>> ObtenerHistorialIncidenteAsync(long idIncidente)
        {
            return await _incidenteRepository.ObtenerHistorialAsync(idIncidente);
        }
    }
}