-- ==============================================================================
-- SISTEMA: SITRAC-ANCI
-- OBJETIVO: Inicialización de catálogos base y tabla principal de incidentes (Modelo v3)
-- ==============================================================================

CREATE TABLE TbAnciTipoClasificacionEmpresa (
    IdTipoClasificacionEmpresa BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoClasificacionEmpresa VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoClasificacionEmpresa_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoClasificacionEmpresa_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoClasificacionEmpresa_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoClasificacionEmpresa_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoClasificacionEmpresa_IdTipoClasificacionEmpresa PRIMARY KEY (IdTipoClasificacionEmpresa)
);

CREATE TABLE TbAnciTipoDatosPersonales (
    IdTipoDatosPersonales BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoDatosPersonales VARCHAR(20) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoDatosPersonales_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoDatosPersonales_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoDatosPersonales_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoDatosPersonales_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoDatosPersonales_IdTipoDatosPersonales PRIMARY KEY (IdTipoDatosPersonales)
);

CREATE TABLE TbAnciTipoCanalEntrada (
    IdTipoCanalEntrada BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoCanalEntrada VARCHAR(100) NOT NULL,
    DescripcionTipoCanalEntrada VARCHAR(255) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoCanalEntrada_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoCanalEntrada_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoCanalEntrada_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoCanalEntrada_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoCanalEntrada_IdTipoCanalEntrada PRIMARY KEY (IdTipoCanalEntrada)
);

CREATE TABLE TbAnciTipoEstadoIncidente (
    IdTipoEstadoIncidente BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoEstadoIncidente VARCHAR(50) NOT NULL,
    OrdenTipoEstadoIncidente INT NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoEstadoIncidente_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoEstadoIncidente_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoEstadoIncidente_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoEstadoIncidente_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoEstadoIncidente_IdTipoEstadoIncidente PRIMARY KEY (IdTipoEstadoIncidente)
);

CREATE TABLE TbAnciTipoGravedad (
    IdTipoGravedad BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoGravedad VARCHAR(50) NOT NULL,
    OrdenTipoGravedad INT NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoGravedad_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoGravedad_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoGravedad_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoGravedad_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoGravedad_IdTipoGravedad PRIMARY KEY (IdTipoGravedad)
);

CREATE TABLE TbAnciIncidente (
    IdIncidente BIGINT IDENTITY(1,1) NOT NULL,
    FechaTomaConocimiento DATETIME NOT NULL,
    FechaRegistroSistema DATETIME NOT NULL CONSTRAINT DF_TbAnciIncidente_FechaRegistroSistema DEFAULT GETDATE(),
    CodTipoCanalEntrada BIGINT NOT NULL,
    CodigoTicketInvgate VARCHAR(100) NULL,
    IndEsIncidenteReal BIT NOT NULL CONSTRAINT DF_TbAnciIncidente_IndEsIncidenteReal DEFAULT 1,
    CodTipoDatosPersonales BIGINT NOT NULL,
    CodTipoClasificacionEmpresa BIGINT NOT NULL,
    CodTipoGravedad BIGINT NOT NULL,
    CodTipoEstadoIncidente BIGINT NOT NULL,
    DescripcionSistemaAfectado VARCHAR(255) NULL,
    DescripcionVectorAtaque VARCHAR(500) NULL,
    DescripcionOrigenDeteccion VARCHAR(500) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciIncidente_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciIncidente_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciIncidente_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciIncidente_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciIncidente_IdIncidente PRIMARY KEY (IdIncidente),
    CONSTRAINT FK_TbAnciIncidente_TbAnciTipoCanalEntrada FOREIGN KEY (CodTipoCanalEntrada) REFERENCES TbAnciTipoCanalEntrada(IdTipoCanalEntrada),
    CONSTRAINT FK_TbAnciIncidente_TbAnciTipoDatosPersonales FOREIGN KEY (CodTipoDatosPersonales) REFERENCES TbAnciTipoDatosPersonales(IdTipoDatosPersonales),
    CONSTRAINT FK_TbAnciIncidente_TbAnciTipoClasificacionEmpresa FOREIGN KEY (CodTipoClasificacionEmpresa) REFERENCES TbAnciTipoClasificacionEmpresa(IdTipoClasificacionEmpresa),
    CONSTRAINT FK_TbAnciIncidente_TbAnciTipoGravedad FOREIGN KEY (CodTipoGravedad) REFERENCES TbAnciTipoGravedad(IdTipoGravedad),
    CONSTRAINT FK_TbAnciIncidente_TbAnciTipoEstadoIncidente FOREIGN KEY (CodTipoEstadoIncidente) REFERENCES TbAnciTipoEstadoIncidente(IdTipoEstadoIncidente)
);