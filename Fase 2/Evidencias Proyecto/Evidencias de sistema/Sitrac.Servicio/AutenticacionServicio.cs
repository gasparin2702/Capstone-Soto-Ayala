using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sitrac.Repositorio.Interfaces;
using Sitrac.Repositorio.Models;
using Sitrac.Servicio.Configuration;
using Sitrac.Servicio.Dtos;
using Sitrac.Servicio.Interfaces;

namespace Sitrac.Servicio
{
    public class AutenticacionServicio : IAutenticacionServicio
    {
        private readonly IUsuarioRepositorio _usuarioRepository;
        private readonly ConfiguracionJwt _jwtSettings;

        public AutenticacionServicio(IUsuarioRepositorio usuarioRepository, IOptions<ConfiguracionJwt> jwtSettings)
        {
            _usuarioRepository = usuarioRepository;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<LoginRespuesta?> LoginAsync(LoginSolicitud request)
        {
            var user = await _usuarioRepository.ObtenerPorNombreUsuarioAsync(request.Username);
            if (user == null) return null;

            if (!VerifyPassword(request.Password, user.PasswordHash))
                return null;

            var token = GenerateJwtToken(user);
            return new LoginRespuesta
            {
                Token = token,
                NombreCompleto = user.NombreCompleto,
                Rol = user.Rol
            };
        }

        public async Task<bool> RegistrarUsuarioAsync(string username, string password, string nombreCompleto, string rol)
        {
            var hash = HashPassword(password);
            var user = new Usuario
            {
                NombreUsuario = username,
                PasswordHash = hash,
                NombreCompleto = nombreCompleto,
                Rol = rol
            };
            await _usuarioRepository.InsertarAsync(user);
            return true;
        }

        private string GenerateJwtToken(Usuario user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString()),
                    new Claim(ClaimTypes.Name, user.NombreUsuario),
                    new Claim(ClaimTypes.Role, user.Rol),
                    new Claim("NombreCompleto", user.NombreCompleto)
                }),
                Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // ACTUALIZADO: Uso de la API estática Pbkdf2 de .NET 10
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
            
            byte[] hashBytes = new byte[48];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);
            return Convert.ToBase64String(hashBytes);
        }

        private static bool VerifyPassword(string password, string hashedPassword)
        {
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
            
            for (int i = 0; i < 32; i++)
                if (hashBytes[i + 16] != hash[i]) return false;
            return true;
        }
    }
}