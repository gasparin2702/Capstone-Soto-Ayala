namespace Sitrac.Repositorio.Models
{
    public class Transaccion
    {
        public long IdTransaccion { get; set; }
        public string CodigoTransaccion { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public long IdModulo { get; set; }
    }
}
