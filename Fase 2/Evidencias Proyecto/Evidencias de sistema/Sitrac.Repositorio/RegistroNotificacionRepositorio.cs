using Dapper;
using Sitrac.AccesoDatos;
using Sitrac.Repositorio.Interfaces;
using Sitrac.Repositorio.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Repositorio
{
    public class RegistroNotificacionRepositorio : IRegistroNotificacionRepositorio
    {
        private readonly IFabricaConexionBd _connectionFactory;

        public RegistroNotificacionRepositorio(IFabricaConexionBd connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InsertarAsync(NotificacionLog log)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbSitracNotificacionLog (
                    IdHito, NumeroNotificacion, Destinatario, DestinatarioCorreo, ReferenciaTicket,
                    IdMensajeProveedor, IdempotenciaKey, Canal, IdRegla, Estado,
                    Intentos, ProximoReintentoAt, FechaProgramada
                ) VALUES (
                    @IdHito, @NumeroNotificacion, @Destinatario, @DestinatarioCorreo, @ReferenciaTicket,
                    @IdMensajeProveedor, @IdempotenciaKey, @Canal, @IdRegla, @Estado,
                    @Intentos, @ProximoReintentoAt, @FechaProgramada
                )";

            await connection.ExecuteAsync(sql, log);
        }

        public async Task InsertarLoteAsync(IEnumerable<NotificacionLog> logs)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbSitracNotificacionLog (
                    IdHito, NumeroNotificacion, Destinatario, DestinatarioCorreo, ReferenciaTicket,
                    IdMensajeProveedor, IdempotenciaKey, Canal, IdRegla, Estado,
                    Intentos, ProximoReintentoAt, FechaProgramada
                ) VALUES (
                    @IdHito, @NumeroNotificacion, @Destinatario, @DestinatarioCorreo, @ReferenciaTicket,
                    @IdMensajeProveedor, @IdempotenciaKey, @Canal, @IdRegla, @Estado,
                    @Intentos, @ProximoReintentoAt, @FechaProgramada
                )";

            await connection.ExecuteAsync(sql, logs);
        }

        public async Task<IEnumerable<NotificacionLog>> ObtenerPendientesParaEnvioAsync(int maximo, int maximoIntentos)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT TOP (@Maximo)
                    l.IdNotificacion, l.IdHito, l.NumeroNotificacion, l.Destinatario, l.DestinatarioCorreo,
                    l.ReferenciaTicket, l.IdMensajeProveedor, l.IdempotenciaKey, l.Canal, l.IdRegla,
                    l.Estado, l.Intentos, l.ProximoReintentoAt, l.FechaProgramada, l.FechaEnvio, l.MensajeError,
                    i.CodTipoGravedad, i.DescripcionSistemaAfectado AS SistemaAfectado
                FROM TbSitracNotificacionLog l
                INNER JOIN TbSitracHito h ON l.IdHito = h.IdHito
                INNER JOIN TbSitracIncidente i ON h.IdIncidente = i.IdIncidente
                WHERE l.Estado = 'Pendiente'
                  AND l.AuditNotDeleted = 1
                  AND l.FechaProgramada <= GETUTCDATE()
                  AND (l.ProximoReintentoAt IS NULL OR l.ProximoReintentoAt <= GETUTCDATE())
                  AND l.Intentos < @MaximoIntentos
                ORDER BY l.FechaProgramada ASC, l.IdNotificacion ASC";

            return await connection.QueryAsync<NotificacionLog>(sql, new { Maximo = maximo, MaximoIntentos = maximoIntentos });
        }

        public async Task MarcarEnviadaAsync(
            long idNotificacion,
            DateTime fechaEnvio,
            string? destinatarioCorreo = null,
            string? idMensajeProveedor = null,
            string? referenciaTicket = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE TbSitracNotificacionLog
                SET Estado = 'Enviada',
                    FechaEnvio = @FechaEnvio,
                    DestinatarioCorreo = COALESCE(@DestinatarioCorreo, DestinatarioCorreo),
                    IdMensajeProveedor = COALESCE(@IdMensajeProveedor, IdMensajeProveedor),
                    ReferenciaTicket = COALESCE(@ReferenciaTicket, ReferenciaTicket),
                    Intentos = Intentos + 1,
                    ProximoReintentoAt = NULL,
                    AuditLastUpdateDate = GETUTCDATE()
                WHERE IdNotificacion = @IdNotificacion AND Estado = 'Pendiente'";

            await connection.ExecuteAsync(sql, new
            {
                IdNotificacion = idNotificacion,
                FechaEnvio = fechaEnvio,
                DestinatarioCorreo = destinatarioCorreo,
                IdMensajeProveedor = idMensajeProveedor,
                ReferenciaTicket = referenciaTicket
            });
        }

        public async Task MarcarFallidaAsync(long idNotificacion, string mensajeError, int maximoIntentos, int retrasoBaseSegundos = 30)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                DECLARE @IntentosActuales INT = 0;
                DECLARE @NuevoIntentos INT;
                DECLARE @Agotado BIT = 0;

                SELECT @IntentosActuales = Intentos
                FROM TbSitracNotificacionLog
                WHERE IdNotificacion = @IdNotificacion AND Estado = 'Pendiente';

                SET @NuevoIntentos = @IntentosActuales + 1;
                SET @Agotado = CASE WHEN @NuevoIntentos >= @MaximoIntentos THEN 1 ELSE 0 END;

                UPDATE TbSitracNotificacionLog
                SET Estado = CASE WHEN @Agotado = 1 THEN 'Fallida' ELSE 'Pendiente' END,
                    Intentos = @NuevoIntentos,
                    MensajeError = LEFT(@MensajeError, 500),
                    ProximoReintentoAt = CASE
                        WHEN @Agotado = 1 THEN NULL
                        ELSE DATEADD(
                            SECOND,
                            CAST(@RetrasoBaseSegundos * POWER(CAST(2 AS FLOAT), @IntentosActuales) AS INT),
                            GETUTCDATE())
                    END,
                    AuditLastUpdateDate = GETUTCDATE()
                WHERE IdNotificacion = @IdNotificacion AND Estado = 'Pendiente'";

            await connection.ExecuteAsync(sql, new
            {
                IdNotificacion = idNotificacion,
                MensajeError = mensajeError,
                MaximoIntentos = maximoIntentos,
                RetrasoBaseSegundos = retrasoBaseSegundos
            });
        }

        public async Task MarcarCanceladaAsync(long idNotificacion, string motivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE TbSitracNotificacionLog
                SET Estado = 'Cancelada',
                    MensajeError = LEFT(@Motivo, 500),
                    ProximoReintentoAt = NULL,
                    FechaEnvio = NULL,
                    AuditLastUpdateDate = GETUTCDATE()
                WHERE IdNotificacion = @IdNotificacion AND Estado = 'Pendiente'";

            await connection.ExecuteAsync(sql, new { IdNotificacion = idNotificacion, Motivo = motivo });
        }

        public async Task CancelarPendientesPorHitoAsync(long idHito)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE TbSitracNotificacionLog
                SET Estado = 'Cancelada',
                    AuditLastUpdateDate = GETUTCDATE()
                WHERE IdHito = @IdHito AND Estado = 'Pendiente'";

            await connection.ExecuteAsync(sql, new { IdHito = idHito });
        }
    }
}
