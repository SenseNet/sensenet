using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    public partial class MsSqlDataProvider
    {
        /* ------------------------------------------------ Nodes */

        #region InsertNodeAndVersionScript
        protected override string InsertNodeAndVersionScript => @"-- MsSqlDataProvider.InsertNodeAndVersion
DECLARE @NodeId int, @VersionId int
DECLARE @NodeTimestamp timestamp, @VersionTimestamp timestamp

INSERT INTO Nodes
    ([NodeTypeId],[ContentListTypeId],[ContentListId],[CreatingInProgress],[IsDeleted],[IsInherited],[ParentNodeId],[Name],[DisplayName],[Index],[Locked],[LockedById],[ETag],[LockType],[LockTimeout],[LockDate],[LockToken],[LastLockUpdate],    [CreationDate],    [CreatedById],    [ModificationDate],    [ModifiedById],[IsSystem],[OwnerId],[SavingState],[Path]) VALUES
    (@NodeTypeId, @ContentListTypeId, @ContentListId, @CreatingInProgress, @IsDeleted, @IsInherited, @ParentNodeId, @Name, @DisplayName, @Index, @Locked, @LockedById, @ETag, @LockType, @LockTimeout, @LockDate, @LockToken, @LastLockUpdate, @NodeCreationDate, @NodeCreatedById, @NodeModificationDate, @NodeModifiedById, @IsSystem, @OwnerId, @SavingState, @Path )

SELECT @NodeId = @@IDENTITY

-- skip the rest, if the insert above was not successful
IF (@NodeId is NOT NULL)
BEGIN
    INSERT INTO Versions 
        ([NodeId],[MajorNumber],[MinorNumber],       [CreationDate],       [CreatedById],       [ModificationDate],       [ModifiedById],[Status],[ChangedData],[DynamicProperties]) VALUES
        (@NodeId, @MajorNumber, @MinorNumber, @VersionCreationDate, @VersionCreatedById, @VersionModificationDate, @VersionModifiedById, @Status, @ChangedData, @DynamicProperties )

    SELECT @VersionId = @@IDENTITY
    SELECT @VersionTimestamp = [Timestamp] FROM Versions WHERE VersionId = @VersionId

    IF @Status = 1
        UPDATE Nodes SET LastMinorVersionId = @VersionId, LastMajorVersionId = @VersionId WHERE NodeId = @NodeId
    ELSE
        UPDATE Nodes SET LastMinorVersionId = @VersionId WHERE NodeId = @NodeId
    SELECT @NodeTimestamp = [Timestamp] FROM Nodes WHERE NodeId = @NodeId

    SELECT @NodeId NodeId, @VersionId VersionId, @NodeTimestamp NodeTimestamp, @VersionTimestamp VersionTimestamp, LastMajorVersionId, LastMinorVersionId, Path FROM Nodes WHERE NodeId = @NodeId
END
";
        #endregion
        #region InsertReferencePropertiesHeadScript
        protected override string InsertReferencePropertiesHeadScript => @"-- MsSqlDataProvider.InsertReferenceProperties
DECLARE @ReferredNodeIdTable AS TABLE(Id INT)
";
        #endregion
        #region InsertReferencePropertiesScript
        protected override string InsertReferencePropertiesScript => @"--
IF DATALENGTH(@ReferredNodeIds{0}) > 0 BEGIN
    DELETE FROM @ReferredNodeIdTable
    INSERT INTO @ReferredNodeIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@ReferredNodeIds{0}, ',');
    INSERT INTO ReferenceProperties (VersionId, PropertyTypeId, ReferredNodeId)
	    SELECT	@VersionId AS VersionId, @PropertyTypeId{0} AS PropertyTypeId, Id AS ReferredNodeId FROM @ReferredNodeIdTable
END
";
        #endregion
        #region InsertLongtextPropertiesHeadScript
        protected override string InsertLongtextPropertiesHeadScript => @"-- MsSqlDataProvider.InsertLongtextProperties
";
        #endregion
        #region InsertLongtextPropertiesScript
        protected override string InsertLongtextPropertiesScript => @"INSERT INTO LongTextProperties
    ([VersionId],[PropertyTypeId],[Length],[Value]) VALUES
    (@VersionId, @PropertyTypeId{0}, @Length{0}, @Value{0} )
";
        #endregion

        #region UpdateVersionScript
        protected override string UpdateVersionScript => @"-- MsSqlDataProvider.UpdateVersion
UPDATE Versions SET
    NodeId = @NodeId,
    MajorNumber = @MajorNumber,
    MinorNumber = @MinorNumber,
    CreationDate = @CreationDate,
    CreatedById = @CreatedById,
    ModificationDate = @ModificationDate,
    ModifiedById = @ModifiedById,
    Status = @Status,
    ChangedData = @ChangedData,
    DynamicProperties = @DynamicProperties
WHERE VersionId = @VersionId

SELECT [Timestamp] FROM Versions WHERE VersionId = @VersionId
";
        #endregion
        #region UpdateNodeScript
        protected override string UpdateNodeScript => @"-- MsSqlDataProvider.UpdateNode
UPDATE Nodes SET
    NodeTypeId = @NodeTypeId,
    ContentListTypeId = @ContentListTypeId,
    ContentListId = @ContentListId,
    CreatingInProgress = @CreatingInProgress,
    IsDeleted = @IsDeleted,
    IsInherited = @IsInherited,
    ParentNodeId = @ParentNodeId,
    [Name] = @Name,
    DisplayName = @DisplayName,
    Path = @Path,
    [Index] = @Index,
    Locked = @Locked,
    LockedById = @LockedById,
    ETag = @ETag,
    LockType = @LockType,
    LockTimeout = @LockTimeout,
    LockDate = @LockDate,
    LockToken = @LockToken,
    LastLockUpdate = @LastLockUpdate,
    CreationDate = @CreationDate,
    CreatedById = @CreatedById,
    ModificationDate = @ModificationDate,
    ModifiedById = @ModifiedById,
    IsSystem = @IsSystem,
    OwnerId = @OwnerId,
    SavingState = @SavingState
FROM
    Nodes
WHERE NodeId = @NodeId AND [Timestamp] = @NodeTimestamp

