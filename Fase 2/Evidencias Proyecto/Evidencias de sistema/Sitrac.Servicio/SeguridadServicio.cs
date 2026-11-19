using Sitrac.Repositorio.Interfaces;
using Sitrac.Servicio.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Servicio
{
    public class SeguridadServicio : ISeguridadServicio
    {
        private readonly ISeguridadRepositorio _seguridadRepositorio;

        public SeguridadServicio(ISeguridadRepositorio seguridadRepositorio)
        {
            _seguridadRepositorio = seguridadRepositorio;
        }

        public Task<bool> PuedeEjecutarAsync(long idUsuario, string codigoTransaccion)
        {
            return _seguridadRepositorio.UsuarioTieneTransaccionAsync(idUsuario, codigoTransaccion);
        }

        public Task<IEnumerable<string>> ObtenerTransaccionesAsync(long idUsuario)
        {
            return _seguridadRepositorio.ObtenerTransaccionesPorUsuarioAsync(idUsuario);
        }

        public Task<IEnumerable<string>> ObtenerRolesAsync(long idUsuario)
        {
            return _seguridadRepositorio.ObtenerRolesPorUsuarioAsync(idUsuario);
        }
    }
}
