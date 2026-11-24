using Dapper;
using Sitrac.AccesoDatos;
using Sitrac.Repositorio.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sitrac.Repositorio
{
    public class ResponsableRepositorio : IResponsableRepositorio
    {
        private readonly IFabricaConexionBd _connectionFactory;

        public ResponsableRepositorio(IFabricaConexionBd connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IReadOnlyList<string>> ObtenerCorreosDelegadosActivosAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT LTRIM(RTRIM(EmailResponsable)) AS EmailResponsable
                FROM TbSitracResponsable
                WHERE AuditNotDeleted = 1
                  AND IndEsDelegadoCiberseguridad = 1
                  AND LTRIM(RTRIM(EmailResponsable)) <> ''";

            var correos = await connection.QueryAsync<string>(sql);
            return FiltrarCorreos(correos);
        }

        public async Task<IReadOnlyList<string>> ObtenerTodosCorreosActivosAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT LTRIM(RTRIM(EmailResponsable)) AS EmailResponsable
                FROM TbSitracResponsable
                WHERE AuditNotDeleted = 1
                  AND LTRIM(RTRIM(EmailResponsable)) <> ''";

            var correos = await connection.QueryAsync<string>(sql);
            return FiltrarCorreos(correos);
        }

        private static IReadOnlyList<string> FiltrarCorreos(IEnumerable<string> correos)
        {
            return correos
                .Where(correo => !string.IsNullOrWhiteSpace(correo))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