IF @@ROWCOUNT = 0 BEGIN
    DECLARE @Count int
    SELECT @Count = COUNT(*) FROM Nodes WHERE NodeId = @NodeId
    IF @Count = 0
        RAISERROR (N'Cannot update a deleted Node. Id: %d, path: %s.', 12, 1, @NodeId, @Path);
    ELSE
        RAISERROR (N'Node is out of date Id: %d, path: %s.', 12, 1, @NodeId, @Path);
END
ELSE BEGIN
    SELECT [Timestamp] FROM Nodes WHERE NodeId = @NodeId
END
";
        #endregion
        #region UpdateSubTreePathScript
        protected override string UpdateSubTreePathScript => @"-- MsSqlDataProvider.UpdateSubTreePath
DECLARE @OldPathLen int
SET @OldPathLen = LEN(@OldPath)

UPDATE Nodes
SET Path = @NewPath + RIGHT(Path, LEN(Path) - @OldPathLen)
WHERE Path LIKE REPLACE(@OldPath, '_', '[_]') + '/%'
";
        #endregion
        #region UpdateReferencePropertiesHeadScript
        protected override string UpdateReferencePropertiesHeadScript => @"-- MsSqlDataProvider.UpdateReferenceProperties
DECLARE @ReferredNodeIdTable AS TABLE(Id INT)
";
        #endregion
        #region UpdateReferencePropertiesScript
        protected override string UpdateReferencePropertiesScript => @"--
DELETE FROM ReferenceProperties WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId{0}
IF DATALENGTH(@ReferredNodeIds{0}) > 0 BEGIN
    DELETE FROM @ReferredNodeIdTable
    INSERT INTO @ReferredNodeIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@ReferredNodeIds{0}, ',');
    INSERT INTO ReferenceProperties (VersionId, PropertyTypeId, ReferredNodeId)
	    SELECT	@VersionId AS VersionId, @PropertyTypeId{0} AS PropertyTypeId, Id AS ReferredNodeId FROM @ReferredNodeIdTable
END
";
        #endregion
        #region UpdateLongtextPropertiesHeadScript
        protected override string UpdateLongtextPropertiesHeadScript => @"-- MsSqlDataProvider.UpdateLongtextProperties
";
        #endregion
        #region UpdateLongtextPropertiesScript
        protected override string UpdateLongtextPropertiesScript => @"-- MsSqlDataProvider.UpdateLongtextProperties
DELETE FROM LongTextProperties WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId{0}
" + InsertLongtextPropertiesScript;
        #endregion

        #region CopyVersionAndUpdateScript
        //UNDONE:DB: Copy BinaryProperies via BlobStorage (see the script)
        protected override string CopyVersionAndUpdateScript => @"-- MsSqlDataProvider.CopyVersionAndUpdate
DECLARE @NewVersionId int
    
-- Before inserting set versioning status code from ""Locked"" to ""Draft"" on all older versions
UPDATE Versions SET Status = 4 WHERE NodeId = @NodeId AND Status = 2

IF @DestinationVersionId IS NULL
BEGIN
    -- Insert version row
    INSERT INTO Versions
        ( NodeId, MajorNumber, MinorNumber, CreationDate, CreatedById, ModificationDate, ModifiedById, Status, ChangedData, DynamicProperties)
        VALUES
        (@NodeId,@MajorNumber,@MinorNumber,@CreationDate,@CreatedById,@ModificationDate,@ModifiedById,@Status,@ChangedData,@DynamicProperties)
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
        ChangedData = @ChangedData,
        DynamicProperties = @DynamicProperties
    WHERE VersionId = @NewVersionId

    -- Delete previous property values
    DELETE FROM BinaryProperties WHERE VersionId = @NewVersionId;
    DELETE FROM ReferenceProperties WHERE VersionId = @NewVersionId;
    DELETE FROM LongTextProperties WHERE VersionId = @NewVersionId;
END    

-- Copy properties
INSERT INTO BinaryProperties ([VersionId],[PropertyTypeId],[FileId])
    SELECT @NewVersionId,[PropertyTypeId],[FileId] FROM BinaryProperties WHERE VersionId = @PreviousVersionId
INSERT INTO ReferenceProperties
    ([VersionId],[PropertyTypeId],[ReferredNodeId])
    SELECT @NewVersionId,[PropertyTypeId],[ReferredNodeId]
    FROM ReferenceProperties WHERE VersionId = @PreviousVersionId
INSERT INTO LongTextProperties
    ([VersionId],[PropertyTypeId],[Length],[Value])
    SELECT @NewVersionId,[PropertyTypeId],[Length],[Value]
    FROM LongTextProperties WHERE VersionId = @PreviousVersionId

-- Return
SELECT VersionId, [Timestamp] FROM Versions WHERE VersionId = @NewVersionId

SELECT B.BinaryPropertyId, B.PropertyTypeId FROM BinaryProperties B JOIN Files F ON B.FileId = F.FileId
    WHERE B.VersionId = @NewVersionId AND Staging IS NULL
";
        #endregion

        #region DeleteNodeScript
        protected override string DeleteNodeScript => @"-- MsSqlDataProvider.LoadTextPropertyValues
