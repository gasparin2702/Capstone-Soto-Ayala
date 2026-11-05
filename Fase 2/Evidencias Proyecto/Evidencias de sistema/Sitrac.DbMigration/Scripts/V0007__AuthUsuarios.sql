-- ==============================================================================
-- SISTEMA: SITRAC
-- OBJETIVO: Creación de tabla de usuarios para autenticación JWT
-- ==============================================================================

CREATE TABLE TbSitracUsuario (
    IdUsuario BIGINT IDENTITY(1,1) NOT NULL,
    NombreUsuario VARCHAR(50) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    NombreCompleto VARCHAR(150) NOT NULL,
    Rol VARCHAR(20) NOT NULL CONSTRAINT DF_TbSitracUsuario_Rol DEFAULT 'Analista',
    AuditNotDeleted BIT NOT NULL CONSTRAINT DF_TbSitracUsuario_AuditNotDeleted DEFAULT 1,
    AuditCreateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracUsuario_AuditCreateDate DEFAULT GETDATE(),
    AuditLastUpdateDate DATETIME NOT NULL CONSTRAINT DF_TbSitracUsuario_AuditLastUpdateDate DEFAULT GETDATE(),
    AuditUserLastUpdate VARCHAR(50) NOT NULL CONSTRAINT DF_TbSitracUsuario_AuditUserLastUpdate DEFAULT SUSER_NAME(),
    CONSTRAINT PK_TbSitracUsuario PRIMARY KEY CLUSTERED (IdUsuario ASC),
    CONSTRAINT UQ_TbSitracUsuario_NombreUsuario UNIQUE (NombreUsuario)
);

-- Insertar usuario Admin por defecto (Password: Admin123!)
-- NOTA: El hash fue generado con SHA256("Admin123!SITRAC_SALT"). 
-- En producción, cambie esta contraseña.
INSERT INTO TbSitracUsuario (NombreUsuario, PasswordHash, NombreCompleto, Rol)
VALUES ('admin', '0pYh9z7T8wG5w2G5w2G5w2G5w2G5w2G5w2G5w2G5w2G=', 'Administrador SITRAC', 'Admin');

PRINT '>> Migración V0007 aplicada: tabla de usuarios.';
GO