using System.Threading.Tasks;
using Sitrac.Repositorio.Models;

namespace Sitrac.Repositorio.Interfaces
{
    public interface IUsuarioRepositorio
    {
        Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario);
        Task<long> InsertarAsync(Usuario usuario);
    }
}