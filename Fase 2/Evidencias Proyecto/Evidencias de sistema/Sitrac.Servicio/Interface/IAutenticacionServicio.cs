using System.Threading.Tasks;
using Sitrac.Servicio.Dtos;

namespace Sitrac.Servicio.Interfaces
{
    public interface IAutenticacionServicio
    {
        Task<LoginRespuesta?> LoginAsync(LoginSolicitud request);
        Task<long?> RegistrarUsuarioAsync(string username, string password, string nombreCompleto, string rol);
    }
}