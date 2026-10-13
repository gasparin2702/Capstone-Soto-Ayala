namespace Sitrac.Service.Dtos
{
    public record ResultadoEnvio(bool Exito, string? MensajeError, System.DateTime Timestamp)
    {
        public static ResultadoEnvio Ok() => new(true, null, System.DateTime.UtcNow);
        public static ResultadoEnvio Fallo(string motivo) => new(false, motivo, System.DateTime.UtcNow);
    }
}