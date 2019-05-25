------------------------------------------------                             --------------------------------------------------------------
------------------------------------------------  DROP EXISTING SPs AND UDFs --------------------------------------------------------------
------------------------------------------------                             --------------------------------------------------------------

/****** Object:  StoredProcedure [dbo].[proc_TextProperty_Delete]    Script Date: 08/07/2007 14:52:40 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_TextProperty_Delete]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_TextProperty_Delete]
GO
/****** Object:  StoredProcedure [dbo].[proc_TextProperty_LoadValue]    Script Date: 08/07/2007 14:52:41 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_TextProperty_LoadValue]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_TextProperty_LoadValue]
GO
/****** Object:  StoredProcedure [dbo].[proc_TextProperty_InsertNVarchar]    Script Date: 08/07/2007 14:52:41 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_TextProperty_InsertNVarchar]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_TextProperty_InsertNVarchar]
GO
/****** Object:  StoredProcedure [dbo].[proc_VersionNumbers_GetByNodeId]    Script Date: 08/07/2007 14:52:44 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_VersionNumbers_GetByNodeId]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_VersionNumbers_GetByNodeId]
GO
/****** Object:  StoredProcedure [dbo].[proc_BinaryProperty_LoadValue]    Script Date: 08/07/2007 14:52:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_LoadValue]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_BinaryProperty_LoadValue]
GO
/****** Object:  StoredProcedure [dbo].[proc_BinaryProperty_Update]    Script Date: 08/07/2007 14:52:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_Update]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_BinaryProperty_Update]
GO
/****** Object:  StoredProcedure [dbo].[proc_Version_Update]    Script Date: 08/07/2007 14:52:43 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Version_Update]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Version_Update]
GO
/****** Object:  StoredProcedure [dbo].[proc_Version_Insert]    Script Date: 08/07/2007 14:52:42 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Version_Insert]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Version_Insert]
GO
/****** Object:  StoredProcedure [dbo].[proc_Version_CopyAndUpdate]    Script Date: 08/07/2007 14:52:42 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Version_CopyAndUpdate]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Version_CopyAndUpdate]
GO
/****** Object:  StoredProcedure [dbo].[proc_BinaryProperty_GetPointer]    Script Date: 08/07/2007 14:52:12 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_GetPointer]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_BinaryProperty_GetPointer]
GO
/****** Object:  StoredProcedure [dbo].[proc_BinaryProperty_Delete]    Script Date: 08/07/2007 14:52:11 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_Delete]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_BinaryProperty_Delete]
GO
/****** Object:  StoredProcedure [dbo].[proc_BinaryProperty_WriteStream]    Script Date: 08/07/2007 14:52:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_WriteStream]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_BinaryProperty_WriteStream]
GO
/****** Object:  StoredProcedure [dbo].[proc_Schema_LoadAll]    Script Date: 08/07/2007 14:52:40 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Schema_LoadAll]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Schema_LoadAll]
GO
/****** Object:  StoredProcedure [dbo].[proc_FlatProperties_GetExistingPages]    Script Date: 08/07/2007 14:52:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_FlatProperties_GetExistingPages]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_FlatProperties_GetExistingPages]
GO
/****** Object:  StoredProcedure [dbo].[proc_VersionNumbers_GetByPath]    Script Date: 08/07/2007 14:52:44 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_VersionNumbers_GetByPath]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_VersionNumbers_GetByPath]
GO
/****** Object:  StoredProcedure [dbo].[proc_Node_UpdateSubTreePath]    Script Date: 08/07/2007 14:52:40 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_UpdateSubTreePath]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Node_UpdateSubTreePath]
GO
/****** Object:  StoredProcedure [dbo].[proc_NodeAndVersion_Insert]    Script Date: 08/07/2007 14:52:37 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_NodeAndVersion_Insert]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_NodeAndVersion_Insert]
GO
/****** Object:  StoredProcedure [dbo].[proc_Node_Update]    Script Date: 08/07/2007 14:52:39 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_Update]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Node_Update]
GO
/****** Object:  StoredProcedure [dbo].[proc_Node_SetLastVersion]    Script Date: 08/07/2007 14:52:38 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_SetLastVersion]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Node_SetLastVersion]
GO
/****** Object:  StoredProcedure [dbo].[proc_LoadChildTypesToAllow] ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_LoadChildTypesToAllow]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_LoadChildTypesToAllow]
GO
/****** Object:  StoredProcedure [dbo].[proc_Node_Move]    Script Date: 10/01/2007 17:24:52 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_Move]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Node_Move]
GO
/****** Object:  StoredProcedure [dbo].[proc_Node_DeletePhysical]    Script Date: 08/24/2007 08:40:19 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_DeletePhysical]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Node_DeletePhysical]
GO
/****** Object:  StoredProcedure [dbo].[proc_Node_DeleteVersion]    Script Date: 08/24/2007 08:40:19 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_DeleteVersion]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Node_DeleteVersion]
GO
/****** Object:  StoredProcedure [dbo].[proc_Node_Delete]    Script Date: 08/26/2007 08:40:19 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_Delete]') AND type in (N'P', N'PC'))
--DROP PROCEDURE [dbo].[proc_Node_Delete]
--GO
/****** Object:  StoredProcedure [dbo].[proc_Node_HasChild]    Script Date: 09/25/2007 13:34:24 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_HasChild]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Node_HasChild]
GO
/****** Object:  StoredProcedure [dbo].[proc_TextProperty_InsertNText]    Script Date: 08/07/2007 14:52:41 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_TextProperty_InsertNText]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_TextProperty_InsertNText]
GO
/****** Object:  UserDefinedFunction [dbo].[udfSplitCsvToIntTable]    Script Date: 08/07/2007 14:52:44 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[udfSplitCsvToIntTable]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[udfSplitCsvToIntTable]
GO

/****** Object:  StoredProcedure [dbo].[proc_Node_LoadData_Batch]   ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_LoadData_Batch]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Node_LoadData_Batch]
GO

/****** Object:  StoredProcedure [dbo].[proc_ReferenceProperty_Update]    Script Date: 09/10/2008 17:17:41 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_ReferenceProperty_Update]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_ReferenceProperty_Update]
GO

/****** Object:  UserDefinedFunction [dbo].[udfGetAllDerivatedNodeTypesByNodeTypeId]    Script Date: 05/13/2009 11:32:51 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[udfGetAllDerivatedNodeTypesByNodeTypeId]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[udfGetAllDerivatedNodeTypesByNodeTypeId]
GO


/****** Object:  StoredProcedure [dbo].[proc_Node_GetTreeSize]    Script Date: 04/01/2010 13:10:18 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_GetTreeSize]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Node_GetTreeSize]

/****** Object:  StoredProcedure [dbo].[proc_LogAddCategory]    Script Date: 10/09/2009 10:01:53 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_LogAddCategory]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_LogAddCategory]
GO
/****** Object:  StoredProcedure [dbo].[proc_LogWrite]    Script Date: 10/09/2009 10:01:53 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_LogWrite]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_LogWrite]
GO
/****** Object:  StoredProcedure [dbo].[proc_LogSelect]    Script Date: 10/10/2009 15:12:55 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_LogSelect]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_LogSelect]
GO
/****** Object:  StoredProcedure [dbo].[proc_NodeHead_Load_Batch]    Script Date: 03/14/2010 19:43:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_NodeHead_Load_Batch]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_NodeHead_Load_Batch]
GO
------------------------------------------------                      --------------------------------------------------------------
------------------------------------------------  CREATE SPs AND UDFs --------------------------------------------------------------
------------------------------------------------                      --------------------------------------------------------------



/****** Object:  UserDefinedFunction [dbo].[udfSplitCsvToIntTable]    Script Date: 08/07/2007 14:52:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[udfSplitCsvToIntTable]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
execute dbo.sp_executesql @statement = N'CREATE FUNCTION [dbo].[udfSplitCsvToIntTable]
(
	@Csv varchar(8000)
)
RETURNS @temptable TABLE ([Id] int)
AS
BEGIN
	IF @Csv IS NOT NULL
	BEGIN
		DECLARE @pos int
		DECLARE @p int
		SET @pos = 1
		SELECT @p = charindex('','', @Csv)
		WHILE @p <> 0
		BEGIN
			INSERT INTO @temptable SELECT CONVERT(int, SUBSTRING(@Csv, @pos, @p - @pos))
			SET @pos = @p + 1
			SELECT @p = CHARINDEX('','', @Csv, @pos)
		END

		INSERT INTO @temptable
		SELECT CONVERT(int, SUBSTRING(@Csv, @pos, len(@Csv) - @pos + 1))
	END
	RETURN
END' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_Node_UpdateSubTreePath]    Script Date: 08/07/2007 14:52:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_UpdateSubTreePath]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Node_UpdateSubTreePath]
(
	@OldPath nvarchar(450),
	@NewPath nvarchar(450)
)
AS
		DECLARE @OldPathLen int
		SET @OldPathLen = LEN(@OldPath)

		UPDATE Nodes
		SET Path = @NewPath + RIGHT(Path, LEN(Path) - @OldPathLen)
		WHERE Path LIKE REPLACE(@OldPath, ''_'', ''[_]'') + ''/%''
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_Node_SetLastVersion]    Script Date: 08/07/2007 14:52:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_SetLastVersion]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Node_SetLastVersion]
(
	@NodeId int
)
AS
UPDATE Nodes
	SET LastMinorVersionId = (
		SELECT TOP (1) VersionId
		FROM Versions
		WHERE NodeId = @NodeId
		ORDER BY MajorNumber DESC, MinorNumber DESC/*, ModificationDate DESC*/),
		LastMajorVersionId = (
		SELECT TOP (1) VersionId
		FROM Versions
		WHERE NodeId = @NodeId AND MinorNumber = 0 AND Status = 1 /* Public */
		ORDER BY MajorNumber DESC, MinorNumber DESC/*, ModificationDate DESC*/)
	WHERE NodeId = @NodeId