DECLARE @Path NVARCHAR(450)
IF (EXISTS (SELECT NodeId FROM Nodes WHERE NodeId = @NodeId)) BEGIN
    IF @Timestamp IS NOT NULL AND (NOT EXISTS (SELECT NodeId FROM Nodes WHERE NodeId = @NodeId and @Timestamp = [Timestamp])) BEGIN
        SELECT @Path = [Path] FROM Nodes WHERE NodeId = @NodeId
        RAISERROR (N'Node is out of date. Id: %d, path: %s.', 12, 1, @NodeId, @Path);
    END
    ELSE BEGIN
        DECLARE @startpath nvarchar(450)
        SELECT @startpath = REPLACE(REPLACE(Path, '[', '\['), ']', '\]') FROM Nodes WHERE NodeId = @NodeId

        DECLARE @NIDall TABLE (Id INT IDENTITY(1, 1), NodeId INT)
        DECLARE @NIDpartition TABLE (Id INT IDENTITY(1, 1), NodeId INT)
        DECLARE @VID TABLE (Id INT IDENTITY(1, 1), VersionId INT)

        INSERT INTO @NIDall
            SELECT NodeId FROM Nodes 
            WHERE Path = @startpath OR Path LIKE REPLACE(@startpath, '_', '[_]') + '/%'
            ORDER BY Path DESC

        DECLARE @nodeCount INT
        SELECT @nodeCount = COUNT(1) FROM @NIDall

        WHILE @nodeCount > 0 BEGIN
            BEGIN TRY
                DELETE FROM @NIDpartition
                INSERT INTO @NIDpartition
                    SELECT TOP(@PartitionSize) NodeId FROM @NIDall ORDER BY Id

                DELETE FROM @VID
                INSERT INTO @VID
                    SELECT VersionId FROM Versions WHERE NodeId IN (SELECT NodeId FROM @NIDpartition)

                --=============================================================

                DELETE BinaryProperties WHERE VersionId IN (SELECT VersionId FROM @VID)
                DELETE LongTextProperties WHERE VersionId IN (SELECT VersionId FROM @VID)
                DELETE ReferenceProperties WHERE (VersionId IN (SELECT VersionId FROM @VID)) OR
                                                    (ReferredNodeId IN (SELECT NodeId FROM @NIDpartition))

                DELETE Versions WHERE NodeId IN (SELECT NodeId FROM @NIDpartition)
                DELETE Nodes WHERE NodeId IN (SELECT NodeId FROM @NIDpartition)

                --=============================================================

                DELETE FROM @NIDall WHERE NodeId IN (SELECT NodeId FROM @NIDpartition)
                SELECT @nodeCount = COUNT(1) FROM @NIDall
                                
                print convert(varchar(10), @nodeCount) + ' nodes left'
            END TRY
            BEGIN CATCH
                DECLARE @ErrMsg nvarchar(4000), @ErrSeverity int
                SELECT 
                    @ErrMsg = ERROR_MESSAGE(),
                    @ErrSeverity = ERROR_SEVERITY()
                RAISERROR(@ErrMsg, @ErrSeverity, 1)
                RETURN
            END CATCH
        END -- WHILE
    END -- ELSE
END -- IF EXISTS
";
        #endregion

        //UNDONE:DB: Need to refactor MoveNodeScript
        #region MoveNodeScript
        protected override string MoveNodeScript => @"-- MsSqlDataProvider.MoveNode
DECLARE @Path nvarchar(450)
DECLARE @HasTrans INT
SET @HasTrans = @@TRANCOUNT

-----------------------------------------------------------------------  Existence checks

IF NOT EXISTS (SELECT NodeId FROM Nodes WHERE NodeId = @TargetNodeId)
    RAISERROR (N'Cannot move under a deleted node. Id: %d', 12, 1, @TargetNodeId);

IF NOT EXISTS (SELECT NodeId FROM Nodes WHERE NodeId = @SourceNodeId)
    RAISERROR (N'Cannot move a deleted node.Id: %d', 12, 1, @SourceNodeId);

IF @SourceTimestamp IS NOT NULL BEGIN
    IF NOT EXISTS (SELECT NodeId FROM Nodes WHERE NodeId = @SourceNodeId and @SourceTimestamp = [Timestamp]) BEGIN
        SELECT @Path = [Path] FROM Nodes WHERE NodeId = @SourceNodeId
        RAISERROR (N'Source node is out of date. Id: %d, path: %s.', 12, 1, @SourceNodeId, @Path);
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

    SET @SourcePathUnderscoreEscaped = REPLACE(@SourcePath, '_', '[_]')

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
        Path LIKE @SourcePathUnderscoreEscaped + '/%'

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
        SELECT NodeTypeId Id FROM NodeTypes WHERE NodeTypeId IN (SELECT TOP 1 NodeTypeId FROM NodeTypes WHERE [Name] = 'SystemFolder')
        UNION ALL
        SELECT p.NodeTypeId Id FROM NodeTypes AS p INNER JOIN TypeSubtree AS t ON p.ParentId = t.Id
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
    IF @SourceIsSystem = 0 AND @TargetIsSystem = 0 SET @SystemFlagUpdatingStrategy = 'NoChange'
    IF @SourceIsSystem = 0 AND @TargetIsSystem = 1 SET @SystemFlagUpdatingStrategy = 'AllSystem'
    IF @SourceIsSystem = 1 AND @TargetIsSystem = 0 SET @SystemFlagUpdatingStrategy = 'Recompute'
    IF @SourceIsSystem = 1 AND @TargetIsSystem = 1 SET @SystemFlagUpdatingStrategy = 'NoChange'
    
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
        RAISERROR('Invalid operation: moving a contentlist / a subtree that contains a contentlist under an another contentlist', 18, 2)
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
    SELECT @OldPathUnderscoreEscaped = REPLACE(@OldPath,'_','[_]')
    SELECT @SourceParentPath = Path FROM Nodes WHERE Nodes.NodeId = (SELECT ParentNodeId FROM Nodes WHERE Nodes.NodeId = @SourceNodeId)
    SELECT @TrashBagTypePath = Path FROM Nodes WHERE (Path LIKE '/Root/System/Schema/ContentTypes/%' AND Name = 'TrashBag')
    SELECT @TargetTypePath = Path FROM Nodes WHERE (Path LIKE '/Root/System/Schema/ContentTypes/%' AND Name = 
            (SELECT Name FROM NodeTypes WHERE NodeTypeId = (SELECT NodeTypeId FROM Nodes WHERE NodeId = @TargetNodeId)))

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
        AND @TargetTypePath+ '/' NOT LIKE REPLACE(@TrashBagTypePath,'_','[_]') + '/%' )
    BEGIN
        -- Get the VersionIds of the nodes to be moved.
        DECLARE @VersionsTemp table (VersionId int)
        INSERT INTO @VersionsTemp
            SELECT VersionId FROM Versions WHERE NodeId IN (SELECT NodeId FROM @AffectedSubtreeIds)
        
        -- Get the PropertyTypeIds of the contentlist properties
        DECLARE @ContentListPropertyTypesTemp table (PropertyTypeId int)
        INSERT INTO @ContentListPropertyTypesTemp
            SELECT PropertyTypeId FROM PropertyTypes WHERE IsContentListProperty = 1

        -- drop binary contentlist properties
        DELETE BinaryProperties
        WHERE
            VersionId IN (SELECT VersionId FROM @VersionsTemp)
            AND
            PropertyTypeId IN (SELECT PropertyTypeId FROM @ContentListPropertyTypesTemp)
        
        -- drop LongTextProperty contentlist properties
        DELETE LongTextProperties
        WHERE
            VersionId IN (SELECT VersionId FROM @VersionsTemp)
            AND
            PropertyTypeId IN (SELECT PropertyTypeId FROM @ContentListPropertyTypesTemp)

