using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Servicio.Interfaces
{
    public interface ISeguridadServicio
    {
        Task<bool> PuedeEjecutarAsync(long idUsuario, string codigoTransaccion);
        Task<IEnumerable<string>> ObtenerTransaccionesAsync(long idUsuario);
        Task<IEnumerable<string>> ObtenerRolesAsync(long idUsuario);
        Task<bool> ExisteAdministradorInicialAsync();
        Task<bool> ExisteRolActivoAsync(string nombreRol);
        Task AsignarRolAUsuarioAsync(long idUsuario, string nombreRol);
        Task AsignarAdministradorAsync(long idUsuario);
    }
}
