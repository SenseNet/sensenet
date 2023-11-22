DECLARE @OperationId varchar(128)
SELECT @OperationId = DB_NAME()

/********************************************************************** RESTORE AccessTokens */

DELETE FROM [AccessTokens]
SET IDENTITY_INSERT [AccessTokens] ON;  
INSERT INTO [AccessTokens]
		  ([AccessTokenId], [Value], [UserId], [ContentId], [Feature], [CreationDate], [ExpirationDate])
	SELECT [AccessTokenId], [Value], [UserId], [ContentId], [Feature], [CreationDate], [ExpirationDate]
	FROM  [ReinstallationTransfer].[dbo].[SavedAccessTokens]
	WHERE OperationId = @OperationId
DBCC checkident ('AccessTokens', reseed)
SET IDENTITY_INSERT [AccessTokens] OFF;

/********************************************************************** RESTORE ClientApps */

DELETE FROM [ClientSecrets]
DELETE FROM [ClientApps]
INSERT INTO [ClientApps]
	      ([ClientId], [Name], [Repository], [UserName], [Authority], [Type])
	SELECT [ClientId], [Name], [Repository], [UserName], [Authority], [Type]
	FROM  [ReinstallationTransfer].[dbo].[SavedClientApps]
	WHERE OperationId = @OperationId

/********************************************************************** RESTORE ClientSecrets */

INSERT INTO [ClientSecrets]
	      ([Id], [ClientId], [Value], [CreationDate], [ValidTill])
	SELECT [Id], [ClientId], [Value], [CreationDate], [ValidTill]
	FROM [ReinstallationTransfer].[dbo].[SavedClientSecrets]
	WHERE OperationId = @OperationId 

/********************************************************************** RESTORE Versions and Nodes */

DECLARE @CreationDate datetime2
DECLARE @VersionCreationDate datetime2
DECLARE @DynamicProperties nvarchar(max)
SELECT
	@CreationDate = CreationDate, 
	@VersionCreationDate = VersionCreationDate, 
	@DynamicProperties = DynamicProperties
FROM [ReinstallationTransfer].[dbo].[SavedVersions] WHERE OperationId = @OperationId
UPDATE [dbo].[Versions]
	SET [CreationDate] = @VersionCreationDate, [DynamicProperties] = @DynamicProperties
WHERE VersionId = 1
UPDATE [dbo].[Nodes]
	SET [CreationDate] = @CreationDate
WHERE NodeId = 1

/********************************************************************** CLEANUP */

DELETE FROM [ReinstallationTransfer].[dbo].[SavedAccessTokens] WHERE OperationId = @OperationId
DELETE FROM [ReinstallationTransfer].[dbo].[SavedVersions] WHERE OperationId = @OperationId
DELETE FROM [ReinstallationTransfer].[dbo].[SavedClientApps] WHERE OperationId = @OperationId
DELETE FROM [ReinstallationTransfer].[dbo].[SavedClientSecrets] WHERE OperationId = @OperationId 