---- drop flat contentlist properties
--DELETE FlatProperties WHERE VersionId IN (SELECT VersionId FROM @VersionsTemp) AND Page >= 10000000

        ---- The target is NOT a ContentList nor a ContentListFolder.
        ---- ContentListTypeId, ContentListId should be updated to null.
        ---- (except if it is the trash)
        UPDATE Nodes
        SET ContentListTypeId = null, ContentListId = null
        WHERE NodeId IN (SELECT NodeId FROM @AffectedSubtreeIds)
    END
    
    -- If the target is a ContentList or a ContentListFolder
    -- then the ContentListTypeId and ContentListId should be updated to the new ContentListTypeId and ContentListId. 
    -- (except if the source node already has a ContentListTypeId)
    IF (@TargetContentListTypeId IS NOT NULL)
    BEGIN
        IF @TargetContentListId IS NULL
            -- In this case the ContentListId is null, because the ContentListId is the NodeId.
            SET @TargetContentListId = @TargetNodeId

        UPDATE Nodes
        SET ContentListTypeId = @TargetContentListTypeId, ContentListId = @TargetContentListId
        WHERE NodeId IN (SELECT NodeId FROM @AffectedSubtreeIds)
    END
    
    --==== Updating subtree by strategy (@SystemFlagUpdatingStrategy: 'NoChange' | 'AllSystem' | 'Recompute'
    IF @SystemFlagUpdatingStrategy = 'NoChange' BEGIN
        --    subtree root
        UPDATE Nodes
        SET Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen), ParentNodeId = @TargetNodeId
        WHERE Nodes.NodeId = @SourceNodeId
        --    subtree elements
        UPDATE Nodes
        SET Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen)
        WHERE Path LIKE @OldPathUnderscoreEscaped + '/%'
    END
    ELSE IF @SystemFlagUpdatingStrategy = 'AllSystem' BEGIN
        --    subtree root
        UPDATE Nodes
        SET Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen), ParentNodeId = @TargetNodeId, IsSystem = 1
        WHERE Nodes.NodeId = @SourceNodeId
        --    subtree elements
        UPDATE Nodes
        SET Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen), IsSystem = 1
        WHERE Path LIKE @OldPathUnderscoreEscaped + '/%'
    END
    ELSE IF @SystemFlagUpdatingStrategy = 'Recompute' BEGIN
        --    subtree root
        UPDATE Nodes
        SET Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen), ParentNodeId = @TargetNodeId, IsSystem = @SourceIsSystemFolder
        WHERE Nodes.NodeId = @SourceNodeId
        --    reset subtree elements
        UPDATE Nodes
        SET Path = @TargetPath + RIGHT(Path, LEN(Path) - @OldPathLen), IsSystem = 0
        WHERE Path LIKE @OldPathUnderscoreEscaped + '/%'

        -- set IsSystem flag on all nodes that have SystemFolder ancestor in this subtree
        DECLARE @currentPath nvarchar(450)
        DECLARE sysfolder_cursor CURSOR FOR  
            SELECT [Path] FROM Nodes WHERE [Path] LIKE  REPLACE(@TargetPath,'_','[_]') + '/%' AND NodeTypeId IN (SELECT NodeTypeId FROM @SystemFolderIds)
        OPEN sysfolder_cursor   
        FETCH NEXT FROM sysfolder_cursor INTO @currentPath   
        WHILE @@FETCH_STATUS = 0   
        BEGIN   
            UPDATE Nodes SET IsSystem = 1 WHERE NodeId IN (
                SELECT NodeId FROM Nodes WHERE Path = @currentPath OR Path LIKE REPLACE(@currentPath,'_','[_]') + '/%'
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
";
        #endregion

        #region LoadTextPropertyValuesScript
        protected override string LoadTextPropertyValuesScript => @"-- MsSqlDataProvider.LoadTextPropertyValues
SELECT PropertyTypeId, Value FROM LongTextProperties WHERE VersionId = @VersionId AND PropertyTypeId IN ({0})
";
        #endregion

        #region LoadNodesScript
        protected override string LoadNodesScript => @"-- MsSqlDataProvider.LoadNodes
-- Transform the input to a queryable format
DECLARE @VersionIdTable AS TABLE(Id INT)
INSERT INTO @VersionIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@VersionIds, ',');

-- BaseData
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
    V.Timestamp AS VersionTimestamp,
    V.DynamicProperties
FROM dbo.Nodes AS N 
    INNER JOIN dbo.Versions AS V ON N.NodeId = V.NodeId
WHERE V.VersionId IN (SELECT Id FROM @VersionIdTable)

-- BinaryProperties
SELECT B.BinaryPropertyId, B.VersionId, B.PropertyTypeId, F.FileId, F.ContentType, F.FileNameWithoutExtension,
    F.Extension, F.[Size], F.[BlobProvider], F.[BlobProviderData], F.[Checksum], NULL AS Stream, 0 AS Loaded, F.[Timestamp]
FROM dbo.BinaryProperties B
    JOIN dbo.Files F ON B.FileId = F.FileId
WHERE VersionId IN (SELECT Id FROM @VersionIdTable) AND Staging IS NULL

-- ReferenceProperties
SELECT VersionId, PropertyTypeId, ReferredNodeId
FROM dbo.ReferenceProperties
WHERE VersionId IN (SELECT Id FROM @VersionIdTable)

-- LongTextProperties
SELECT VersionId, PropertyTypeId, [Length], [Value]
FROM dbo.LongTextProperties
WHERE VersionId IN (SELECT Id FROM @VersionIdTable) AND Length < @LongTextMaxSize
";
        #endregion

        #region DeleteVersionsScript
        protected override string ManageLastVersionsScript => @"-- MsSqlDataProvider.DeleteVersions
