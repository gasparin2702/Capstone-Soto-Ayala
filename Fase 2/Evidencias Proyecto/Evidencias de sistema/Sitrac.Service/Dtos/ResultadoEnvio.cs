namespace Sitrac.Service.Dtos
{
    public record ResultadoEnvio(bool Exito, string? MensajeError, DateTime Timestamp)
    {
        public static ResultadoEnvio Ok() => new(true, null, DateTime.UtcNow);
        public static ResultadoEnvio Fallo(string motivo) => new(false, motivo, DateTime.UtcNow);
    }
}
