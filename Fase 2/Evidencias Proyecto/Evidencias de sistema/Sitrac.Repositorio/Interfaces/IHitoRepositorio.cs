using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repositorio.Models;

namespace Sitrac.Repositorio.Interfaces
{
    public interface IHitoRepositorio
    {
        Task<long> CrearAsync(Hito hito);
        Task<Hito?> ObtenerPorIdAsync(long idHito);
        Task<IEnumerable<Hito>> ObtenerPorIncidenteAsync(long idIncidente);
        Task<IEnumerable<Hito>> ObtenerPendientesParaEvaluarAsync();
        Task<Hito?> ObtenerHitoPendientePorIncidenteYTipoAsync(long idIncidente, long codTipoHito);
        Task<bool> ReservarNotificacionAsync(long idHito, long idRegla, DateTime proximaNotificacion, DateTime ahoraUtc);
        Task CerrarHitoAsync(long idHito, string? referenciaExterna, DateTime fechaCumplimiento);
        Task CancelarHitosAbiertosAsync(long idIncidente);
    }
}