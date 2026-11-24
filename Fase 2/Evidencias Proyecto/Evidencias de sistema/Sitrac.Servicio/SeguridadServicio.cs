using Sitrac.Repositorio.Interfaces;
using Sitrac.Servicio.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Servicio
{
    public class SeguridadServicio : ISeguridadServicio
    {
        private const string RolAdministrador = "Administrador SITRAC";

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

        public Task<bool> ExisteAdministradorInicialAsync()
        {
            return _seguridadRepositorio.ExisteUsuarioConRolAsync(RolAdministrador);
        }

        public Task<bool> ExisteRolActivoAsync(string nombreRol)
        {
            return _seguridadRepositorio.ExisteRolActivoAsync(nombreRol);
        }

        public Task AsignarRolAUsuarioAsync(long idUsuario, string nombreRol)
        {
            return _seguridadRepositorio.AsignarRolAUsuarioAsync(idUsuario, nombreRol);
        }

        public Task AsignarAdministradorAsync(long idUsuario)
        {
            return _seguridadRepositorio.AsignarRolAUsuarioAsync(idUsuario, RolAdministrador);
        }
    }
}
