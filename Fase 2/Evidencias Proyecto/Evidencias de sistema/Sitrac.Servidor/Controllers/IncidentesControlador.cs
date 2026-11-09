using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sitrac.Servicio.Dtos;
using Sitrac.Servicio.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Servidor.Controllers
{
    [Authorize] // Protege todos los endpoints por defecto
    [ApiController]
    [Route("api/Incidentes")]
    public class IncidentesControlador : ControllerBase
    {
        private readonly IIncidenteServicio _incidenteService;
        private readonly IConfiguration _configuration;

        public IncidentesControlador(IIncidenteServicio incidenteService, IConfiguration configuration)
        {
            _incidenteService = incidenteService;
            _configuration = configuration;
        }

        [AllowAnonymous] // Webhook público, pero validado con API Key
        [HttpPost("invgate/webhook")]
        public async Task<IActionResult> RecepcionarTicketInvgate([FromBody] CrearIncidenteSolicitud request)
        {
            // Validación de API Key (Seguridad)
            var apiKeyEsperada = _configuration["WebhookApiKey"] ?? "SITRAC_INVGATE_SECRET_2026";
            if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey) || apiKey != apiKeyEsperada)
            {
                return Unauthorized(new { Error = "API Key inválida o ausente." });
            }

            try
            {
                var idGenerado = await _incidenteService.RegistrarIncidenteInvgateAsync(request);
                return Created($"/api/incidentes/{idGenerado}", new { Mensaje = "Incidente registrado exitosamente", Id = idGenerado });
            }
            catch (Exception)
            {
                // Hardening: No exponer ex.Message
                return StatusCode(500, new { Error = "Error interno al procesar el ticket." });
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
            catch (Exception)
            {
                return StatusCode(500, new { Error = "Error al obtener la cola de incidentes." });
            }
        }

        // ... (Aplica el mismo patrón a los demás endpoints: eliminar ex.Message del catch y retornar mensaje genérico)

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerDetalleIncidente(long id)
        {
            try
            {
                var detalle = await _incidenteService.ObtenerDetalleIncidenteAsync(id);
                if (detalle == null) return NotFound(new { Error = $"No se encontró el incidente con ID {id}" });
                return Ok(detalle);
            }
            catch (Exception)
            {
                return StatusCode(500, new { Error = "Error al obtener el detalle del incidente." });
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
            catch (Exception)
            {
                return StatusCode(500, new { Error = "Error al obtener el historial del incidente." });
            }
        }

        [HttpPost("{id}/confirmar-aviso")]
        public async Task<IActionResult> ConfirmarAviso(long id, [FromBody] ConfirmarAvisoSolicitud request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _incidenteService.ConfirmarAvisoAsync(id, request);
                return Ok(new { Mensaje = "Aviso confirmado.", IdIncidente = id });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { Error = "Error interno." }); }
        }

        [HttpPost("{id}/informe")]
        public async Task<IActionResult> RegistrarInforme(long id, [FromBody] RegistrarInformeSolicitud request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _incidenteService.RegistrarInformeAsync(id, request);
                return Ok(new { Mensaje = "Informe registrado.", IdIncidente = id });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { Error = "Error interno." }); }
        }

        [HttpPost("{id}/cerrar")]
        public async Task<IActionResult> CerrarIncidente(long id, [FromBody] CerrarIncidenteSolicitud request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _incidenteService.CerrarIncidenteAsync(id, request);
                return Ok(new { Mensaje = "Incidente cerrado.", IdIncidente = id });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { Error = "Error interno." }); }
        }
    }
}