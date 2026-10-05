-- ==============================================================================
-- SISTEMA: SITRAC-ANCI
-- OBJETIVO: Creación de tablas operacionales y de flujo (Responsables, SLAs, Comunicaciones) v3
-- ==============================================================================

CREATE TABLE TbAnciTipoPlazoSla (
    IdTipoPlazoSla BIGINT IDENTITY(1,1) NOT NULL,
    CodTipoClasificacionEmpresa BIGINT NOT NULL,
    HorasAvisoTemprano INT NOT NULL,
    HorasInformePreliminar INT NOT NULL,
    DiasInformeFinal INT NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoPlazoSla_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoPlazoSla_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoPlazoSla_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoPlazoSla_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoPlazoSla_IdTipoPlazoSla PRIMARY KEY (IdTipoPlazoSla),
    CONSTRAINT FK_TbAnciTipoPlazoSla_TbAnciTipoClasificacionEmpresa FOREIGN KEY (CodTipoClasificacionEmpresa) REFERENCES TbAnciTipoClasificacionEmpresa(IdTipoClasificacionEmpresa)
);

CREATE TABLE TbAnciIncidentePlazoSla (
    IdIncidente BIGINT NOT NULL,
    IdTipoPlazoSla BIGINT NOT NULL,
    FechaTomaConocimiento DATETIME NOT NULL,
    FechaLimiteAvisoTemprano DATETIME NOT NULL,
    FechaLimiteInformePreliminar DATETIME NOT NULL,
    FechaLimiteInformeFinal DATETIME NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciIncidentePlazoSla_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciIncidentePlazoSla_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciIncidentePlazoSla_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciIncidentePlazoSla_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciIncidentePlazoSla_IdIncidente PRIMARY KEY (IdIncidente),
    CONSTRAINT FK_TbAnciIncidentePlazoSla_TbAnciIncidente FOREIGN KEY (IdIncidente) REFERENCES TbAnciIncidente(IdIncidente),
    CONSTRAINT FK_TbAnciIncidentePlazoSla_TbAnciTipoPlazoSla FOREIGN KEY (IdTipoPlazoSla) REFERENCES TbAnciTipoPlazoSla(IdTipoPlazoSla)
);

CREATE TABLE TbAnciResponsable (
    IdResponsable BIGINT IDENTITY(1,1) NOT NULL,
    NombreResponsable VARCHAR(150) NOT NULL,
    EmailResponsable VARCHAR(150) NOT NULL,
    TelefonoResponsable VARCHAR(50) NULL,
    DescripcionRolOrganizacional VARCHAR(100) NULL,
    IndEsDelegadoCiberseguridad BIT NOT NULL CONSTRAINT DF_TbAnciResponsable_IndEsDelegado DEFAULT 0,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciResponsable_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciResponsable_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciResponsable_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciResponsable_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciResponsable_IdResponsable PRIMARY KEY (IdResponsable)
);

CREATE TABLE TbAnciTipoComunicacion (
    IdTipoComunicacion BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoComunicacion VARCHAR(100) NOT NULL,
    CodigoAnci VARCHAR(100) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoComunicacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoComunicacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoComunicacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoComunicacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoComunicacion_IdTipoComunicacion PRIMARY KEY (IdTipoComunicacion)
);

CREATE TABLE TbAnciComunicacion (
    IdComunicacion BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    CodTipoComunicacion BIGINT NOT NULL,
    FechaHora DATETIME NOT NULL,
    IdResponsableEnvia BIGINT NOT NULL,
    ReferenciaArchivoAdjunto VARCHAR(500) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciComunicacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciComunicacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciComunicacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciComunicacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciComunicacion_IdComunicacion PRIMARY KEY (IdComunicacion),
    CONSTRAINT FK_TbAnciComunicacion_TbAnciIncidente FOREIGN KEY (IdIncidente) REFERENCES TbAnciIncidente(IdIncidente),
    CONSTRAINT FK_TbAnciComunicacion_TbAnciTipoComunicacion FOREIGN KEY (CodTipoComunicacion) REFERENCES TbAnciTipoComunicacion(IdTipoComunicacion),
    CONSTRAINT FK_TbAnciComunicacion_TbAnciResponsable FOREIGN KEY (IdResponsableEnvia) REFERENCES TbAnciResponsable(IdResponsable)
);

