-- ==============================================================================
-- SISTEMA: SITRAC-ANCI
-- OBJETIVO: Tablas de hitos regulatorios, reglas de escalamiento y log de notificaciones
-- LEY: N° 21.663 · Marco de Ciberseguridad
-- ==============================================================================

-- ==============================================================================
-- TABLA 1: Hitos regulatorios por incidente
-- Cada fila = una obligación legal (Aviso Temprano, Informe Preliminar, Informe Final)
-- ==============================================================================
CREATE TABLE TbAnciHito (
    IdHito BIGINT IDENTITY(1,1) NOT NULL,
    IdIncidente BIGINT NOT NULL,
    CodTipoHito BIGINT NOT NULL,                     -- 1=Aviso Temprano, 2=Informe Preliminar, 3=Informe Final
    Secuencia INT NOT NULL CONSTRAINT DF_TbAnciHito_Secuencia DEFAULT 1,
    FechaBase DATETIME NOT NULL,                     -- desde cuándo corre el reloj
    FechaLimite DATETIME NOT NULL,                   -- cuándo vence
    Estado VARCHAR(20) NOT NULL CONSTRAINT DF_TbAnciHito_Estado DEFAULT 'Pendiente',
    FechaCumplimiento DATETIME NULL,
    ReferenciaANCI VARCHAR(200) NULL,
    -- Control de notificación (idempotencia + repetición)
    UltimaReglaNotificada BIGINT NULL,               -- IdRegla de la última notificación
    ProximaNotificacionAt DATETIME NULL,             -- cuándo toca repetir
    ConteoNotificaciones INT NOT NULL CONSTRAINT DF_TbAnciHito_ConteoNotificaciones DEFAULT 0,
    SilenciadoHasta DATETIME NULL,                   -- (Fase 3) para acknowledgment
    -- Auditoría
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciHito_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciHito_AuditCreateDate DEFAULT GETUTCDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciHito_AuditLastUpdateDate DEFAULT GETUTCDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciHito_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciHito PRIMARY KEY CLUSTERED (IdHito ASC),
    CONSTRAINT FK_TbAnciHito_Incidente FOREIGN KEY (IdIncidente) REFERENCES TbAnciIncidente (IdIncidente),
    CONSTRAINT CK_TbAnciHito_Estado CHECK (Estado IN ('Pendiente', 'Cumplido', 'Cancelado', 'Aplazado'))
);

CREATE INDEX IX_TbAnciHito_Incidente ON TbAnciHito (IdIncidente);
CREATE INDEX IX_TbAnciHito_Pendientes ON TbAnciHito (Estado, FechaLimite) WHERE AuditNotDeleted = 1;
CREATE INDEX IX_TbAnciHito_CicloNotif ON TbAnciHito (Estado, ProximaNotificacionAt) WHERE AuditNotDeleted = 1;

-- ==============================================================================
-- TABLA 2: Reglas de escalamiento por umbral
-- Cada fila = un umbral de notificación. La regla vigente es la de MENOR T tal que restante <= T.
-- Umbrales positivos: minutos antes de vencer. Cero: al vencer. Negativos: minutos tras vencer.
-- ==============================================================================
CREATE TABLE TbAnciReglaEscalamiento (
    IdRegla BIGINT IDENTITY(1,1) NOT NULL,
    CodTipoHito BIGINT NOT NULL,
    TiempoRestanteMinutos INT NOT NULL,
    RepetirCadaMinutos INT NULL,                     -- NULL = no repite
    NotificarEmail BIT NOT NULL CONSTRAINT DF_TbAnciReglaEscalamiento_NotificarEmail DEFAULT 1,
    NotificarTeams BIT NOT NULL CONSTRAINT DF_TbAnciReglaEscalamiento_NotificarTeams DEFAULT 1,
    Descripcion VARCHAR(200) NOT NULL,
    OrdenVisual INT NOT NULL CONSTRAINT DF_TbAnciReglaEscalamiento_OrdenVisual DEFAULT 0,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciReglaEscalamiento_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciReglaEscalamiento_AuditCreateDate DEFAULT GETUTCDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciReglaEscalamiento_AuditLastUpdateDate DEFAULT GETUTCDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciReglaEscalamiento_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciReglaEscalamiento PRIMARY KEY CLUSTERED (IdRegla ASC)
);

