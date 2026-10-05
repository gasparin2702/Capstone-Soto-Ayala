-- ==============================================================================
-- SISTEMA: SITRAC-ANCI
-- OBJETIVO: Creación de Informes, Revisiones de DP y anexos de Documentos v3
-- ==============================================================================

CREATE TABLE TbAnciTipoEstadoRevisionDp (
    IdTipoEstadoRevisionDp BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoEstadoRevisionDp VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoEstadoRevisionDp_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoEstadoRevisionDp_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoEstadoRevisionDp_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoEstadoRevisionDp_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoEstadoRevisionDp_IdTipoEstadoRevisionDp PRIMARY KEY (IdTipoEstadoRevisionDp)
);

CREATE TABLE TbAnciRevisionDatosPersonales (
    IdRevisionDatosPersonales BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    CodTipoDatosPersonales BIGINT NULL,
    FechaAuditoria DATETIME NULL,
    FechaNotificaLegal DATETIME NULL,
    FechaNotificaDpo DATETIME NULL,
    IdResponsableValida BIGINT NULL,
    CodTipoEstadoRevisionDp BIGINT NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciRevisionDp_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciRevisionDp_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciRevisionDp_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciRevisionDp_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciRevisionDp_IdRevisionDatosPersonales PRIMARY KEY (IdRevisionDatosPersonales),
    CONSTRAINT IUQ_TbAnciRevisionDp_IdIncidente UNIQUE (IdIncidente),
    CONSTRAINT FK_TbAnciRevisionDp_TbAnciIncidente FOREIGN KEY (IdIncidente) REFERENCES TbAnciIncidente(IdIncidente),
    CONSTRAINT FK_TbAnciRevisionDp_TbAnciTipoDatosPersonales FOREIGN KEY (CodTipoDatosPersonales) REFERENCES TbAnciTipoDatosPersonales(IdTipoDatosPersonales),
    CONSTRAINT FK_TbAnciRevisionDp_TbAnciResponsable FOREIGN KEY (IdResponsableValida) REFERENCES TbAnciResponsable(IdResponsable),
    CONSTRAINT FK_TbAnciRevisionDp_TbAnciTipoEstadoRevisionDp FOREIGN KEY (CodTipoEstadoRevisionDp) REFERENCES TbAnciTipoEstadoRevisionDp(IdTipoEstadoRevisionDp)
);

CREATE TABLE TbAnciRevisionDatosPersonalesHistorial (
    IdRevisionHistorial BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    CodTipoEstadoRevisionDpAnt BIGINT NULL,
    CodTipoEstadoRevisionDpNvo BIGINT NOT NULL,
    IdResponsableCambia BIGINT NOT NULL,
    FechaCambio DATETIME NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciRevisionDpHist_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciRevisionDpHist_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciRevisionDpHist_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciRevisionDpHist_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciRevisionDpHist_IdRevisionHistorial PRIMARY KEY (IdRevisionHistorial),
    CONSTRAINT FK_TbAnciRevisionDpHist_TbAnciIncidente FOREIGN KEY (IdIncidente) REFERENCES TbAnciIncidente(IdIncidente),
    CONSTRAINT FK_TbAnciRevisionDpHist_TbAnciTipoEstadoRevisionDpAnt FOREIGN KEY (CodTipoEstadoRevisionDpAnt) REFERENCES TbAnciTipoEstadoRevisionDp(IdTipoEstadoRevisionDp),
    CONSTRAINT FK_TbAnciRevisionDpHist_TbAnciTipoEstadoRevisionDpNvo FOREIGN KEY (CodTipoEstadoRevisionDpNvo) REFERENCES TbAnciTipoEstadoRevisionDp(IdTipoEstadoRevisionDp),
    CONSTRAINT FK_TbAnciRevisionDpHist_TbAnciResponsable FOREIGN KEY (IdResponsableCambia) REFERENCES TbAnciResponsable(IdResponsable)
);

CREATE TABLE TbAnciTipoClasificacionArt27 (
    IdTipoClasificacionArt27 BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoClasificacionArt27 VARCHAR(100) NOT NULL,
    DescripcionTipoClasificacionArt27 VARCHAR(255) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoClasificacionArt27_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoClasificacionArt27_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoClasificacionArt27_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoClasificacionArt27_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoClasificacionArt27_IdTipoClasificacionArt27 PRIMARY KEY (IdTipoClasificacionArt27)
);

CREATE TABLE TbAnciTipoInformeCierre (
    IdTipoInformeCierre BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoInformeCierre VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoInformeCierre_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoInformeCierre_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoInformeCierre_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoInformeCierre_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoInformeCierre_IdTipoInformeCierre PRIMARY KEY (IdTipoInformeCierre)
);

