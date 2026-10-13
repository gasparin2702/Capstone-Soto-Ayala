using System.Collections.Generic;
using System.Threading.Tasks;
using Sitrac.Repository.Models;

namespace Sitrac.Repository.Interfaces
{
    public interface IReglaEscalamientoRepository
    {
        Task<IEnumerable<ReglaEscalamiento>> ObtenerReglasActivasAsync();
        Task<IEnumerable<ReglaEscalamiento>> ObtenerPorTipoHitoAsync(long codTipoHito);
    }
}
