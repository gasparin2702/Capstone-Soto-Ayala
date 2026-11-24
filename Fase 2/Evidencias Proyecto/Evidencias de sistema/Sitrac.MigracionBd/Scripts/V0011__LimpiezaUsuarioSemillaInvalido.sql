DECLARE @IdUsuario BIGINT = 1;
DECLARE @NombreUsuario VARCHAR(50) = 'admin';
DECLARE @HashDocumentado VARCHAR(255) = 'HASH_PLACEHOLDER_CAMBIAR_EN_PROD';
DECLARE @HashV0007 VARCHAR(255) = '0pYh9z7T8wG5w2G5w2G5w2G5w2G5w2G5w2G5w2G5w2G5w2G5w2G5w2G=';
DECLARE @Parametros NVARCHAR(MAX) = N'@IdUsuario BIGINT, @NombreUsuario VARCHAR(50), @HashDocumentado VARCHAR(255), @HashV0007 VARCHAR(255)';

IF OBJECT_ID(N'dbo.TbSitracUsuarioRol', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TbSitracUsuarioRol', N'IdUsuario') IS NOT NULL
   AND OBJECT_ID(N'dbo.TbSitracUsuario', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TbSitracUsuario', N'IdUsuario') IS NOT NULL
   AND COL_LENGTH(N'dbo.TbSitracUsuario', N'NombreUsuario') IS NOT NULL
   AND COL_LENGTH(N'dbo.TbSitracUsuario', N'PasswordHash') IS NOT NULL
BEGIN
    EXEC sp_executesql N'
        DELETE ur
        FROM dbo.TbSitracUsuarioRol ur
        WHERE ur.IdUsuario = @IdUsuario
          AND EXISTS (
              SELECT 1
              FROM dbo.TbSitracUsuario u
              WHERE u.IdUsuario = @IdUsuario
                AND u.NombreUsuario = @NombreUsuario
                AND (u.PasswordHash = @HashDocumentado OR u.PasswordHash = @HashV0007)
          );',
        @Parametros,
        @IdUsuario = @IdUsuario,
        @NombreUsuario = @NombreUsuario,
        @HashDocumentado = @HashDocumentado,
        @HashV0007 = @HashV0007;

    EXEC sp_executesql N'
        DELETE u
        FROM dbo.TbSitracUsuario u
        WHERE u.IdUsuario = @IdUsuario
          AND u.NombreUsuario = @NombreUsuario
          AND (u.PasswordHash = @HashDocumentado OR u.PasswordHash = @HashV0007);',
        @Parametros,
        @IdUsuario = @IdUsuario,
        @NombreUsuario = @NombreUsuario,
        @HashDocumentado = @HashDocumentado,
        @HashV0007 = @HashV0007;
END;
