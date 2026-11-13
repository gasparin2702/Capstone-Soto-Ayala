-- ==============================================================================
-- SISTEMA: SITRAC
-- OBJETIVO: Creación de Informes, Revisiones de DP y anexos de Documentos v3
-- ==============================================================================

CREATE TABLE TbSitracTipoEstadoRevisionDp (
    IdTipoEstadoRevisionDp BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoEstadoRevisionDp VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoEstadoRevisionDp_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoEstadoRevisionDp_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoEstadoRevisionDp_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoEstadoRevisionDp_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoEstadoRevisionDp_IdTipoEstadoRevisionDp PRIMARY KEY (IdTipoEstadoRevisionDp)
);

CREATE TABLE TbSitracRevisionDatosPersonales (
    IdRevisionDatosPersonales BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    CodTipoDatosPersonales BIGINT NULL,
    FechaAuditoria DATETIME NULL,
    FechaNotificaLegal DATETIME NULL,
    FechaNotificaDpo DATETIME NULL,
    IdResponsableValida BIGINT NULL,
    CodTipoEstadoRevisionDp BIGINT NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracRevisionDp_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracRevisionDp_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracRevisionDp_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracRevisionDp_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracRevisionDp_IdRevisionDatosPersonales PRIMARY KEY (IdRevisionDatosPersonales),
    CONSTRAINT IUQ_TbSitracRevisionDp_IdIncidente UNIQUE (IdIncidente),
    CONSTRAINT FK_TbSitracRevisionDp_TbSitracIncidente FOREIGN KEY (IdIncidente) REFERENCES TbSitracIncidente(IdIncidente),
    CONSTRAINT FK_TbSitracRevisionDp_TbSitracTipoDatosPersonales FOREIGN KEY (CodTipoDatosPersonales) REFERENCES TbSitracTipoDatosPersonales(IdTipoDatosPersonales),
    CONSTRAINT FK_TbSitracRevisionDp_TbSitracResponsable FOREIGN KEY (IdResponsableValida) REFERENCES TbSitracResponsable(IdResponsable),
    CONSTRAINT FK_TbSitracRevisionDp_TbSitracTipoEstadoRevisionDp FOREIGN KEY (CodTipoEstadoRevisionDp) REFERENCES TbSitracTipoEstadoRevisionDp(IdTipoEstadoRevisionDp)
);

CREATE TABLE TbSitracRevisionDatosPersonalesHistorial (
    IdRevisionHistorial BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    CodTipoEstadoRevisionDpAnt BIGINT NULL,
    CodTipoEstadoRevisionDpNvo BIGINT NOT NULL,
    IdResponsableCambia BIGINT NOT NULL,
    FechaCambio DATETIME NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracRevisionDpHist_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracRevisionDpHist_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracRevisionDpHist_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracRevisionDpHist_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracRevisionDpHist_IdRevisionHistorial PRIMARY KEY (IdRevisionHistorial),
    CONSTRAINT FK_TbSitracRevisionDpHist_TbSitracIncidente FOREIGN KEY (IdIncidente) REFERENCES TbSitracIncidente(IdIncidente),
    CONSTRAINT FK_TbSitracRevisionDpHist_TbSitracTipoEstadoRevisionDpAnt FOREIGN KEY (CodTipoEstadoRevisionDpAnt) REFERENCES TbSitracTipoEstadoRevisionDp(IdTipoEstadoRevisionDp),
    CONSTRAINT FK_TbSitracRevisionDpHist_TbSitracTipoEstadoRevisionDpNvo FOREIGN KEY (CodTipoEstadoRevisionDpNvo) REFERENCES TbSitracTipoEstadoRevisionDp(IdTipoEstadoRevisionDp),
    CONSTRAINT FK_TbSitracRevisionDpHist_TbSitracResponsable FOREIGN KEY (IdResponsableCambia) REFERENCES TbSitracResponsable(IdResponsable)
);

CREATE TABLE TbSitracTipoClasificacionArt27 (
    IdTipoClasificacionArt27 BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoClasificacionArt27 VARCHAR(100) NOT NULL,
    DescripcionTipoClasificacionArt27 VARCHAR(255) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoClasificacionArt27_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoClasificacionArt27_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoClasificacionArt27_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoClasificacionArt27_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoClasificacionArt27_IdTipoClasificacionArt27 PRIMARY KEY (IdTipoClasificacionArt27)
);

