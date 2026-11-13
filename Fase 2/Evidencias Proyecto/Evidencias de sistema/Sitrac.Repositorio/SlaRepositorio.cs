using Dapper;
using Sitrac.AccesoDatos;
using Sitrac.Repositorio.Interfaces;
using Sitrac.Repositorio.Models;
using System.Threading.Tasks;

namespace Sitrac.Repositorio
{
    public class SlaRepositorio : ISlaRepositorio
    {
        private readonly IFabricaConexionBd _connectionFactory;

        public SlaRepositorio(IFabricaConexionBd connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<SitracTipoPlazoSla?> ObtenerParametrosPorEmpresaAsync(long codTipoClasificacionEmpresa)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT IdTipoPlazoSla, CodTipoClasificacionEmpresa, HorasAvisoTemprano, HorasInformePreliminar, DiasInformeFinal
                FROM TbSitracTipoPlazoSla
                WHERE CodTipoClasificacionEmpresa = @CodEmpresa AND AuditNotDeleted = 1";
            
            return await connection.QueryFirstOrDefaultAsync<SitracTipoPlazoSla>(sql, new { CodEmpresa = codTipoClasificacionEmpresa });
        }

        public async Task InsertarPlazosAsync(SitracIncidentePlazoSla plazos)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbSitracIncidentePlazoSla (
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
