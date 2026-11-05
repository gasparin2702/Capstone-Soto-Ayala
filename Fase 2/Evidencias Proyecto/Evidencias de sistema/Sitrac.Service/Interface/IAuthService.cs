using System.Threading.Tasks;
using Sitrac.Service.Dtos;

namespace Sitrac.Service.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<bool> RegistrarUsuarioAsync(string username, string password, string nombreCompleto, string rol);
    }
}