IF @VersionIds IS NOT NULL BEGIN
    DECLARE @VersionIdTable AS TABLE(Id INT)
    INSERT INTO @VersionIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@VersionIds, ',');

    DELETE FROM LongTextProperties WHERE VersionId IN (SELECT Id FROM @VersionIdTable)
    DELETE FROM ReferenceProperties WHERE VersionId IN (SELECT Id FROM @VersionIdTable)

    UPDATE Nodes SET LastMinorVersionId = NULL, LastMajorVersionId = NULL WHERE NodeId = @NodeId
    DELETE FROM Versions WHERE VersionId IN (SELECT Id FROM @VersionIdTable)
END

UPDATE Nodes
    SET LastMinorVersionId = (SELECT TOP (1) VersionId FROM Versions WHERE NodeId = @NodeId
            ORDER BY MajorNumber DESC, MinorNumber DESC),
        LastMajorVersionId = (SELECT TOP (1) VersionId FROM Versions WHERE NodeId = @NodeId AND MinorNumber = 0 AND Status = 1
            ORDER BY MajorNumber DESC, MinorNumber DESC)
WHERE NodeId = @NodeId

-- Return the new timestamp and version ids
SELECT [Timestamp] as NodeTimestamp, LastMajorVersionId, LastMinorVersionId FROM Nodes WHERE NodeId = @NodeId
";
        #endregion

        #region NodeExistsScript
        protected override string NodeExistsScript => @"-- MsSqlDataProvider.NodeExists
IF EXISTS (SELECT * FROM Nodes WHERE [Path] = @Path COLLATE Latin1_General_CI_AS)
    SELECT 1
ELSE
    SELECT 0
";
        #endregion

        /* ------------------------------------------------ NodeHead */

        #region LoadNodeHeadScript
        private string LoadNodeHeadSkeletonScript = @"-- MsSqlDataProvider.{0}
{1}
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
    Nodes Node
    {2}
WHERE 
    {3}
";

        private string LoadNodeHeadScript(string trace, string scriptHead = null, string join = null, string where = null)
        {
            return string.Format(LoadNodeHeadSkeletonScript,
                trace,
                scriptHead ?? string.Empty,
                join ?? string.Empty,
                where ?? string.Empty);
        }
        protected override string LoadNodeHeadByPathScript => LoadNodeHeadScript("LoadNodeHead by Path", where: "Node.Path = @Path COLLATE Latin1_General_CI_AS");
        protected override string LoadNodeHeadByIdScript => LoadNodeHeadScript("LoadNodeHead by NodeId", where: "Node.NodeId = @NodeId");
        protected override string LoadNodeHeadByVersionIdScript => LoadNodeHeadScript("LoadNodeHead by VersionId",
            join: "JOIN Versions V ON V.NodeId = Node.NodeId", where: "V.VersionId = @VersionId");
        protected override string LoadNodeHeadsByIdSetScript => LoadNodeHeadScript("LoadNodeHead by NodeId set",
            scriptHead: @"DECLARE @NodeIdTable AS TABLE(Id INT) INSERT INTO @NodeIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@NodeIds, ',');",
            where: "Node.NodeId IN (SELECT Id FROM @NodeIdTable)");
        #endregion

        #region GetNodeVersionsScript
        protected override string GetNodeVersionsScript => @"-- MsSqlDataProvider.GetNodeVersions
SELECT VersionId, MajorNumber, MinorNumber, Status
FROM Versions
WHERE NodeId = @NodeId
ORDER BY MajorNumber, MinorNumber
";
        #endregion

        #region GetVersionNumbersByNodeIdScript
        protected override string GetVersionNumbersByNodeIdScript => @"-- MsSqlDataProvider.GetVersionNumbersByNodeId
SELECT MajorNumber, MinorNumber, [Status] FROM Versions
WHERE NodeId = @NodeId
ORDER BY MajorNumber, MinorNumber
";
        #endregion

        #region GetVersionNumbersByPathScript
        protected override string GetVersionNumbersByPathScript => @"-- MsSqlDataProvider.GetVersionNumbersByPath
SELECT v.MajorNumber, v.MinorNumber, v.[Status] FROM Versions v
    INNER JOIN Nodes n ON n.NodeId = v.NodeId
WHERE n.[Path] = @Path
ORDER BY v.MajorNumber, v.MinorNumber
";
        #endregion

        #region InstanceCountScript
        protected override string InstanceCountScript => @"-- MsSqlDataProvider.InstanceCount
SELECT COUNT(*) FROM Nodes WHERE NodeTypeId IN ({0})
";
        #endregion

        /* ------------------------------------------------ NodeQuery */

        #region GetChildrenIdentfiersScript
        protected override string GetChildrenIdentfiersScript => @"-- MsSqlDataProvider.GetChildrenIdentfiers
SELECT NodeId FROM Nodes WHERE ParentNodeId = @ParentNodeId
";
        #endregion

        #region QueryNodesByReferenceScript
        protected override string QueryNodesByReferenceScript => @"-- MsSqlDataProvider.QueryNodesByReference
SELECT V.NodeId FROM ReferenceProperties R
    JOIN Versions V ON R.VersionId = V.VersionId
    JOIN Nodes N ON V.VersionId = N.LastMinorVersionId
WHERE R.PropertyTypeId = @PropertyTypeId AND R.ReferredNodeId = @ReferredNodeId
";
        #endregion
        #region QueryNodesByReferenceAndTypeScript
        protected override string QueryNodesByReferenceAndTypeScript => @"-- MsSqlDataProvider.QueryNodesByReferenceAndType
SELECT N.NodeId FROM ReferenceProperties R
    JOIN Versions V ON R.VersionId = V.VersionId
    JOIN Nodes N ON V.VersionId = N.LastMinorVersionId
WHERE R.PropertyTypeId = @PropertyTypeId AND R.ReferredNodeId = @ReferredNodeId AND N.NodeTypeId IN ({0})
";
        #endregion

        /* ------------------------------------------------ Tree */

        #region LoadChildTypesToAllowScript
        protected override string LoadChildTypesToAllowScript => @"-- MsSqlDataProvider.LoadChildTypesToAllow
DECLARE @FolderNodeTypeId int
SELECT @FolderNodeTypeId = NodeTypeId FROM NodeTypes WHERE Name = 'Folder'
DECLARE @PageNodeTypeId int
SELECT @PageNodeTypeId = NodeTypeId FROM NodeTypes WHERE Name = 'Page'

