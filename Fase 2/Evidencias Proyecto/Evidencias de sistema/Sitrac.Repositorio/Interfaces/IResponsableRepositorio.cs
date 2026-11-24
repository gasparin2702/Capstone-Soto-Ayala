using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitrac.Repositorio.Interfaces
{
    public interface IResponsableRepositorio
    {
        Task<IReadOnlyList<string>> ObtenerCorreosDelegadosActivosAsync();
        Task<IReadOnlyList<string>> ObtenerTodosCorreosActivosAsync();
    }
}
