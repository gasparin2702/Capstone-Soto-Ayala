using Microsoft.AspNetCore.Mvc;
using Sitrac.Servicio.Dtos;
using Sitrac.Servicio.Interfaces;
using System.Threading.Tasks;

namespace Sitrac.Servidor.Controllers
{
    [ApiController]
    [Route("api/Auth")]
    public class AutenticacionControlador : ControllerBase
    {
        private readonly IAutenticacionServicio _authService;

        public AutenticacionControlador(IAutenticacionServicio authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginSolicitud request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _authService.LoginAsync(request);
            if (response == null) return Unauthorized(new { Error = "Credenciales inválidas." });

            return Ok(response);
        }

        // Endpoint temporal para registrar el primer admin (Protegido en prod)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterSolicitud request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _authService.RegistrarUsuarioAsync(request.Username, request.Password, request.NombreCompleto, "Admin");
            return Ok(new { Mensaje = "Usuario registrado exitosamente." });
        }
    }

    public class RegisterSolicitud
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
    }
}