CREATE TABLE TbSitracTipoInformeCierre (
    IdTipoInformeCierre BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoInformeCierre VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoInformeCierre_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoInformeCierre_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoInformeCierre_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoInformeCierre_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoInformeCierre_IdTipoInformeCierre PRIMARY KEY (IdTipoInformeCierre)
);

-- ==============================================================================
-- TABLA: INFORME PRELIMINAR (Segundo reporte 24/72h)
-- ==============================================================================
CREATE TABLE TbSitracInformePreliminar (
    IdInformePreliminar BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    IdComunicacion BIGINT NULL,
    CodTipoGravedad BIGINT NOT NULL,
    CodTipoClasificacionArt27 BIGINT NULL,
    FechaEstimadaInicio DATETIME NULL,
    IndiciosOcurrencia VARCHAR(MAX) NULL,
    ActivosAfectados VARCHAR(MAX) NULL,
    EvidenciaDisponible VARCHAR(MAX) NULL,
    EvidenciaPosibleDelito VARCHAR(MAX) NULL,
    RepercusionesOtrasInstituciones VARCHAR(MAX) NULL,
    OtraInformacionRelevante VARCHAR(MAX) NULL,
    EvaluacionGravedadImpacto VARCHAR(MAX) NULL,
    EstadoActualIncidente VARCHAR(MAX) NULL,
    DescripcionIocs VARCHAR(MAX) NULL,
    DescripcionMedidasMitigacion VARCHAR(MAX) NULL,

    -- Bloque DPO / Datos Personales
    HayDatosPersonales VARCHAR(20) NULL,
    FechaNotificacionDpo DATETIME NULL,
    ResponsableDpo VARCHAR(150) NULL,

    FechaEnvio DATETIME NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracInformePreliminar_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracInformePreliminar_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracInformePreliminar_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracInformePreliminar_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracInformePreliminar_IdInformePreliminar PRIMARY KEY (IdInformePreliminar),
    CONSTRAINT FK_TbSitracInformePreliminar_TbSitracIncidente FOREIGN KEY (IdIncidente) REFERENCES TbSitracIncidente(IdIncidente),
    CONSTRAINT FK_TbSitracInformePreliminar_TbSitracComunicacion FOREIGN KEY (IdComunicacion) REFERENCES TbSitracComunicacion(IdComunicacion),
    CONSTRAINT FK_TbSitracInformePreliminar_TbSitracTipoGravedad FOREIGN KEY (CodTipoGravedad) REFERENCES TbSitracTipoGravedad(IdTipoGravedad),
    CONSTRAINT FK_TbSitracInformePreliminar_TbSitracTipoClasificacionArt27 FOREIGN KEY (CodTipoClasificacionArt27) REFERENCES TbSitracTipoClasificacionArt27(IdTipoClasificacionArt27)
);

-- ==============================================================================
-- TABLA: INFORME FINAL (Cierre y OIV)
-- ==============================================================================
CREATE TABLE TbSitracInformeCierre (
    IdInformeCierre BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    IdComunicacion BIGINT NULL,
    CodTipoInformeCierre BIGINT NOT NULL,
    FechaEstimadaInicio DATETIME NULL,
    IndiciosOcurrencia VARCHAR(MAX) NULL,
    ActivosAfectados VARCHAR(MAX) NULL,
    EvidenciaPosibleDelito VARCHAR(MAX) NULL,
    RepercusionesOtrasInstituciones VARCHAR(MAX) NULL,
    OtraInformacionRelevante VARCHAR(MAX) NULL,
    DescripcionIocs VARCHAR(MAX) NULL,
    DescripcionDetallada VARCHAR(MAX) NULL,
    TipoAmenazaCausaPrincipal VARCHAR(MAX) NULL,
    RepercusionesTransfronterizas VARCHAR(MAX) NULL,
    MedidasMitigacionAplicadas VARCHAR(MAX) NULL,
    
    -- Campos exclusivos OIV
    VulnerabilidadesExplotadas VARCHAR(MAX) NULL,
    AplicacionesAbusivas VARCHAR(MAX) NULL,
    ControlesTecnicosPrevistos VARCHAR(MAX) NULL,
    CausasAusenciaControles VARCHAR(MAX) NULL,
    MedidasFrenteAlertasCsirt VARCHAR(MAX) NULL,

    -- Bloque DPO / Datos Personales
    HayDatosPersonales VARCHAR(20) NULL,
    FechaNotificacionDpo DATETIME NULL,
    ResponsableDpo VARCHAR(150) NULL,

    FechaEnvio DATETIME NULL,
    IdResponsableConfirma BIGINT NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracInformeCierre_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracInformeCierre_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracInformeCierre_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracInformeCierre_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracInformeCierre_IdInformeCierre PRIMARY KEY (IdInformeCierre),
    CONSTRAINT FK_TbSitracInformeCierre_TbSitracIncidente FOREIGN KEY (IdIncidente) REFERENCES TbSitracIncidente(IdIncidente),
    CONSTRAINT FK_TbSitracInformeCierre_TbSitracComunicacion FOREIGN KEY (IdComunicacion) REFERENCES TbSitracComunicacion(IdComunicacion),
    CONSTRAINT FK_TbSitracInformeCierre_TbSitracTipoInformeCierre FOREIGN KEY (CodTipoInformeCierre) REFERENCES TbSitracTipoInformeCierre(IdTipoInformeCierre),
    CONSTRAINT FK_TbSitracInformeCierre_TbSitracResponsable FOREIGN KEY (IdResponsableConfirma) REFERENCES TbSitracResponsable(IdResponsable)
);

