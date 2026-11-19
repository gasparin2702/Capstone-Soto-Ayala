namespace Sitrac.Servicio.Seguridad;

public static class TransaccionesCatalogo
{
    public const string PrefijoSistemaPorDefecto = "SITR";

    // Autenticacion
    public const string IniciarSesion = PrefijoSistemaPorDefecto + "_AUTE_INICIAR_SESION";
    public const string RegistrarUsuario = PrefijoSistemaPorDefecto + "_AUTE_REGISTRAR_USUARIO";

    // Integraciones
    public const string IngestarIncidente = PrefijoSistemaPorDefecto + "_INTE_INGESTAR_INCIDENTE";

    // Incidentes
    public const string ConsultarIncidentes = PrefijoSistemaPorDefecto + "_INCI_CONSULTAR_INCIDENTES";
    public const string ConsultarIncidente = PrefijoSistemaPorDefecto + "_INCI_CONSULTAR_INCIDENTE";
    public const string ConsultarHistorial = PrefijoSistemaPorDefecto + "_INCI_CONSULTAR_HISTORIAL";
    public const string ConfirmarAviso = PrefijoSistemaPorDefecto + "_INCI_CONFIRMAR_AVISO";
    public const string RegistrarInforme = PrefijoSistemaPorDefecto + "_INCI_REGISTRAR_INFORME";
    public const string CerrarIncidente = PrefijoSistemaPorDefecto + "_INCI_CERRAR_INCIDENTE";

    public static readonly string[] Todas = new[]
    {
        IniciarSesion, RegistrarUsuario, IngestarIncidente,
        ConsultarIncidentes, ConsultarIncidente, ConsultarHistorial,
        ConfirmarAviso, RegistrarInforme, CerrarIncidente
    };
}
