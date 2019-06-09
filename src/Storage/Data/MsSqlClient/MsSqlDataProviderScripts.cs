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
    DELETE FROM LongTextProperties WHERE VersionId = @NewVersionId;
END    

-- Copy properties
INSERT INTO BinaryProperties ([VersionId],[PropertyTypeId],[FileId])
    SELECT @NewVersionId,[PropertyTypeId],[FileId] FROM BinaryProperties WHERE VersionId = @PreviousVersionId
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
			    --DELETE ReferenceProperties WHERE (VersionId IN (SELECT VersionId FROM @VID)) OR
			    --									(ReferredNodeId IN (SELECT NodeId FROM @NIDpartition))

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
WHERE V.VersionId IN (select Id from @VersionIdTable)

-- BinaryProperties
SELECT B.BinaryPropertyId, B.VersionId, B.PropertyTypeId, F.FileId, F.ContentType, F.FileNameWithoutExtension,
    F.Extension, F.[Size], F.[BlobProvider], F.[BlobProviderData], F.[Checksum], NULL AS Stream, 0 AS Loaded, F.[Timestamp]
FROM dbo.BinaryProperties B
    JOIN dbo.Files F ON B.FileId = F.FileId
WHERE VersionId IN (select Id from @VersionIdTable) AND Staging IS NULL

    -- ReferenceProperties
    --SELECT VersionId, PropertyTypeId, ReferredNodeId
    --FROM dbo.ReferenceProperties
    --WHERE VersionId IN (select Id from @VersionIdTable)

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
    --DELETE FROM ReferenceProperties WHERE VersionId IN (SELECT Id FROM @VersionIdTable)

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

        #region InstanceCountScript
        protected override string InstanceCountScript => @"-- MsSqlDataProvider.InstanceCount
SELECT COUNT(*) FROM Nodes WHERE NodeTypeId IN ({0})
";
        #endregion

        /* ------------------------------------------------ NodeQuery */

        /* ------------------------------------------------ Tree */

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

        /* ------------------------------------------------ IndexingActivity */

        #region GetLastIndexingActivityIdScript
        protected override string GetLastIndexingActivityIdScript => @"-- MsSqlDataProvider.GetLastIndexingActivityId
SELECT CASE WHEN i.last_value IS NULL THEN 0 ELSE CONVERT(int, i.last_value) END last_value FROM sys.identity_columns i JOIN sys.tables t ON i.object_id = t.object_id WHERE t.name = 'IndexingActivities'";
        #endregion

        /* ------------------------------------------------ Schema */

        #region LoadSchemaScript
        //UNDONE:DB: LoadSchema script: ContentListTypes is commnted out
        protected override string LoadSchemaScript => @"-- MsSqlDataProvider.LoadSchema
SELECT [Timestamp] FROM SchemaModification
SELECT * FROM PropertyTypes
SELECT * FROM NodeTypes
--SELECT * FROM ContentListTypes
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
UPDATE [SchemaModification] SET [ModificationDate] = GETUTCDATE(), LockToken = NULL,  @Timestamp = [Timestamp]
    WHERE LockToken = @LockToken
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
