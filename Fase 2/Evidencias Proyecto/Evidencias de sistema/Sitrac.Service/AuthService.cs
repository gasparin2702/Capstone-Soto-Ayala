using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sitrac.Repository.Interfaces;
using Sitrac.Repository.Models;
using Sitrac.Service.Configuration;
using Sitrac.Service.Dtos;
using Sitrac.Service.Interfaces;

namespace Sitrac.Service
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IUsuarioRepository usuarioRepository, IOptions<JwtSettings> jwtSettings)
        {
            _usuarioRepository = usuarioRepository;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _usuarioRepository.ObtenerPorNombreUsuarioAsync(request.Username);
            if (user == null) return null;

            if (!VerifyPassword(request.Password, user.PasswordHash))
                return null;

            var token = GenerateJwtToken(user);
            return new LoginResponse
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