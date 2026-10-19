using Dapper;
using Sitrac.DataAccess;
using Sitrac.Repository.Interfaces;
using Sitrac.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Repository
{
    public class IncidenteRepository : IIncidenteRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public IncidenteRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<SitracIncidente?> ObtenerPorIdAsync(long idIncidente)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = @"
                SELECT 
                    IdIncidente, FechaTomaConocimiento, FechaRegistroSistema, 
                    CodTipoCanalEntrada, CodigoTicketInvgate, IndEsIncidenteReal, 
                    CodTipoDatosPersonales, CodTipoClasificacionEmpresa, 
                    CodTipoGravedad, CodTipoEstadoIncidente, DescripcionSistemaAfectado
                FROM TbSitracIncidente 
                WHERE IdIncidente = @Id AND AuditNotDeleted = 1";

            return await connection.QueryFirstOrDefaultAsync<SitracIncidente>(sql, new { Id = idIncidente });
        }

        public async Task<long> InsertarAsync(SitracIncidente incidente)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbSitracIncidente (
                    FechaTomaConocimiento, CodTipoCanalEntrada, CodigoTicketInvgate, 
                    IndEsIncidenteReal, CodTipoDatosPersonales, CodTipoClasificacionEmpresa, 
                    CodTipoGravedad, CodTipoEstadoIncidente, DescripcionSistemaAfectado
                ) VALUES (
                    @FechaTomaConocimiento, @CodTipoCanalEntrada, @CodigoTicketInvgate, 
                    @IndEsIncidenteReal, @CodTipoDatosPersonales, @CodTipoClasificacionEmpresa, 
                    @CodTipoGravedad, @CodTipoEstadoIncidente, @DescripcionSistemaAfectado
                );
                SELECT CAST(SCOPE_IDENTITY() as bigint);";

            return await connection.QuerySingleAsync<long>(sql, incidente);
        }

        public async Task<IEnumerable<IncidenteSlaInfo>> ObtenerIncidentesParaEvaluacionSlaAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    i.IdIncidente, 
                    i.CodigoTicketInvgate, 
                    i.CodTipoEstadoIncidente,
                    i.CodTipoGravedad,
                    i.CodTipoClasificacionEmpresa,
                    s.FechaLimiteAvisoTemprano, 
                    s.FechaLimiteInformePreliminar, 
                    s.FechaLimiteInformeFinal
                FROM TbSitracIncidente i
                INNER JOIN TbSitracIncidentePlazoSla s ON i.IdIncidente = s.IdIncidente
                WHERE i.CodTipoEstadoIncidente < 4 AND i.AuditNotDeleted = 1";

            return await connection.QueryAsync<IncidenteSlaInfo>(sql);
        }

        public async Task AvanzarEstadoConHistorialAsync(long idIncidente, long nuevoEstado, SitracEventoHistorial historial)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string sqlUpdate = @"
                    UPDATE TbSitracIncidente 
                    SET CodTipoEstadoIncidente = @NuevoEstado, AuditLastUpdateDate = GETDATE()
                    WHERE IdIncidente = @IdIncidente";
                await connection.ExecuteAsync(sqlUpdate, new { NuevoEstado = nuevoEstado, IdIncidente = idIncidente }, transaction);

                const string sqlInsert = @"
                    INSERT INTO TbSitracEventoHistorial (
                        IdIncidente, CodigoTicketInvgate, IdTipoAmbitoCambio,
                        IdTipoEstadoIncidenteAnterior, IdTipoEstadoIncidenteNuevo,
                        IdResponsableCambia, Comentario
                    ) VALUES (
                        @IdIncidente, @CodigoTicketInvgate, @IdTipoAmbitoCambio,
                        @IdTipoEstadoIncidenteAnterior, @IdTipoEstadoIncidenteNuevo,
                        @IdResponsableCambia, @Comentario
                    )";
                await connection.ExecuteAsync(sqlInsert, historial, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ===== FIX Bug A: agregado i.CodTipoEstadoIncidente al SELECT =====
        public async Task<IEnumerable<IncidenteResumen>> ObtenerActivosResumenAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    i.IdIncidente,
                    i.CodigoTicketInvgate,
                    i.CodTipoEstadoIncidente,
                    c.NombreTipoCanalEntrada AS Origen,
                    e.NombreTipoClasificacionEmpresa AS Clasificacion,
                    g.OrdenTipoGravedad AS Gravedad,
                    est.NombreTipoEstadoIncidente AS Estado,
                    i.FechaTomaConocimiento,
                    sla.FechaLimiteAvisoTemprano,
                    sla.FechaLimiteInformePreliminar,
                    sla.FechaLimiteInformeFinal
                FROM TbSitracIncidente i
                INNER JOIN TbSitracTipoCanalEntrada c ON i.CodTipoCanalEntrada = c.IdTipoCanalEntrada
                INNER JOIN TbSitracTipoClasificacionEmpresa e ON i.CodTipoClasificacionEmpresa = e.IdTipoClasificacionEmpresa
                INNER JOIN TbSitracTipoGravedad g ON i.CodTipoGravedad = g.IdTipoGravedad
                INNER JOIN TbSitracTipoEstadoIncidente est ON i.CodTipoEstadoIncidente = est.IdTipoEstadoIncidente
                LEFT JOIN TbSitracIncidentePlazoSla sla ON i.IdIncidente = sla.IdIncidente
                WHERE i.CodTipoEstadoIncidente < 4 AND i.AuditNotDeleted = 1";

            return await connection.QueryAsync<IncidenteResumen>(sql);
        }

        public async Task<IncidenteDetalle?> ObtenerDetallePorIdAsync(long idIncidente)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    i.IdIncidente,
                    i.CodigoTicketInvgate,
                    c.NombreTipoCanalEntrada AS Origen,
                    e.NombreTipoClasificacionEmpresa AS Clasificacion,
                    g.OrdenTipoGravedad AS Gravedad,
                    est.NombreTipoEstadoIncidente AS Estado,
                    i.DescripcionSistemaAfectado AS SistemaAfectado,
                    i.DescripcionVectorAtaque AS VectorAtaque,
                    i.DescripcionOrigenDeteccion AS OrigenDeteccion,
                    i.FechaTomaConocimiento,
                    sla.FechaLimiteAvisoTemprano,
                    sla.FechaLimiteInformePreliminar,
                    sla.FechaLimiteInformeFinal
                FROM TbSitracIncidente i
                INNER JOIN TbSitracTipoCanalEntrada c ON i.CodTipoCanalEntrada = c.IdTipoCanalEntrada
                INNER JOIN TbSitracTipoClasificacionEmpresa e ON i.CodTipoClasificacionEmpresa = e.IdTipoClasificacionEmpresa
                INNER JOIN TbSitracTipoGravedad g ON i.CodTipoGravedad = g.IdTipoGravedad
                INNER JOIN TbSitracTipoEstadoIncidente est ON i.CodTipoEstadoIncidente = est.IdTipoEstadoIncidente
                LEFT JOIN TbSitracIncidentePlazoSla sla ON i.IdIncidente = sla.IdIncidente
                WHERE i.IdIncidente = @IdIncidente AND i.AuditNotDeleted = 1";

            return await connection.QueryFirstOrDefaultAsync<IncidenteDetalle>(sql, new { IdIncidente = idIncidente });
        }

        // ===== Bug B revertido: usa IIF(r.IdResponsable = 1, 1, 0) porque IndEsAutomatico no existe =====
        public async Task<IEnumerable<EventoHistorialResumen>> ObtenerHistorialAsync(long idIncidente)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    h.IdEventoHistorial,
                    amb.NombreAmbitoCambio AS AmbitoCambio,
                    estAnt.NombreTipoEstadoIncidente AS EstadoAnterior,
                    estNvo.NombreTipoEstadoIncidente AS EstadoNuevo,
                    gAnt.OrdenTipoGravedad AS GravedadAnterior,
                    gNvo.OrdenTipoGravedad AS GravedadNueva,
                    r.NombreResponsable AS Responsable,
                    CAST(IIF(r.IdResponsable = 1, 1, 0) AS BIT) AS EsAccionAutomatica,
                    h.FechaCambio,
                    h.Comentario
                FROM TbSitracEventoHistorial h
                INNER JOIN TbSitracTipoAmbitoCambio amb ON h.IdTipoAmbitoCambio = amb.IdTipoAmbitoCambio
                INNER JOIN TbSitracResponsable r ON h.IdResponsableCambia = r.IdResponsable
                LEFT JOIN TbSitracTipoEstadoIncidente estAnt ON h.IdTipoEstadoIncidenteAnterior = estAnt.IdTipoEstadoIncidente
                LEFT JOIN TbSitracTipoEstadoIncidente estNvo ON h.IdTipoEstadoIncidenteNuevo = estNvo.IdTipoEstadoIncidente
                LEFT JOIN TbSitracTipoGravedad gAnt ON h.IdTipoGravedadAnterior = gAnt.IdTipoGravedad
                LEFT JOIN TbSitracTipoGravedad gNvo ON h.IdTipoGravedadNueva = gNvo.IdTipoGravedad
                WHERE h.IdIncidente = @IdIncidente
                ORDER BY h.FechaCambio DESC";

            return await connection.QueryAsync<EventoHistorialResumen>(sql, new { IdIncidente = idIncidente });
        }
    }
}