' 
END
GO

/****** Object:  StoredProcedure [dbo].[proc_LoadChildTypesToAllow] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_LoadChildTypesToAllow]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_LoadChildTypesToAllow]
(
	@NodeId int
)
AS
DECLARE @FolderNodeTypeId int
SELECT @FolderNodeTypeId = PropertySetId FROM SchemaPropertySets WHERE Name = ''Folder''
DECLARE @PageNodeTypeId int
SELECT @PageNodeTypeId = PropertySetId FROM SchemaPropertySets WHERE Name = ''Page''

;WITH Tree(Id, ParentId, TypeId) AS
(
	SELECT NodeId, ParentNodeId, NodeTypeId
	FROM Nodes WHERE NodeId = @NodeId
	UNION ALL
	SELECT NodeId, ParentNodeId, NodeTypeId
	FROM Nodes
		INNER JOIN Tree ON Tree.Id = Nodes.ParentNodeId
	WHERE Tree.TypeId IN (@FolderNodeTypeId, @PageNodeTypeId)
)
SELECT DISTINCT S.Name FROM SchemaPropertySets S
	JOIN Nodes N ON N.NodeTypeId = S.PropertySetId
	INNER JOIN Tree ON N.NodeId = Tree.Id

'
END
GO

/****** Object:  StoredProcedure [dbo].[proc_Node_Move]    Script Date: 05/16/2008 16:06:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_Move]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Node_Move]
(
	@SourceNodeId int,
	@TargetNodeId int,
    @SourceTimestamp timestamp,
    @TargetTimestamp timestamp
)
AS

DECLARE @Path nvarchar(450)
DECLARE @HasTrans INT
SET @HasTrans = @@TRANCOUNT

-----------------------------------------------------------------------  Existence checks

IF NOT EXISTS (SELECT NodeId FROM Nodes WHERE NodeId = @TargetNodeId)
	RAISERROR (N''Cannot move under a deleted node. Id: %d'', 12, 1, @TargetNodeId);

IF NOT EXISTS (SELECT NodeId FROM Nodes WHERE NodeId = @SourceNodeId)
	RAISERROR (N''Cannot move a deleted node.Id: %d'', 12, 1, @SourceNodeId);

IF @SourceTimestamp IS NOT NULL BEGIN
	IF NOT EXISTS (SELECT NodeId FROM Nodes WHERE NodeId = @SourceNodeId and @SourceTimestamp = [Timestamp]) BEGIN
		SELECT @Path = [Path] FROM Nodes WHERE NodeId = @SourceNodeId
		RAISERROR (N''Source node is out of date. Id: %d, path: %s.'', 12, 1, @SourceNodeId, @Path);
	END
END

BEGIN TRY

	-----------------------------------------------------------------------  Ensure transactionalitality

	IF @HasTrans < 1
	BEGIN
		BEGIN TRAN TRNSP
	END

	-----------------------------------------------------------------------  Declare and Initialize variables and temp table variables

	DECLARE @AffectedSubtreeIds table (NodeId int);
	DECLARE @SourcePath nvarchar(450);
	DECLARE @SourcePathUnderscoreEscaped nvarchar(450);


	-- Get Source Path
	SELECT
		@SourcePath = [Path]
	FROM
		Nodes
	WHERE
		NodeId = @SourceNodeId

	SET @SourcePathUnderscoreEscaped = REPLACE(@SourcePath, ''_'', ''[_]'')

	-- Collect the Ids of the affected entries
	-- (the source node and all nodes under that)
	INSERT INTO
		@AffectedSubtreeIds
	SELECT
		NodeId
	FROM
		Nodes
	WHERE
		Path = @SourcePath
		OR
		Path LIKE @SourcePathUnderscoreEscaped + ''/%''

	-- source and target system flags
	DECLARE @SourceIsSystem tinyint
	DECLARE @SourceType int
	SELECT @SourceIsSystem = IsSystem, @SourceType = NodeTypeId FROM Nodes WHERE NodeId = @SourceNodeId
	DECLARE @TargetIsSystem tinyint
	DECLARE @TargetType int
	SELECT @TargetIsSystem = IsSystem, @TargetType = NodeTypeId FROM Nodes WHERE NodeId = @TargetNodeId

	-- system folder types
	DECLARE @SystemFolderIds TABLE (NodeTypeId int)
	;WITH TypeSubtree (Id)
	AS
	(
		SELECT PropertySetId Id FROM SchemaPropertySets WHERE PropertySetId IN (SELECT TOP 1 PropertySetId FROM SchemaPropertySets WHERE Name = ''SystemFolder'')
		UNION ALL
		SELECT p.PropertySetId Id FROM SchemaPropertySets AS p INNER JOIN TypeSubtree AS t ON p.ParentId = t.Id
	)
	INSERT @SystemFolderIds SELECT Id FROM TypeSubtree

	-- determine whether source is system folders
	DECLARE @SourceIsSystemFolder tinyint
	IF EXISTS (SELECT NodeTypeId FROM @SystemFolderIds WHERE NodeTypeId = @SourceType)
		SET @SourceIsSystemFolder = 1
	ELSE
		SET @SourceIsSystemFolder = 0

	-- determine whether source is system folders
	DECLARE @TargetIsSystemFolder tinyint
	IF EXISTS (SELECT NodeTypeId FROM @SystemFolderIds WHERE NodeTypeId = @TargetType)
		SET @TargetIsSystemFolder = 1
	ELSE
		SET @TargetIsSystemFolder = 0

	DECLARE @SystemFlagUpdatingStrategy varchar(9)
	IF @SourceIsSystem = 0 AND @TargetIsSystem = 0 SET @SystemFlagUpdatingStrategy = ''NoChange''
	IF @SourceIsSystem = 0 AND @TargetIsSystem = 1 SET @SystemFlagUpdatingStrategy = ''AllSystem''
	IF @SourceIsSystem = 1 AND @TargetIsSystem = 0 SET @SystemFlagUpdatingStrategy = ''Recompute''
	IF @SourceIsSystem = 1 AND @TargetIsSystem = 1 SET @SystemFlagUpdatingStrategy = ''NoChange''
	
	-----------------------------------------------------------------------  ContentList functionality: pre-check

	DECLARE @SourceTreeContentListCount int
	
	SELECT
		@SourceTreeContentListCount = COUNT(*)
	FROM
		Nodes
	WHERE
		NodeId IN (SELECT NodeId FROM @AffectedSubtreeIds)
		AND
		ContentListTypeId IS NOT NULL
		AND
		ContentListId IS NULL
	
	-- Must not move contentlists under another list(s)
	IF @SourceTreeContentListCount > 0 AND (SELECT ContentListTypeId FROM Nodes WHERE NodeId = @TargetNodeId) IS NOT NULL
	BEGIN
		RAISERROR(''Invalid operation: moving a contentlist / a subtree that contains a contentlist under an another contentlist'', 18, 2)
	END

----------------------------------------------------------------------- Move

	DECLARE @TargetPath nvarchar(450)
	DECLARE @OldPath nvarchar(450)
	DECLARE @OldPathUnderscoreEscaped nvarchar(450)
	DECLARE @SourceParentPath nvarchar(450)
	DECLARE @TargetTypePath nvarchar(450)
	DECLARE @TrashBagTypePath nvarchar(450)

	SELECT @TargetPath = Path FROM Nodes WHERE Nodes.NodeId = @TargetNodeId
	SELECT @OldPath = Path FROM Nodes WHERE Nodes.NodeId = @SourceNodeId
	SELECT @OldPathUnderscoreEscaped = REPLACE(@OldPath,''_'',''[_]'')
	SELECT @SourceParentPath = Path FROM Nodes WHERE Nodes.NodeId = (SELECT ParentNodeId FROM Nodes WHERE Nodes.NodeId = @SourceNodeId)
	SELECT @TrashBagTypePath = Path FROM Nodes WHERE (Path LIKE ''/Root/System/Schema/ContentTypes/%'' AND Name = ''TrashBag'')
	SELECT @TargetTypePath = Path FROM Nodes WHERE (Path LIKE ''/Root/System/Schema/ContentTypes/%'' AND Name = 
			(SELECT Name FROM SchemaPropertySets 
			 WHERE PropertySetId = (SELECT NodeTypeId FROM Nodes WHERE NodeId = @TargetNodeId)))

	DECLARE @OldPathLen int
	SET @OldPathLen = LEN(@SourceParentPath)


	DECLARE @SourceContentListTypeId int
	DECLARE @SourceContentListId int
	DECLARE @TargetContentListTypeId int
	DECLARE @TargetContentListId int

	SELECT  @SourceContentListTypeId = ContentListTypeId, @SourceContentListId = ContentListId
	FROM Nodes
	WHERE NodeId = @SourceNodeId

	SELECT  @TargetContentListTypeId = ContentListTypeId, @TargetContentListId = ContentListId
	FROM Nodes
	WHERE NodeId = @TargetNodeId
	
	
	-- If the source is under a ContentList (is a ContentListFolder or a ContentListItem)
	-- then the old contentlist properties have to be dropped.
	-- (except when we are moving into the trash)
	IF (@SourceContentListTypeId IS NOT NULL AND @SourceContentListId IS NOT NULL 
		AND (@TargetContentListTypeId IS NULL OR @TargetContentListTypeId <> @SourceContentListTypeId) 
		AND @TargetTypePath+ ''/'' NOT LIKE REPLACE(@TrashBagTypePath,''_'',''[_]'') + ''/%'' )
	BEGIN
		-- Get the VersionIds of the nodes to be moved.
		DECLARE @VersionsTemp table (VersionId int)
		
		INSERT INTO @VersionsTemp
		SELECT VersionId
		FROM Versions
		WHERE NodeId IN (SELECT NodeId FROM @AffectedSubtreeIds)
		
		-- Get the PropertyTypeIds of the contentlist properties
		DECLARE @ContentListPropertyTypesTemp table (PropertyTypeId int)
		
		INSERT INTO @ContentListPropertyTypesTemp
		SELECT PropertyTypeId
		FROM SchemaPropertyTypes
		WHERE IsContentListProperty = 1

		-- drop binary contentlist properties
		DELETE BinaryProperties
		WHERE
			VersionId IN (SELECT VersionId FROM @VersionsTemp)
			AND
			PropertyTypeId IN (SELECT PropertyTypeId FROM @ContentListPropertyTypesTemp)
		
		-- drop NText contentlist properties
		DELETE
			TextPropertiesNText
		WHERE
			VersionId IN (SELECT VersionId FROM @VersionsTemp)
			AND
			PropertyTypeId IN (SELECT PropertyTypeId FROM @ContentListPropertyTypesTemp)
			
		-- drop NVarchar contentlist properties
		DELETE
			TextPropertiesNVarchar
		WHERE
			VersionId IN (SELECT VersionId FROM @VersionsTemp)
			AND
			PropertyTypeId IN (SELECT PropertyTypeId FROM @ContentListPropertyTypesTemp)

		-- drop flat contentlist properties
		DELETE
			FlatProperties
		WHERE
			VersionId IN (SELECT VersionId FROM @VersionsTemp)
			AND
			Page >= 10000000
			
		---- The target is NOT a ContentList nor a ContentListFolder.
		---- ContentListTypeId, ContentListId should be updated to null.
		---- (except if it is the trash)
		UPDATE
			Nodes
		SET
			ContentListTypeId = null,
			ContentListId = null
		WHERE
			NodeId IN (SELECT NodeId FROM @AffectedSubtreeIds)
	END
	
	-- If the target is a ContentList or a ContentListFolder
	-- then the ContentListTypeId and ContentListId should be updated to the new ContentListTypeId and ContentListId. 
	-- (except if the source node already has a ContentListTypeId)
	IF (@TargetContentListTypeId IS NOT NULL)
	BEGIN
		
		IF @TargetContentListId IS NULL
			-- In this case the ContentListId is null, because the ContentListId is the NodeId.
			SET @TargetContentListId = @TargetNodeId

		UPDATE
			Nodes
		SET
			ContentListTypeId = @TargetContentListTypeId,
			ContentListId = @TargetContentListId
		WHERE
			NodeId IN (SELECT NodeId FROM @AffectedSubtreeIds)
		
	END
	
	--==== Updating subtree by strategy (@SystemFlagUpdatingStrategy: ''NoChange'' | ''AllSystem'' | ''Recompute''
	IF @SystemFlagUpdatingStrategy = ''NoChange'' BEGIN
		--	subtree root
		UPDATE Nodes
		SET 
			Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen),
			ParentNodeId = @TargetNodeId
		WHERE Nodes.NodeId = @SourceNodeId
		--	subtree elements
		UPDATE Nodes
		SET Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen)
		WHERE Path LIKE @OldPathUnderscoreEscaped + ''/%''
	END
	ELSE IF @SystemFlagUpdatingStrategy = ''AllSystem'' BEGIN
		--	subtree root
		UPDATE Nodes
		SET 
			Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen),
			ParentNodeId = @TargetNodeId,
			IsSystem = 1
		WHERE Nodes.NodeId = @SourceNodeId
		--	subtree elements
		UPDATE Nodes
			SET Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen),
				IsSystem = 1
		WHERE Path LIKE @OldPathUnderscoreEscaped + ''/%''
	END
	ELSE IF @SystemFlagUpdatingStrategy = ''Recompute'' BEGIN
		--	subtree root
		UPDATE Nodes
		SET 
			Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen),
			ParentNodeId = @TargetNodeId,
			IsSystem = @SourceIsSystemFolder
		WHERE Nodes.NodeId = @SourceNodeId
		--	reset subtree elements
		UPDATE Nodes
			SET Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen),
				IsSystem = 0
		WHERE Path LIKE @OldPathUnderscoreEscaped + ''/%''

		-- set IsSystem flag on all nodes that have SystemFolder ancestor in this subtree
		DECLARE @currentPath nvarchar(450)
		DECLARE sysfolder_cursor CURSOR FOR  
			SELECT [Path] FROM Nodes WHERE [Path] LIKE  REPLACE(@TargetPath,''_'',''[_]'') + ''/%'' AND NodeTypeId IN (SELECT NodeTypeId FROM @SystemFolderIds)
		OPEN sysfolder_cursor   
		FETCH NEXT FROM sysfolder_cursor INTO @currentPath   
		WHILE @@FETCH_STATUS = 0   
		BEGIN   
			UPDATE Nodes SET IsSystem = 1 WHERE NodeId IN (
				SELECT NodeId
				FROM Nodes
				WHERE Path = @currentPath OR Path LIKE REPLACE(@currentPath,''_'',''[_]'') + ''/%''
			)
			FETCH NEXT FROM sysfolder_cursor INTO @currentPath   
		END   
		CLOSE sysfolder_cursor   
		DEALLOCATE sysfolder_cursor
	END

	-- commit
	IF @HasTrans < 1
	BEGIN
		COMMIT TRAN TRNSP
	END
END TRY
BEGIN CATCH

  -- there was an error
  	IF @HasTrans < 1
	BEGIN
		ROLLBACK TRAN TRNSP
	END

  DECLARE @ErrMsg nvarchar(4000), @ErrSeverity int, @ErrState int
  SELECT 
	@ErrMsg = ERROR_MESSAGE(),
    @ErrSeverity = ERROR_SEVERITY(),
    @ErrState = ERROR_STATE();
  IF @ErrSeverity >= 18 
	SET @ErrSeverity = 12
  RAISERROR(@ErrMsg, @ErrSeverity, @ErrState)

END CATCH
' 
END
GO

/****** Object:  StoredProcedure [dbo].[proc_Node_DeletePhysical]    Script Date: 09/14/2008 12:32:04 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_DeletePhysical]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Node_DeletePhysical]
	@NodeId INT,
	@Timestamp TIMESTAMP,
	@PartitionSize INT = 100
AS
BEGIN
	DECLARE @Path NVARCHAR(450)
	IF @Timestamp IS NOT NULL AND (NOT EXISTS (SELECT NodeId FROM Nodes WHERE NodeId = @NodeId and @Timestamp = [Timestamp])) BEGIN
		SELECT @Path = [Path] FROM Nodes WHERE NodeId = @NodeId
		RAISERROR (N''Node is out of date. Id: %d, path: %s.'', 12, 1, @NodeId, @Path);
	END
	ELSE BEGIN
		SET NOCOUNT ON
		DECLARE @LocalTranNeeded BIT
			
		IF @@TRANCOUNT < 1
			SET @LocalTranNeeded = 1
		ELSE
			SET @LocalTranNeeded = 0

		DECLARE @startpath nvarchar(450)
		SELECT @startpath = REPLACE(REPLACE(Path, ''['', ''\[''), '']'', ''\]'') FROM Nodes WHERE NodeId = @NodeId

		DECLARE @NIDall TABLE (Id INT IDENTITY(1, 1), NodeId INT)
		DECLARE @NIDpartition TABLE (Id INT IDENTITY(1, 1), NodeId INT)
		DECLARE @VID TABLE (Id INT IDENTITY(1, 1), VersionId INT)

		INSERT INTO @NIDall 
			SELECT NodeId FROM Nodes 
			WHERE Path = @startpath OR Path LIKE REPLACE(@startpath, ''_'', ''[_]'') + ''/%''
			ORDER BY Path DESC

		DECLARE @nodeCount INT
		SELECT @nodeCount = COUNT(1) FROM @NIDall

		WHILE @nodeCount > 0 BEGIN
			BEGIN TRY
				IF @LocalTranNeeded = 1
					BEGIN TRAN LocalTran

				DELETE FROM @NIDpartition
				INSERT INTO @NIDpartition
					SELECT TOP(@PartitionSize) NodeId FROM @NIDall ORDER BY Id

				DELETE FROM @VID
				INSERT INTO @VID
					SELECT VersionId FROM Versions WHERE NodeId IN (SELECT NodeId FROM @NIDpartition)

				--=============================================================

				DELETE BinaryProperties WHERE VersionId IN (SELECT VersionId FROM @VID)
				DELETE TextPropertiesNText WHERE VersionId IN (SELECT VersionId FROM @VID)
				DELETE TextPropertiesNVarchar WHERE VersionId IN (SELECT VersionId FROM @VID)
				DELETE FlatProperties WHERE VersionId IN (SELECT VersionId FROM @VID)
				DELETE ReferenceProperties WHERE (VersionId IN (SELECT VersionId FROM @VID)) OR
												 (ReferredNodeId IN (SELECT NodeId FROM @NIDpartition))

				DELETE Versions WHERE NodeId IN (SELECT NodeId FROM @NIDpartition)
				DELETE Nodes WHERE NodeId IN (SELECT NodeId FROM @NIDpartition)

				--=============================================================

				IF @LocalTranNeeded = 1
					COMMIT TRAN LocalTran

				DELETE FROM @NIDall WHERE NodeId IN (SELECT NodeId FROM @NIDpartition)
				SELECT @nodeCount = COUNT(1) FROM @NIDall
								
				print convert(varchar(10), @nodeCount) + '' nodes left''
			END TRY
			BEGIN CATCH
				SET NOCOUNT OFF
				IF @LocalTranNeeded = 1
					ROLLBACK TRAN LocalTran
				DECLARE @ErrMsg nvarchar(4000), @ErrSeverity int
				SELECT 
					@ErrMsg = ERROR_MESSAGE(),
					@ErrSeverity = ERROR_SEVERITY()
				RAISERROR(@ErrMsg, @ErrSeverity, 1)
				RETURN
			END CATCH
		END -- WHILE
	END -- ELSE
	SET NOCOUNT OFF
END
' 
END
GO

/****** Object:  StoredProcedure [dbo].[proc_Node_DeleteVersion]    Script Date: 08/24/2007 08:40:19 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_DeleteVersion]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Node_DeleteVersion] 
(
	@VersionId int
)
AS
BEGIN

DECLARE @NodeId int
SELECT @NodeId = NodeId FROM Versions WHERE VersionId = @VersionId

	DELETE FROM BinaryProperties WHERE VersionId = @VersionId

	DELETE FROM TextPropertiesNText WHERE VersionId = @VersionId

	DELETE FROM TextPropertiesNVarchar WHERE VersionId = @VersionId

	DELETE FROM ReferenceProperties WHERE VersionId = @VersionId

	DELETE FROM FlatProperties WHERE VersionId = @VersionId

--ALTER TABLE [BinaryProperties] NOCHECK CONSTRAINT ALL
--ALTER TABLE [FlatProperties] NOCHECK CONSTRAINT ALL
--ALTER TABLE [Nodes] NOCHECK CONSTRAINT ALL
--ALTER TABLE [ReferenceProperties] NOCHECK CONSTRAINT ALL
--ALTER TABLE [TextPropertiesNText] NOCHECK CONSTRAINT ALL
--ALTER TABLE [TextPropertiesNVarchar] NOCHECK CONSTRAINT ALL
--ALTER TABLE [Versions] NOCHECK CONSTRAINT ALL
--ALTER TABLE [VersionExtensions] NOCHECK CONSTRAINT ALL

	UPDATE Nodes SET LastMinorVersionId = NULL, LastMajorVersionId = NULL WHERE NodeId = @NodeId

	DELETE FROM Versions WHERE VersionId = @VersionId

--ALTER TABLE [BinaryProperties] CHECK CONSTRAINT ALL
--ALTER TABLE [FlatProperties] CHECK CONSTRAINT ALL
--ALTER TABLE [Nodes] CHECK CONSTRAINT ALL
--ALTER TABLE [ReferenceProperties] CHECK CONSTRAINT ALL
--ALTER TABLE [TextPropertiesNText] CHECK CONSTRAINT ALL
--ALTER TABLE [TextPropertiesNVarchar] CHECK CONSTRAINT ALL
--ALTER TABLE [Versions] CHECK CONSTRAINT ALL
--ALTER TABLE [VersionExtensions] CHECK CONSTRAINT ALL

	EXEC proc_Node_SetLastVersion @NodeId = @NodeId

-- Return the new timestamp and version ids
SELECT [Timestamp] as NodeTimestamp, LastMajorVersionId, LastMinorVersionId FROM Nodes WHERE NodeId = @NodeId

END
' 
END
GO

/****** Object:  StoredProcedure [dbo].[proc_Node_HasChild]    Script Date: 09/25/2007 12:04:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_HasChild]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Node_HasChild]
	@NodeId int
AS
BEGIN

SELECT Count(*)
  FROM Nodes
 WHERE ParentNodeId = @NodeId

END
' 
END
GO

/****** Object:  StoredProcedure [dbo].[proc_NodeAndVersion_Insert]    Script Date: 04/15/2008 13:46:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_NodeAndVersion_Insert]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_NodeAndVersion_Insert]
(
	@NodeTypeId int,
	@ContentListTypeId int = null,
	@ContentListId int = null,
	@CreatingInProgress tinyint,
	@IsDeleted tinyint,
	@IsInherited tinyint,
	@ParentNodeId int,
	@Name nvarchar(450),
	@DisplayName nvarchar(450),
	@Path nvarchar(450),
	@Index int,
	@Locked tinyint,
	@LockedById int,
	@ETag varchar(50),
	@LockType int,
	@LockTimeout int,
	@LockDate datetime,
	@LockToken varchar(50),
	@LastLockUpdate datetime,
	@NodeCreationDate datetime,
	@NodeCreatedById int,
	@NodeModificationDate datetime,
	@NodeModifiedById int,
	@IsSystem tinyint,
	@OwnerId int,
	@SavingState int,
	@ChangedData ntext,
	@MajorNumber smallint,
	@MinorNumber smallint,
	@Status smallint,
	@CreationDate datetime,
	@CreatedById int,
	@ModificationDate datetime,
	@ModifiedById int
)
AS
-- for result
DECLARE @NodeId int, @VersionId int
DECLARE @NodeTimestamp timestamp, @VersionTimestamp timestamp

-- with calculated path
INSERT INTO Nodes
	(NodeTypeId, ContentListTypeId, ContentListId, CreatingInProgress, IsDeleted, IsInherited, ParentNodeId, [Name], DisplayName, [Index], Locked, LockedById, ETag, LockType, LockTimeout, LockDate, LockToken, LastLockUpdate, CreationDate, CreatedById, ModificationDate, ModifiedById, IsSystem, OwnerId, SavingState
		, Path)
	VALUES
	(@NodeTypeId, @ContentListTypeId, @ContentListId, @CreatingInProgress, @IsDeleted, @IsInherited, @ParentNodeId, @Name, @DisplayName, @Index, @Locked, @LockedById, @ETag, @LockType, @LockTimeout, @LockDate, @LockToken, @LastLockUpdate, @NodeCreationDate, @NodeCreatedById, @NodeModificationDate, @NodeModifiedById, @IsSystem, @OwnerId, @SavingState
		,(select [Path] from Nodes where NodeId = @ParentNodeId) + ''/'' + @Name)

SELECT @NodeId = @@IDENTITY

-- skip the rest, if the insert above was not successful
IF (@NodeId is NOT NULL)
BEGIN
	INSERT INTO Versions 
		( NodeId,  MajorNumber,  MinorNumber,  CreationDate,  CreatedById,  ModificationDate,  ModifiedById,  Status,  ChangedData) VALUES
		(@NodeId, @MajorNumber, @MinorNumber, @CreationDate, @CreatedById, @ModificationDate, @ModifiedById, @Status, @ChangedData)

	SELECT @VersionId = @@IDENTITY
	SELECT @VersionTimestamp = [Timestamp] FROM Versions WHERE VersionId = @VersionId

	IF @Status = 1
		UPDATE Nodes SET LastMinorVersionId = @VersionId, LastMajorVersionId = @VersionId WHERE NodeId = @NodeId
	ELSE
		UPDATE Nodes SET LastMinorVersionId = @VersionId WHERE NodeId = @NodeId
	SELECT @NodeTimestamp = [Timestamp] FROM Nodes WHERE NodeId = @NodeId

	SELECT @NodeId, @VersionId, @NodeTimestamp, @VersionTimestamp, LastMajorVersionId, LastMinorVersionId, Path FROM Nodes WHERE NodeId = @NodeId
END
' 
END
GO

/****** Object:  StoredProcedure [dbo].[proc_Node_Update]    Script Date: 04/15/2008 09:15:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_Update]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Node_Update]
(
	@NodeId int,
	@NodeTypeId int,
	@ContentListTypeId int = null,
	@ContentListId int = null,
	@CreatingInProgress tinyint,
	@IsDeleted tinyint,
	@IsInherited tinyint,
	@ParentNodeId int,
	@Name nvarchar(450),
	@DisplayName nvarchar(450),
	@Path nvarchar(450),
	@Index int,
	@Locked tinyint,
	@LockedById int,
	@ETag varchar(50),
	@LockType int,
	@LockTimeout int,
	@LockDate datetime,
	@LockToken varchar(50),
	@LastLockUpdate datetime,
	@CreationDate datetime,
	@CreatedById int,
	@ModificationDate datetime,
	@ModifiedById int,
	@IsSystem tinyint,
	@OwnerId int,
	@SavingState int,
	@NodeTimestamp timestamp
)
AS

IF (@NodeId = 2)
	BEGIN

-- Root node: no need to deal with parent
UPDATE Node SET
	Node.NodeTypeId = @NodeTypeId,
	Node.ContentListTypeId = @ContentListTypeId,
	Node.ContentListId = @ContentListId,
	Node.CreatingInProgress = @CreatingInProgress,
	Node.IsDeleted = @IsDeleted,
	Node.IsInherited = @IsInherited,
	Node.ParentNodeId = @ParentNodeId,
	Node.[Name] = @Name,
	Node.DisplayName = @DisplayName,
	Node.Path = ''/Root'',
	Node.[Index] = @Index,
	Node.Locked = @Locked,
	Node.LockedById = @LockedById,
	Node.ETag = @ETag,
	Node.LockType = @LockType,
	Node.LockTimeout = @LockTimeout,
	Node.LockDate = @LockDate,
	Node.LockToken = @LockToken,
	Node.LastLockUpdate = @LastLockUpdate,
	Node.CreationDate = @CreationDate,
	Node.CreatedById = @CreatedById,
	Node.ModificationDate = @ModificationDate,
	Node.ModifiedById = @ModifiedById,
	Node.IsSystem = @IsSystem,
	Node.OwnerId = @OwnerId,
	Node.SavingState = @SavingState
FROM
	Nodes Node 
WHERE Node.NodeId = @NodeId AND Node.[Timestamp] = @NodeTimestamp
		
	END
	ELSE BEGIN

-- with calculated path
UPDATE Node SET
	Node.NodeTypeId = @NodeTypeId,
	Node.ContentListTypeId = @ContentListTypeId,
	Node.ContentListId = @ContentListId,
	Node.CreatingInProgress = @CreatingInProgress,
	Node.IsDeleted = @IsDeleted,
	Node.IsInherited = @IsInherited,
	Node.ParentNodeId = @ParentNodeId,
	Node.[Name] = @Name,
	Node.DisplayName = @DisplayName,
	Node.Path = Parent.Path + ''/'' + @Name,
	Node.[Index] = @Index,
	Node.Locked = @Locked,
	Node.LockedById = @LockedById,
	Node.ETag = @ETag,
	Node.LockType = @LockType,
	Node.LockTimeout = @LockTimeout,
	Node.LockDate = @LockDate,
	Node.LockToken = @LockToken,
	Node.LastLockUpdate = @LastLockUpdate,
	Node.CreationDate = @CreationDate,
	Node.CreatedById = @CreatedById,
	Node.ModificationDate = @ModificationDate,
	Node.ModifiedById = @ModifiedById,
	Node.IsSystem = @IsSystem,
	Node.OwnerId = @OwnerId,
	Node.SavingState = @SavingState
FROM
	Nodes Node JOIN Nodes Parent ON Parent.NodeId = Node.ParentNodeId
WHERE Node.NodeId = @NodeId AND Node.[Timestamp] = @NodeTimestamp

	END

IF @@ROWCOUNT = 0 BEGIN
	DECLARE @Count int
	SELECT @Count = COUNT(*) FROM Nodes WHERE NodeId = @NodeId
	IF @Count = 0
		RAISERROR (N''Cannot update a deleted Node. Id: %d, path: %s.'', 12, 1, @NodeId, @Path);
	ELSE
		RAISERROR (N''Node is out of date Id: %d, path: %s.'', 12, 1, @NodeId, @Path);
END
ELSE BEGIN
	SELECT [Path], [Timestamp] FROM Nodes WHERE NodeId = @NodeId
END
'
END
GO

/****** Object:  StoredProcedure [dbo].[proc_Schema_LoadAll]    Script Date: 08/07/2007 14:52:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Schema_LoadAll]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Schema_LoadAll]
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL SNAPSHOT;
	
	BEGIN TRANSACTION;

	SELECT TOP 1 * FROM [dbo].[SchemaModification]
	SELECT * FROM [dbo].[SchemaDataTypes]
	SELECT * FROM [dbo].[SchemaPropertySetTypes]
	SELECT * FROM [dbo].[SchemaPropertySets]
	SELECT * FROM [dbo].[SchemaPropertyTypes]
	SELECT * FROM [dbo].[SchemaPropertySetsPropertyTypes]

	COMMIT TRANSACTION;
END
' 
END
GO

/****** Object:  StoredProcedure [dbo].[proc_VersionNumbers_GetByPath]    Script Date: 08/07/2007 14:52:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_VersionNumbers_GetByPath]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_VersionNumbers_GetByPath]
(
	@Path nvarchar(450)
)
AS
	SELECT MajorNumber, MinorNumber, Status
	FROM Nodes node
	INNER JOIN Versions version ON node.NodeId = version.NodeId
	WHERE Path = @Path
	ORDER BY MajorNumber, MinorNumber
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_VersionNumbers_GetByNodeId]    Script Date: 08/07/2007 14:52:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_VersionNumbers_GetByNodeId]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_VersionNumbers_GetByNodeId]
(
	@NodeId int
)
AS
	SELECT MajorNumber, MinorNumber, Status FROM Versions
	WHERE NodeId = @NodeId
	ORDER BY MajorNumber, MinorNumber
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_TextProperty_Delete]    Script Date: 08/07/2007 14:52:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_TextProperty_Delete]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_TextProperty_Delete]
(
	@VersionId int,
	@PropertyTypeId int
)
AS
	DELETE FROM TextPropertiesNText
	 WHERE (VersionId = @VersionId) AND (PropertyTypeId = @PropertyTypeId)

	DELETE FROM TextPropertiesNVarchar
	 WHERE (VersionId = @VersionId) AND (PropertyTypeId = @PropertyTypeId)
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_TextProperty_LoadValue]    Script Date: 08/07/2007 14:52:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_TextProperty_LoadValue]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_TextProperty_LoadValue]
(
	@VersionId int,
	@PropertyTypeId int
)
AS
	SELECT Value
	FROM TextPropertiesNText
	WHERE VersionId = @VersionId
	AND PropertyTypeId = @PropertyTypeId
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_TextProperty_InsertNText]    Script Date: 08/07/2007 14:52:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_TextProperty_InsertNText]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_TextProperty_InsertNText]
(
	@VersionId int,
	@PropertyTypeId int,
	@Value ntext
)
AS
	  INSERT INTO TextPropertiesNText (
					  VersionId,
					  PropertyTypeId,
					  Value)
	   VALUES (		  @VersionId,
					  @PropertyTypeId,
					  @Value)
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_TextProperty_InsertNVarchar]    Script Date: 08/07/2007 14:52:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_TextProperty_InsertNVarchar]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_TextProperty_InsertNVarchar]
(
	@VersionId int,
	@PropertyTypeId int,
	@Value nvarchar(4000)
)
AS
	  INSERT INTO TextPropertiesNVarchar (
					  VersionId,
					  PropertyTypeId,
					  Value)
	   VALUES (		  @VersionId,
					  @PropertyTypeId,
					  @Value)
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_BinaryProperty_LoadValue]   ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_LoadValue]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_LoadValue]
	@VersionId int,
	@PropertyTypeId int
AS
BEGIN
	SELECT B.BinaryPropertyId, B.VersionId, B.PropertyTypeId, F.FileId, F.ContentType, F.FileNameWithoutExtension,
		F.Extension, F.[Size], F.[Checksum], NULL AS Stream, 0 AS Loaded, F.[Timestamp], F.[BlobProvider], F.[BlobProviderData] 
	FROM dbo.BinaryProperties B
		JOIN dbo.Files F ON B.FileId = F.FileId
	WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId AND Staging IS NULL
END' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_BinaryProperty_Update]    Script Date: 08/07/2007 14:52:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_Update]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_Update]
(
	@BinaryPropertyId int,
	@ContentType nvarchar(450),
	@FileNameWithoutExtension nvarchar(450),
	@Extension nvarchar(50),
	@Size bigint,
	@Checksum varchar(200),
	@BlobProvider nvarchar(450),
	@BlobProviderData nvarchar(max)
)
AS
DECLARE @FileId int
SELECT @FileId = FileId FROM BinaryProperties WHERE BinaryPropertyId = @BinaryPropertyId

DECLARE @EnsureNewFileRow tinyint
IF (@BlobProvider IS NULL) AND (EXISTS (SELECT FileId FROM Files WHERE @FileId = FileId AND BlobProvider IS NOT NULL))
	SET @EnsureNewFileRow = 1
ELSE
	SET @EnsureNewFileRow = 0

IF (@EnsureNewFileRow = 1) OR (EXISTS (SELECT FileId FROM BinaryProperties WHERE FileId = @FileId AND BinaryPropertyId != @BinaryPropertyId)) BEGIN
	INSERT INTO Files (ContentType, FileNameWithoutExtension, Extension, [Size], [BlobProvider], [BlobProviderData], [Checksum], [Stream])
	    VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, @BlobProvider, @BlobProviderData,
			CASE WHEN (@Size <= 0) THEN NULL ELSE @Checksum END,
			CASE WHEN (@Size <= 0) THEN NULL ELSE CONVERT(varbinary, '''') END)

	SELECT @FileId = @@IDENTITY

	UPDATE BinaryProperties SET FileId = @FileId WHERE BinaryPropertyId = @BinaryPropertyId
END
ELSE BEGIN
	UPDATE Files
	SET	ContentType = @ContentType,
		FileNameWithoutExtension = @FileNameWithoutExtension,
		Extension = @Extension,
		[Size] = @Size,
		[BlobProvider] = @BlobProvider,
		[BlobProviderData] = @BlobProviderData,
		-- [Checksum] = IIF (@Size <= 0, NULL, @Checksum)
		-- [Stream]   = IIF (@Size <= 0, NULL, CONVERT(varbinary, '''')
		[Checksum] = CASE WHEN (@Size <= 0) THEN NULL ELSE @Checksum END,
		[Stream]   = CASE WHEN (@Size <= 0) THEN NULL ELSE CONVERT(varbinary, '''') END
	WHERE FileId = @FileId
END
SELECT @FileId' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_BinaryProperty_GetPointer]    Script Date: 08/07/2007 14:52:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_GetPointer]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_GetPointer]
(
	@VersionId int,
	@PropertyTypeId int,
	@FileId int OUTPUT,
	@Length int OUTPUT
)
AS
	SELECT @FileId = F.FileId, 
		@Length = DATALENGTH(F.Stream)
	FROM BinaryProperties B
		JOIN Files F ON F.FileId = B.FileId
	WHERE B.VersionId = @VersionId AND B.PropertyTypeId = @PropertyTypeId AND F.Staging IS NULL
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_BinaryProperty_Delete]    Script Date: 08/07/2007 14:52:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_Delete]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_Delete]
(
	@VersionId int,
	@PropertyTypeId int
)
AS
	DELETE FROM BinaryProperties
	WHERE VersionId = @VersionId
	 AND PropertyTypeId = @PropertyTypeId
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_BinaryProperty_WriteStream]    Script Date: 08/07/2007 14:52:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_WriteStream]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_WriteStream]
(
	@Id int,
	@Offset int,
	@Value varbinary(max)
)
AS
	UPDATE Files SET Stream = @Value WHERE FileId = @Id;
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_FlatProperties_GetExistingPages]    Script Date: 08/07/2007 14:52:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_FlatProperties_GetExistingPages]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_FlatProperties_GetExistingPages]
(
	@VersionId int
)
AS
	SELECT Page FROM FlatProperties WHERE VersionId = @VersionId
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_Version_Update]    Script Date: 08/07/2007 14:52:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Version_Update]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Version_Update]
(
	@VersionId int,
	@NodeId int,
	@MajorNumber smallint = null,
	@MinorNumber smallint = null,
	@CreationDate datetime,
	@CreatedById int,
	@ModificationDate datetime,
	@ModifiedById int,
	@Status smallint = null,
	@ChangedData ntext = null
)
AS
if(@MajorNumber is null) begin
	if(@MinorNumber is not null or @Status is not null)
		RAISERROR(''If @MajorNumber is null, @MinorNumber and @Status must be null.'', 17, 1)
	UPDATE Versions SET
		NodeId = @NodeId,
		CreationDate = @CreationDate,
		CreatedById = @CreatedById,
		ModificationDate = @ModificationDate,
		ModifiedById = @ModifiedById,
		ChangedData = @ChangedData
	WHERE VersionId = @VersionId
end
else begin
	if(@MinorNumber is null or @Status is null)
		RAISERROR(''If @MajorNumber is not null, @MinorNumber and @Status must not be null.'', 17, 1)
	UPDATE Versions SET
		NodeId = @NodeId,
		MajorNumber = @MajorNumber,
		MinorNumber = @MinorNumber,
		CreationDate = @CreationDate,
		CreatedById = @CreatedById,
		ModificationDate = @ModificationDate,
		ModifiedById = @ModifiedById,
		Status = @Status,
		ChangedData = @ChangedData
	WHERE VersionId = @VersionId
	EXEC proc_Node_SetLastVersion @NodeId
end

-- Return
DECLARE @NodeTimestamp timestamp
DECLARE @LastMajorVersionId int
DECLARE @LastMinorVersionId int
SELECT @NodeTimestamp = [Timestamp], @LastMajorVersionId = LastMajorVersionId, @LastMinorVersionId = LastMinorVersionId FROM Nodes WHERE NodeId = @NodeId
SELECT @NodeTimestamp as NodeTimestamp, [Timestamp] as VersionTimestamp, @LastMajorVersionId as LastMajorVersionId, @LastMinorVersionId as LastMinorVersionId FROM Versions WHERE VersionId = @VersionId
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_Version_Insert]    Script Date: 08/07/2007 14:52:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Version_Insert]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Version_Insert]
(
	@NodeId int,
	@MajorNumber smallint,
	@MinorNumber smallint,
	@CreationDate datetime,
	@CreatedById int,
	@ModificationDate datetime,
	@ModifiedById int,
	@Status smallint,
	@ChangedData ntext
)
AS

-- Before inserting set versioning status code from "Locked" to "Draft" on all older versions
UPDATE Versions SET Status = 4 WHERE NodeId = @NodeId AND Status = 2

INSERT INTO Versions (
	NodeId,
	MajorNumber,
	MinorNumber,
	CreationDate,
	CreatedById,
	ModificationDate,
	ModifiedById,
	Status,
	ChangedData
	) VALUES (
	@NodeId,
	@MajorNumber,
	@MinorNumber,
	@CreationDate,
	@CreatedById,
	@ModificationDate,
	@ModifiedById,
	@Status,
	@ChangedData
	)


-- > instead of EXEC proc_Node_SetLastVersion @NodeId
DECLARE @VersionId int
SELECT @VersionId = @@IDENTITY
IF @Status = 1
	UPDATE Nodes SET LastMinorVersionId = @VersionId, LastMajorVersionId = @VersionId WHERE NodeId = @NodeId
ELSE
	UPDATE Nodes SET LastMinorVersionId = @VersionId WHERE NodeId = @NodeId
-- <


SELECT VersionId, [Timestamp] FROM Versions WHERE VersionId = @VersionId
SELECT LastMajorVersionId, LastMinorVersionId FROM Nodes WHERE NodeId = @NodeId 
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_Version_CopyAndUpdate]    Script Date: 04/20/2010 14:35:48 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Version_CopyAndUpdate]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Version_CopyAndUpdate] 
	@PreviousVersionId int,
	@DestinationVersionId int,
	@NodeId int,
	@MajorNumber smallint,
	@MinorNumber smallint,
	@CreationDate datetime,
	@CreatedById int,
	@ModificationDate datetime,
	@ModifiedById int,
	@Status smallint,
	@ChangedData ntext
AS
BEGIN
	DECLARE @NewVersionId int
	
	-- Before inserting set versioning status code from "Locked" to "Draft" on all older versions
	UPDATE Versions SET Status = 4 WHERE NodeId = @NodeId AND Status = 2

	IF @DestinationVersionId IS NULL
	BEGIN
		-- Insert version row
		INSERT INTO Versions
			( NodeId, MajorNumber, MinorNumber, CreationDate, CreatedById, ModificationDate, ModifiedById, Status, ChangedData)
			VALUES
			(@NodeId,@MajorNumber,@MinorNumber,@CreationDate,@CreatedById,@ModificationDate,@ModifiedById,@Status,@ChangedData)
		SELECT @NewVersionId = @@IDENTITY
	END
	ELSE
	BEGIN
		-- Update existing version
		SET @NewVersionId = @DestinationVersionId;
		
		UPDATE Versions SET
			NodeId = @NodeId,
			MajorNumber = @MajorNumber,
			MinorNumber = @MinorNumber,
			CreationDate = @CreationDate,
			CreatedById = @CreatedById,
			ModificationDate = @ModificationDate,
			ModifiedById = @ModifiedById,
			Status = @Status,
			ChangedData =	@ChangedData
		WHERE VersionId = @NewVersionId
		
		-- Delete previous property values
		DELETE FROM BinaryProperties WHERE VersionId = @NewVersionId;
		DELETE FROM FlatProperties WHERE VersionId = @NewVersionId;
		DELETE FROM ReferenceProperties WHERE VersionId = @NewVersionId;
		DELETE FROM TextPropertiesNVarchar WHERE VersionId = @NewVersionId;
		DELETE FROM TextPropertiesNText WHERE VersionId = @NewVersionId;
	END	
	

	-- Copy properties
	INSERT INTO BinaryProperties ([VersionId],[PropertyTypeId],[FileId])
		SELECT @NewVersionId,[PropertyTypeId],[FileId] FROM BinaryProperties WHERE VersionId = @PreviousVersionId
	INSERT INTO FlatProperties
		([VersionId],[Page]
			,[nvarchar_1],[nvarchar_2],[nvarchar_3],[nvarchar_4],[nvarchar_5],[nvarchar_6],[nvarchar_7],[nvarchar_8],[nvarchar_9],[nvarchar_10],[nvarchar_11],[nvarchar_12],[nvarchar_13],[nvarchar_14],[nvarchar_15],[nvarchar_16],[nvarchar_17],[nvarchar_18],[nvarchar_19],[nvarchar_20],[nvarchar_21],[nvarchar_22],[nvarchar_23],[nvarchar_24],[nvarchar_25],[nvarchar_26],[nvarchar_27],[nvarchar_28],[nvarchar_29],[nvarchar_30],[nvarchar_31],[nvarchar_32],[nvarchar_33],[nvarchar_34],[nvarchar_35],[nvarchar_36],[nvarchar_37],[nvarchar_38],[nvarchar_39],[nvarchar_40]
			,[nvarchar_41],[nvarchar_42],[nvarchar_43],[nvarchar_44],[nvarchar_45],[nvarchar_46],[nvarchar_47],[nvarchar_48],[nvarchar_49],[nvarchar_50],[nvarchar_51],[nvarchar_52],[nvarchar_53],[nvarchar_54],[nvarchar_55],[nvarchar_56],[nvarchar_57],[nvarchar_58],[nvarchar_59],[nvarchar_60],[nvarchar_61],[nvarchar_62],[nvarchar_63],[nvarchar_64],[nvarchar_65],[nvarchar_66],[nvarchar_67],[nvarchar_68],[nvarchar_69],[nvarchar_70],[nvarchar_71],[nvarchar_72],[nvarchar_73],[nvarchar_74],[nvarchar_75],[nvarchar_76],[nvarchar_77],[nvarchar_78],[nvarchar_79],[nvarchar_80]
			,[int_1],[int_2],[int_3],[int_4],[int_5],[int_6],[int_7],[int_8],[int_9],[int_10],[int_11],[int_12],[int_13],[int_14],[int_15],[int_16],[int_17],[int_18],[int_19],[int_20],[int_21],[int_22],[int_23],[int_24],[int_25],[int_26],[int_27],[int_28],[int_29],[int_30],[int_31],[int_32],[int_33],[int_34],[int_35],[int_36],[int_37],[int_38],[int_39],[int_40]
			,[datetime_1],[datetime_2],[datetime_3],[datetime_4],[datetime_5],[datetime_6],[datetime_7],[datetime_8],[datetime_9],[datetime_10],[datetime_11],[datetime_12],[datetime_13],[datetime_14],[datetime_15],[datetime_16],[datetime_17],[datetime_18],[datetime_19],[datetime_20],[datetime_21],[datetime_22],[datetime_23],[datetime_24],[datetime_25]
			,[money_1],[money_2],[money_3],[money_4],[money_5],[money_6],[money_7],[money_8],[money_9],[money_10],[money_11],[money_12],[money_13],[money_14],[money_15]
		)
		SELECT @NewVersionId,[Page]
			,[nvarchar_1],[nvarchar_2],[nvarchar_3],[nvarchar_4],[nvarchar_5],[nvarchar_6],[nvarchar_7],[nvarchar_8],[nvarchar_9],[nvarchar_10],[nvarchar_11],[nvarchar_12],[nvarchar_13],[nvarchar_14],[nvarchar_15],[nvarchar_16],[nvarchar_17],[nvarchar_18],[nvarchar_19],[nvarchar_20],[nvarchar_21],[nvarchar_22],[nvarchar_23],[nvarchar_24],[nvarchar_25],[nvarchar_26],[nvarchar_27],[nvarchar_28],[nvarchar_29],[nvarchar_30],[nvarchar_31],[nvarchar_32],[nvarchar_33],[nvarchar_34],[nvarchar_35],[nvarchar_36],[nvarchar_37],[nvarchar_38],[nvarchar_39],[nvarchar_40]
			,[nvarchar_41],[nvarchar_42],[nvarchar_43],[nvarchar_44],[nvarchar_45],[nvarchar_46],[nvarchar_47],[nvarchar_48],[nvarchar_49],[nvarchar_50],[nvarchar_51],[nvarchar_52],[nvarchar_53],[nvarchar_54],[nvarchar_55],[nvarchar_56],[nvarchar_57],[nvarchar_58],[nvarchar_59],[nvarchar_60],[nvarchar_61],[nvarchar_62],[nvarchar_63],[nvarchar_64],[nvarchar_65],[nvarchar_66],[nvarchar_67],[nvarchar_68],[nvarchar_69],[nvarchar_70],[nvarchar_71],[nvarchar_72],[nvarchar_73],[nvarchar_74],[nvarchar_75],[nvarchar_76],[nvarchar_77],[nvarchar_78],[nvarchar_79],[nvarchar_80]
			,[int_1],[int_2],[int_3],[int_4],[int_5],[int_6],[int_7],[int_8],[int_9],[int_10],[int_11],[int_12],[int_13],[int_14],[int_15],[int_16],[int_17],[int_18],[int_19],[int_20],[int_21],[int_22],[int_23],[int_24],[int_25],[int_26],[int_27],[int_28],[int_29],[int_30],[int_31],[int_32],[int_33],[int_34],[int_35],[int_36],[int_37],[int_38],[int_39],[int_40]
			,[datetime_1],[datetime_2],[datetime_3],[datetime_4],[datetime_5],[datetime_6],[datetime_7],[datetime_8],[datetime_9],[datetime_10],[datetime_11],[datetime_12],[datetime_13],[datetime_14],[datetime_15],[datetime_16],[datetime_17],[datetime_18],[datetime_19],[datetime_20],[datetime_21],[datetime_22],[datetime_23],[datetime_24],[datetime_25]
			,[money_1],[money_2],[money_3],[money_4],[money_5],[money_6],[money_7],[money_8],[money_9],[money_10],[money_11],[money_12],[money_13],[money_14],[money_15]
		FROM FlatProperties WHERE VersionId = @PreviousVersionId
	INSERT INTO ReferenceProperties
		([VersionId],[PropertyTypeId],[ReferredNodeId])
		SELECT @NewVersionId,[PropertyTypeId],[ReferredNodeId]
		FROM ReferenceProperties WHERE VersionId = @PreviousVersionId
	INSERT INTO TextPropertiesNVarchar
		([VersionId],[PropertyTypeId],[Value])
		SELECT @NewVersionId,[PropertyTypeId],[Value]
		FROM TextPropertiesNVarchar WHERE VersionId = @PreviousVersionId
	INSERT INTO TextPropertiesNText
		([VersionId],[PropertyTypeId],[Value])
		SELECT @NewVersionId,[PropertyTypeId],[Value]
		FROM TextPropertiesNText WHERE VersionId = @PreviousVersionId

	-- Set last version pointers
	EXEC proc_Node_SetLastVersion @NodeId
	
	-- Return
	DECLARE @NodeTimestamp timestamp
	DECLARE @LastMajorVersionId int
	DECLARE @LastMinorVersionId int
	SELECT @NodeTimestamp = [Timestamp], @LastMajorVersionId = LastMajorVersionId, @LastMinorVersionId = LastMinorVersionId FROM Nodes WHERE NodeId = @NodeId

	SELECT VersionId, @NodeTimestamp as NodeTimestamp, [Timestamp] as Versiontimestamp, @LastMajorVersionId as LastMajorVersionId, @LastMinorVersionId as LastMinorVersionId FROM Versions WHERE VersionId = @NewVersionId

	SELECT B.BinaryPropertyId, B.PropertyTypeId FROM BinaryProperties B JOIN Files F ON B.FileId = F.FileId
		WHERE B.VersionId = @NewVersionId AND Staging IS NULL
END' 
END
GO

/**************   FUNCTIONS   ***************/

