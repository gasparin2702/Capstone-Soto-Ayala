-- ==============================================================================
-- SISTEMA: SITRAC
-- OBJETIVO: Tablas de hitos regulatorios, reglas de escalamiento y log de notificaciones
-- LEY: N° 21.663 · Marco de Ciberseguridad
-- ==============================================================================

-- ==============================================================================
-- TABLA 1: Hitos regulatorios por incidente
-- Cada fila = una obligación legal (Aviso Temprano, Informe Preliminar, Informe Final)
-- ==============================================================================
CREATE TABLE TbSitracHito (
    IdHito BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    CodTipoHito BIGINT NOT NULL,
    Secuencia INT NOT NULL CONSTRAINT DF_TbSitracHito_Secuencia DEFAULT 1,
    FechaBase DATETIME NOT NULL,
    FechaLimite DATETIME NOT NULL,
    Estado VARCHAR(20) NOT NULL CONSTRAINT DF_TbSitracHito_Estado DEFAULT 'Pendiente',
    FechaCumplimiento DATETIME NULL,
    ReferenciaExterna VARCHAR(200) NULL,
    UltimaReglaNotificada BIGINT NULL,
    ProximaNotificacionAt DATETIME NULL,
    ConteoNotificaciones INT NOT NULL CONSTRAINT DF_TbSitracHito_ConteoNotificaciones DEFAULT 0,
    SilenciadoHasta DATETIME NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracHito_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracHito_AuditCreateDate DEFAULT GETUTCDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracHito_AuditLastUpdateDate DEFAULT GETUTCDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracHito_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracHito PRIMARY KEY CLUSTERED (IdHito ASC),
    CONSTRAINT FK_TbSitracHito_Incidente FOREIGN KEY (IdIncidente) REFERENCES TbSitracIncidente (IdIncidente),
    CONSTRAINT CK_TbSitracHito_Estado CHECK (Estado IN ('Pendiente', 'Cumplido', 'Cancelado', 'Aplazado'))
);

CREATE INDEX IX_TbSitracHito_Incidente ON TbSitracHito (IdIncidente);
CREATE INDEX IX_TbSitracHito_Pendientes ON TbSitracHito (Estado, FechaLimite) WHERE AuditNotDeleted = 1;
CREATE INDEX IX_TbSitracHito_CicloNotif ON TbSitracHito (Estado, ProximaNotificacionAt) WHERE AuditNotDeleted = 1;

-- ==============================================================================
-- TABLA 2: Reglas de escalamiento por umbral
-- ==============================================================================
CREATE TABLE TbSitracReglaEscalamiento (
    IdRegla BIGINT IDENTITY(1,1) NOT NULL,
    CodTipoHito BIGINT NOT NULL,
    TiempoRestanteMinutos INT NOT NULL,
    RepetirCadaMinutos INT NULL,
    NotificarEmail BIT NOT NULL CONSTRAINT DF_TbSitracReglaEscalamiento_NotificarEmail DEFAULT 1,
    NotificarTeams BIT NOT NULL CONSTRAINT DF_TbSitracReglaEscalamiento_NotificarTeams DEFAULT 1,
    Descripcion VARCHAR(200) NOT NULL,
    OrdenVisual INT NOT NULL CONSTRAINT DF_TbSitracReglaEscalamiento_OrdenVisual DEFAULT 0,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracReglaEscalamiento_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracReglaEscalamiento_AuditCreateDate DEFAULT GETUTCDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracReglaEscalamiento_AuditLastUpdateDate DEFAULT GETUTCDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracReglaEscalamiento_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracReglaEscalamiento PRIMARY KEY CLUSTERED (IdRegla ASC)
);

CREATE INDEX IX_TbSitracReglaEscalamiento_Hito ON TbSitracReglaEscalamiento (CodTipoHito, TiempoRestanteMinutos DESC);

-- ==============================================================================
-- TABLA 3: Log de notificaciones (outbox + auditoría)
-- ==============================================================================
CREATE TABLE TbSitracNotificacionLog (
    IdNotificacion BIGINT IDENTITY(1,1) NOT NULL,
    IdHito BIGINT NOT NULL,
    NumeroNotificacion INT NOT NULL,
    Destinatario VARCHAR(200) NOT NULL,
    Canal VARCHAR(20) NOT NULL,
    IdRegla BIGINT NULL,
    Estado VARCHAR(20) NOT NULL CONSTRAINT DF_TbSitracNotificacionLog_Estado DEFAULT 'Pendiente',
    Intentos INT NOT NULL CONSTRAINT DF_TbSitracNotificacionLog_Intentos DEFAULT 0,
    ProximoReintentoAt DATETIME NULL,
    FechaProgramada DATETIME NOT NULL CONSTRAINT DF_TbSitracNotificacionLog_FechaProgramada DEFAULT GETUTCDATE(),
    FechaEnvio DATETIME NULL,
    MensajeError VARCHAR(500) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracNotificacionLog_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracNotificacionLog_AuditCreateDate DEFAULT GETUTCDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracNotificacionLog_AuditLastUpdateDate DEFAULT GETUTCDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracNotificacionLog_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracNotificacionLog PRIMARY KEY CLUSTERED (IdNotificacion ASC),
    CONSTRAINT FK_TbSitracNotificacionLog_Hito FOREIGN KEY (IdHito) REFERENCES TbSitracHito (IdHito),
    CONSTRAINT CK_TbSitracNotificacionLog_Estado CHECK (Estado IN ('Pendiente', 'Enviada', 'Fallida', 'Cancelada')),
    CONSTRAINT CK_TbSitracNotificacionLog_Canal CHECK (Canal IN ('Email', 'Teams'))
);