CREATE TABLE TbAnciAprobacion (
    IdAprobacion BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    IdResponsable BIGINT NOT NULL,
    IdComunicacion BIGINT NULL,
    DescripcionEtapa VARCHAR(100) NOT NULL,
    FechaHora DATETIME NOT NULL,
    Comentario VARCHAR(500) NULL,
    IndEsAprobacionValida BIT NOT NULL CONSTRAINT DF_TbAnciAprobacion_IndEsValida DEFAULT 1,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciAprobacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciAprobacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciAprobacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciAprobacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciAprobacion_IdAprobacion PRIMARY KEY (IdAprobacion),
    CONSTRAINT FK_TbAnciAprobacion_TbAnciIncidente FOREIGN KEY (IdIncidente) REFERENCES TbAnciIncidente(IdIncidente),
    CONSTRAINT FK_TbAnciAprobacion_TbAnciResponsable FOREIGN KEY (IdResponsable) REFERENCES TbAnciResponsable(IdResponsable),
    CONSTRAINT FK_TbAnciAprobacion_TbAnciComunicacion FOREIGN KEY (IdComunicacion) REFERENCES TbAnciComunicacion(IdComunicacion)
);

CREATE TABLE TbAnciTipoCanalNotificacion (
    IdTipoCanalNotificacion BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoCanalNotificacion VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoCanalNotificacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoCanalNotificacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoCanalNotificacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoCanalNotificacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoCanalNotificacion_IdTipoCanalNotificacion PRIMARY KEY (IdTipoCanalNotificacion)
);

CREATE TABLE TbAnciTipoNotificacion (
    IdTipoNotificacion BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoNotificacion VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoNotificacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoNotificacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoNotificacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoNotificacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoNotificacion_IdTipoNotificacion PRIMARY KEY (IdTipoNotificacion)
);

CREATE TABLE TbAnciNotificacion (
    IdNotificacion BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    IdResponsable BIGINT NOT NULL,
    CodTipoCanalNotificacion BIGINT NOT NULL,
    CodTipoNotificacion BIGINT NOT NULL,
    FechaHoraEnvio DATETIME NOT NULL,
    IntervaloMinutos INT NOT NULL,
    IndDetenida BIT NOT NULL CONSTRAINT DF_TbAnciNotificacion_IndDetenida DEFAULT 0,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciNotificacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciNotificacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciNotificacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciNotificacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciNotificacion_IdNotificacion PRIMARY KEY (IdNotificacion),
    CONSTRAINT FK_TbAnciNotificacion_TbAnciIncidente FOREIGN KEY (IdIncidente) REFERENCES TbAnciIncidente(IdIncidente),
    CONSTRAINT FK_TbAnciNotificacion_TbAnciResponsable FOREIGN KEY (IdResponsable) REFERENCES TbAnciResponsable(IdResponsable),
    CONSTRAINT FK_TbAnciNotificacion_TbAnciTipoCanalNotificacion FOREIGN KEY (CodTipoCanalNotificacion) REFERENCES TbAnciTipoCanalNotificacion(IdTipoCanalNotificacion),
    CONSTRAINT FK_TbAnciNotificacion_TbAnciTipoNotificacion FOREIGN KEY (CodTipoNotificacion) REFERENCES TbAnciTipoNotificacion(IdTipoNotificacion)
);

CREATE TABLE TbAnciFalsoPositivo (
    IdFalsoPositivo BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    DescripcionJustificacion VARCHAR(MAX) NOT NULL,
    IdResponsableRegistra BIGINT NOT NULL,
    FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TbAnciFalsoPositivo_FechaRegistro DEFAULT GETDATE(),
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciFalsoPositivo_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciFalsoPositivo_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciFalsoPositivo_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciFalsoPositivo_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciFalsoPositivo_IdFalsoPositivo PRIMARY KEY (IdFalsoPositivo),
    CONSTRAINT IUQ_TbAnciFalsoPositivo_IdIncidente UNIQUE (IdIncidente),
    CONSTRAINT FK_TbAnciFalsoPositivo_TbAnciIncidente FOREIGN KEY (IdIncidente) REFERENCES TbAnciIncidente(IdIncidente),
    CONSTRAINT FK_TbAnciFalsoPositivo_TbAnciResponsable FOREIGN KEY (IdResponsableRegistra) REFERENCES TbAnciResponsable(IdResponsable)
);