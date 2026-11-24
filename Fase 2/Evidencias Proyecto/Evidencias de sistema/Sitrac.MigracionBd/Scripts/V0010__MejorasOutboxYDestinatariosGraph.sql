IF COL_LENGTH(N'dbo.TbSitracNotificacionLog', N'DestinatarioCorreo') IS NULL
BEGIN
    EXEC sp_executesql N'ALTER TABLE dbo.TbSitracNotificacionLog ADD DestinatarioCorreo VARCHAR(300) NULL;';
END;

IF COL_LENGTH(N'dbo.TbSitracNotificacionLog', N'ReferenciaTicket') IS NULL
BEGIN
    EXEC sp_executesql N'ALTER TABLE dbo.TbSitracNotificacionLog ADD ReferenciaTicket VARCHAR(100) NULL;';
END;

IF COL_LENGTH(N'dbo.TbSitracNotificacionLog', N'IdMensajeProveedor') IS NULL
BEGIN
    EXEC sp_executesql N'ALTER TABLE dbo.TbSitracNotificacionLog ADD IdMensajeProveedor VARCHAR(200) NULL;';
END;

IF COL_LENGTH(N'dbo.TbSitracNotificacionLog', N'IdempotenciaKey') IS NULL
BEGIN
    EXEC sp_executesql N'ALTER TABLE dbo.TbSitracNotificacionLog ADD IdempotenciaKey VARCHAR(200) NULL;';
END;

IF COL_LENGTH(N'dbo.TbSitracNotificacionLog', N'IdempotenciaKey') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'UX_TbSitracNotificacionLog_IdempotenciaKey'
         AND object_id = OBJECT_ID(N'dbo.TbSitracNotificacionLog')
   )
BEGIN
    EXEC sp_executesql N'CREATE UNIQUE INDEX UX_TbSitracNotificacionLog_IdempotenciaKey ON dbo.TbSitracNotificacionLog (IdempotenciaKey) WHERE IdempotenciaKey IS NOT NULL;';
END;
