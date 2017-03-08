SET NOCOUNT ON

--====================================================================================== Nodetypes

DECLARE @id int
SELECT @id = PropertySetTypeId FROM [dbo].[SchemaPropertySetTypes] WHERE Name = 'NodeType'
INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName])
	VALUES (null, 'ContentType', @id, 'SenseNet.ContentRepository.Schema.ContentType')
INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName])
	VALUES (null, 'GenericContent', @id, 'SenseNet.ContentRepository.GenericContent')
GO
--====================================================================================== Create PropertyTypes and assign to NodeTypes
-------- Create PropertyGenerator procedure

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[xCreateAndAssignPropertyType]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[xCreateAndAssignPropertyType]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[xCreateAndAssignPropertyType]
	@PropertySetName varchar(450),
	@PropertyName varchar(450),
	@DataTypeName varchar(50),
	@Mapping int,
	@IsDeclared tinyint,
	@IsContentListProperty tinyint
AS
BEGIN
	-- @DataTypeName --> @DataTypeId
	DECLARE @DataTypeId int
	SELECT @DataTypeId = [DataTypeId] FROM [dbo].[SchemaDataTypes] WHERE [Name] = @DataTypeName
	-- @PropertySetName --> @PropertySetId
	DECLARE @PropertySetId int
	SELECT @PropertySetId = [PropertySetId] FROM [dbo].[SchemaPropertySets] WHERE [Name] = @PropertySetName
	-- Check PropertyType existence
	DECLARE @PropertyTypeId int
	SELECT @PropertyTypeId = [PropertyTypeId] FROM [dbo].[SchemaPropertyTypes] WHERE [Name] = @PropertyName
	-- Create PropertyType
	IF @PropertyTypeId IS NULL BEGIN
		INSERT INTO [dbo].[SchemaPropertyTypes] ([Name], [DataTypeId], [Mapping], [IsContentListProperty]) VALUES (@PropertyName, @DataTypeId, @Mapping, @IsContentListProperty)
		SET @PropertyTypeId = @@IDENTITY
	END
	-- Assign
	INSERT INTO [dbo].[SchemaPropertySetsPropertyTypes] ([PropertyTypeId], [PropertySetId], [IsDeclared]) VALUES (@PropertyTypeId, @PropertySetId, @IsDeclared)
END
GO
----------------------------------------------------------------------------------------------------

EXEC dbo.xCreateAndAssignPropertyType 'ContentType',           'Binary',           'Binary',      0, 1, 0
EXEC dbo.xCreateAndAssignPropertyType 'Folder',                'VersioningMode',   'Int',         0, 1, 0
EXEC dbo.xCreateAndAssignPropertyType 'GenericContent',        'VersioningMode',   'Int',         0, 1, 0

----------------------------------------------------------------------------------------------------

DROP PROCEDURE dbo.xCreateAndAssignPropertyType

--====================================================================================== Nodes
-------- Create NodeGenerator procedure

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[xCreateNode]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[xCreateNode]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE dbo.xCreateNode
	@NodeTypeName nvarchar(1000),
	@Index int,
	@ParentPath nvarchar(1000),
	@Name nvarchar(1000)
AS
BEGIN
	DECLARE @path nvarchar(1000)
	DECLARE @nodeTypeId int
	DECLARE @nodeId int
	DECLARE @parentId int
	DECLARE @adminId int
	DECLARE @versionId int

	SELECT @path = @ParentPath + '/' + @Name
	SELECT @nodeTypeId = PropertySetId FROM SchemaPropertySets WHERE [Name] LIKE ('%' + @NodeTypeName)
	SELECT @parentId = NodeId FROM Nodes WHERE [Path] = @ParentPath
	
	SELECT @adminId = 1

	INSERT INTO [dbo].[Nodes]
			   ([NodeTypeId], [IsDeleted], [IsInherited], [ParentNodeId], [Name], [Path], [Index], [Locked], [LockedById], [ETag], [LockType], [LockTimeout],   [LockDate], [LockToken], [LastLockUpdate], [LastMinorVersionId], [LastMajorVersionId], [CreationDate], [CreatedById], [ModificationDate], [ModifiedById], [OwnerId], [IsSystem])
		 VALUES( @nodeTypeId,           0,             1,      @parentId,  @Name,  @path,  @Index,        0,         null,     '',          0,             0, '1900-01-01',          '',     '1900-01-01',                 null,                 null,   getutcdate(),      @adminId,       getutcdate(),       @adminId,  @adminId,          1)
	SELECT @nodeId = @@IDENTITY

	INSERT INTO [dbo].[Versions] ([NodeId], [MajorNumber], [MinorNumber], [CreationDate], [CreatedById], [ModificationDate], [ModifiedById])
		 VALUES                  ( @nodeId,             1,             0,   '2007-07-07',      @adminId,       '2007-07-08',       @adminId)
	SELECT @versionId = @@IDENTITY

	UPDATE [dbo].[Nodes] SET [LastMinorVersionId] = @versionId, [LastMajorVersionId] = @versionId WHERE NodeId = @nodeId

END
GO

SET NOCOUNT ON

--================================================================================================ System structure

EXEC dbo.xCreateNode 'SystemFolder',           3, '/Root',                                           /**/ 'System'
EXEC dbo.xCreateNode 'SystemFolder',           1, '/Root/System',                                    /**/     'Schema'
EXEC dbo.xCreateNode 'SystemFolder',           1, '/Root/System/Schema',                             /**/         'ContentTypes'
EXEC dbo.xCreateNode 'SystemFolder',           2, '/Root/System',                                    /**/     'Settings'

--================================================================================================

DROP PROCEDURE dbo.xCreateNode
GO

SET NOCOUNT OFF