CREATE INDEX IX_TbAnciReglaEscalamiento_Hito ON TbAnciReglaEscalamiento (CodTipoHito, TiempoRestanteMinutos DESC);

-- ==============================================================================
-- TABLA 3: Log de notificaciones (outbox + auditoría)
-- Una fila por cada destinatario × canal × intento programado.
-- Se insertan como Pendiente en la misma transacción que reserva el hito.
-- El Sender las toma y las envía.
-- ==============================================================================
CREATE TABLE TbAnciNotificacionLog (
    IdNotificacion BIGINT IDENTITY(1,1) NOT NULL,
    IdHito BIGINT NOT NULL,
    NumeroNotificacion INT NOT NULL,                 -- secuencia dentro del hito
    Destinatario VARCHAR(200) NOT NULL,
    Canal VARCHAR(20) NOT NULL,                      -- Email / Teams
    IdRegla BIGINT NULL,                             -- qué regla disparó
    Estado VARCHAR(20) NOT NULL CONSTRAINT DF_TbAnciNotificacionLog_Estado DEFAULT 'Pendiente',
    Intentos INT NOT NULL CONSTRAINT DF_TbAnciNotificacionLog_Intentos DEFAULT 0,
    ProximoReintentoAt DATETIME NULL,
    FechaProgramada DATETIME NOT NULL CONSTRAINT DF_TbAnciNotificacionLog_FechaProgramada DEFAULT GETUTCDATE(),
    FechaEnvio DATETIME NULL,
    MensajeError VARCHAR(500) NULL,
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbAnciNotificacionLog_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciNotificacionLog_AuditCreateDate DEFAULT GETUTCDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbAnciNotificacionLog_AuditLastUpdateDate DEFAULT GETUTCDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbAnciNotificacionLog_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbAnciNotificacionLog PRIMARY KEY CLUSTERED (IdNotificacion ASC),
    CONSTRAINT FK_TbAnciNotificacionLog_Hito FOREIGN KEY (IdHito) REFERENCES TbAnciHito (IdHito),
    CONSTRAINT CK_TbAnciNotificacionLog_Estado CHECK (Estado IN ('Pendiente', 'Enviada', 'Fallida', 'Cancelada')),
    CONSTRAINT CK_TbAnciNotificacionLog_Canal CHECK (Canal IN ('Email', 'Teams'))
);

CREATE INDEX IX_TbAnciNotificacionLog_Hito ON TbAnciNotificacionLog (IdHito, NumeroNotificacion);
CREATE INDEX IX_TbAnciNotificacionLog_Outbox ON TbAnciNotificacionLog (Estado, ProximoReintentoAt) WHERE Estado = 'Pendiente';

-- ==============================================================================
-- SEED: Reglas de escalamiento
-- CodTipoHito: 1 = Aviso Temprano, 2 = Informe Preliminar, 3 = Informe Final
-- ==============================================================================

-- Aviso Temprano (plazo 3 horas desde toma de conocimiento)
INSERT INTO TbAnciReglaEscalamiento (CodTipoHito, TiempoRestanteMinutos, RepetirCadaMinutos, Descripcion, OrdenVisual) VALUES
(1, 120, NULL, 'Aviso Temprano: faltan 2 horas', 10),
(1, 60,  NULL, 'Aviso Temprano: falta 1 hora', 20),
(1, 30,  NULL, 'Aviso Temprano: faltan 30 minutos', 30),
(1, 10,  NULL, 'Aviso Temprano: faltan 10 minutos', 40),
(1, 0,   15,   'Aviso Temprano VENCIDO: primera hora', 50),
(1, -60, 60,   'Aviso Temprano VENCIDO: seguimiento horario', 60);