CREATE TABLE TbSitracTipoDocumento (
    IdTipoDocumento BIGINT IDENTITY(1,1) NOT NULL,
    NombreTipoDocumento VARCHAR(50) NOT NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracTipoDocumento_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoDocumento_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracTipoDocumento_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracTipoDocumento_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracTipoDocumento_IdTipoDocumento PRIMARY KEY (IdTipoDocumento)
);

CREATE TABLE TbSitracInformePreliminarDocumento (
    IdInformePreliminarDocumento BIGINT IDENTITY(1,1) NOT NULL,
    IdInformePreliminar BIGINT NOT NULL,
    CodTipoDocumento BIGINT NOT NULL,
    NombreArchivo VARCHAR(255) NOT NULL,
    ReferenciaArchivo VARCHAR(500) NOT NULL,
    FechaCarga DATETIME NOT NULL CONSTRAINT DF_TbSitracInformePreliminarDoc_FechaCarga DEFAULT GETDATE(),
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracInformePreliminarDoc_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracInformePreliminarDoc_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracInformePreliminarDoc_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracInformePreliminarDoc_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracInformePreliminarDoc_IdInformePreliminarDocumento PRIMARY KEY (IdInformePreliminarDocumento),
    CONSTRAINT FK_TbSitracInformePreliminarDoc_TbSitracInformePreliminar FOREIGN KEY (IdInformePreliminar) REFERENCES TbSitracInformePreliminar(IdInformePreliminar),
    CONSTRAINT FK_TbSitracInformePreliminarDoc_TbSitracTipoDocumento FOREIGN KEY (CodTipoDocumento) REFERENCES TbSitracTipoDocumento(IdTipoDocumento)
);

CREATE TABLE TbSitracInformeCierreDocumento (
    IdInformeCierreDocumento BIGINT IDENTITY(1,1) NOT NULL,
    IdInformeCierre BIGINT NOT NULL,
    CodTipoDocumento BIGINT NOT NULL,
    NombreArchivo VARCHAR(255) NOT NULL,
    ReferenciaArchivo VARCHAR(500) NOT NULL,
    FechaCarga DATETIME NOT NULL CONSTRAINT DF_TbSitracInformeCierreDoc_FechaCarga DEFAULT GETDATE(),
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracInformeCierreDoc_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracInformeCierreDoc_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracInformeCierreDoc_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracInformeCierreDoc_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracInformeCierreDoc_IdInformeCierreDocumento PRIMARY KEY (IdInformeCierreDocumento),
    CONSTRAINT FK_TbSitracInformeCierreDoc_TbSitracInformeCierre FOREIGN KEY (IdInformeCierre) REFERENCES TbSitracInformeCierre(IdInformeCierre),
    CONSTRAINT FK_TbSitracInformeCierreDoc_TbSitracTipoDocumento FOREIGN KEY (CodTipoDocumento) REFERENCES TbSitracTipoDocumento(IdTipoDocumento)
);