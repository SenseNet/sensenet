USE [ReinstallationTransfer]

DECLARE @Source varchar(128) SET @Source = 'SnWebApplication.Api.Sql.TokenAuth2'
DECLARE @Target varchar(128) SET @Target = 'SnWebApplication.Api.Sql.TokenAuth3'

UPDATE [SavedAccessTokens]  SET [OperationId] = @Target WHERE [OperationId] = @Source
UPDATE [SavedClientApps]    SET [OperationId] = @Target WHERE [OperationId] = @Source
UPDATE [SavedClientSecrets] SET [OperationId] = @Target WHERE [OperationId] = @Source
UPDATE [SavedVersions]      SET [OperationId] = @Target WHERE [OperationId] = @Source
