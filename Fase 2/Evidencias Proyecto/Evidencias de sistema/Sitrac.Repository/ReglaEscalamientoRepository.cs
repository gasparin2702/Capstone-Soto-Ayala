using Dapper;
using Sitrac.DataAccess;
using Sitrac.Repository.Interfaces;
using Sitrac.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Repository
{
    public class ReglaEscalamientoRepository : IReglaEscalamientoRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ReglaEscalamientoRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<ReglaEscalamiento>> ObtenerReglasActivasAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT IdRegla, CodTipoHito, TiempoRestanteMinutos, RepetirCadaMinutos,
                       NotificarEmail, NotificarTeams, Descripcion, OrdenVisual
                FROM TbSitracReglaEscalamiento
                WHERE AuditNotDeleted = 1
                ORDER BY CodTipoHito ASC, TiempoRestanteMinutos DESC";

            return await connection.QueryAsync<ReglaEscalamiento>(sql);
        }

        public async Task<IEnumerable<ReglaEscalamiento>> ObtenerPorTipoHitoAsync(long codTipoHito)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT IdRegla, CodTipoHito, TiempoRestanteMinutos, RepetirCadaMinutos,
                       NotificarEmail, NotificarTeams, Descripcion, OrdenVisual
                FROM TbSitracReglaEscalamiento
                WHERE CodTipoHito = @CodTipoHito AND AuditNotDeleted = 1
                ORDER BY TiempoRestanteMinutos DESC";

            return await connection.QueryAsync<ReglaEscalamiento>(sql, new { CodTipoHito = codTipoHito });
        }
    }
}