CREATE INDEX IX_TbSitracNotificacionLog_Hito ON TbSitracNotificacionLog (IdHito, NumeroNotificacion);
CREATE INDEX IX_TbSitracNotificacionLog_Outbox ON TbSitracNotificacionLog (Estado, ProximoReintentoAt) WHERE Estado = 'Pendiente';

-- ==============================================================================
-- SEED: Reglas de escalamiento
-- ==============================================================================

-- Aviso Temprano
INSERT INTO TbSitracReglaEscalamiento (CodTipoHito, TiempoRestanteMinutos, RepetirCadaMinutos, Descripcion, OrdenVisual) VALUES
(1, 120, NULL, 'Aviso Temprano: faltan 2 horas', 10),
(1, 60,  NULL, 'Aviso Temprano: falta 1 hora', 20),
(1, 30,  NULL, 'Aviso Temprano: faltan 30 minutos', 30),
(1, 10,  NULL, 'Aviso Temprano: faltan 10 minutos', 40),
(1, 0,   15,   'Aviso Temprano VENCIDO: primera hora', 50),
(1, -60, 60,   'Aviso Temprano VENCIDO: seguimiento horario', 60);

-- Informe Preliminar
INSERT INTO TbSitracReglaEscalamiento (CodTipoHito, TiempoRestanteMinutos, RepetirCadaMinutos, Descripcion, OrdenVisual) VALUES
(2, 1440, NULL, 'Informe Preliminar: faltan 24 horas', 10),
(2, 480,  NULL, 'Informe Preliminar: faltan 8 horas', 20),
(2, 120,  NULL, 'Informe Preliminar: faltan 2 horas', 30),
(2, 30,   NULL, 'Informe Preliminar: faltan 30 minutos', 40),
(2, 0,    30,   'Informe Preliminar VENCIDO: primera hora', 50),
(2, -60,  60,   'Informe Preliminar VENCIDO: seguimiento horario', 60);

-- Informe Final
INSERT INTO TbSitracReglaEscalamiento (CodTipoHito, TiempoRestanteMinutos, RepetirCadaMinutos, Descripcion, OrdenVisual) VALUES
(3, 10080, NULL, 'Informe Final: faltan 7 días', 10),
(3, 7200,  NULL, 'Informe Final: faltan 5 días', 20),
(3, 2880,  NULL, 'Informe Final: faltan 2 días', 30),
(3, 1440,  NULL, 'Informe Final: falta 1 día', 40),
(3, 240,   NULL, 'Informe Final: faltan 4 horas', 50),
(3, 0,     120,  'Informe Final VENCIDO: plazo de 15 días alcanzado', 60);

-- ==============================================================================
-- RELLENO: Crear hitos para incidentes abiertos existentes
-- ==============================================================================

INSERT INTO TbSitracHito (IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado)
SELECT i.IdIncidente, 1, 1, i.FechaTomaConocimiento, sla.FechaLimiteAvisoTemprano, 'Pendiente'
FROM TbSitracIncidente i
INNER JOIN TbSitracIncidentePlazoSla sla ON sla.IdIncidente = i.IdIncidente
WHERE i.CodTipoEstadoIncidente = 1 AND i.AuditNotDeleted = 1
  AND NOT EXISTS (SELECT 1 FROM TbSitracHito h WHERE h.IdIncidente = i.IdIncidente AND h.CodTipoHito = 1);

INSERT INTO TbSitracHito (IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado)
SELECT i.IdIncidente, 2, 1, i.FechaTomaConocimiento, sla.FechaLimiteInformePreliminar, 'Pendiente'
FROM TbSitracIncidente i
INNER JOIN TbSitracIncidentePlazoSla sla ON sla.IdIncidente = i.IdIncidente
WHERE i.CodTipoEstadoIncidente = 2 AND i.AuditNotDeleted = 1
  AND NOT EXISTS (SELECT 1 FROM TbSitracHito h WHERE h.IdIncidente = i.IdIncidente AND h.CodTipoHito = 2);

INSERT INTO TbSitracHito (IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado)
SELECT i.IdIncidente, 3, 1, i.FechaTomaConocimiento, sla.FechaLimiteInformeFinal, 'Pendiente'
FROM TbSitracIncidente i
INNER JOIN TbSitracIncidentePlazoSla sla ON sla.IdIncidente = i.IdIncidente
WHERE i.CodTipoEstadoIncidente = 3 AND i.AuditNotDeleted = 1
  AND NOT EXISTS (SELECT 1 FROM TbSitracHito h WHERE h.IdIncidente = i.IdIncidente AND h.CodTipoHito = 3);

PRINT '>> Migración V0006 aplicada: hitos, reglas de escalamiento y log de notificaciones.';
GO