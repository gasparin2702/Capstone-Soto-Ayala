using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Sitrac.AccesoDatos
{
    public class FabricaConexionBd : IFabricaConexionBd
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public FabricaConexionBd(IConfiguration configuration)
        {
            _configuration = configuration;
            var cadena = _configuration.GetConnectionString("SitracConnection")
                ?? throw new ArgumentNullException("La cadena de conexión SitracConnection no está configurada.");
            _connectionString = cadena.Trim().Trim('"', '\'');
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}