-- Informe Preliminar (plazo 24h OIV / 72h Esencial)
INSERT INTO TbAnciReglaEscalamiento (CodTipoHito, TiempoRestanteMinutos, RepetirCadaMinutos, Descripcion, OrdenVisual) VALUES
(2, 1440, NULL, 'Informe Preliminar: faltan 24 horas', 10),
(2, 480,  NULL, 'Informe Preliminar: faltan 8 horas', 20),
(2, 120,  NULL, 'Informe Preliminar: faltan 2 horas', 30),
(2, 30,   NULL, 'Informe Preliminar: faltan 30 minutos', 40),
(2, 0,    30,   'Informe Preliminar VENCIDO: primera hora', 50),
(2, -60,  60,   'Informe Preliminar VENCIDO: seguimiento horario', 60);

-- Informe Final (plazo 15 días desde el envío del aviso temprano)
INSERT INTO TbAnciReglaEscalamiento (CodTipoHito, TiempoRestanteMinutos, RepetirCadaMinutos, Descripcion, OrdenVisual) VALUES
(3, 10080, NULL, 'Informe Final: faltan 7 días', 10),
(3, 7200,  NULL, 'Informe Final: faltan 5 días', 20),
(3, 2880,  NULL, 'Informe Final: faltan 2 días', 30),
(3, 1440,  NULL, 'Informe Final: falta 1 día', 40),
(3, 240,   NULL, 'Informe Final: faltan 4 horas', 50),
(3, 0,     120,  'Informe Final VENCIDO: plazo de 15 días alcanzado', 60);

-- ==============================================================================
-- RELLENO: Crear hitos para incidentes abiertos existentes (estados 1, 2, 3)
-- ==============================================================================

-- Aviso Temprano: incidentes en estado 1 (En triage)
INSERT INTO TbAnciHito (IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado)
SELECT
    i.IdIncidente,
    1,
    1,
    i.FechaTomaConocimiento,
    sla.FechaLimiteAvisoTemprano,
    'Pendiente'
FROM TbAnciIncidente i
INNER JOIN TbAnciIncidentePlazoSla sla ON sla.IdIncidente = i.IdIncidente
WHERE i.CodTipoEstadoIncidente = 1
  AND i.AuditNotDeleted = 1
  AND NOT EXISTS (SELECT 1 FROM TbAnciHito h WHERE h.IdIncidente = i.IdIncidente AND h.CodTipoHito = 1);

-- Informe Preliminar: incidentes en estado 2 (Aviso enviado)
INSERT INTO TbAnciHito (IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado)
SELECT
    i.IdIncidente,
    2,
    1,
    i.FechaTomaConocimiento,
    sla.FechaLimiteInformePreliminar,
    'Pendiente'
FROM TbAnciIncidente i
INNER JOIN TbAnciIncidentePlazoSla sla ON sla.IdIncidente = i.IdIncidente
WHERE i.CodTipoEstadoIncidente = 2
  AND i.AuditNotDeleted = 1
  AND NOT EXISTS (SELECT 1 FROM TbAnciHito h WHERE h.IdIncidente = i.IdIncidente AND h.CodTipoHito = 2);

-- Informe Final: incidentes en estado 3 (Preliminar enviado)
INSERT INTO TbAnciHito (IdIncidente, CodTipoHito, Secuencia, FechaBase, FechaLimite, Estado)
SELECT
    i.IdIncidente,
    3,
    1,
    i.FechaTomaConocimiento,
    sla.FechaLimiteInformeFinal,
    'Pendiente'
FROM TbAnciIncidente i
INNER JOIN TbAnciIncidentePlazoSla sla ON sla.IdIncidente = i.IdIncidente
WHERE i.CodTipoEstadoIncidente = 3
  AND i.AuditNotDeleted = 1
  AND NOT EXISTS (SELECT 1 FROM TbAnciHito h WHERE h.IdIncidente = i.IdIncidente AND h.CodTipoHito = 3);

PRINT '>> Migración V0006 aplicada: hitos, reglas de escalamiento y log de notificaciones.';
GO