;WITH Tree(Id, ParentId, TypeId) AS
(
    SELECT NodeId, ParentNodeId, NodeTypeId FROM Nodes WHERE NodeId = @NodeId
    UNION ALL
    SELECT NodeId, ParentNodeId, NodeTypeId FROM Nodes
        JOIN Tree ON Tree.Id = Nodes.ParentNodeId
    WHERE Tree.TypeId IN (@FolderNodeTypeId, @PageNodeTypeId)
)
SELECT DISTINCT S.Name FROM NodeTypes S
    JOIN Nodes N ON N.NodeTypeId = S.NodeTypeId
    JOIN Tree ON N.NodeId = Tree.Id
";
        #endregion

        /* ------------------------------------------------ TreeLock */

        #region GetContentListTypesInTreeScript
        protected override string GetContentListTypesInTreeScript => @"-- MsSqlDataProvider.GetContentListTypesInTree
SELECT ContentListTypeId FROM Nodes 
WHERE ContentListId IS NULL AND 
      ContentListTypeId IS NOT NULL AND Path LIKE REPLACE(@Path, '_', '[_]') + '/%' COLLATE Latin1_General_CI_AS
";
        #endregion

        #region AcquireTreeLockScript
        protected override string AcquireTreeLockScript => @"-- MsSqlDataProvider.GetContentListTypesInTree
BEGIN TRAN
IF NOT EXISTS (
        SELECT TreeLockId FROM TreeLocks
        WHERE @TimeMin < LockedAt AND (
            [Path] LIKE (REPLACE(@Path0, '_', '[_]') + '/%') OR
            [Path] IN ( {0} ) ) )
    INSERT INTO TreeLocks ([Path] ,[LockedAt])
        OUTPUT INSERTED.TreeLockId
        VALUES (@Path0, GETDATE())
COMMIT
";
        #endregion

        #region IsTreeLockedScript
        protected override string IsTreeLockedScript => @"-- MsSqlDataProvider.IsTreeLocked
SELECT TreeLockId
FROM TreeLocks
WHERE @TimeLimit < LockedAt AND (
    [Path] LIKE (REPLACE(@Path0, '_', '[_]') + '/%') OR
    [Path] IN ( {0} ) )
";
        #endregion

        #region ReleaseTreeLockScript
        protected override string ReleaseTreeLockScript => @"-- MsSqlDataProvider.ReleaseTreeLock
DELETE FROM TreeLocks WHERE TreeLockId IN ({0})
";
        #endregion

        #region LoadAllTreeLocksScript
        protected override string LoadAllTreeLocksScript => @"-- MsSqlDataProvider.LoadAllTreeLocks
SELECT [TreeLockId], [Path] FROM TreeLocks
";
        #endregion

        #region DeleteUnusedLocksScript
        protected override string DeleteUnusedLocksScript => @"-- MsSqlDataProvider.DeleteUnusedLocks
DELETE FROM TreeLocks WHERE LockedAt < @TimeMin
";
        #endregion

        /* ------------------------------------------------ IndexDocument */

        #region SaveIndexDocumentScript
        protected override string SaveIndexDocumentScript => @"-- MsSqlDataProvider.SaveIndexDocument
UPDATE Versions SET [IndexDocument] = @IndexDocument WHERE VersionId = @VersionId
SELECT Timestamp FROM Versions WHERE VersionId = @VersionId"
;
        #endregion

        #region LoadIndexDocumentsHeadScript
        private string LoadIndexDocumentsCommonScript => @"SELECT N.NodeTypeId, V.VersionId, V.NodeId, N.ParentNodeId, N.Path, N.IsSystem,
    N.LastMinorVersionId, N.LastMajorVersionId, V.Status, V.IndexDocument, N.Timestamp NodeTimestamp, V.Timestamp VersionTimestamp
FROM Nodes N INNER JOIN Versions V ON N.NodeId = V.NodeId
";
        #endregion

        #region LoadIndexDocumentsByVersionIdScript
        protected override string LoadIndexDocumentsByVersionIdScript => @"-- MsSqlDataProvider.LoadIndexDocumentsByVersionId
DECLARE @VersionIdTable AS TABLE(Id INT)
INSERT INTO @VersionIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@VersionIds, ',');
" + LoadIndexDocumentsCommonScript + @"WHERE V.VersionId IN (SELECT Id FROM @VersionIdTable)
";
        #endregion

        /* ------------------------------------------------ IndexingActivity */

        #region LoadIndexDocumentCollectionBlockByPathScript
        protected override string LoadIndexDocumentCollectionBlockByPathScript => @"-- MsSqlDataProvider.LoadIndexDocumentCollectionBlockByPath
;WITH IndexDocumentsRanked AS (
    SELECT N.NodeTypeId, V.VersionId, V.NodeId, N.ParentNodeId, N.Path, N.IsSystem, N.LastMinorVersionId, N.LastMajorVersionId,
        V.Status,V.IndexDocument,N.Timestamp AS NodeTimeStamp, V.Timestamp AS VersionTimeStamp,
        ROW_NUMBER() OVER ( ORDER BY Path ) AS RowNum
    FROM Nodes N INNER JOIN Versions V ON N.NodeId = V.NodeId
    WHERE Path = @Path COLLATE Latin1_General_CI_AS OR Path LIKE REPLACE(@Path, '_', '[_]') + '/%' COLLATE Latin1_General_CI_AS
)
SELECT * FROM IndexDocumentsRanked WHERE RowNum BETWEEN @Offset + 1 AND @Offset + @Count
";
        #endregion
        #region LoadIndexDocumentCollectionBlockByPathAndTypeScript
        protected override string LoadIndexDocumentCollectionBlockByPathAndTypeScript => @"-- MsSqlDataProvider.LoadIndexDocumentCollectionBlockByPathAndType
;WITH IndexDocumentsRanked AS (
    SELECT N.NodeTypeId, V.VersionId, V.NodeId, N.ParentNodeId, N.Path, N.IsSystem, N.LastMinorVersionId, N.LastMajorVersionId,
        V.Status,V.IndexDocument,N.Timestamp AS NodeTimeStamp, V.Timestamp AS VersionTimeStamp,
        ROW_NUMBER() OVER ( ORDER BY Path ) AS RowNum
    FROM Nodes N INNER JOIN Versions V ON N.NodeId = V.NodeId
    WHERE N.NodeTypeId NOT IN ({0})
        AND (Path = @Path COLLATE Latin1_General_CI_AS OR Path LIKE REPLACE(@Path, '_', '[_]') + '/%' COLLATE Latin1_General_CI_AS)
)
SELECT * FROM IndexDocumentsRanked WHERE RowNum BETWEEN @Offset + 1 AND @Offset + @Count
";
        #endregion

        #region GetLastIndexingActivityIdScript
        protected override string GetLastIndexingActivityIdScript => @"-- MsSqlDataProvider.GetLastIndexingActivityId