CREATE TABLE TbAnciInformePreliminar (
    IdInformePreliminar BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    IdComunicacion BIGINT NULL,
    CodTipoGravedad BIGINT NOT NULL,
    CodTipoClasificacionArt27 BIGINT NULL,
    DescripcionImpacto VARCHAR(MAX) NULL,
    DescripcionIocs VARCHAR(MAX) NULL,
    DescripcionMedidasMitigacion VARCHAR(MAX) NULL,
    FechaEnvio DATETIME NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciInformePreliminar_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciInformePreliminar_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciInformePreliminar_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciInformePreliminar_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciInformePreliminar_IdInformePreliminar PRIMARY KEY (IdInformePreliminar),
    CONSTRAINT FK_TbAnciInformePreliminar_TbAnciIncidente FOREIGN KEY (IdIncidente) REFERENCES TbAnciIncidente(IdIncidente),
    CONSTRAINT FK_TbAnciInformePreliminar_TbAnciComunicacion FOREIGN KEY (IdComunicacion) REFERENCES TbAnciComunicacion(IdComunicacion),
    CONSTRAINT FK_TbAnciInformePreliminar_TbAnciTipoGravedad FOREIGN KEY (CodTipoGravedad) REFERENCES TbAnciTipoGravedad(IdTipoGravedad),
    CONSTRAINT FK_TbAnciInformePreliminar_TbAnciTipoClasificacionArt27 FOREIGN KEY (CodTipoClasificacionArt27) REFERENCES TbAnciTipoClasificacionArt27(IdTipoClasificacionArt27)
);

CREATE TABLE TbAnciInformeCierre (
    IdInformeCierre BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    IdComunicacion BIGINT NULL,
    CodTipoInformeCierre BIGINT NOT NULL,
    DescripcionCronologiaCausaRaiz VARCHAR(MAX) NULL,
    DescripcionEvaluacionImpacto VARCHAR(MAX) NULL,
    DescripcionMedidasMitigacion VARCHAR(MAX) NULL,
    DescripcionLeccionesAprendidas VARCHAR(MAX) NULL,
    DescripcionPlanPrevencion VARCHAR(MAX) NULL,
    FechaEnvio DATETIME NULL,
    IdResponsableConfirma BIGINT NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciInformeCierre_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciInformeCierre_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciInformeCierre_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciInformeCierre_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciInformeCierre_IdInformeCierre PRIMARY KEY (IdInformeCierre),
    CONSTRAINT FK_TbAnciInformeCierre_TbAnciIncidente FOREIGN KEY (IdIncidente) REFERENCES TbAnciIncidente(IdIncidente),
    CONSTRAINT FK_TbAnciInformeCierre_TbAnciComunicacion FOREIGN KEY (IdComunicacion) REFERENCES TbAnciComunicacion(IdComunicacion),
    CONSTRAINT FK_TbAnciInformeCierre_TbAnciTipoInformeCierre FOREIGN KEY (CodTipoInformeCierre) REFERENCES TbAnciTipoInformeCierre(IdTipoInformeCierre),
    CONSTRAINT FK_TbAnciInformeCierre_TbAnciResponsable FOREIGN KEY (IdResponsableConfirma) REFERENCES TbAnciResponsable(IdResponsable)
);

CREATE TABLE TbAnciTipoDocumento (
    IdTipoDocumento BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoDocumento VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciTipoDocumento_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoDocumento_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciTipoDocumento_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciTipoDocumento_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciTipoDocumento_IdTipoDocumento PRIMARY KEY (IdTipoDocumento)
);

CREATE TABLE TbAnciInformePreliminarDocumento (
    IdInformePreliminarDocumento BIGINT IDENTITY(1,1) NOT NULL,
    IdInformePreliminar BIGINT NOT NULL,
    CodTipoDocumento BIGINT NOT NULL,
    NombreArchivo VARCHAR(255) NOT NULL,
    ReferenciaArchivo VARCHAR(500) NOT NULL,
    FechaCarga DATETIME NOT NULL CONSTRAINT DF_TbAnciInformePreliminarDoc_FechaCarga DEFAULT GETDATE(),
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciInformePreliminarDoc_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciInformePreliminarDoc_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciInformePreliminarDoc_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciInformePreliminarDoc_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciInformePreliminarDoc_IdInformePreliminarDocumento PRIMARY KEY (IdInformePreliminarDocumento),
    CONSTRAINT FK_TbAnciInformePreliminarDoc_TbAnciInformePreliminar FOREIGN KEY (IdInformePreliminar) REFERENCES TbAnciInformePreliminar(IdInformePreliminar),
    CONSTRAINT FK_TbAnciInformePreliminarDoc_TbAnciTipoDocumento FOREIGN KEY (CodTipoDocumento) REFERENCES TbAnciTipoDocumento(IdTipoDocumento)
);

-- NUEVA TABLA AGREGADA EN EL MODELO V3
CREATE TABLE TbAnciInformeCierreDocumento (
    IdInformeCierreDocumento BIGINT IDENTITY(1,1) NOT NULL,
    IdInformeCierre BIGINT NOT NULL,
    CodTipoDocumento BIGINT NOT NULL,
    NombreArchivo VARCHAR(255) NOT NULL,
    ReferenciaArchivo VARCHAR(500) NOT NULL,
    FechaCarga DATETIME NOT NULL CONSTRAINT DF_TbAnciInformeCierreDoc_FechaCarga DEFAULT GETDATE(),
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciInformeCierreDoc_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciInformeCierreDoc_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciInformeCierreDoc_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciInformeCierreDoc_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciInformeCierreDoc_IdInformeCierreDocumento PRIMARY KEY (IdInformeCierreDocumento),
    CONSTRAINT FK_TbAnciInformeCierreDoc_TbAnciInformeCierre FOREIGN KEY (IdInformeCierre) REFERENCES TbAnciInformeCierre(IdInformeCierre),
    CONSTRAINT FK_TbAnciInformeCierreDoc_TbAnciTipoDocumento FOREIGN KEY (CodTipoDocumento) REFERENCES TbAnciTipoDocumento(IdTipoDocumento)
);