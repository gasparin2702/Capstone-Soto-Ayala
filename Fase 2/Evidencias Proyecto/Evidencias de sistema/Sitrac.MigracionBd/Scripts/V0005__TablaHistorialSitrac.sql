-- ==============================================================================
-- SISTEMA: SITRAC
-- OBJETIVO: Creación de tabla de trazabilidad (Historial) y catálogo
-- ==============================================================================

CREATE TABLE TbSitracTipoAmbitoCambio(
    IdTipoAmbitoCambio bigint NOT NULL,
    NombreAmbitoCambio varchar(50) NOT NULL,
    AuditNotDeleted bit NOT NULL CONSTRAINT DF_TbSitracTipoAmbitoCambio_AuditNotDeleted DEFAULT ((1)),
    AuditCreateDate datetime NOT NULL CONSTRAINT DF_TbSitracTipoAmbitoCambio_AuditCreateDate DEFAULT (getdate()),
    AuditLastUpdateDate datetime NULL,
    AuditUserLastUpdate varchar(100) NULL,
    CONSTRAINT PK_TbSitracTipoAmbitoCambio PRIMARY KEY CLUSTERED (IdTipoAmbitoCambio ASC)
);

CREATE TABLE TbSitracEventoHistorial(
    IdEventoHistorial bigint IDENTITY(1,1) NOT NULL,
    IdIncidente bigint NOT NULL,
    CodigoTicketInvgate varchar(50) NOT NULL,
    IdTipoAmbitoCambio bigint NOT NULL,
    IdTipoEstadoIncidenteAnterior bigint NULL,
    IdTipoEstadoIncidenteNuevo bigint NULL,
    IdTipoEstadoRevisionDpAnterior bigint NULL,
    IdTipoEstadoRevisionDpNuevo bigint NULL,
    IdTipoGravedadAnterior bigint NULL,
    IdTipoGravedadNueva bigint NULL,
    IdResponsableCambia bigint NOT NULL,
    FechaCambio datetime NOT NULL CONSTRAINT DF_TbSitracEventoHistorial_FechaCambio DEFAULT (getdate()),
    Comentario varchar(1000) NULL,
    AuditNotDeleted bit NOT NULL CONSTRAINT DF_TbSitracEventoHistorial_AuditNotDeleted DEFAULT ((1)),
    AuditCreateDate datetime NOT NULL CONSTRAINT DF_TbSitracEventoHistorial_AuditCreateDate DEFAULT (getdate()),
    AuditLastUpdateDate datetime NULL,
    AuditUserLastUpdate varchar(100) NULL,
    CONSTRAINT PK_TbSitracEventoHistorial PRIMARY KEY CLUSTERED (IdEventoHistorial ASC),
    CONSTRAINT FK_EventoHistorial_TipoAmbitoCambio FOREIGN KEY(IdTipoAmbitoCambio) REFERENCES TbSitracTipoAmbitoCambio (IdTipoAmbitoCambio),
    CONSTRAINT FK_EventoHistorial_Incidente FOREIGN KEY(IdIncidente) REFERENCES TbSitracIncidente (IdIncidente),
    CONSTRAINT FK_EventoHistorial_EstadoIncAnterior FOREIGN KEY(IdTipoEstadoIncidenteAnterior) REFERENCES TbSitracTipoEstadoIncidente (IdTipoEstadoIncidente),
    CONSTRAINT FK_EventoHistorial_EstadoIncNuevo FOREIGN KEY(IdTipoEstadoIncidenteNuevo) REFERENCES TbSitracTipoEstadoIncidente (IdTipoEstadoIncidente),
    CONSTRAINT FK_EventoHistorial_GravedadAnterior FOREIGN KEY(IdTipoGravedadAnterior) REFERENCES TbSitracTipoGravedad (IdTipoGravedad),
    CONSTRAINT FK_EventoHistorial_GravedadNueva FOREIGN KEY(IdTipoGravedadNueva) REFERENCES TbSitracTipoGravedad (IdTipoGravedad),
    CONSTRAINT FK_EventoHistorial_ResponsableCambia FOREIGN KEY(IdResponsableCambia) REFERENCES TbSitracResponsable (IdResponsable)
);

-- Inyección del catálogo base + ámbitos para acciones manuales (Bloque B)
INSERT INTO TbSitracTipoAmbitoCambio (IdTipoAmbitoCambio, NombreAmbitoCambio, AuditNotDeleted, AuditCreateDate) 
VALUES 
    (1, 'Cambio de Estado Automático SLA', 1, GETDATE()),
    (2, 'Confirmación de Aviso Temprano', 1, GETDATE()),
    (3, 'Envío de Informe', 1, GETDATE()),
    (4, 'Cierre Manual de Incidente', 1, GETDATE()),
    (5, 'Creación de Hito Regulatorio', 1, GETDATE());