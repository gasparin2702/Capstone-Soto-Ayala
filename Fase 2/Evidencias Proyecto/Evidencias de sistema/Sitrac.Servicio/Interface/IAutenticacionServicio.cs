using System.Threading.Tasks;
using Sitrac.Servicio.Dtos;

namespace Sitrac.Servicio.Interfaces
{
    public interface IAutenticacionServicio
    {
        Task<LoginRespuesta?> LoginAsync(LoginSolicitud request);
        Task<bool> RegistrarUsuarioAsync(string username, string password, string nombreCompleto, string rol);
    }
}