SELECT CASE WHEN i.last_value IS NULL THEN 0 ELSE CONVERT(int, i.last_value) END last_value FROM sys.identity_columns i JOIN sys.tables t ON i.object_id = t.object_id WHERE t.name = 'IndexingActivities'
";
        #endregion

        #region LoadIndexingActivitiesSkeletonScript
        private string LoadIndexingActivitiesSkeletonScript => @"-- MsSqlDataProvider.{0}
{1}SELECT TOP(@Top)
    I.IndexingActivityId, I.ActivityType, I.CreationDate, I.RunningState, I.LockTime, I.NodeId, I.VersionId,
    I.[Path] COLLATE Latin1_General_CI_AS AS Path, [Extension], V.IndexDocument, N.NodeTypeId, N.ParentNodeId, N.IsSystem,
	N.LastMinorVersionId, N.LastMajorVersionId, V.Status, N.Timestamp NodeTimestamp, V.Timestamp VersionTimestamp
FROM IndexingActivities I
	LEFT OUTER JOIN Versions V ON V.VersionId = I.VersionId
	LEFT OUTER JOIN Nodes N on N.NodeId = V.NodeId
{2}
ORDER BY IndexingActivityId
";
        #endregion
        #region LoadIndexingActivitiesPageScript
        protected override string LoadIndexingActivitiesPageScript =>
            string.Format(LoadIndexingActivitiesSkeletonScript, "LoadIndexingActivitiesPageScript",
                "",
                "WHERE IndexingActivityId >= @From AND IndexingActivityId <= @To");
        #endregion
        #region LoadIndexingActivitiyGapsScript
        protected override string LoadIndexingActivitiyGapsScript =>
            string.Format(LoadIndexingActivitiesSkeletonScript, "LoadIndexingActivitiyGapsScript",
                @"DECLARE @GapTable AS TABLE(Gap INT)
INSERT INTO @GapTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@Gaps, ',')
",
                "WHERE IndexingActivityId IN (SELECT Gap FROM @GapTable)");
        #endregion

        #region LoadExecutableIndexingActivitiesScript
        private string LoadExecutableIndexingActivitiesCommonScript => @"-- MsSqlDataProvider.{0}
UPDATE IndexingActivities WITH (TABLOCK) SET RunningState = 'Running', LockTime = GETUTCDATE()
OUTPUT INSERTED.IndexingActivityId, INSERTED.ActivityType, INSERTED.CreationDate, INSERTED.RunningState, INSERTED.LockTime,
	INSERTED.NodeId, INSERTED.VersionId, INSERTED.Path, INSERTED.Extension,
	V.IndexDocument, N.NodeTypeId, N.ParentNodeId, N.IsSystem,
	N.LastMinorVersionId, N.LastMajorVersionId, V.Status, N.Timestamp NodeTimestamp, V.Timestamp VersionTimestamp
	FROM IndexingActivities I
		LEFT OUTER JOIN Versions V ON V.VersionId = I.VersionId
		LEFT OUTER JOIN Nodes N on N.NodeId = I.NodeId
WHERE IndexingActivityId IN (
	SELECT TOP (@Top) NEW.IndexingActivityId FROM IndexingActivities NEW
	WHERE 
		(NEW.RunningState = 'Waiting' OR ((NEW.RunningState = 'Running' AND NEW.LockTime < @TimeLimit))) AND
		NOT EXISTS (
			SELECT IndexingActivityId FROM IndexingActivities OLD
			WHERE (OLD.IndexingActivityId < NEW.IndexingActivityId) AND
				  (
					  (OLD.RunningState = 'Waiting' OR OLD.RunningState = 'Running') AND
					  (
							NEW.NodeId = OLD.NodeId OR
							(NEW.VersionId != 0 AND NEW.VersionId = OLD.VersionId) OR
							NEW.[Path] LIKE OLD.[Path] + '/%' OR
							OLD.[Path] LIKE NEW.[Path] + '/%'
					  )
				  )
		)
	ORDER BY NEW.IndexingActivityId
)
{1}";
        #endregion
        #region LoadExecutableIndexingActivitiesScript
        protected override string LoadExecutableIndexingActivitiesScript => string.Format(LoadExecutableIndexingActivitiesCommonScript,
            "LoadExecutableIndexingActivitiesScript");
        #endregion
        #region LoadExecutableAndFinishedIndexingActivitiesScript
        protected override string LoadExecutableAndFinishedIndexingActivitiesScript => string.Format(LoadExecutableIndexingActivitiesCommonScript,
            "LoadExecutableAndFinishedIndexingActivitiesScript",
            @"
-- Load set of finished activity ids.
DECLARE @IdTable AS TABLE(Id INT)
INSERT INTO @IdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@WaitingIds, ',');
SELECT IndexingActivityId FROM IndexingActivities
WHERE RunningState = 'Done' AND IndexingActivityId IN (SELECT Id FROM @IdTable)
");
        #endregion


        #region RegisterIndexingActivityScript
        protected override string RegisterIndexingActivityScript => @"-- MsSqlDataProvider.RegisterIndexingActivity
INSERT INTO [IndexingActivities]
    ([ActivityType],[CreationDate],[RunningState],[LockTime],[NodeId],[VersionId],[Path],[VersionTimestamp],[Extension]) VALUES
    (@ActivityType, @CreationDate, @RunningState, @LockTime, @NodeId, @VersionId, @Path, @VersionTimestamp, @Extension)
SELECT @@IDENTITY";
        #endregion

        #region UpdateIndexingActivityRunningStateScript
        protected override string UpdateIndexingActivityRunningStateScript => @"-- MsSqlDataProvider.UpdateIndexingActivityRunningState
