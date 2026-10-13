using System;

namespace Sitrac.Service.Helpers
{
    public static class PrioridadCalculator
    {
        public const int PesoGravedad = 3;
        public const int PesoClasificacion = 2;
        public const int PesoUrgencia = 40;
        public const int PesoHito = 2;

        public const int UmbralMaxima = 30;
        public const int UmbralAlta = 22;
        public const int UmbralMedia = 15;

        // Lookup tables for gravedad mappings (index 0 = gravedad 1)
        private static readonly string[] _emojiGravedadMap = { "⚪", "🔵", "🟠", "🔴" };
        private static readonly string[] _nombreGravedadMap = { "Bajo", "Medio", "Alto", "Crítico" };

        public static double Calcular(
            int gravedad,
            string clasificacion,
            long hitoCodigo,
            DateTime? fechaLimite,
            DateTime? fechaVencimiento,
            DateTime ahora)
        {
            var g = Math.Max(1, Math.Min(4, gravedad));
            var cls = clasificacion == "OIV" ? 2 : 1; // Optimized: ordinal comparison (data is always exact match)
            var urgencia = CalcularUrgencia(fechaLimite, fechaVencimiento, ahora);
            var hito = PesoDelHito(hitoCodigo);

            var score =
                (g * PesoGravedad) +
                (cls * PesoClasificacion) +
                (urgencia * PesoUrgencia) +
                (hito * PesoHito);

            return Math.Round(score, 2);
        }

        public static double CalcularUrgencia(DateTime? fechaLimite, DateTime? fechaVencimiento, DateTime ahora)
        {
            if (!fechaLimite.HasValue) return 0;

            var rem = (fechaLimite.Value - ahora).TotalMilliseconds;
            if (rem <= 0) return 1;

            var ventana = fechaVencimiento.HasValue
                ? (fechaLimite.Value - fechaVencimiento.Value).TotalMilliseconds
                : rem;

            if (ventana <= 0) return 1;

            var frac = Math.Min(1, rem / ventana);
            return 1 - frac;
        }

        public static int PesoDelHito(long hitoCodigo) => hitoCodigo switch
        {
            1 => 3,
            2 => 2,
            _ => 1
        };

        public static string Etiqueta(double score)
        {
            if (score >= UmbralMaxima) return "Máxima";
            if (score >= UmbralAlta) return "Alta";
            if (score >= UmbralMedia) return "Media";
            return "Baja";
        }

        public static string ColorEtiqueta(double score)
        {
            if (score >= UmbralMaxima) return "#E4002B";
            if (score >= UmbralAlta) return "#DFC500";
            if (score >= UmbralMedia) return "#00B482";
            return "#6B7785";
        }

        public static string EmojiGravedad(int gravedad)
        {
            if (gravedad < 1 || gravedad > 4) return "⚪";
            return _emojiGravedadMap[gravedad - 1];
        }

        public static string NombreGravedad(int gravedad)
        {
            if (gravedad < 1 || gravedad > 4) return "Bajo";
            return _nombreGravedadMap[gravedad - 1];
        }
    }
}