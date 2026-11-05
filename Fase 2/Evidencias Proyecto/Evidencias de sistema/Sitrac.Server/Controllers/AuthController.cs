using Microsoft.AspNetCore.Mvc;
using Sitrac.Service.Dtos;
using Sitrac.Service.Interfaces;
using System.Threading.Tasks;

namespace Sitrac.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _authService.LoginAsync(request);
            if (response == null) return Unauthorized(new { Error = "Credenciales inválidas." });

            return Ok(response);
        }

        // Endpoint temporal para registrar el primer admin (Protegido en prod)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _authService.RegistrarUsuarioAsync(request.Username, request.Password, request.NombreCompleto, "Admin");
            return Ok(new { Mensaje = "Usuario registrado exitosamente." });
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
    }
}