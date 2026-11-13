using Dapper;
using Sitrac.AccesoDatos;
using Sitrac.Repositorio.Interfaces;
using Sitrac.Repositorio.Models;
using System.Threading.Tasks;

namespace Sitrac.Repositorio
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly IFabricaConexionBd _connectionFactory;

        public UsuarioRepositorio(IFabricaConexionBd connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT IdUsuario, NombreUsuario, PasswordHash, NombreCompleto, Rol
                FROM TbSitracUsuario
                WHERE NombreUsuario = @NombreUsuario AND AuditNotDeleted = 1";

            return await connection.QueryFirstOrDefaultAsync<Usuario>(sql, new { NombreUsuario = nombreUsuario });
        }

        public async Task<long> InsertarAsync(Usuario usuario)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbSitracUsuario (NombreUsuario, PasswordHash, NombreCompleto, Rol)
                VALUES (@NombreUsuario, @PasswordHash, @NombreCompleto, @Rol);
                SELECT CAST(SCOPE_IDENTITY() as bigint);";

            return await connection.QuerySingleAsync<long>(sql, usuario);
        }
    }
}