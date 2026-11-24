namespace Sitrac.Servicio.Dtos
{
    public record ResultadoEnvio(bool Exito, string? MensajeError, System.DateTime Timestamp)
    {
        public string? DestinatarioCorreo { get; init; }
        public string? IdMensajeProveedor { get; init; }

        public static ResultadoEnvio Ok(string? destinatarioCorreo = null, string? idMensajeProveedor = null)
        {
            return new ResultadoEnvio(true, null, System.DateTime.UtcNow)
            {
                DestinatarioCorreo = destinatarioCorreo,
                IdMensajeProveedor = idMensajeProveedor
            };
        }

        public static ResultadoEnvio Fallo(string motivo) => new(false, motivo, System.DateTime.UtcNow);
    }
}
