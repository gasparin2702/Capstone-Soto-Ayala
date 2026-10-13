namespace Sitrac.Repository.Models
{
    public class ReglaEscalamiento
    {
        public long IdRegla { get; set; }
        public long CodTipoHito { get; set; }
        public int TiempoRestanteMinutos { get; set; }
        public int? RepetirCadaMinutos { get; set; }
        public bool NotificarEmail { get; set; }
        public bool NotificarTeams { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int OrdenVisual { get; set; }
    }
}
