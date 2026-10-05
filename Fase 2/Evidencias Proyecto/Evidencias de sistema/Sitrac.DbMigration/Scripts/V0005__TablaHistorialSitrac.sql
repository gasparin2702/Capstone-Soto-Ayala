-- ==============================================================================
-- SISTEMA: SITRAC-ANCI
-- OBJETIVO: Creación de tabla de trazabilidad (Historial) y catálogo
-- ==============================================================================

CREATE TABLE TbAnciTipoAmbitoCambio(
    IdTipoAmbitoCambio bigint NOT NULL,
    NombreAmbitoCambio varchar(50) NOT NULL,
    AuditNotDeleted bit NOT NULL CONSTRAINT DF_TbAnciTipoAmbitoCambio_AuditNotDeleted DEFAULT ((1)),
    AuditCreateDate datetime NOT NULL CONSTRAINT DF_TbAnciTipoAmbitoCambio_AuditCreateDate DEFAULT (getdate()),
    AuditLastUpdateDate datetime NULL,
    AuditUserLastUpdate varchar(100) NULL,
    CONSTRAINT PK_TbAnciTipoAmbitoCambio PRIMARY KEY CLUSTERED (IdTipoAmbitoCambio ASC)
);

CREATE TABLE TbAnciEventoHistorial(
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
    FechaCambio datetime NOT NULL CONSTRAINT DF_TbAnciEventoHistorial_FechaCambio DEFAULT (getdate()),
    Comentario varchar(1000) NULL,
    AuditNotDeleted bit NOT NULL CONSTRAINT DF_TbAnciEventoHistorial_AuditNotDeleted DEFAULT ((1)),
    AuditCreateDate datetime NOT NULL CONSTRAINT DF_TbAnciEventoHistorial_AuditCreateDate DEFAULT (getdate()),
    AuditLastUpdateDate datetime NULL,
    AuditUserLastUpdate varchar(100) NULL,
    CONSTRAINT PK_TbAnciEventoHistorial PRIMARY KEY CLUSTERED (IdEventoHistorial ASC),
    CONSTRAINT FK_EventoHistorial_TipoAmbitoCambio FOREIGN KEY(IdTipoAmbitoCambio) REFERENCES TbAnciTipoAmbitoCambio (IdTipoAmbitoCambio),
    CONSTRAINT FK_EventoHistorial_Incidente FOREIGN KEY(IdIncidente) REFERENCES TbAnciIncidente (IdIncidente),
    CONSTRAINT FK_EventoHistorial_EstadoIncAnterior FOREIGN KEY(IdTipoEstadoIncidenteAnterior) REFERENCES TbAnciTipoEstadoIncidente (IdTipoEstadoIncidente),
    CONSTRAINT FK_EventoHistorial_EstadoIncNuevo FOREIGN KEY(IdTipoEstadoIncidenteNuevo) REFERENCES TbAnciTipoEstadoIncidente (IdTipoEstadoIncidente),
    CONSTRAINT FK_EventoHistorial_GravedadAnterior FOREIGN KEY(IdTipoGravedadAnterior) REFERENCES TbAnciTipoGravedad (IdTipoGravedad),
    CONSTRAINT FK_EventoHistorial_GravedadNueva FOREIGN KEY(IdTipoGravedadNueva) REFERENCES TbAnciTipoGravedad (IdTipoGravedad),
    CONSTRAINT FK_EventoHistorial_ResponsableCambia FOREIGN KEY(IdResponsableCambia) REFERENCES TbAnciResponsable (IdResponsable)
);

-- Inyección del catálogo base
INSERT INTO TbAnciTipoAmbitoCambio (IdTipoAmbitoCambio, NombreAmbitoCambio, AuditNotDeleted, AuditCreateDate) 
VALUES (1, 'Cambio de Estado Automático SLA', 1, GETDATE());