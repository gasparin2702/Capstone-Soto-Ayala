using Microsoft.AspNetCore.Mvc;
using Sitrac.Service.Dtos;
using Sitrac.Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Sitrac.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IncidentesController : ControllerBase
    {
        private readonly IIncidenteService _incidenteService;

        public IncidentesController(IIncidenteService incidenteService)
        {
            _incidenteService = incidenteService;
        }

        [HttpPost("invgate/webhook")]
        public async Task<IActionResult> RecepcionarTicketInvgate([FromBody] CrearIncidenteRequest request)
        {
            try
            {
                var idGenerado = await _incidenteService.RegistrarIncidenteInvgateAsync(request);
                return Created($"/api/incidentes/{idGenerado}", new { Mensaje = "Incidente registrado exitosamente", Id = idGenerado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Error interno del servidor", Detalle = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerIncidentesActivos()
        {
            try
            {
                var incidentes = await _incidenteService.ObtenerIncidentesActivosAsync();
                return Ok(incidentes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Error al obtener la cola de incidentes", Detalle = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerDetalleIncidente(long id)
        {
            try
            {
                var detalle = await _incidenteService.ObtenerDetalleIncidenteAsync(id);
                if (detalle == null) return NotFound(new { Error = $"No se encontró el incidente con ID {id}" });
                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Error al obtener el detalle del incidente", Detalle = ex.Message });
            }
        }

        [HttpGet("{id}/historial")]
        public async Task<IActionResult> ObtenerHistorialIncidente(long id)
        {
            try
            {
                var historial = await _incidenteService.ObtenerHistorialIncidenteAsync(id);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Error al obtener el historial del incidente", Detalle = ex.Message });
            }
        }
    }
}