using Dapper;
using Sitrac.AccesoDatos;
using Sitrac.Repositorio.Interfaces;
using Sitrac.Repositorio.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Repositorio
{
    public class ReglaEscalamientoRepositorio : IReglaEscalamientoRepositorio
    {
        private readonly IFabricaConexionBd _connectionFactory;

        public ReglaEscalamientoRepositorio(IFabricaConexionBd connectionFactory)
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
