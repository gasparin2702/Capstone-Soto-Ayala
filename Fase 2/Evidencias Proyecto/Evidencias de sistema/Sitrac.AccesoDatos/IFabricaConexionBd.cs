using System.Data;

namespace Sitrac.AccesoDatos
{
    public interface IFabricaConexionBd
    {
        IDbConnection CreateConnection();
    }
}