/****** Object:  StoredProcedure [dbo].[proc_ReferenceProperty_Update]    Script Date: 09/10/2008 17:19:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_ReferenceProperty_Update]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_ReferenceProperty_Update]
(
	@VersionId int,
	@PropertyTypeId int,
	@ReferredNodeIdListXml xml
)
AS

-- ReferredNodeId (Parse XML into ID table)
DECLARE @ReferredNodeIds TABLE (Id int NOT NULL)
INSERT INTO @ReferredNodeIds (Id)
SELECT Id.value(''.'', ''int'') AS Id FROM @ReferredNodeIdListXml.nodes(''/Identifiers/ReferredNodeIds/Id'') as Ids(Id) 

-- Remove obsolete items
DELETE FROM ReferenceProperties
WHERE VersionId = @VersionId
AND PropertyTypeId = @PropertyTypeId

-- Add new items
INSERT INTO ReferenceProperties (VersionId, PropertyTypeId, ReferredNodeId)
	SELECT	@VersionId AS VersionId,
			@PropertyTypeId AS PropertyTypeId,
			Id AS ReferredNodeId
	FROM @ReferredNodeIds' 
END
GO


/****** Object:  StoredProcedure [dbo].[proc_Node_LoadData_Batch]   ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_LoadData_Batch]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Node_LoadData_Batch]
	@IdsInXml xml
AS
BEGIN

	DECLARE @versionids AS TABLE(Id INT)
	INSERT @versionids 
	SELECT Id.value(''.'', ''int'') FROM @IdsInXml.nodes(''/Identifiers/VersionIds/Id'') as Identifiers(Id)


	-- #1: FlatProperties
	SELECT * FROM FlatProperties
		WHERE VersionId IN (select Id from @versionids)


	-- #2: BinaryProperties
	SELECT B.BinaryPropertyId, B.VersionId, B.PropertyTypeId, F.FileId, F.ContentType, F.FileNameWithoutExtension,
		F.Extension, F.[Size], F.[BlobProvider], F.[BlobProviderData], F.[Checksum], NULL AS Stream, 0 AS Loaded, F.[Timestamp]
	FROM dbo.BinaryProperties B
		JOIN dbo.Files F ON B.FileId = F.FileId
	WHERE VersionId IN (select Id from @versionids) AND Staging IS NULL


	-- #3: ReferencePropertyInfo + Referred NodeToken
	SELECT VersionId, PropertyTypeId, ReferredNodeId
	FROM dbo.ReferenceProperties
	WHERE VersionId IN (select Id from @versionids)


	-- #4: TextPropertyInfo (NText:Lazy, NVarchar(4000):loaded)
	SELECT VersionId, PropertyTypeId, NULL AS Value, 0 AS Loaded
	FROM dbo.TextPropertiesNText
	WHERE VersionId IN (select Id from @versionids)
	UNION ALL
	SELECT VersionId, PropertyTypeId, Value, 1 AS Loaded
	FROM dbo.TextPropertiesNVarchar
	WHERE VersionId IN (select Id from @versionids)

	-- #5: BaseData
	SELECT N.NodeId, N.NodeTypeId, N.ContentListTypeId, N.ContentListId, N.CreatingInProgress, N.IsDeleted, N.IsInherited, 
		N.ParentNodeId, N.[Name], N.DisplayName, N.[Path], N.[Index], N.Locked, N.LockedById, 
		N.ETag, N.LockType, N.LockTimeout, N.LockDate, N.LockToken, N.LastLockUpdate,
		N.CreationDate AS NodeCreationDate, N.CreatedById AS NodeCreatedById, 
		N.ModificationDate AS NodeModificationDate, N.ModifiedById AS NodeModifiedById,
		N.IsSystem, N.OwnerId,
		N.SavingState, V.ChangedData,
		N.Timestamp AS NodeTimestamp,
		V.VersionId, V.MajorNumber, V.MinorNumber, V.CreationDate, V.CreatedById, 
		V.ModificationDate, V.ModifiedById, V.[Status],
		V.Timestamp AS VersionTimestamp
	FROM dbo.Nodes AS N 
		INNER JOIN dbo.Versions AS V ON N.NodeId = V.NodeId
	WHERE V.VersionId IN (select Id from @versionids)

END
'
END
GO

/****** Object:  UserDefinedFunction [dbo].[udfGetAllDerivatedNodeTypesByNodeTypeId]    Script Date: 05/13/2009 11:33:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[udfGetAllDerivatedNodeTypesByNodeTypeId]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
execute dbo.sp_executesql @statement = N'-- =============================================
-- Author:		SN\molnarg
-- Create date: 2009-05-12
-- Description:	Returns a table that with one column of type int.
--				The table contains all the node types (IDs) derivated from the given node type (ID).
-- =============================================
CREATE FUNCTION [dbo].[udfGetAllDerivatedNodeTypesByNodeTypeId] 
(
	@BaseNodeTypeId int	
)
RETURNS TABLE 
AS
RETURN 
(
	WITH AllDerivates (NodeTypeId)
	AS
	(
		SELECT @BaseNodeTypeId

		UNION ALL
		
		SELECT PropertySetId
		FROM SchemaPropertySets JOIN AllDerivates
			ON	SchemaPropertySets.ParentId = AllDerivates.NodeTypeId
	)
	SELECT NodeTypeId
	FROM AllDerivates
)
' 
END
GO

/****** Object:  StoredProcedure [dbo].[proc_Node_GetTreeSize]    Script Date: 04/01/2010 13:10:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Node_GetTreeSize]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'
CREATE PROCEDURE [dbo].[proc_Node_GetTreeSize] 
	@NodePath nvarchar(450),
	@IncludeChildren tinyint
AS
BEGIN

    WITH x AS (
		SELECT Path, SUM(F.Size) SumSize
		FROM BinaryProperties B
			JOIN Files F ON B.FileId = F.FileId
		LEFT OUTER JOIN Versions V on V.VersionId = B.VersionId
		LEFT OUTER JOIN Nodes N on V.NodeId = N.NodeId
		WHERE (N.[Path] = @NodePath OR (@IncludeChildren = 1 AND N.[Path] + ''/'' LIKE REPLACE(@NodePath, ''_'', ''[_]'') + ''/%'')) AND F.Staging IS NULL
		GROUP BY N.[Path]
    )

	SELECT SUM(SumSize) from x

END
' 
END
GO

/****** Object:  StoredProcedure [dbo].[proc_LogWrite]    Script Date: 10/09/2009 10:01:53 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_LogWrite]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_LogWrite]
	@eventID int,
	@priority int,
	@severity varchar(30),
	@title nvarchar(256),
	@timestamp datetime,
	@machineName varchar(32),
	@AppDomainName varchar(512),
	@ProcessID varchar(256),
	@ProcessName varchar(512),
	@ThreadName varchar(512),
	@Win32ThreadId varchar(128),
	@message nvarchar(1500),
	@formattedmessage ntext,
	@LogId int out,
	@ContentPath nvarchar(450) = null,
	@UserName nvarchar(450) = null
AS
BEGIN
declare @xmlDoc xml
set @xmlDoc = CAST(@FormattedMessage as xml)

declare @Category nvarchar(50)
set @Category = CAST(@xmlDoc.query(''//Category/text()'') as nvarchar(50))

declare @ContentId int
set @ContentId = CAST(CAST(@xmlDoc.query(''//Id/text()'') as nvarchar(450)) as int)

set @ContentPath = CAST(@xmlDoc.query(''//Path/text()'') as nvarchar(450))
set @UserName = CAST(@xmlDoc.query(''//UserName/text()'') as nvarchar(450))
INSERT INTO [dbo].[LogEntries]
           ([EventId]
           ,[Category]
           ,[Priority]
           ,[Severity]
           ,[Title]
           ,[ContentId]
           ,[ContentPath]
           ,[UserName]
           ,[LogDate]
           ,[MachineName]
           ,[AppDomainName]
           ,[ProcessID]
           ,[ProcessName]
           ,[ThreadName]
           ,[Win32ThreadId]
           ,[Message]
           ,[FormattedMessage])
     VALUES
           (@eventID
           ,@Category
           ,@priority
           ,@severity
           ,@title
           ,@ContentId
           ,@ContentPath
           ,@UserName
           ,@timestamp
           ,@machineName
           ,@AppDomainName
           ,@ProcessID
           ,@ProcessName
           ,@ThreadName
           ,@Win32ThreadId
           ,@message
           ,@formattedmessage)
SELECT @LogId = @@IDENTITY
END
' 
END
GO

/****** Object:  StoredProcedure [dbo].[proc_LogAddCategory]    Script Date: 10/09/2009 10:01:53 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_LogAddCategory]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_LogAddCategory]
	@categoryName nvarchar(50), @logID int
AS
-- do nothing
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_LogSelect]    Script Date: 10/10/2009 15:16:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[dbo].[proc_LogSelect]') AND OBJECTPROPERTY(id,N'IsProcedure') = 1)
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_LogSelect] 
	-- Add the parameters for the stored procedure here
	@startDate	datetime,
	@endDate	datetime,
	@usrName	nvarchar(450),
	@title		nvarchar(256),
	@params		nvarchar(500)
AS
BEGIN
	SELECT TOP 100 * FROM [LogEntries] 
	WHERE ((@startDate is null) OR ((@startDate is not null) AND ([LogDate] >= @startDate)))
	AND	  ((@endDate is null) OR ((@endDate is not null) AND ([LogDate] <= @endDate)))
	AND	  ((@usrName is null) OR ((@usrName is not null) AND ([UserName] like @usrName)))
	AND	  ((@title is null) OR ((@title is not null) AND ([Title] like @title)))
	AND	  ((@params is null) OR ((@params is not null) AND ([FormattedMessage] like @params)))
	Order by [LogDate]
END
' 
END
GO
/****** Object:  StoredProcedure [dbo].[proc_NodeHead_Load_Batch]    Script Date: 03/14/2010 19:43:14 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE  PROCEDURE [dbo].[proc_NodeHead_Load_Batch]
(
	@IdsInXml xml
)as
BEGIN
	declare @nodeids as table(id int)
	insert @nodeids 
	SELECT id.value('.', 'int') FROM @IdsInXml.nodes('/NodeHeads/id') as Identifiers(id)

	SELECT
		Node.NodeId,             -- 0
		Node.Name,               -- 1
		Node.DisplayName,        -- 2
		Node.Path,               -- 3
		Node.ParentNodeId,       -- 4
		Node.NodeTypeId,         -- 5
		Node.ContentListTypeId,  -- 6
		Node.ContentListId,      -- 7
		Node.CreationDate,       -- 8
		Node.ModificationDate,   -- 9
		Node.LastMinorVersionId, -- 10
		Node.LastMajorVersionId, -- 11
		Node.OwnerId,            -- 12
		Node.CreatedById,        -- 13
		Node.ModifiedById,       -- 14
		Node.[Index],            -- 15
		Node.LockedById,         -- 16
		Node.Timestamp           -- 17
	FROM
		Nodes Node RIGHT OUTER JOIN @nodeids nodelist ON Node.NodeId = nodelist.id  
END
GO