UPDATE IndexingActivities SET RunningState = @RunningState, LockTime = GETUTCDATE() WHERE IndexingActivityId = @IndexingActivityId
";
        #endregion

        #region RefreshIndexingActivityLockTimeScript
        protected override string RefreshIndexingActivityLockTimeScript => @"-- MsSqlDataProvider.RefreshIndexingActivityLockTime
DECLARE @IdTable AS TABLE(Id INT)
INSERT INTO @IdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@Ids, ',');
UPDATE IndexingActivities SET LockTime = @LockTime WHERE IndexingActivityId IN (SELECT Id FROM @IdTable)
";
        #endregion

        #region DeleteFinishedIndexingActivitiesScript
        protected override string DeleteFinishedIndexingActivitiesScript => @"-- MsSqlDataProvider.DeleteFinishedIndexingActivities
DELETE FROM IndexingActivities WHERE RunningState = 'Done' AND (LockTime < DATEADD(MINUTE, -23, GETUTCDATE()) OR LockTime IS NULL)
";
        #endregion

        #region DeleteAllIndexingActivitiesScript
        protected override string DeleteAllIndexingActivitiesScript => @"-- MsSqlDataProvider.DeleteAllIndexingActivities
DELETE FROM IndexingActivities
";
        #endregion

        /* ------------------------------------------------ Schema */

        #region LoadSchemaScript
        protected override string LoadSchemaScript => @"-- MsSqlDataProvider.LoadSchema
SELECT [Timestamp] FROM SchemaModification
SELECT * FROM PropertyTypes
SELECT * FROM NodeTypes
SELECT * FROM ContentListTypes
";
        #endregion

        #region StartSchemaUpdateScript
        protected override string StartSchemaUpdateScript => @"-- MsSqlDataProvider.StartSchemaUpdate
DECLARE @Result INT
SET @Result = 0

IF NOT EXISTS (SELECT TOP 1 SchemaModificationId FROM SchemaModification)
    INSERT INTO SchemaModification (ModificationDate, LockToken) VALUES (GETUTCDATE(), @LockToken)
ELSE
BEGIN
    UPDATE [SchemaModification] SET [ModificationDate] = GETUTCDATE(), LockToken = @LockToken
        WHERE Timestamp = @Timestamp AND (LockToken IS NULL OR LockToken = @LockToken)

    IF @@ROWCOUNT = 0 BEGIN
        IF EXISTS (SELECT TOP 1 SchemaModificationId FROM SchemaModification WHERE LockToken IS NULL OR LockToken = @Locktoken)
            SET @Result = -1 -- Out of date
        ELSE
            SET @Result = -2 -- Locked by another
    END
END

SELECT TOP 1 @Result [Result], [Timestamp] FROM SchemaModification
";
        #endregion

        #region FinishSchemaUpdateScript
        protected override string FinishSchemaUpdateScript => @"-- MsSqlDataProvider.StartSchemaUpdate
DECLARE @Timestamp [timestamp]
UPDATE [SchemaModification] SET [ModificationDate] = GETUTCDATE(), LockToken = NULL
    WHERE LockToken = @LockToken
IF @@ROWCOUNT = 1
    SELECT TOP 1 [Timestamp] FROM [SchemaModification]
ELSE
    SELECT @Timestamp [Timestamp]
";
        #endregion

        /* ------------------------------------------------ Logging */

        #region WriteAuditEventScript
        protected override string WriteAuditEventScript => @"-- MsSqlDataProvider.WriteAuditEvent
INSERT INTO [dbo].[LogEntries]
    ([EventId], [Category], [Priority], [Severity], [Title], [ContentId], [ContentPath], [UserName], [LogDate], [MachineName], [AppDomainName], [ProcessId], [ProcessName], [ThreadName], [Win32ThreadId], [Message], [FormattedMessage])
VALUES
    (@EventId,  @Category,  @Priority,  @Severity,  @Title,  @ContentId,  @ContentPath,  @UserName,  @LogDate,  @MachineName,  @AppDomainName,  @ProcessId,  @ProcessName,  @ThreadName,  @Win32ThreadId,  @Message,  @FormattedMessage)
SELECT @@IDENTITY
";
        #endregion

        /* ------------------------------------------------ Provider Tools */

        #region GetNameOfLastNodeWithNameBaseScript
        protected override string GetNameOfLastNodeWithNameBaseScript => @"-- MsSqlDataProvider.GetNameOfLastNodeWithNameBase
DECLARE @NameEscaped nvarchar(450)
SET @NameEscaped = REPLACE(@Name, '_', '[_]')
SELECT TOP 1 Name FROM Nodes WHERE ParentNodeId=@ParentId AND (
	Name LIKE @NameEscaped + '([0-9])' + @Extension OR
	Name LIKE @NameEscaped + '([0-9][0-9])' + @Extension OR
	Name LIKE @NameEscaped + '([0-9][0-9][0-9])' + @Extension OR
	Name LIKE @NameEscaped + '([0-9][0-9][0-9][0-9])' + @Extension
)
ORDER BY LEN(Name) DESC, Name DESC
";
        #endregion

        #region GetTreeSizeScript
        protected override string GetTreeSizeScript => @"-- MsSqlDataProvider.GetTreeSize
SELECT SUM(F.Size) Size
FROM Files F
    JOIN BinaryProperties B ON B.FileId = F.FileId
    JOIN Versions V on V.VersionId = B.VersionId
    JOIN Nodes N on V.NodeId = N.NodeId
WHERE F.Staging IS NULL AND (N.[Path] = @NodePath OR (@IncludeChildren = 1 AND N.[Path] + '/' LIKE REPLACE(@NodePath, '_', '[_]') + '/%'))
";
        #endregion

        #region GetNodeCountScript
        protected override string GetNodeCountScript => @"-- MsSqlDataProvider.GetNodeCount
SELECT COUNT (1) FROM Nodes NOLOCK
";

        #endregion

        #region GetVersionCountScript
        protected override string GetVersionCountScript => @"-- MsSqlDataProvider.GetVersionCount
SELECT COUNT (1) FROM Versions NOLOCK
";

        #endregion

        /* ------------------------------------------------ Installation */

        #region LoadEntityTreeScript
        protected override string LoadEntityTreeScript { get; } = @"-- MsSqlDataProvider.LoadEntityTree
SELECT NodeId, ParentNodeId, OwnerId FROM Nodes ORDER BY Path
";
        #endregion

        /* ------------------------------------------------ Tools */
    }

}
