-- ==============================================================================
-- SISTEMA: SITRAC
-- OBJETIVO: Creación de tablas operacionales y de flujo (Responsables, SLAs, Comunicaciones) v3
-- ==============================================================================

CREATE TABLE TbSitracTipoPlazoSla (
    IdTipoPlazoSla BIGINT IDENTITY(1,1) NOT NULL,
    CodTipoClasificacionEmpresa BIGINT NOT NULL,
    HorasAvisoTemprano INT NOT NULL,
    HorasInformePreliminar INT NOT NULL,
    DiasInformeFinal INT NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoPlazoSla_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoPlazoSla_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoPlazoSla_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoPlazoSla_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoPlazoSla_IdTipoPlazoSla PRIMARY KEY (IdTipoPlazoSla),
    CONSTRAINT FK_TbSitracTipoPlazoSla_TbSitracTipoClasificacionEmpresa FOREIGN KEY (CodTipoClasificacionEmpresa) REFERENCES TbSitracTipoClasificacionEmpresa(IdTipoClasificacionEmpresa)
);

CREATE TABLE TbSitracIncidentePlazoSla (
    IdIncidente BIGINT NOT NULL,
    IdTipoPlazoSla BIGINT NOT NULL,
    FechaTomaConocimiento DATETIME NOT NULL,
    FechaLimiteAvisoTemprano DATETIME NOT NULL,
    FechaLimiteInformePreliminar DATETIME NOT NULL,
    FechaLimiteInformeFinal DATETIME NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracIncidentePlazoSla_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracIncidentePlazoSla_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracIncidentePlazoSla_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracIncidentePlazoSla_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracIncidentePlazoSla_IdIncidente PRIMARY KEY (IdIncidente),
    CONSTRAINT FK_TbSitracIncidentePlazoSla_TbSitracIncidente FOREIGN KEY (IdIncidente) REFERENCES TbSitracIncidente(IdIncidente),
    CONSTRAINT FK_TbSitracIncidentePlazoSla_TbSitracTipoPlazoSla FOREIGN KEY (IdTipoPlazoSla) REFERENCES TbSitracTipoPlazoSla(IdTipoPlazoSla)
);

CREATE TABLE TbSitracResponsable (
    IdResponsable BIGINT IDENTITY(1,1) NOT NULL,
    NombreResponsable VARCHAR(150) NOT NULL,
    EmailResponsable VARCHAR(150) NOT NULL,
    TelefonoResponsable VARCHAR(50) NULL,
    DescripcionRolOrganizacional VARCHAR(100) NULL,
    IndEsDelegadoCiberseguridad BIT NOT NULL CONSTRAINT DF_TbSitracResponsable_IndEsDelegado DEFAULT 0,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracResponsable_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracResponsable_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracResponsable_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracResponsable_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracResponsable_IdResponsable PRIMARY KEY (IdResponsable)
);

CREATE TABLE TbSitracTipoComunicacion (
    IdTipoComunicacion BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoComunicacion VARCHAR(100) NOT NULL,
    CodigoSitrac VARCHAR(100) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoComunicacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoComunicacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoComunicacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoComunicacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoComunicacion_IdTipoComunicacion PRIMARY KEY (IdTipoComunicacion)
);

CREATE TABLE TbSitracComunicacion (
    IdComunicacion BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    CodTipoComunicacion BIGINT NOT NULL,
    FechaHora DATETIME NOT NULL,
    IdResponsableEnvia BIGINT NOT NULL,
    ReferenciaArchivoAdjunto VARCHAR(500) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracComunicacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracComunicacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracComunicacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracComunicacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracComunicacion_IdComunicacion PRIMARY KEY (IdComunicacion),
    CONSTRAINT FK_TbSitracComunicacion_TbSitracIncidente FOREIGN KEY (IdIncidente) REFERENCES TbSitracIncidente(IdIncidente),
    CONSTRAINT FK_TbSitracComunicacion_TbSitracTipoComunicacion FOREIGN KEY (CodTipoComunicacion) REFERENCES TbSitracTipoComunicacion(IdTipoComunicacion),
    CONSTRAINT FK_TbSitracComunicacion_TbSitracResponsable FOREIGN KEY (IdResponsableEnvia) REFERENCES TbSitracResponsable(IdResponsable)
);

