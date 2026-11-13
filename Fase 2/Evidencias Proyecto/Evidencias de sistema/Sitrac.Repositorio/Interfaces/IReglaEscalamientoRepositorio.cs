using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repositorio.Models;

namespace Sitrac.Repositorio.Interfaces
{
    public interface IReglaEscalamientoRepositorio
    {
        Task<IEnumerable<ReglaEscalamiento>> ObtenerReglasActivasAsync();
        Task<IEnumerable<ReglaEscalamiento>> ObtenerPorTipoHitoAsync(long codTipoHito);
    }
}
