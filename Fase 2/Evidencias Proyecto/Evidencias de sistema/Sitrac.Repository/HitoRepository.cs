using Dapper;
using Sitrac.DataAccess;
using Sitrac.Repository.Interfaces;
using Sitrac.Repository.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Repository
{
    public class HitoRepository : IHitoRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public HitoRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<long> CrearAsync(Hito hito)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TbSitracHito (
                    IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado,
                    FechaCumplimiento, ReferenciaExterna
                ) VALUES (
                    @IdIncidente, @CodTipoHito, @Secuencia, @FechaBase, @FechaLimite, @Estado,
                    @FechaCumplimiento, @ReferenciaExterna
                );
                SELECT CAST(SCOPE_IDENTITY() as bigint);";

            return await connection.QuerySingleAsync<long>(sql, hito);
        }

        public async Task<Hito?> ObtenerPorIdAsync(long idHito)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT IdHito, IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado,
                       FechaCumplimiento, ReferenciaExterna, UltimaReglaNotificada, ProximaNotificacionAt,
                       ConteoNotificaciones, SilenciadoHasta
                FROM TbSitracHito
                WHERE IdHito = @IdHito AND AuditNotDeleted = 1";

            return await connection.QueryFirstOrDefaultAsync<Hito>(sql, new { IdHito = idHito });
        }

        public async Task<IEnumerable<Hito>> ObtenerPorIncidenteAsync(long idIncidente)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT IdHito, IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado,
                       FechaCumplimiento, ReferenciaExterna, UltimaReglaNotificada, ProximaNotificacionAt,
                       ConteoNotificaciones, SilenciadoHasta
                FROM TbSitracHito
                WHERE IdIncidente = @IdIncidente AND AuditNotDeleted = 1
                ORDER BY CodTipoHito ASC, Secuencia ASC";

            return await connection.QueryAsync<Hito>(sql, new { IdIncidente = idIncidente });
        }

        public async Task<IEnumerable<Hito>> ObtenerPendientesParaEvaluarAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT IdHito, IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado,
                       FechaCumplimiento, ReferenciaExterna, UltimaReglaNotificada, ProximaNotificacionAt,
                       ConteoNotificaciones, SilenciadoHasta
                FROM TbSitracHito
                WHERE Estado = 'Pendiente'
                  AND AuditNotDeleted = 1
                  AND (SilenciadoHasta IS NULL OR SilenciadoHasta <= GETUTCDATE())";

            return await connection.QueryAsync<Hito>(sql);
        }

        public async Task<Hito?> ObtenerHitoPendientePorIncidenteYTipoAsync(long idIncidente, long codTipoHito)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT TOP 1 IdHito, IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado,
                       FechaCumplimiento, ReferenciaExterna, UltimaReglaNotificada, ProximaNotificacionAt,
                       ConteoNotificaciones, SilenciadoHasta
                FROM TbSitracHito
                WHERE IdIncidente = @IdIncidente
                  AND CodTipoHito = @CodTipoHito
                  AND Estado = 'Pendiente'
                  AND AuditNotDeleted = 1
                ORDER BY Secuencia DESC";

            return await connection.QueryFirstOrDefaultAsync<Hito>(sql,
                new { IdIncidente = idIncidente, CodTipoHito = codTipoHito });
        }

        public async Task<bool> ReservarNotificacionAsync(
            long idHito, long idRegla, DateTime proximaNotificacion, DateTime ahoraUtc)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE TbSitracHito
                SET UltimaReglaNotificada = @IdRegla,
                    ProximaNotificacionAt = @ProximaNotificacion,
                    ConteoNotificaciones = ConteoNotificaciones + 1,
                    AuditLastUpdateDate = @Ahora
                WHERE IdHito = @IdHito
                  AND Estado = 'Pendiente'
                  AND AuditNotDeleted = 1
                  AND (UltimaReglaNotificada IS NULL
                       OR UltimaReglaNotificada <> @IdRegla
                       OR ProximaNotificacionAt IS NULL
                       OR ProximaNotificacionAt <= @Ahora);";

            var filas = await connection.ExecuteAsync(sql, new
            {
                IdHito = idHito,
                IdRegla = idRegla,
                ProximaNotificacion = proximaNotificacion,
                Ahora = ahoraUtc
            });

            return filas == 1;
        }

        public async Task CerrarHitoAsync(long idHito, string? referenciaExterna, DateTime fechaCumplimiento)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE TbSitracHito
                SET Estado = 'Cumplido',
                    FechaCumplimiento = @FechaCumplimiento,
                    ReferenciaExterna = @ReferenciaExterna,
                    AuditLastUpdateDate = GETUTCDATE()
                WHERE IdHito = @IdHito AND Estado = 'Pendiente'";

            await connection.ExecuteAsync(sql, new
            {
                IdHito = idHito,
                FechaCumplimiento = fechaCumplimiento,
                ReferenciaExterna = referenciaExterna
            });
        }

        public async Task CancelarHitosAbiertosAsync(long idIncidente)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE TbSitracHito
                SET Estado = 'Cancelado',
                    AuditLastUpdateDate = GETUTCDATE()
                WHERE IdIncidente = @IdIncidente
                  AND Estado = 'Pendiente'
                  AND AuditNotDeleted = 1";

            await connection.ExecuteAsync(sql, new { IdIncidente = idIncidente });
        }
    }
}