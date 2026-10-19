-- ==============================================================================
-- SISTEMA: SITRAC
-- OBJETIVO: Inicialización de catálogos base y tabla principal de incidentes (Modelo v3)
-- ==============================================================================

CREATE TABLE TbSitracTipoClasificacionEmpresa (
    IdTipoClasificacionEmpresa BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoClasificacionEmpresa VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoClasificacionEmpresa_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoClasificacionEmpresa_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoClasificacionEmpresa_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoClasificacionEmpresa_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoClasificacionEmpresa_IdTipoClasificacionEmpresa PRIMARY KEY (IdTipoClasificacionEmpresa)
);

CREATE TABLE TbSitracTipoDatosPersonales (
    IdTipoDatosPersonales BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoDatosPersonales VARCHAR(20) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoDatosPersonales_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoDatosPersonales_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoDatosPersonales_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoDatosPersonales_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoDatosPersonales_IdTipoDatosPersonales PRIMARY KEY (IdTipoDatosPersonales)
);

CREATE TABLE TbSitracTipoCanalEntrada (
    IdTipoCanalEntrada BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoCanalEntrada VARCHAR(100) NOT NULL,
    DescripcionTipoCanalEntrada VARCHAR(255) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoCanalEntrada_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoCanalEntrada_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoCanalEntrada_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoCanalEntrada_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoCanalEntrada_IdTipoCanalEntrada PRIMARY KEY (IdTipoCanalEntrada)
);

CREATE TABLE TbSitracTipoEstadoIncidente (
    IdTipoEstadoIncidente BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoEstadoIncidente VARCHAR(50) NOT NULL,
    OrdenTipoEstadoIncidente INT NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoEstadoIncidente_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoEstadoIncidente_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoEstadoIncidente_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoEstadoIncidente_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoEstadoIncidente_IdTipoEstadoIncidente PRIMARY KEY (IdTipoEstadoIncidente)
);

CREATE TABLE TbSitracTipoGravedad (
    IdTipoGravedad BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoGravedad VARCHAR(50) NOT NULL,
    OrdenTipoGravedad INT NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoGravedad_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoGravedad_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoGravedad_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoGravedad_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoGravedad_IdTipoGravedad PRIMARY KEY (IdTipoGravedad)
);

CREATE TABLE TbSitracIncidente (
    IdIncidente BIGINT IDENTITY(1,1) NOT NULL,
    FechaTomaConocimiento DATETIME NOT NULL,
    FechaRegistroSistema DATETIME NOT NULL CONSTRAINT DF_TbSitracIncidente_FechaRegistroSistema DEFAULT GETDATE(),
    CodTipoCanalEntrada BIGINT NOT NULL,
    CodigoTicketInvgate VARCHAR(100) NULL,
    IndEsIncidenteReal BIT NOT NULL CONSTRAINT DF_TbSitracIncidente_IndEsIncidenteReal DEFAULT 1,
    CodTipoDatosPersonales BIGINT NOT NULL,
    CodTipoClasificacionEmpresa BIGINT NOT NULL,
    CodTipoGravedad BIGINT NOT NULL,
    CodTipoEstadoIncidente BIGINT NOT NULL,
    DescripcionSistemaAfectado VARCHAR(255) NULL,
    DescripcionVectorAtaque VARCHAR(500) NULL,
    DescripcionOrigenDeteccion VARCHAR(500) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracIncidente_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracIncidente_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracIncidente_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracIncidente_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracIncidente_IdIncidente PRIMARY KEY (IdIncidente),
    CONSTRAINT FK_TbSitracIncidente_TbSitracTipoCanalEntrada FOREIGN KEY (CodTipoCanalEntrada) REFERENCES TbSitracTipoCanalEntrada(IdTipoCanalEntrada),
    CONSTRAINT FK_TbSitracIncidente_TbSitracTipoDatosPersonales FOREIGN KEY (CodTipoDatosPersonales) REFERENCES TbSitracTipoDatosPersonales(IdTipoDatosPersonales),
    CONSTRAINT FK_TbSitracIncidente_TbSitracTipoClasificacionEmpresa FOREIGN KEY (CodTipoClasificacionEmpresa) REFERENCES TbSitracTipoClasificacionEmpresa(IdTipoClasificacionEmpresa),
    CONSTRAINT FK_TbSitracIncidente_TbSitracTipoGravedad FOREIGN KEY (CodTipoGravedad) REFERENCES TbSitracTipoGravedad(IdTipoGravedad),
    CONSTRAINT FK_TbSitracIncidente_TbSitracTipoEstadoIncidente FOREIGN KEY (CodTipoEstadoIncidente) REFERENCES TbSitracTipoEstadoIncidente(IdTipoEstadoIncidente)
);