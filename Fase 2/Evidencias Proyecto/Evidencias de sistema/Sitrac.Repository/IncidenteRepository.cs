using Dapper;
using Sitrac.DataAccess;
using Sitrac.Repository.Interfaces;
using Sitrac.Repository.Models;
using System;
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
            
            const string sqlIncidente = @"
                SELECT 
                    i.IdIncidente,
                    i.CodigoTicketInvgate,
                    c.NombreTipoCanalEntrada AS Origen,
                    e.NombreTipoClasificacionEmpresa AS Clasificacion,
                    g.OrdenTipoGravedad AS Gravedad,
                    est.NombreTipoEstadoIncidente AS Estado,
                    i.CodTipoEstadoIncidente,
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

            const string sqlHitos = @"
                SELECT 
                    h.IdHito, 
                    h.CodTipoHito,
                    t.NombreTipoHito,
                    h.FechaBase, 
                    h.FechaLimite, 
                    h.Estado, 
                    h.FechaCumplimiento, 
                    h.ReferenciaExterna
                FROM TbSitracHito h
                INNER JOIN TbSitracTipoHito t ON h.CodTipoHito = t.IdTipoHito
                WHERE h.IdIncidente = @IdIncidente AND h.AuditNotDeleted = 1
                ORDER BY h.CodTipoHito ASC";

            using var multi = await connection.QueryMultipleAsync(sqlIncidente + ";" + sqlHitos, new { IdIncidente = idIncidente });
            
            var detalle = await multi.ReadFirstOrDefaultAsync<IncidenteDetalle>();
            if (detalle == null) return null;

            detalle.Hitos = (await multi.ReadAsync<HitoResumen>()).ToList();
            return detalle;
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
                    CAST(IIF(h.IdTipoAmbitoCambio = 1, 1, 0) AS BIT) AS EsAccionAutomatica,
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

        // ===== MÉTODOS TRANSACCIONALES NUEVOS (BLOQUE B) =====

        public async Task ConfirmarAvisoTransaccionalAsync(
            long idIncidente, long nuevoEstado, SitracEventoHistorial historial,
            Hito? hitoACerrar, Hito? hitoACrear, DateTime ahora)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                if (hitoACerrar != null)
                {
                    const string sqlCerrar = @"
                        UPDATE TbSitracHito
                        SET Estado = 'Cumplido', FechaCumplimiento = @Ahora, ReferenciaExterna = @Ref, AuditLastUpdateDate = GETUTCDATE()
                        WHERE IdHito = @IdHito AND Estado = 'Pendiente'";
                    await connection.ExecuteAsync(sqlCerrar, new { IdHito = hitoACerrar.IdHito, Ahora = ahora, Ref = hitoACerrar.ReferenciaExterna }, transaction);
                }

                if (hitoACrear != null)
                {
                    const string sqlCrear = @"
                        INSERT INTO TbSitracHito (IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado)
                        VALUES (@IdIncidente, @CodTipoHito, @Secuencia, @FechaBase, @FechaLimite, 'Pendiente');";
                    await connection.ExecuteAsync(sqlCrear, hitoACrear, transaction);
                }

                const string sqlUpdate = @"
                    UPDATE TbSitracIncidente SET CodTipoEstadoIncidente = @NuevoEstado, AuditLastUpdateDate = GETDATE()
                    WHERE IdIncidente = @IdIncidente";
                await connection.ExecuteAsync(sqlUpdate, new { NuevoEstado = nuevoEstado, IdIncidente = idIncidente }, transaction);

                const string sqlInsertHist = @"
                    INSERT INTO TbSitracEventoHistorial (IdIncidente, CodigoTicketInvgate, IdTipoAmbitoCambio, IdTipoEstadoIncidenteAnterior, IdTipoEstadoIncidenteNuevo, IdResponsableCambia, Comentario)
                    VALUES (@IdIncidente, @CodigoTicketInvgate, @IdTipoAmbitoCambio, @IdTipoEstadoIncidenteAnterior, @IdTipoEstadoIncidenteNuevo, @IdResponsableCambia, @Comentario)";
                await connection.ExecuteAsync(sqlInsertHist, historial, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task RegistrarInformeTransaccionalAsync(
            long idIncidente, long? nuevoEstado, SitracEventoHistorial historial,
            Hito hitoACerrar, Hito? hitoACrear, DateTime ahora)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string sqlCerrar = @"
                    UPDATE TbSitracHito
                    SET Estado = 'Cumplido', FechaCumplimiento = @Ahora, ReferenciaExterna = @Ref, AuditLastUpdateDate = GETUTCDATE()
                    WHERE IdHito = @IdHito AND Estado = 'Pendiente'";
                var filas = await connection.ExecuteAsync(sqlCerrar, new { IdHito = hitoACerrar.IdHito, Ahora = ahora, Ref = hitoACerrar.ReferenciaExterna }, transaction);

                if (filas == 0) throw new InvalidOperationException($"El hito {hitoACerrar.CodTipoHito} ya estaba cerrado o no existe.");

                if (hitoACrear != null)
                {
                    const string sqlCrear = @"
                        INSERT INTO TbSitracHito (IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado)
                        VALUES (@IdIncidente, @CodTipoHito, @Secuencia, @FechaBase, @FechaLimite, 'Pendiente');";
                    await connection.ExecuteAsync(sqlCrear, hitoACrear, transaction);
                }

                if (nuevoEstado.HasValue)
                {
                    const string sqlUpdate = @"
                        UPDATE TbSitracIncidente SET CodTipoEstadoIncidente = @NuevoEstado, AuditLastUpdateDate = GETDATE()
                        WHERE IdIncidente = @IdIncidente";
                    await connection.ExecuteAsync(sqlUpdate, new { NuevoEstado = nuevoEstado.Value, IdIncidente = idIncidente }, transaction);
                }

                const string sqlInsertHist = @"
                    INSERT INTO TbSitracEventoHistorial (IdIncidente, CodigoTicketInvgate, IdTipoAmbitoCambio, IdTipoEstadoIncidenteAnterior, IdTipoEstadoIncidenteNuevo, IdResponsableCambia, Comentario)
                    VALUES (@IdIncidente, @CodigoTicketInvgate, @IdTipoAmbitoCambio, @IdTipoEstadoIncidenteAnterior, @IdTipoEstadoIncidenteNuevo, @IdResponsableCambia, @Comentario)";
                await connection.ExecuteAsync(sqlInsertHist, historial, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task CerrarIncidenteTransaccionalAsync(long idIncidente, SitracEventoHistorial historial, DateTime ahora)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string sqlUpdate = @"
                    UPDATE TbSitracIncidente SET CodTipoEstadoIncidente = 4, AuditLastUpdateDate = GETDATE()
                    WHERE IdIncidente = @IdIncidente";
                await connection.ExecuteAsync(sqlUpdate, new { IdIncidente = idIncidente }, transaction);

                const string sqlCancelar = @"
                    UPDATE TbSitracHito SET Estado = 'Cancelado', AuditLastUpdateDate = GETUTCDATE()
                    WHERE IdIncidente = @IdIncidente AND Estado = 'Pendiente' AND AuditNotDeleted = 1";
                await connection.ExecuteAsync(sqlCancelar, new { IdIncidente = idIncidente }, transaction);

                const string sqlInsertHist = @"
                    INSERT INTO TbSitracEventoHistorial (IdIncidente, CodigoTicketInvgate, IdTipoAmbitoCambio, IdTipoEstadoIncidenteAnterior, IdTipoEstadoIncidenteNuevo, IdResponsableCambia, Comentario)
                    VALUES (@IdIncidente, @CodigoTicketInvgate, @IdTipoAmbitoCambio, @IdTipoEstadoIncidenteAnterior, @IdTipoEstadoIncidenteNuevo, @IdResponsableCambia, @Comentario)";
                await connection.ExecuteAsync(sqlInsertHist, historial, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}