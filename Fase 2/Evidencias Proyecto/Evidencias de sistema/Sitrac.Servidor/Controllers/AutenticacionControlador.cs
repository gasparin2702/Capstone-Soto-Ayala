using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sitrac.Servicio.Configuration;
using Sitrac.Servicio.Dtos;
using Sitrac.Servicio.Interfaces;
using Sitrac.Servicio.Seguridad;
using System.Threading.Tasks;

namespace Sitrac.Servidor.Controllers
{
    [ApiController]
    [Route("api/Auth")]
    public class AutenticacionControlador : ControllerBase
    {
        private readonly IAutenticacionServicio _authService;
        private readonly ISeguridadServicio _seguridadServicio;
        private readonly ConfiguracionSeguridad _configuracionSeguridad;

        public AutenticacionControlador(
            IAutenticacionServicio authService,
            ISeguridadServicio seguridadServicio,
            IOptions<ConfiguracionSeguridad> configuracionSeguridad)
        {
            _authService = authService;
            _seguridadServicio = seguridadServicio;
            _configuracionSeguridad = configuracionSeguridad.Value;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginSolicitud request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _authService.LoginAsync(request);
            if (response == null) return Unauthorized(new { Error = "Credenciales inválidas." });

            return Ok(response);
        }

        [Authorize(Policy = TransaccionesCatalogo.RegistrarUsuario)]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterSolicitud request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var nombreRol = string.IsNullOrWhiteSpace(request.NombreRol) ? null : request.NombreRol.Trim();
            if (nombreRol != null && !await _seguridadServicio.ExisteRolActivoAsync(nombreRol))
            {
                return BadRequest(new { Error = $"El rol '{nombreRol}' no existe o no está activo." });
            }

            var idUsuario = await _authService.RegistrarUsuarioAsync(request.Username, request.Password, request.NombreCompleto, "Admin");
            if (idUsuario == null) return StatusCode(500, new { Error = "No se pudo registrar el usuario." });

            if (nombreRol != null)
            {
                await _seguridadServicio.AsignarRolAUsuarioAsync(idUsuario.Value, nombreRol);
            }

            return Ok(new { Mensaje = "Usuario registrado exitosamente.", RolAsignado = nombreRol });
        }

        /// <summary>
        /// Crea el administrador inicial del sistema. Es de un solo uso: si ya existe un usuario
        /// asignado al rol Administrador SITRAC, responde 409 Conflict.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("bootstrap")]
        public async Task<IActionResult> Bootstrap([FromBody] RegisterSolicitud request)
        {
            if (!_configuracionSeguridad.HabilitarBootstrap)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Mensaje = "El bootstrap está deshabilitado por configuración." });
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (await _seguridadServicio.ExisteAdministradorInicialAsync())
                return Conflict(new { Mensaje = "Bootstrap ya fue ejecutado. El administrador inicial ya existe." });

            var idUsuario = await _authService.RegistrarUsuarioAsync(request.Username, request.Password, request.NombreCompleto, "Admin");
            if (idUsuario == null) return StatusCode(500, new { Error = "No se pudo crear el administrador inicial." });

            await _seguridadServicio.AsignarAdministradorAsync(idUsuario.Value);
            return Ok(new { mensaje = "Administrador inicial creado." });
        }
    }

    public class RegisterSolicitud
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string? NombreRol { get; set; }
    }
}
