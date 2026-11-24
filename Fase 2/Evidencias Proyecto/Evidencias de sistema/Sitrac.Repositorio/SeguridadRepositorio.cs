using Dapper;
using Sitrac.AccesoDatos;
using Sitrac.Repositorio.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Repositorio
{
    public class SeguridadRepositorio : ISeguridadRepositorio
    {
        private readonly IFabricaConexionBd _connectionFactory;

        public SeguridadRepositorio(IFabricaConexionBd connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<string>> ObtenerTransaccionesPorUsuarioAsync(long idUsuario)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT t.CodigoTransaccion
                FROM TbSitracUsuarioRol ur
                INNER JOIN TbSitracRolPerfil rp ON rp.IdRol = ur.IdRol
                INNER JOIN TbSitracPerfilTransaccion pt ON pt.IdPerfil = rp.IdPerfil
                INNER JOIN TbSitracTransaccion t ON t.IdTransaccion = pt.IdTransaccion
                INNER JOIN TbSitracPerfil p ON p.IdPerfil = rp.IdPerfil
                WHERE ur.IdUsuario = @IdUsuario
                  AND ur.AuditNotDeleted = 1
                  AND rp.AuditNotDeleted = 1
                  AND pt.AuditNotDeleted = 1
                  AND p.AuditNotDeleted = 1
                  AND t.AuditNotDeleted = 1
                ORDER BY t.CodigoTransaccion";

            return await connection.QueryAsync<string>(sql, new { IdUsuario = idUsuario });
        }

        public async Task<IEnumerable<string>> ObtenerPerfilesPorUsuarioAsync(long idUsuario)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT p.CodigoPerfil
                FROM TbSitracUsuarioRol ur
                INNER JOIN TbSitracRolPerfil rp ON rp.IdRol = ur.IdRol
                INNER JOIN TbSitracPerfil p ON p.IdPerfil = rp.IdPerfil
                WHERE ur.IdUsuario = @IdUsuario
                  AND ur.AuditNotDeleted = 1
                  AND rp.AuditNotDeleted = 1
                  AND p.AuditNotDeleted = 1
                ORDER BY p.CodigoPerfil";

            return await connection.QueryAsync<string>(sql, new { IdUsuario = idUsuario });
        }

        public async Task<IEnumerable<string>> ObtenerRolesPorUsuarioAsync(long idUsuario)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT r.NombreRol
                FROM TbSitracUsuarioRol ur
                INNER JOIN TbSitracRol r ON r.IdRol = ur.IdRol
                WHERE ur.IdUsuario = @IdUsuario
                  AND ur.AuditNotDeleted = 1
                  AND r.AuditNotDeleted = 1
                ORDER BY r.NombreRol";

            return await connection.QueryAsync<string>(sql, new { IdUsuario = idUsuario });
        }

        public async Task<bool> UsuarioTieneTransaccionAsync(long idUsuario, string codigoTransaccion)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT COUNT(1)
                FROM TbSitracUsuarioRol ur
                INNER JOIN TbSitracRolPerfil rp ON rp.IdRol = ur.IdRol
                INNER JOIN TbSitracPerfilTransaccion pt ON pt.IdPerfil = rp.IdPerfil
                INNER JOIN TbSitracTransaccion t ON t.IdTransaccion = pt.IdTransaccion
                INNER JOIN TbSitracPerfil p ON p.IdPerfil = rp.IdPerfil
                WHERE ur.IdUsuario = @IdUsuario
                  AND t.CodigoTransaccion = @CodigoTransaccion
                  AND ur.AuditNotDeleted = 1
                  AND rp.AuditNotDeleted = 1
                  AND pt.AuditNotDeleted = 1
                  AND p.AuditNotDeleted = 1
                  AND t.AuditNotDeleted = 1";

            var total = await connection.ExecuteScalarAsync<int>(sql, new
            {
                IdUsuario = idUsuario,
                CodigoTransaccion = codigoTransaccion
            });

            return total > 0;
        }

        public async Task<bool> ExisteUsuarioConRolAsync(string nombreRol)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM TbSitracUsuarioRol ur
                    INNER JOIN TbSitracRol r ON r.IdRol = ur.IdRol
                    WHERE r.NombreRol = @NombreRol
                      AND ur.AuditNotDeleted = 1
                      AND r.AuditNotDeleted = 1
                ) THEN 1 ELSE 0 END";

            return await connection.ExecuteScalarAsync<bool>(sql, new { NombreRol = nombreRol });
        }

        public async Task<bool> ExisteRolActivoAsync(string nombreRol)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM TbSitracRol
                    WHERE NombreRol = @NombreRol
                      AND AuditNotDeleted = 1
                ) THEN 1 ELSE 0 END";

            return await connection.ExecuteScalarAsync<bool>(sql, new { NombreRol = nombreRol });
        }

        public async Task AsignarRolAUsuarioAsync(long idUsuario, string nombreRol)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbSitracUsuarioRol (IdUsuario, IdRol)
                SELECT @IdUsuario, r.IdRol
                FROM TbSitracRol r
                WHERE r.NombreRol = @NombreRol
                  AND r.AuditNotDeleted = 1
                  AND NOT EXISTS (
                      SELECT 1
                      FROM TbSitracUsuarioRol ur
                      WHERE ur.IdUsuario = @IdUsuario
                        AND ur.IdRol = r.IdRol
                  )";

            await connection.ExecuteAsync(sql, new { IdUsuario = idUsuario, NombreRol = nombreRol });
        }
    }
}
