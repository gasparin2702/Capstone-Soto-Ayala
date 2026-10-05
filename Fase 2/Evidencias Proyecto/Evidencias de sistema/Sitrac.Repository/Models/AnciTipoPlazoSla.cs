namespace Sitrac.Repository.Models
{
    public class AnciTipoPlazoSla
    {
        public long IdTipoPlazoSla { get; set; }
        public long CodTipoClasificacionEmpresa { get; set; }
        public int HorasAvisoTemprano { get; set; }
        public int HorasInformePreliminar { get; set; }
        public int DiasInformeFinal { get; set; }
    }
}