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

        public async Task<AnciIncidente?> ObtenerPorIdAsync(long idIncidente)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            const string sql = @"
                SELECT 
                    IdIncidente, FechaTomaConocimiento, FechaRegistroSistema, 
                    CodTipoCanalEntrada, CodigoTicketInvgate, IndEsIncidenteReal, 
                    CodTipoDatosPersonales, CodTipoClasificacionEmpresa, 
                    CodTipoGravedad, CodTipoEstadoIncidente, DescripcionSistemaAfectado
                FROM TbAnciIncidente 
                WHERE IdIncidente = @Id AND AuditNotDeleted = 1";

            return await connection.QueryFirstOrDefaultAsync<AnciIncidente>(sql, new { Id = idIncidente });
        }

        public async Task<long> InsertarAsync(AnciIncidente incidente)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbAnciIncidente (
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
                SELECT i.IdIncidente, i.CodigoTicketInvgate, i.CodTipoEstadoIncidente, 
                       s.FechaLimiteAvisoTemprano, s.FechaLimiteInformePreliminar, s.FechaLimiteInformeFinal
                FROM TbAnciIncidente i
                INNER JOIN TbAnciIncidentePlazoSla s ON i.IdIncidente = s.IdIncidente
                WHERE i.CodTipoEstadoIncidente < 4 AND i.AuditNotDeleted = 1";
            
            return await connection.QueryAsync<IncidenteSlaInfo>(sql);
        }

        public async Task AvanzarEstadoConHistorialAsync(long idIncidente, long nuevoEstado, AnciEventoHistorial historial)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                const string sqlUpdate = @"
                    UPDATE TbAnciIncidente 
                    SET CodTipoEstadoIncidente = @NuevoEstado, AuditLastUpdateDate = GETDATE()
                    WHERE IdIncidente = @IdIncidente";
                await connection.ExecuteAsync(sqlUpdate, new { NuevoEstado = nuevoEstado, IdIncidente = idIncidente }, transaction);

                const string sqlInsert = @"
                    INSERT INTO TbAnciEventoHistorial (
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

        public async Task<IEnumerable<IncidenteResumen>> ObtenerActivosResumenAsync()
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
                    i.FechaTomaConocimiento,
                    sla.FechaLimiteAvisoTemprano,
                    sla.FechaLimiteInformePreliminar,
                    sla.FechaLimiteInformeFinal
                FROM TbAnciIncidente i
                INNER JOIN TbAnciTipoCanalEntrada c ON i.CodTipoCanalEntrada = c.IdTipoCanalEntrada
                INNER JOIN TbAnciTipoClasificacionEmpresa e ON i.CodTipoClasificacionEmpresa = e.IdTipoClasificacionEmpresa
                INNER JOIN TbAnciTipoGravedad g ON i.CodTipoGravedad = g.IdTipoGravedad
                INNER JOIN TbAnciTipoEstadoIncidente est ON i.CodTipoEstadoIncidente = est.IdTipoEstadoIncidente
                LEFT JOIN TbAnciIncidentePlazoSla sla ON i.IdIncidente = sla.IdIncidente
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
                FROM TbAnciIncidente i
                INNER JOIN TbAnciTipoCanalEntrada c ON i.CodTipoCanalEntrada = c.IdTipoCanalEntrada
                INNER JOIN TbAnciTipoClasificacionEmpresa e ON i.CodTipoClasificacionEmpresa = e.IdTipoClasificacionEmpresa
                INNER JOIN TbAnciTipoGravedad g ON i.CodTipoGravedad = g.IdTipoGravedad
                INNER JOIN TbAnciTipoEstadoIncidente est ON i.CodTipoEstadoIncidente = est.IdTipoEstadoIncidente
                LEFT JOIN TbAnciIncidentePlazoSla sla ON i.IdIncidente = sla.IdIncidente
                WHERE i.IdIncidente = @IdIncidente AND i.AuditNotDeleted = 1";
            
            return await connection.QueryFirstOrDefaultAsync<IncidenteDetalle>(sql, new { IdIncidente = idIncidente });
        }

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
                FROM TbAnciEventoHistorial h
                INNER JOIN TbAnciTipoAmbitoCambio amb ON h.IdTipoAmbitoCambio = amb.IdTipoAmbitoCambio
                INNER JOIN TbAnciResponsable r ON h.IdResponsableCambia = r.IdResponsable
                LEFT JOIN TbAnciTipoEstadoIncidente estAnt ON h.IdTipoEstadoIncidenteAnterior = estAnt.IdTipoEstadoIncidente
                LEFT JOIN TbAnciTipoEstadoIncidente estNvo ON h.IdTipoEstadoIncidenteNuevo = estNvo.IdTipoEstadoIncidente
                LEFT JOIN TbAnciTipoGravedad gAnt ON h.IdTipoGravedadAnterior = gAnt.IdTipoGravedad
                LEFT JOIN TbAnciTipoGravedad gNvo ON h.IdTipoGravedadNueva = gNvo.IdTipoGravedad
                WHERE h.IdIncidente = @IdIncidente
                ORDER BY h.FechaCambio DESC";
            
            return await connection.QueryAsync<EventoHistorialResumen>(sql, new { IdIncidente = idIncidente });
        }
    }
}