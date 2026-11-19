-- ==============================================================================
-- SISTEMA: SITRAC
-- OBJETIVO: Estructura de modulos, transacciones, perfiles y roles (ATLAS)
-- NOTA: El prefijo de sistema es provisorio y se parametriza en V0009.
-- ==============================================================================

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- ==========================================================================
    -- TABLA 0: Modulos del sistema
    -- ==========================================================================
    CREATE TABLE TbSitracModulo (
        IdModulo BIGINT IDENTITY(1,1) NOT NULL,
        CodigoModulo VARCHAR(10) NOT NULL,
        NombreModulo VARCHAR(100) NOT NULL,
        AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracModulo_AuditNotDeleted DEFAULT 1,
        AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracModulo_AuditCreateDate DEFAULT GETUTCDATE(),
        AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracModulo_AuditLastUpdateDate DEFAULT GETUTCDATE(),
        AuditUserLastUpdate VARCHAR(100) NULL,
        CONSTRAINT PK_TbSitracModulo PRIMARY KEY CLUSTERED (IdModulo ASC),
        CONSTRAINT UQ_TbSitracModulo_CodigoModulo UNIQUE (CodigoModulo)
    );

    -- ==========================================================================
    -- TABLA 1: Catalogo de transacciones
    -- ==========================================================================
    CREATE TABLE TbSitracTransaccion (
        IdTransaccion BIGINT IDENTITY(1,1) NOT NULL,
        CodigoTransaccion VARCHAR(50) NOT NULL,
        Descripcion VARCHAR(500) NOT NULL,
        IdModulo BIGINT NOT NULL,
        AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTransaccion_AuditNotDeleted DEFAULT 1,
        AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTransaccion_AuditCreateDate DEFAULT GETUTCDATE(),
        AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTransaccion_AuditLastUpdateDate DEFAULT GETUTCDATE(),
        AuditUserLastUpdate VARCHAR(100) NULL,
        CONSTRAINT PK_TbSitracTransaccion PRIMARY KEY CLUSTERED (IdTransaccion ASC),
        CONSTRAINT UQ_TbSitracTransaccion_CodigoTransaccion UNIQUE (CodigoTransaccion),
        CONSTRAINT FK_TbSitracTransaccion_Modulo FOREIGN KEY (IdModulo) REFERENCES TbSitracModulo (IdModulo),
        CONSTRAINT CK_TbSitracTransaccion_Longitud CHECK (LEN(CodigoTransaccion) <= 50),
        CONSTRAINT CK_TbSitracTransaccion_Mayusculas CHECK (CodigoTransaccion = UPPER(CodigoTransaccion)),
        CONSTRAINT CK_TbSitracTransaccion_SinGestionar CHECK (CodigoTransaccion NOT LIKE '%GESTIONAR%'),
        CONSTRAINT CK_TbSitracTransaccion_SinSp CHECK (CodigoTransaccion NOT LIKE '%SP[_]%'),
        CONSTRAINT CK_TbSitracTransaccion_SinApi CHECK (CodigoTransaccion NOT LIKE '%API[_]%')
    );

    CREATE INDEX IX_TbSitracTransaccion_Modulo ON TbSitracTransaccion (IdModulo);

    -- ==========================================================================
    -- TABLA 2: Catalogo de perfiles
    -- ==========================================================================
    CREATE TABLE TbSitracPerfil (
        IdPerfil BIGINT IDENTITY(1,1) NOT NULL,
        CodigoPerfil VARCHAR(50) NOT NULL,
        Descripcion VARCHAR(500) NOT NULL,
        AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracPerfil_AuditNotDeleted DEFAULT 1,
        AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracPerfil_AuditCreateDate DEFAULT GETUTCDATE(),
        AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracPerfil_AuditLastUpdateDate DEFAULT GETUTCDATE(),
        AuditUserLastUpdate VARCHAR(100) NULL,
        CONSTRAINT PK_TbSitracPerfil PRIMARY KEY CLUSTERED (IdPerfil ASC),
        CONSTRAINT UQ_TbSitracPerfil_CodigoPerfil UNIQUE (CodigoPerfil),
        CONSTRAINT CK_TbSitracPerfil_Longitud CHECK (LEN(CodigoPerfil) <= 50)
    );

    -- ==========================================================================
    -- TABLA 3: Malla de transacciones y perfiles
    -- REGLA NORMATIVA: una transaccion pertenece a un solo perfil activo.
    -- ==========================================================================
    CREATE TABLE TbSitracPerfilTransaccion (
        IdPerfil BIGINT NOT NULL,
        IdTransaccion BIGINT NOT NULL,
        AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracPerfilTransaccion_AuditNotDeleted DEFAULT 1,
        AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracPerfilTransaccion_AuditCreateDate DEFAULT GETUTCDATE(),
        AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracPerfilTransaccion_AuditLastUpdateDate DEFAULT GETUTCDATE(),
        AuditUserLastUpdate VARCHAR(100) NULL,
        CONSTRAINT PK_TbSitracPerfilTransaccion PRIMARY KEY CLUSTERED (IdPerfil ASC, IdTransaccion ASC),
        CONSTRAINT FK_TbSitracPerfilTransaccion_Perfil FOREIGN KEY (IdPerfil) REFERENCES TbSitracPerfil (IdPerfil),
        CONSTRAINT FK_TbSitracPerfilTransaccion_Transaccion FOREIGN KEY (IdTransaccion) REFERENCES TbSitracTransaccion (IdTransaccion)
    );

    CREATE UNIQUE INDEX UX_TbSitracPerfilTransaccion_IdTransaccion_Activa
        ON TbSitracPerfilTransaccion (IdTransaccion)
        WHERE AuditNotDeleted = 1;

    -- ==========================================================================
    -- TABLA 4: Catalogo de roles
    -- ==========================================================================
    CREATE TABLE TbSitracRol (
        IdRol BIGINT IDENTITY(1,1) NOT NULL,
        NombreRol VARCHAR(50) NOT NULL,
        Descripcion VARCHAR(500) NOT NULL,
        AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracRol_AuditNotDeleted DEFAULT 1,
        AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracRol_AuditCreateDate DEFAULT GETUTCDATE(),
        AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracRol_AuditLastUpdateDate DEFAULT GETUTCDATE(),
        AuditUserLastUpdate VARCHAR(100) NULL,
        CONSTRAINT PK_TbSitracRol PRIMARY KEY CLUSTERED (IdRol ASC),
        CONSTRAINT UQ_TbSitracRol_NombreRol UNIQUE (NombreRol)
    );

    -- ==========================================================================
    -- TABLA 5: Malla de roles y perfiles
    -- ==========================================================================
    CREATE TABLE TbSitracRolPerfil (
        IdRol BIGINT NOT NULL,
        IdPerfil BIGINT NOT NULL,
        AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracRolPerfil_AuditNotDeleted DEFAULT 1,
        AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracRolPerfil_AuditCreateDate DEFAULT GETUTCDATE(),
        AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracRolPerfil_AuditLastUpdateDate DEFAULT GETUTCDATE(),
        AuditUserLastUpdate VARCHAR(100) NULL,
        CONSTRAINT PK_TbSitracRolPerfil PRIMARY KEY CLUSTERED (IdRol ASC, IdPerfil ASC),
        CONSTRAINT FK_TbSitracRolPerfil_Rol FOREIGN KEY (IdRol) REFERENCES TbSitracRol (IdRol),
        CONSTRAINT FK_TbSitracRolPerfil_Perfil FOREIGN KEY (IdPerfil) REFERENCES TbSitracPerfil (IdPerfil)
    );

    CREATE INDEX IX_TbSitracRolPerfil_Perfil ON TbSitracRolPerfil (IdPerfil);

    -- ==========================================================================
    -- TABLA 6: Asignacion de roles a usuarios
    -- ==========================================================================
    CREATE TABLE TbSitracUsuarioRol (
        IdUsuario BIGINT NOT NULL,
        IdRol BIGINT NOT NULL,
        AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracUsuarioRol_AuditNotDeleted DEFAULT 1,
        AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracUsuarioRol_AuditCreateDate DEFAULT GETUTCDATE(),
        AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracUsuarioRol_AuditLastUpdateDate DEFAULT GETUTCDATE(),
        AuditUserLastUpdate VARCHAR(100) NULL,
        CONSTRAINT PK_TbSitracUsuarioRol PRIMARY KEY CLUSTERED (IdUsuario ASC, IdRol ASC),
        CONSTRAINT FK_TbSitracUsuarioRol_Usuario FOREIGN KEY (IdUsuario) REFERENCES TbSitracUsuario (IdUsuario),
        CONSTRAINT FK_TbSitracUsuarioRol_Rol FOREIGN KEY (IdRol) REFERENCES TbSitracRol (IdRol)
    );

    CREATE INDEX IX_TbSitracUsuarioRol_Rol ON TbSitracUsuarioRol (IdRol);

    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TbSitracTransaccion')
        THROW 51000, 'V0008 fallo: TbSitracTransaccion no existe', 1;

    COMMIT TRANSACTION;
    PRINT '>> Migración V0008 aplicada: estructura de transacciones, perfiles y roles.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
