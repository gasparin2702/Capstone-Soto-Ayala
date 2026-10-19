using System.Threading.Tasks;
using Sitrac.Repository.Models;

namespace Sitrac.Repository.Interfaces
{
    public interface ISlaRepository
    {
        Task<SitracTipoPlazoSla?> ObtenerParametrosPorEmpresaAsync(long codTipoClasificacionEmpresa);
        Task InsertarPlazosAsync(SitracIncidentePlazoSla plazos);
    }
}
