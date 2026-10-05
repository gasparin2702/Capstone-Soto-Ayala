using Dapper;
using Sitrac.DataAccess;
using Sitrac.Repository.Interfaces;
using Sitrac.Repository.Models;
using System.Threading.Tasks;

namespace Sitrac.Repository
{
    public class SlaRepository : ISlaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SlaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<AnciTipoPlazoSla?> ObtenerParametrosPorEmpresaAsync(long codTipoClasificacionEmpresa)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT IdTipoPlazoSla, CodTipoClasificacionEmpresa, HorasAvisoTemprano, HorasInformePreliminar, DiasInformeFinal
                FROM TbAnciTipoPlazoSla
                WHERE CodTipoClasificacionEmpresa = @CodEmpresa AND AuditNotDeleted = 1";
            
            return await connection.QueryFirstOrDefaultAsync<AnciTipoPlazoSla>(sql, new { CodEmpresa = codTipoClasificacionEmpresa });
        }

        public async Task InsertarPlazosAsync(AnciIncidentePlazoSla plazos)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbAnciIncidentePlazoSla (
                    IdIncidente, IdTipoPlazoSla, FechaTomaConocimiento, 
                    FechaLimiteAvisoTemprano, FechaLimiteInformePreliminar, FechaLimiteInformeFinal
                ) VALUES (
                    @IdIncidente, @IdTipoPlazoSla, @FechaTomaConocimiento, 
                    @FechaLimiteAvisoTemprano, @FechaLimiteInformePreliminar, @FechaLimiteInformeFinal
                )";
            
            await connection.ExecuteAsync(sql, plazos);
        }
    }
}