-- ==============================================================================
-- SISTEMA: SITRAC
-- OBJETIVO: Seed de modulos, transacciones, perfiles, roles y mallas ATLAS
-- NOTA: El prefijo "SITR" es provisorio. Si el Gestor de Roles y Perfiles
--       confirma otro prefijo, este es el UNICO valor SQL a modificar.
-- ==============================================================================

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Prefijo VARCHAR(10) = 'SITR';

    DECLARE @TxIniciarSesion VARCHAR(50) = CONCAT(@Prefijo, '_AUTE_INICIAR_SESION');
    DECLARE @TxRegistrarUsuario VARCHAR(50) = CONCAT(@Prefijo, '_AUTE_REGISTRAR_USUARIO');
    DECLARE @TxIngestarIncidente VARCHAR(50) = CONCAT(@Prefijo, '_INTE_INGESTAR_INCIDENTE');
    DECLARE @TxConsultarIncidentes VARCHAR(50) = CONCAT(@Prefijo, '_INCI_CONSULTAR_INCIDENTES');
    DECLARE @TxConsultarIncidente VARCHAR(50) = CONCAT(@Prefijo, '_INCI_CONSULTAR_INCIDENTE');
    DECLARE @TxConsultarHistorial VARCHAR(50) = CONCAT(@Prefijo, '_INCI_CONSULTAR_HISTORIAL');
    DECLARE @TxConfirmarAviso VARCHAR(50) = CONCAT(@Prefijo, '_INCI_CONFIRMAR_AVISO');
    DECLARE @TxRegistrarInforme VARCHAR(50) = CONCAT(@Prefijo, '_INCI_REGISTRAR_INFORME');
    DECLARE @TxCerrarIncidente VARCHAR(50) = CONCAT(@Prefijo, '_INCI_CERRAR_INCIDENTE');

    DECLARE @PerfilAcceso VARCHAR(50) = CONCAT(@Prefijo, ' - Acceder al sistema');
    DECLARE @PerfilUsuarios VARCHAR(50) = CONCAT(@Prefijo, ' - Administrar usuarios');
    DECLARE @PerfilConsultar VARCHAR(50) = CONCAT(@Prefijo, ' - Consultar incidentes');
    DECLARE @PerfilGestionar VARCHAR(50) = CONCAT(@Prefijo, ' - Gestionar ciclo de incidentes');
    DECLARE @PerfilIngestar VARCHAR(50) = CONCAT(@Prefijo, ' - Ingestar incidentes');

    DECLARE @IdModuloAute BIGINT;
    DECLARE @IdModuloInte BIGINT;
    DECLARE @IdModuloInci BIGINT;

    DECLARE @IdTxIniciarSesion BIGINT;
    DECLARE @IdTxRegistrarUsuario BIGINT;
    DECLARE @IdTxIngestarIncidente BIGINT;
    DECLARE @IdTxConsultarIncidentes BIGINT;
    DECLARE @IdTxConsultarIncidente BIGINT;
    DECLARE @IdTxConsultarHistorial BIGINT;
    DECLARE @IdTxConfirmarAviso BIGINT;
    DECLARE @IdTxRegistrarInforme BIGINT;
    DECLARE @IdTxCerrarIncidente BIGINT;

    DECLARE @IdPerfilAcceso BIGINT;
    DECLARE @IdPerfilUsuarios BIGINT;
    DECLARE @IdPerfilConsultar BIGINT;
    DECLARE @IdPerfilGestionar BIGINT;
    DECLARE @IdPerfilIngestar BIGINT;

    DECLARE @IdRolAdministrador BIGINT;
    DECLARE @IdRolAnalista BIGINT;
    DECLARE @IdRolAuditor BIGINT;
    DECLARE @IdRolInvgate BIGINT;

    -- ==========================================================================
    -- 1. Modulos
    -- ==========================================================================
    IF NOT EXISTS (SELECT 1 FROM TbSitracModulo WHERE CodigoModulo = 'AUTE')
        INSERT INTO TbSitracModulo (CodigoModulo, NombreModulo)
        VALUES ('AUTE', 'Autenticación');

    IF NOT EXISTS (SELECT 1 FROM TbSitracModulo WHERE CodigoModulo = 'INTE')
        INSERT INTO TbSitracModulo (CodigoModulo, NombreModulo)
        VALUES ('INTE', 'Integraciones');

    IF NOT EXISTS (SELECT 1 FROM TbSitracModulo WHERE CodigoModulo = 'INCI')
        INSERT INTO TbSitracModulo (CodigoModulo, NombreModulo)
        VALUES ('INCI', 'Incidentes');

    SELECT @IdModuloAute = IdModulo FROM TbSitracModulo WHERE CodigoModulo = 'AUTE';
    SELECT @IdModuloInte = IdModulo FROM TbSitracModulo WHERE CodigoModulo = 'INTE';
    SELECT @IdModuloInci = IdModulo FROM TbSitracModulo WHERE CodigoModulo = 'INCI';

    -- ==========================================================================
    -- 2. Transacciones
    -- ==========================================================================
    IF NOT EXISTS (SELECT 1 FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxIniciarSesion)
        INSERT INTO TbSitracTransaccion (CodigoTransaccion, Descripcion, IdModulo)
        VALUES (@TxIniciarSesion, 'Permite al usuario autenticarse en el sistema y obtener un token de acceso.', @IdModuloAute);

    IF NOT EXISTS (SELECT 1 FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxRegistrarUsuario)
        INSERT INTO TbSitracTransaccion (CodigoTransaccion, Descripcion, IdModulo)
        VALUES (@TxRegistrarUsuario, 'Permite al administrador registrar nuevos usuarios en el sistema.', @IdModuloAute);

    IF NOT EXISTS (SELECT 1 FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxIngestarIncidente)
        INSERT INTO TbSitracTransaccion (CodigoTransaccion, Descripcion, IdModulo)
        VALUES (@TxIngestarIncidente, 'Permite a INVGATE registrar un incidente mediante el webhook de integración.', @IdModuloInte);

    IF NOT EXISTS (SELECT 1 FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxConsultarIncidentes)
        INSERT INTO TbSitracTransaccion (CodigoTransaccion, Descripcion, IdModulo)
        VALUES (@TxConsultarIncidentes, 'Permite consultar la lista de incidentes registrados en el sistema.', @IdModuloInci);

    IF NOT EXISTS (SELECT 1 FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxConsultarIncidente)
        INSERT INTO TbSitracTransaccion (CodigoTransaccion, Descripcion, IdModulo)
        VALUES (@TxConsultarIncidente, 'Permite consultar el detalle de un incidente registrado en el sistema.', @IdModuloInci);

    IF NOT EXISTS (SELECT 1 FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxConsultarHistorial)
        INSERT INTO TbSitracTransaccion (CodigoTransaccion, Descripcion, IdModulo)
        VALUES (@TxConsultarHistorial, 'Permite consultar el historial de cambios de un incidente.', @IdModuloInci);

    IF NOT EXISTS (SELECT 1 FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxConfirmarAviso)
        INSERT INTO TbSitracTransaccion (CodigoTransaccion, Descripcion, IdModulo)
        VALUES (@TxConfirmarAviso, 'Permite confirmar el aviso temprano de un incidente.', @IdModuloInci);

    IF NOT EXISTS (SELECT 1 FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxRegistrarInforme)
        INSERT INTO TbSitracTransaccion (CodigoTransaccion, Descripcion, IdModulo)
        VALUES (@TxRegistrarInforme, 'Permite registrar el informe preliminar o final de un incidente.', @IdModuloInci);

    IF NOT EXISTS (SELECT 1 FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxCerrarIncidente)
        INSERT INTO TbSitracTransaccion (CodigoTransaccion, Descripcion, IdModulo)
        VALUES (@TxCerrarIncidente, 'Permite cerrar un incidente que cuenta con su informe final registrado.', @IdModuloInci);

    SELECT @IdTxIniciarSesion = IdTransaccion FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxIniciarSesion;
    SELECT @IdTxRegistrarUsuario = IdTransaccion FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxRegistrarUsuario;
    SELECT @IdTxIngestarIncidente = IdTransaccion FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxIngestarIncidente;
    SELECT @IdTxConsultarIncidentes = IdTransaccion FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxConsultarIncidentes;
    SELECT @IdTxConsultarIncidente = IdTransaccion FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxConsultarIncidente;
    SELECT @IdTxConsultarHistorial = IdTransaccion FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxConsultarHistorial;
    SELECT @IdTxConfirmarAviso = IdTransaccion FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxConfirmarAviso;
    SELECT @IdTxRegistrarInforme = IdTransaccion FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxRegistrarInforme;
    SELECT @IdTxCerrarIncidente = IdTransaccion FROM TbSitracTransaccion WHERE CodigoTransaccion = @TxCerrarIncidente;

    -- ==========================================================================
    -- 3. Perfiles
    -- ==========================================================================
    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfil WHERE CodigoPerfil = @PerfilAcceso)
        INSERT INTO TbSitracPerfil (CodigoPerfil, Descripcion)
        VALUES (@PerfilAcceso, 'Permite al usuario autenticarse en el sistema y obtener un token de acceso.');

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfil WHERE CodigoPerfil = @PerfilUsuarios)
        INSERT INTO TbSitracPerfil (CodigoPerfil, Descripcion)
        VALUES (@PerfilUsuarios, 'Permite al administrador registrar nuevos usuarios en el sistema.');

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfil WHERE CodigoPerfil = @PerfilConsultar)
        INSERT INTO TbSitracPerfil (CodigoPerfil, Descripcion)
        VALUES (@PerfilConsultar, 'Permite consultar la lista de incidentes, su detalle y su historial de cambios.');

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfil WHERE CodigoPerfil = @PerfilGestionar)
        INSERT INTO TbSitracPerfil (CodigoPerfil, Descripcion)
        VALUES (@PerfilGestionar, 'Permite confirmar avisos tempranos, registrar informes y cerrar incidentes.');

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfil WHERE CodigoPerfil = @PerfilIngestar)
        INSERT INTO TbSitracPerfil (CodigoPerfil, Descripcion)
        VALUES (@PerfilIngestar, 'Permite a INVGATE ingresar incidentes en el sistema.');

    SELECT @IdPerfilAcceso = IdPerfil FROM TbSitracPerfil WHERE CodigoPerfil = @PerfilAcceso;
    SELECT @IdPerfilUsuarios = IdPerfil FROM TbSitracPerfil WHERE CodigoPerfil = @PerfilUsuarios;
    SELECT @IdPerfilConsultar = IdPerfil FROM TbSitracPerfil WHERE CodigoPerfil = @PerfilConsultar;
    SELECT @IdPerfilGestionar = IdPerfil FROM TbSitracPerfil WHERE CodigoPerfil = @PerfilGestionar;
    SELECT @IdPerfilIngestar = IdPerfil FROM TbSitracPerfil WHERE CodigoPerfil = @PerfilIngestar;

    -- ==========================================================================
    -- 4. Malla Perfil-Transaccion (cada transaccion en un unico perfil)
    -- ==========================================================================
    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfilTransaccion WHERE IdPerfil = @IdPerfilAcceso AND IdTransaccion = @IdTxIniciarSesion)
        INSERT INTO TbSitracPerfilTransaccion (IdPerfil, IdTransaccion)
        VALUES (@IdPerfilAcceso, @IdTxIniciarSesion);

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfilTransaccion WHERE IdPerfil = @IdPerfilUsuarios AND IdTransaccion = @IdTxRegistrarUsuario)
        INSERT INTO TbSitracPerfilTransaccion (IdPerfil, IdTransaccion)
        VALUES (@IdPerfilUsuarios, @IdTxRegistrarUsuario);

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfilTransaccion WHERE IdPerfil = @IdPerfilConsultar AND IdTransaccion = @IdTxConsultarIncidentes)
        INSERT INTO TbSitracPerfilTransaccion (IdPerfil, IdTransaccion)
        VALUES (@IdPerfilConsultar, @IdTxConsultarIncidentes);

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfilTransaccion WHERE IdPerfil = @IdPerfilConsultar AND IdTransaccion = @IdTxConsultarIncidente)
        INSERT INTO TbSitracPerfilTransaccion (IdPerfil, IdTransaccion)
        VALUES (@IdPerfilConsultar, @IdTxConsultarIncidente);

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfilTransaccion WHERE IdPerfil = @IdPerfilConsultar AND IdTransaccion = @IdTxConsultarHistorial)
        INSERT INTO TbSitracPerfilTransaccion (IdPerfil, IdTransaccion)
        VALUES (@IdPerfilConsultar, @IdTxConsultarHistorial);

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfilTransaccion WHERE IdPerfil = @IdPerfilGestionar AND IdTransaccion = @IdTxConfirmarAviso)
        INSERT INTO TbSitracPerfilTransaccion (IdPerfil, IdTransaccion)
        VALUES (@IdPerfilGestionar, @IdTxConfirmarAviso);

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfilTransaccion WHERE IdPerfil = @IdPerfilGestionar AND IdTransaccion = @IdTxRegistrarInforme)
        INSERT INTO TbSitracPerfilTransaccion (IdPerfil, IdTransaccion)
        VALUES (@IdPerfilGestionar, @IdTxRegistrarInforme);

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfilTransaccion WHERE IdPerfil = @IdPerfilGestionar AND IdTransaccion = @IdTxCerrarIncidente)
        INSERT INTO TbSitracPerfilTransaccion (IdPerfil, IdTransaccion)
        VALUES (@IdPerfilGestionar, @IdTxCerrarIncidente);

    IF NOT EXISTS (SELECT 1 FROM TbSitracPerfilTransaccion WHERE IdPerfil = @IdPerfilIngestar AND IdTransaccion = @IdTxIngestarIncidente)
        INSERT INTO TbSitracPerfilTransaccion (IdPerfil, IdTransaccion)
        VALUES (@IdPerfilIngestar, @IdTxIngestarIncidente);

    -- ==========================================================================
    -- 5. Roles
    -- ==========================================================================
    IF NOT EXISTS (SELECT 1 FROM TbSitracRol WHERE NombreRol = 'Administrador SITRAC')
        INSERT INTO TbSitracRol (NombreRol, Descripcion)
        VALUES ('Administrador SITRAC', 'Acceso total al sistema SITRAC.');

    IF NOT EXISTS (SELECT 1 FROM TbSitracRol WHERE NombreRol = 'Analista de Ciberseguridad')
        INSERT INTO TbSitracRol (NombreRol, Descripcion)
        VALUES ('Analista de Ciberseguridad', 'Gestión operativa del ciclo de incidentes.');

    IF NOT EXISTS (SELECT 1 FROM TbSitracRol WHERE NombreRol = 'Auditor de Incidentes')
        INSERT INTO TbSitracRol (NombreRol, Descripcion)
        VALUES ('Auditor de Incidentes', 'Solo lectura para auditoría.');

    IF NOT EXISTS (SELECT 1 FROM TbSitracRol WHERE NombreRol = 'Cuenta de Servicio INVGATE')
        INSERT INTO TbSitracRol (NombreRol, Descripcion)
        VALUES ('Cuenta de Servicio INVGATE', 'Integración automática desde INVGATE.');

    SELECT @IdRolAdministrador = IdRol FROM TbSitracRol WHERE NombreRol = 'Administrador SITRAC';
    SELECT @IdRolAnalista = IdRol FROM TbSitracRol WHERE NombreRol = 'Analista de Ciberseguridad';
    SELECT @IdRolAuditor = IdRol FROM TbSitracRol WHERE NombreRol = 'Auditor de Incidentes';
    SELECT @IdRolInvgate = IdRol FROM TbSitracRol WHERE NombreRol = 'Cuenta de Servicio INVGATE';

    -- ==========================================================================
    -- 6. Malla Rol-Perfil
    -- ==========================================================================
    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolAdministrador AND IdPerfil = @IdPerfilAcceso)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolAdministrador, @IdPerfilAcceso);
    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolAdministrador AND IdPerfil = @IdPerfilUsuarios)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolAdministrador, @IdPerfilUsuarios);
    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolAdministrador AND IdPerfil = @IdPerfilConsultar)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolAdministrador, @IdPerfilConsultar);
    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolAdministrador AND IdPerfil = @IdPerfilGestionar)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolAdministrador, @IdPerfilGestionar);
    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolAdministrador AND IdPerfil = @IdPerfilIngestar)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolAdministrador, @IdPerfilIngestar);

    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolAnalista AND IdPerfil = @IdPerfilAcceso)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolAnalista, @IdPerfilAcceso);
    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolAnalista AND IdPerfil = @IdPerfilConsultar)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolAnalista, @IdPerfilConsultar);
    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolAnalista AND IdPerfil = @IdPerfilGestionar)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolAnalista, @IdPerfilGestionar);

    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolAuditor AND IdPerfil = @IdPerfilAcceso)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolAuditor, @IdPerfilAcceso);
    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolAuditor AND IdPerfil = @IdPerfilConsultar)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolAuditor, @IdPerfilConsultar);

    IF NOT EXISTS (SELECT 1 FROM TbSitracRolPerfil WHERE IdRol = @IdRolInvgate AND IdPerfil = @IdPerfilIngestar)
        INSERT INTO TbSitracRolPerfil (IdRol, IdPerfil) VALUES (@IdRolInvgate, @IdPerfilIngestar);

    -- ==========================================================================
    -- 7. Migracion de usuarios existentes con Rol = 'Admin'
    -- ==========================================================================
    INSERT INTO TbSitracUsuarioRol (IdUsuario, IdRol)
    SELECT u.IdUsuario, @IdRolAdministrador
    FROM TbSitracUsuario u
    WHERE u.Rol = 'Admin'
      AND u.AuditNotDeleted = 1
      AND NOT EXISTS (
          SELECT 1
          FROM TbSitracUsuarioRol ur
          WHERE ur.IdUsuario = u.IdUsuario
            AND ur.IdRol = @IdRolAdministrador
      );

    -- ==========================================================================
    -- Verificacion
    -- ==========================================================================
    IF (SELECT COUNT(*) FROM TbSitracTransaccion WHERE AuditNotDeleted = 1) <> 9
        THROW 51001, 'V0009 fallo: se esperaban 9 transacciones', 1;

    IF (SELECT COUNT(*) FROM TbSitracPerfil WHERE AuditNotDeleted = 1) <> 5
        THROW 51002, 'V0009 fallo: se esperaban 5 perfiles', 1;

    IF (SELECT COUNT(*) FROM TbSitracRol WHERE AuditNotDeleted = 1) <> 4
        THROW 51003, 'V0009 fallo: se esperaban 4 roles', 1;

    IF (SELECT COUNT(*) FROM TbSitracPerfilTransaccion WHERE AuditNotDeleted = 1) <> 9
        THROW 51004, 'V0009 fallo: se esperaban 9 asignaciones Perfil-Transaccion', 1;

    IF EXISTS (
        SELECT 1
        FROM TbSitracTransaccion t
        LEFT JOIN TbSitracPerfilTransaccion pt
            ON pt.IdTransaccion = t.IdTransaccion
           AND pt.AuditNotDeleted = 1
        WHERE t.AuditNotDeleted = 1
        GROUP BY t.IdTransaccion
        HAVING COUNT(pt.IdPerfil) <> 1
    )
        THROW 51005, 'V0009 fallo: cada transaccion debe pertenecer a un unico perfil activo', 1;

    COMMIT TRANSACTION;
    PRINT '>> Migración V0009 aplicada: seed de transacciones, perfiles y roles.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
