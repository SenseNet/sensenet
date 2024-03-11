DECLARE @OperationId varchar(128)
SELECT @OperationId = DB_NAME()

DELETE FROM [ReinstallationTransfer].[dbo].[SavedAccessTokens] WHERE OperationId = @OperationId 
INSERT INTO [ReinstallationTransfer].[dbo].[SavedAccessTokens]
		 ([OperationId], [AccessTokenId], [Value], [UserId], [ContentId], [Feature], [CreationDate], [ExpirationDate])
	SELECT @OperationId, [AccessTokenId], [Value], [UserId], [ContentId], [Feature], [CreationDate], [ExpirationDate]
	FROM [dbo].[AccessTokens]

DELETE FROM [ReinstallationTransfer].[dbo].[SavedClientApps] WHERE OperationId = @OperationId 
INSERT INTO [ReinstallationTransfer].[dbo].[SavedClientApps]
	     ([OperationId], [ClientId], [Name], [Repository], [UserName], [Authority], [Type])
	SELECT @OperationId, [ClientId], [Name], [Repository], [UserName], [Authority], [Type]
	FROM [ClientApps]

DELETE FROM [ReinstallationTransfer].[dbo].[SavedClientSecrets] WHERE OperationId = @OperationId 
INSERT INTO [ReinstallationTransfer].[dbo].[SavedClientSecrets]
	     ([OperationId], [Id], [ClientId], [Value], [CreationDate], [ValidTill])
	SELECT @OperationId, [Id], [ClientId], [Value], [CreationDate], [ValidTill]
	FROM [ClientSecrets]

DELETE FROM [ReinstallationTransfer].[dbo].[SavedVersions] WHERE OperationId = @OperationId 
INSERT INTO [ReinstallationTransfer].[dbo].[SavedVersions]
          ([OperationId], [NodeId], [Path], [VersionId], [CreationDate], [VersionCreationDate], [DynamicProperties])
	SELECT @OperationId, n.NodeId, n.Path, v.VersionId, n.CreationDate, v.CreationDate, v.DynamicProperties
	FROM [Nodes] n JOIN [Versions] v on n.NodeId = v.NodeId
	WHERE v.VersionId = 1
