using System.Threading.Tasks;
using Sitrac.Repository.Models;

namespace Sitrac.Repository.Interfaces
{
    public interface ISlaRepository
    {
        Task<AnciTipoPlazoSla?> ObtenerParametrosPorEmpresaAsync(long codTipoClasificacionEmpresa);
        Task InsertarPlazosAsync(AnciIncidentePlazoSla plazos);
    }
}