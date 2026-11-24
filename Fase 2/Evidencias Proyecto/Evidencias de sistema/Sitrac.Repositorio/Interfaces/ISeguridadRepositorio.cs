using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Repositorio.Interfaces
{
    public interface ISeguridadRepositorio
    {
        Task<IEnumerable<string>> ObtenerTransaccionesPorUsuarioAsync(long idUsuario);
        Task<IEnumerable<string>> ObtenerPerfilesPorUsuarioAsync(long idUsuario);
        Task<IEnumerable<string>> ObtenerRolesPorUsuarioAsync(long idUsuario);
        Task<bool> UsuarioTieneTransaccionAsync(long idUsuario, string codigoTransaccion);
        Task<bool> ExisteUsuarioConRolAsync(string nombreRol);
        Task<bool> ExisteRolActivoAsync(string nombreRol);
        Task AsignarRolAUsuarioAsync(long idUsuario, string nombreRol);
    }
}
