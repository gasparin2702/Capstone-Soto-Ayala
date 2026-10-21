using Dapper;
using Sitrac.DataAccess;
using Sitrac.Repository.Interfaces;
using Sitrac.Repository.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Repository
{
    public class NotificacionLogRepository : INotificacionLogRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public NotificacionLogRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InsertarAsync(NotificacionLog log)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbSitracNotificacionLog (
                    IdHito, NumeroNotificacion, Destinatario, Canal, IdRegla, Estado,
                    Intentos, ProximoReintentoAt, FechaProgramada
                ) VALUES (
                    @IdHito, @NumeroNotificacion, @Destinatario, @Canal, @IdRegla, @Estado,
                    @Intentos, @ProximoReintentoAt, @FechaProgramada
                )";

            await connection.ExecuteAsync(sql, log);
        }

        public async Task InsertarLoteAsync(IEnumerable<NotificacionLog> logs)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbSitracNotificacionLog (
                    IdHito, NumeroNotificacion, Destinatario, Canal, IdRegla, Estado,
                    Intentos, ProximoReintentoAt, FechaProgramada
                ) VALUES (
                    @IdHito, @NumeroNotificacion, @Destinatario, @Canal, @IdRegla, @Estado,
                    @Intentos, @ProximoReintentoAt, @FechaProgramada
                )";

            await connection.ExecuteAsync(sql, logs);
        }

        public async Task<IEnumerable<NotificacionLog>> ObtenerPendientesParaEnvioAsync(int maximo)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT TOP (@Maximo)
                    l.IdNotificacion, l.IdHito, l.NumeroNotificacion, l.Destinatario, l.Canal, l.IdRegla,
                    l.Estado, l.Intentos, l.ProximoReintentoAt, l.FechaProgramada, l.FechaEnvio, l.MensajeError,
                    i.CodTipoGravedad, i.DescripcionSistemaAfectado AS SistemaAfectado
                FROM TbSitracNotificacionLog l
                INNER JOIN TbSitracHito h ON l.IdHito = h.IdHito
                INNER JOIN TbSitracIncidente i ON h.IdIncidente = i.IdIncidente
                WHERE l.Estado = 'Pendiente'
                  AND l.AuditNotDeleted = 1
                  AND l.FechaProgramada <= GETUTCDATE()
                ORDER BY l.FechaProgramada ASC";

            return await connection.QueryAsync<NotificacionLog>(sql, new { Maximo = maximo });
        }

        public async Task MarcarEnviadaAsync(long idNotificacion, DateTime fechaEnvio)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE TbSitracNotificacionLog
                SET Estado = 'Enviada',
                    FechaEnvio = @FechaEnvio,
                    Intentos = Intentos + 1,
                    AuditLastUpdateDate = GETUTCDATE()
                WHERE IdNotificacion = @IdNotificacion AND Estado = 'Pendiente'";

            await connection.ExecuteAsync(sql, new { IdNotificacion = idNotificacion, FechaEnvio = fechaEnvio });
        }

        public async Task MarcarFallidaAsync(long idNotificacion, string mensajeError, DateTime? proximoReintento)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE TbSitracNotificacionLog
                SET Estado = CASE WHEN Intentos + 1 >= 3 THEN 'Fallida' ELSE 'Pendiente' END,
                    Intentos = Intentos + 1,
                    MensajeError = @MensajeError,
                    ProximoReintentoAt = @ProximoReintento,
                    AuditLastUpdateDate = GETUTCDATE()
                WHERE IdNotificacion = @IdNotificacion AND Estado = 'Pendiente'";

            await connection.ExecuteAsync(sql, new
            {
                IdNotificacion = idNotificacion,
                MensajeError = mensajeError,
                ProximoReintento = proximoReintento
            });
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