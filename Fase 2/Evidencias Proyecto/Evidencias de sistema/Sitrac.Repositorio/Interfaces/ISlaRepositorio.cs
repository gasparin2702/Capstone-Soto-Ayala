using System.Threading.Tasks;
using Sitrac.Repositorio.Models;

namespace Sitrac.Repositorio.Interfaces
{
    public interface ISlaRepositorio
    {
        Task<SitracTipoPlazoSla?> ObtenerParametrosPorEmpresaAsync(long codTipoClasificacionEmpresa);
        Task InsertarPlazosAsync(SitracIncidentePlazoSla plazos);
    }
}