CREATE TABLE TbSitracAprobacion (
    IdAprobacion BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    IdResponsable BIGINT NOT NULL,
    IdComunicacion BIGINT NULL,
    DescripcionEtapa VARCHAR(100) NOT NULL,
    FechaHora DATETIME NOT NULL,
    Comentario VARCHAR(500) NULL,
    IndEsAprobacionValida BIT NOT NULL CONSTRAINT DF_TbSitracAprobacion_IndEsValida DEFAULT 1,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracAprobacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracAprobacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracAprobacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracAprobacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracAprobacion_IdAprobacion PRIMARY KEY (IdAprobacion),
    CONSTRAINT FK_TbSitracAprobacion_TbSitracIncidente FOREIGN KEY (IdIncidente) REFERENCES TbSitracIncidente(IdIncidente),
    CONSTRAINT FK_TbSitracAprobacion_TbSitracResponsable FOREIGN KEY (IdResponsable) REFERENCES TbSitracResponsable(IdResponsable),
    CONSTRAINT FK_TbSitracAprobacion_TbSitracComunicacion FOREIGN KEY (IdComunicacion) REFERENCES TbSitracComunicacion(IdComunicacion)
);

CREATE TABLE TbSitracTipoCanalNotificacion (
    IdTipoCanalNotificacion BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoCanalNotificacion VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoCanalNotificacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoCanalNotificacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoCanalNotificacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoCanalNotificacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoCanalNotificacion_IdTipoCanalNotificacion PRIMARY KEY (IdTipoCanalNotificacion)
);

CREATE TABLE TbSitracTipoNotificacion (
    IdTipoNotificacion BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoNotificacion VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoNotificacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoNotificacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoNotificacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoNotificacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoNotificacion_IdTipoNotificacion PRIMARY KEY (IdTipoNotificacion)
);

CREATE TABLE TbSitracNotificacion (
    IdNotificacion BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    IdResponsable BIGINT NOT NULL,
    CodTipoCanalNotificacion BIGINT NOT NULL,
    CodTipoNotificacion BIGINT NOT NULL,
    FechaHoraEnvio DATETIME NOT NULL,
    IntervaloMinutos INT NOT NULL,
    IndDetenida BIT NOT NULL CONSTRAINT DF_TbSitracNotificacion_IndDetenida DEFAULT 0,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracNotificacion_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracNotificacion_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracNotificacion_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracNotificacion_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracNotificacion_IdNotificacion PRIMARY KEY (IdNotificacion),
    CONSTRAINT FK_TbSitracNotificacion_TbSitracIncidente FOREIGN KEY (IdIncidente) REFERENCES TbSitracIncidente(IdIncidente),
    CONSTRAINT FK_TbSitracNotificacion_TbSitracResponsable FOREIGN KEY (IdResponsable) REFERENCES TbSitracResponsable(IdResponsable),
    CONSTRAINT FK_TbSitracNotificacion_TbSitracTipoCanalNotificacion FOREIGN KEY (CodTipoCanalNotificacion) REFERENCES TbSitracTipoCanalNotificacion(IdTipoCanalNotificacion),
    CONSTRAINT FK_TbSitracNotificacion_TbSitracTipoNotificacion FOREIGN KEY (CodTipoNotificacion) REFERENCES TbSitracTipoNotificacion(IdTipoNotificacion)
);

CREATE TABLE TbSitracFalsoPositivo (
    IdFalsoPositivo BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    DescripcionJustificacion VARCHAR(MAX) NOT NULL,
    IdResponsableRegistra BIGINT NOT NULL,
    FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TbSitracFalsoPositivo_FechaRegistro DEFAULT GETDATE(),
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracFalsoPositivo_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracFalsoPositivo_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracFalsoPositivo_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracFalsoPositivo_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracFalsoPositivo_IdFalsoPositivo PRIMARY KEY (IdFalsoPositivo),
    CONSTRAINT IUQ_TbSitracFalsoPositivo_IdIncidente UNIQUE (IdIncidente),
    CONSTRAINT FK_TbSitracFalsoPositivo_TbSitracIncidente FOREIGN KEY (IdIncidente) REFERENCES TbSitracIncidente(IdIncidente),
    CONSTRAINT FK_TbSitracFalsoPositivo_TbSitracResponsable FOREIGN KEY (IdResponsableRegistra) REFERENCES TbSitracResponsable(IdResponsable)
);