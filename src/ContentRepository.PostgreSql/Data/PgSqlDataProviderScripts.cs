using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{
    /// <summary>
    /// Contains all SQL scripts for the PostgreSQL data provider, converted from T-SQL.
    /// Each script is carefully matched to the column order and parameter names
    /// expected by RelationalDataProviderBase.
    /// </summary>
    public partial class PgSqlDataProvider
    {
        // =============================================================================================== Node Insert

        #region InsertNodeAndVersionScript
        // Base class params (Insert):
        //   Node: @NodeTypeId, @ContentListTypeId, @ContentListId, @CreatingInProgress, @IsDeleted, @IsInherited,
        //         @ParentNodeId, @Name, @DisplayName, @Path, @Index, @Locked, @LockedById,
        //         @ETag, @LockType, @LockTimeout, @LockDate, @LockToken, @LastLockUpdate,
        //         @NodeCreationDate, @NodeCreatedById, @NodeModificationDate, @NodeModifiedById,
        //         @IsSystem, @OwnerId, @SavingState, @ChangedData
        //   Version: @MajorNumber, @MinorNumber, @Status,
        //         @VersionCreationDate, @VersionCreatedById, @VersionModificationDate, @VersionModifiedById,
        //         @DynamicProperties, @ContentListProperties
        // Result (by name): NodeId, NodeTimestamp, LastMajorVersionId, LastMinorVersionId, VersionId, VersionTimestamp
        protected override string InsertNodeAndVersionScript => @"-- PgSqlDataProvider.InsertNodeAndVersion
SELECT * FROM sn_insert_node_and_version(
    @NodeTypeId, @ContentListTypeId, @ContentListId, @CreatingInProgress, @IsDeleted, @IsInherited,
    @ParentNodeId, @Name, @DisplayName, @Path, @Index, @Locked, @LockedById,
    @ETag, @LockType, @LockTimeout, @LockDate, @LockToken, @LastLockUpdate,
    @NodeCreationDate, @NodeCreatedById, @NodeModificationDate, @NodeModifiedById,
    @IsSystem, @OwnerId, @SavingState,
    @MajorNumber, @MinorNumber, @Status, @ChangedData,
    @VersionCreationDate, @VersionCreatedById, @VersionModificationDate, @VersionModifiedById,
    @DynamicProperties, @ContentListProperties
);
";
        #endregion

        #region InsertReferencePropertiesHeadScript
        // Base class uses .Append() (not formatted). Params: @VersionId
        // MsSql declares a table variable here. For PgSql, we handle in each per-property script.
        protected override string InsertReferencePropertiesHeadScript => @"-- PgSqlDataProvider.InsertReferencePropertiesHead
DELETE FROM ""ReferenceProperties"" WHERE ""VersionId"" = @VersionId;
";
        #endregion

        #region InsertReferencePropertiesScript
        // Base class uses .AppendFormat(script, index).
        // Params per index: @PropertyTypeId{N}, @ReferredNodeIds{N} (comma-separated string)
        protected override string InsertReferencePropertiesScript => @"
INSERT INTO ""ReferenceProperties"" (""VersionId"", ""PropertyTypeId"", ""ReferredNodeId"")
SELECT @VersionId, @PropertyTypeId{0}, unnest(string_to_array(@ReferredNodeIds{0}, ','))::int
WHERE COALESCE(@ReferredNodeIds{0}, '') <> '';
";
        #endregion

        #region InsertLongtextPropertiesHeadScript
        // Base class uses .Append() (not formatted). No content needed.
        protected override string InsertLongtextPropertiesHeadScript => @"-- PgSqlDataProvider.InsertLongtextPropertiesHead
";
        #endregion

        #region InsertLongtextPropertiesScript
        // Base class uses .AppendFormat(script, index).
        // Params per index: @PropertyTypeId{N}, @Length{N}, @Value{N}
        protected override string InsertLongtextPropertiesScript => @"INSERT INTO ""LongTextProperties""
    (""VersionId"", ""PropertyTypeId"", ""Length"", ""Value"") VALUES
    (@VersionId, @PropertyTypeId{0}, @Length{0}, @Value{0});
";
        #endregion

        // =============================================================================================== Node Update

        #region UpdateVersionScript
        // Base class params: @VersionId, @NodeId, @MajorNumber, @MinorNumber, @Status,
        //   @CreationDate, @CreatedById, @ModificationDate, @ModifiedById,
        //   @ChangedData, @DynamicProperties, @ContentListProperties
        // Return: ExecuteScalar -> single Timestamp value
        protected override string UpdateVersionScript => @"-- PgSqlDataProvider.UpdateVersion
UPDATE ""Versions"" SET
    ""NodeId"" = @NodeId,
    ""MajorNumber"" = @MajorNumber,
    ""MinorNumber"" = @MinorNumber,
    ""CreationDate"" = @CreationDate,
    ""CreatedById"" = @CreatedById,
    ""ModificationDate"" = @ModificationDate,
    ""ModifiedById"" = @ModifiedById,
    ""Status"" = @Status,
    ""ChangedData"" = @ChangedData,
    ""DynamicProperties"" = @DynamicProperties,
    ""ContentListProperties"" = @ContentListProperties
WHERE ""VersionId"" = @VersionId
RETURNING ""Timestamp""
";
        #endregion

        #region UpdateNodeScript
        // Base class params: @NodeId, @NodeTypeId, @ContentListTypeId, @ContentListId,
        //   @CreatingInProgress, @IsDeleted, @IsInherited, @ParentNodeId, @Name, @DisplayName, @Path,
        //   @Index, @Locked, @LockedById, @ETag, @LockType, @LockTimeout, @LockDate, @LockToken, @LastLockUpdate,
        //   @CreationDate, @CreatedById, @ModificationDate, @ModifiedById,
        //   @IsSystem, @OwnerId, @SavingState, @NodeTimestamp (binary)
        // Return: ExecuteScalar -> single Timestamp value
        protected override string UpdateNodeScript => @"-- PgSqlDataProvider.UpdateNode
UPDATE ""Nodes"" SET
    ""NodeTypeId"" = @NodeTypeId,
    ""ContentListTypeId"" = @ContentListTypeId,
    ""ContentListId"" = @ContentListId,
    ""CreatingInProgress"" = @CreatingInProgress,
    ""IsDeleted"" = @IsDeleted,
    ""IsInherited"" = @IsInherited,
    ""ParentNodeId"" = @ParentNodeId,
    ""Name"" = @Name,
    ""DisplayName"" = @DisplayName,
    ""Path"" = @Path,
    ""Index"" = @Index,
    ""Locked"" = @Locked,
    ""LockedById"" = @LockedById,
    ""ETag"" = @ETag,
    ""LockType"" = @LockType,
    ""LockTimeout"" = @LockTimeout,
    ""LockDate"" = @LockDate,
    ""LockToken"" = @LockToken,
    ""LastLockUpdate"" = @LastLockUpdate,
    ""CreationDate"" = @CreationDate,
    ""CreatedById"" = @CreatedById,
    ""ModificationDate"" = @ModificationDate,
    ""ModifiedById"" = @ModifiedById,
    ""IsSystem"" = @IsSystem,
    ""OwnerId"" = @OwnerId,
    ""SavingState"" = @SavingState
WHERE ""NodeId"" = @NodeId AND ""Timestamp"" = @NodeTimestamp
RETURNING ""Timestamp""
";
        #endregion

        #region UpdateSubTreePathScript
        protected override string UpdateSubTreePathScript => @"-- PgSqlDataProvider.UpdateSubTreePath
UPDATE ""Nodes"" SET ""Path"" = @NewPath || SUBSTRING(""Path""::TEXT FROM LENGTH(@OldPath::TEXT) + 1)
WHERE ""Path"" LIKE @OldPath || '/%'
";
        #endregion

        #region ManageLastVersionsScript
        // Base class params: @NodeId, @VersionIds (comma-separated or DBNull)
        // Result (by name): NodeTimestamp, LastMajorVersionId, LastMinorVersionId
        protected override string ManageLastVersionsScript => @"-- PgSqlDataProvider.ManageLastVersions
-- Delete versions if @VersionIds is provided (string_to_array(NULL,...) returns NULL -> no rows matched)
DELETE FROM ""LongTextProperties"" WHERE ""VersionId"" = ANY(string_to_array(@VersionIds, ',')::int[]);
DELETE FROM ""ReferenceProperties"" WHERE ""VersionId"" = ANY(string_to_array(@VersionIds, ',')::int[]);
DELETE FROM ""Versions"" WHERE ""VersionId"" = ANY(string_to_array(@VersionIds, ',')::int[]);

-- Recalculate last versions
UPDATE ""Nodes"" SET
    ""LastMinorVersionId"" = (
        SELECT ""VersionId"" FROM ""Versions""
        WHERE ""NodeId"" = @NodeId
        ORDER BY ""MajorNumber"" DESC, ""MinorNumber"" DESC
        LIMIT 1),
    ""LastMajorVersionId"" = (
        SELECT ""VersionId"" FROM ""Versions""
        WHERE ""NodeId"" = @NodeId AND ""MinorNumber"" = 0 AND ""Status"" = 1
        ORDER BY ""MajorNumber"" DESC, ""MinorNumber"" DESC
        LIMIT 1)
WHERE ""NodeId"" = @NodeId
RETURNING ""Timestamp"" AS ""NodeTimestamp"", ""LastMajorVersionId"", ""LastMinorVersionId""
";
        #endregion

        #region UpdateReferencePropertiesHeadScript
        // Base class uses .Append() (not formatted). Params: @VersionId
        protected override string UpdateReferencePropertiesHeadScript => @"-- PgSqlDataProvider.UpdateReferencePropertiesHead
";
        #endregion

        #region UpdateReferencePropertiesScript
        // Base class uses .AppendFormat(script, index).
        // Params per index: @PropertyTypeId{N}, @ReferredNodeIds{N} (comma-separated string)
        protected override string UpdateReferencePropertiesScript => @"--
DELETE FROM ""ReferenceProperties"" WHERE ""VersionId"" = @VersionId AND ""PropertyTypeId"" = @PropertyTypeId{0};
INSERT INTO ""ReferenceProperties"" (""VersionId"", ""PropertyTypeId"", ""ReferredNodeId"")
SELECT @VersionId, @PropertyTypeId{0}, unnest(string_to_array(@ReferredNodeIds{0}, ','))::int
WHERE COALESCE(@ReferredNodeIds{0}, '') <> '';
";
        #endregion

        #region UpdateLongtextPropertiesHeadScript
        // Base class uses .Append() (not formatted). No format placeholder allowed here!
        protected override string UpdateLongtextPropertiesHeadScript => @"-- PgSqlDataProvider.UpdateLongtextPropertiesHead
";
        #endregion

        #region UpdateLongtextPropertiesScript
        // Base class uses .AppendFormat(script, index).
        // Params per index: @PropertyTypeId{N}, @Length{N}, @Value{N}
        protected override string UpdateLongtextPropertiesScript => @"-- PgSqlDataProvider.UpdateLongtextProperties
DELETE FROM ""LongTextProperties"" WHERE ""VersionId"" = @VersionId AND ""PropertyTypeId"" = @PropertyTypeId{0};
INSERT INTO ""LongTextProperties""
    (""VersionId"", ""PropertyTypeId"", ""Length"", ""Value"") VALUES
    (@VersionId, @PropertyTypeId{0}, @Length{0}, @Value{0});
";
        #endregion

        // =============================================================================================== Copy Version

        #region CopyVersionAndUpdateScript
        // Base class params: @PreviousVersionId, @DestinationVersionId (int or DBNull),
        //   @NodeId, @MajorNumber, @MinorNumber, @Status,
        //   @CreationDate, @CreatedById, @ModificationDate, @ModifiedById,
        //   @ChangedData, @DynamicProperties, @ContentListProperties
        // Result set 1 (by name): VersionId, Timestamp
        // Result set 2 (by name): BinaryPropertyId, PropertyTypeId
        protected override string CopyVersionAndUpdateScript => @"-- PgSqlDataProvider.CopyVersionAndUpdate
-- Set older locked versions to Draft
UPDATE ""Versions"" SET ""Status"" = 4 WHERE ""NodeId"" = @NodeId AND ""Status"" = 2;

-- Insert or update version
WITH target_version AS (
    -- Insert new version if @DestinationVersionId is NULL
    INSERT INTO ""Versions""
        (""NodeId"", ""MajorNumber"", ""MinorNumber"", ""CreationDate"", ""CreatedById"",
         ""ModificationDate"", ""ModifiedById"", ""Status"", ""ChangedData"",
         ""DynamicProperties"", ""ContentListProperties"")
    SELECT
        @NodeId, @MajorNumber, @MinorNumber, @CreationDate, @CreatedById,
        @ModificationDate, @ModifiedById, @Status, @ChangedData,
        @DynamicProperties, @ContentListProperties
    WHERE @DestinationVersionId IS NULL
    RETURNING ""VersionId"", ""Timestamp""
)
SELECT ""VersionId"", ""Timestamp"" FROM target_version;

-- If @DestinationVersionId is not NULL, update existing version
UPDATE ""Versions"" SET
    ""NodeId"" = @NodeId,
    ""MajorNumber"" = @MajorNumber,
    ""MinorNumber"" = @MinorNumber,
    ""CreationDate"" = @CreationDate,
    ""CreatedById"" = @CreatedById,
    ""ModificationDate"" = @ModificationDate,
    ""ModifiedById"" = @ModifiedById,
    ""Status"" = @Status,
    ""ChangedData"" = @ChangedData,
    ""DynamicProperties"" = @DynamicProperties,
    ""ContentListProperties"" = @ContentListProperties
WHERE ""VersionId"" = @DestinationVersionId AND @DestinationVersionId IS NOT NULL
RETURNING ""VersionId"", ""Timestamp"";

-- Copy properties from previous version
INSERT INTO ""BinaryProperties"" (""VersionId"", ""PropertyTypeId"", ""FileId"")
    SELECT COALESCE(@DestinationVersionId, currval('""Versions_VersionId_seq""')), ""PropertyTypeId"", ""FileId""
    FROM ""BinaryProperties"" WHERE ""VersionId"" = @PreviousVersionId;
INSERT INTO ""ReferenceProperties"" (""VersionId"", ""PropertyTypeId"", ""ReferredNodeId"")
    SELECT COALESCE(@DestinationVersionId, currval('""Versions_VersionId_seq""')), ""PropertyTypeId"", ""ReferredNodeId""
    FROM ""ReferenceProperties"" WHERE ""VersionId"" = @PreviousVersionId;
INSERT INTO ""LongTextProperties"" (""VersionId"", ""PropertyTypeId"", ""Length"", ""Value"")
    SELECT COALESCE(@DestinationVersionId, currval('""Versions_VersionId_seq""')), ""PropertyTypeId"", ""Length"", ""Value""
    FROM ""LongTextProperties"" WHERE ""VersionId"" = @PreviousVersionId;

-- Return binary properties info (result set 2)
SELECT B.""BinaryPropertyId"", B.""PropertyTypeId"" FROM ""BinaryProperties"" B
    JOIN ""Files"" F ON B.""FileId"" = F.""FileId""
WHERE B.""VersionId"" = COALESCE(@DestinationVersionId, currval('""Versions_VersionId_seq""'))
    AND F.""Staging"" IS NULL
";
        #endregion

        // =============================================================================================== Changed Data

        #region LoadChangedDataScript
        protected override string LoadChangedDataScript => @"-- PgSqlDataProvider.LoadChangedData
SELECT ""ChangedData"" FROM ""Versions"" WHERE ""VersionId"" = @VersionId
";
        #endregion

        #region SaveChangedDataScript
        protected override string SaveChangedDataScript => @"-- PgSqlDataProvider.SaveChangedData
UPDATE ""Versions"" SET ""ChangedData"" = @ChangedData WHERE ""VersionId"" = @VersionId
RETURNING ""Timestamp""
";
        #endregion

        // =============================================================================================== Load Nodes

        #region LoadNodesScript
        // Base class params: @VersionIds (comma-separated string), @LongTextMaxSize (int)
        // 4 result sets: BaseData (joined Node+Version), Binary, Reference, LongText
        // BaseData columns read BY NAME: NodeId, NodeTypeId, ContentListTypeId, ContentListId,
        //   CreatingInProgress, IsDeleted, ParentNodeId, Name, DisplayName, Path, Index,
        //   Locked, LockedById, ETag, LockType, LockTimeout, LockDate, LockToken, LastLockUpdate,
        //   NodeCreationDate, NodeCreatedById, NodeModificationDate, NodeModifiedById,
        //   IsSystem, OwnerId, SavingState, ChangedData, NodeTimestamp,
        //   VersionId, MajorNumber, MinorNumber, CreationDate, CreatedById,
        //   ModificationDate, ModifiedById, Status, VersionTimestamp,
        //   DynamicProperties, ContentListProperties
        protected override string LoadNodesScript => @"-- PgSqlDataProvider.LoadNodes
-- BaseData (joined Node+Version)
SELECT N.""NodeId"", N.""NodeTypeId"", N.""ContentListTypeId"", N.""ContentListId"",
    N.""CreatingInProgress"", N.""IsDeleted"", N.""ParentNodeId"",
    N.""Name"", N.""DisplayName"", N.""Path""::TEXT, N.""Index"", N.""Locked"", N.""LockedById"",
    N.""ETag"", N.""LockType"", N.""LockTimeout"", N.""LockDate"", N.""LockToken"", N.""LastLockUpdate"",
    N.""CreationDate"" AS ""NodeCreationDate"", N.""CreatedById"" AS ""NodeCreatedById"",
    N.""ModificationDate"" AS ""NodeModificationDate"", N.""ModifiedById"" AS ""NodeModifiedById"",
    N.""IsSystem"", N.""OwnerId"",
    N.""SavingState"", V.""ChangedData"",
    N.""Timestamp"" AS ""NodeTimestamp"",
    V.""VersionId"", V.""MajorNumber"", V.""MinorNumber"", V.""CreationDate"", V.""CreatedById"",
    V.""ModificationDate"", V.""ModifiedById"", V.""Status"",
    V.""Timestamp"" AS ""VersionTimestamp"",
    V.""DynamicProperties"", V.""ContentListProperties""
FROM ""Nodes"" N
    INNER JOIN ""Versions"" V ON N.""NodeId"" = V.""NodeId""
WHERE V.""VersionId"" = ANY(string_to_array(@VersionIds, ',')::int[]);

-- BinaryProperties
SELECT B.""BinaryPropertyId"", B.""VersionId"", B.""PropertyTypeId"",
    F.""FileId"", F.""ContentType"", F.""FileNameWithoutExtension"",
    F.""Extension"", F.""Size"", F.""BlobProvider"", F.""BlobProviderData"",
    F.""Checksum"", NULL AS ""Stream"", 0 AS ""Loaded"", F.""Timestamp""
FROM ""BinaryProperties"" B
    JOIN ""Files"" F ON B.""FileId"" = F.""FileId""
WHERE B.""VersionId"" = ANY(string_to_array(@VersionIds, ',')::int[]) AND F.""Staging"" IS NULL;

-- ReferenceProperties
SELECT ""VersionId"", ""PropertyTypeId"", ""ReferredNodeId"" FROM ""ReferenceProperties""
WHERE ""VersionId"" = ANY(string_to_array(@VersionIds, ',')::int[]);

-- LongTextProperties
SELECT ""VersionId"", ""PropertyTypeId"", ""Length"", ""Value"" FROM ""LongTextProperties""
WHERE ""VersionId"" = ANY(string_to_array(@VersionIds, ',')::int[]) AND ""Length"" < @LongTextMaxSize;
";
        #endregion

        // =============================================================================================== Delete Node

        #region DeleteNodeScript
        // Base class params: @NodeId, @Timestamp (binary, can be null), @PartitionSize
        protected override string DeleteNodeScript => @"-- PgSqlDataProvider.DeleteNode
DELETE FROM ""LongTextProperties"" WHERE ""VersionId"" IN (
    SELECT ""VersionId"" FROM ""Versions"" WHERE ""NodeId"" IN (
        SELECT ""NodeId"" FROM ""Nodes"" WHERE ""NodeId"" = @NodeId OR ""Path"" LIKE (
            SELECT ""Path"" FROM ""Nodes"" WHERE ""NodeId"" = @NodeId) || '/%'));
DELETE FROM ""ReferenceProperties"" WHERE ""VersionId"" IN (
    SELECT ""VersionId"" FROM ""Versions"" WHERE ""NodeId"" IN (
        SELECT ""NodeId"" FROM ""Nodes"" WHERE ""NodeId"" = @NodeId OR ""Path"" LIKE (
            SELECT ""Path"" FROM ""Nodes"" WHERE ""NodeId"" = @NodeId) || '/%'));
DELETE FROM ""BinaryProperties"" WHERE ""VersionId"" IN (
    SELECT ""VersionId"" FROM ""Versions"" WHERE ""NodeId"" IN (
        SELECT ""NodeId"" FROM ""Nodes"" WHERE ""NodeId"" = @NodeId OR ""Path"" LIKE (
            SELECT ""Path"" FROM ""Nodes"" WHERE ""NodeId"" = @NodeId) || '/%'));
DELETE FROM ""Versions"" WHERE ""NodeId"" IN (
    SELECT ""NodeId"" FROM ""Nodes"" WHERE ""NodeId"" = @NodeId OR ""Path"" LIKE (
        SELECT ""Path"" FROM ""Nodes"" WHERE ""NodeId"" = @NodeId) || '/%');
DELETE FROM ""Nodes"" WHERE ""NodeId"" = @NodeId OR ""Path"" LIKE (
    SELECT ""Path"" FROM ""Nodes"" WHERE ""NodeId"" = @NodeId) || '/%';
";
        #endregion

        // =============================================================================================== Move Node

        #region MoveNodeScript
        protected override string MoveNodeScript => @"-- PgSqlDataProvider.MoveNode
UPDATE ""Nodes""
    SET ""ParentNodeId"" = @TargetId,
        ""Path"" = @TargetPath || '/' || ""Name""
    WHERE ""NodeId"" = @SourceId AND ""Timestamp"" = @SourceTimestamp
    RETURNING ""Timestamp""
";
        #endregion

        // =============================================================================================== Text properties

        #region LoadTextPropertyValuesScript
        protected override string LoadTextPropertyValuesScript => @"-- PgSqlDataProvider.LoadTextPropertyValues
SELECT ""PropertyTypeId"", ""Value"" FROM ""LongTextProperties""
WHERE ""VersionId"" = @VersionId AND ""PropertyTypeId"" IN ({0})
";
        #endregion

        // =============================================================================================== Node exists / Head

        #region NodeExistsScript
        protected override string NodeExistsScript => @"-- PgSqlDataProvider.NodeExists
SELECT CASE WHEN EXISTS (SELECT 1 FROM ""Nodes"" WHERE ""Path"" = @Path) THEN 1 ELSE 0 END
";
        #endregion

        #region LoadNodeHeadByPathScript
        protected override string LoadNodeHeadByPathScript => @"-- PgSqlDataProvider.LoadNodeHeadByPath
SELECT
    ""NodeId"",             -- 0
    ""Name"",               -- 1
    ""DisplayName"",        -- 2
    ""Path""::TEXT,         -- 3
    ""ParentNodeId"",       -- 4
    ""NodeTypeId"",         -- 5
    ""ContentListTypeId"",  -- 6
    ""ContentListId"",      -- 7
    ""CreationDate"",       -- 8
    ""ModificationDate"",   -- 9
    ""LastMinorVersionId"", -- 10
    ""LastMajorVersionId"", -- 11
    ""OwnerId"",            -- 12
    ""CreatedById"",        -- 13
    ""ModifiedById"",       -- 14
    ""Index"",              -- 15
    ""LockedById"",         -- 16
    ""Timestamp""           -- 17
FROM ""Nodes""
WHERE ""Path"" = @Path
";
        #endregion

        #region LoadNodeHeadByIdScript
        protected override string LoadNodeHeadByIdScript => @"-- PgSqlDataProvider.LoadNodeHeadById
SELECT
    ""NodeId"",             -- 0
    ""Name"",               -- 1
    ""DisplayName"",        -- 2
    ""Path""::TEXT,         -- 3
    ""ParentNodeId"",       -- 4
    ""NodeTypeId"",         -- 5
    ""ContentListTypeId"",  -- 6
    ""ContentListId"",      -- 7
    ""CreationDate"",       -- 8
    ""ModificationDate"",   -- 9
    ""LastMinorVersionId"", -- 10
    ""LastMajorVersionId"", -- 11
    ""OwnerId"",            -- 12
    ""CreatedById"",        -- 13
    ""ModifiedById"",       -- 14
    ""Index"",              -- 15
    ""LockedById"",         -- 16
    ""Timestamp""           -- 17
FROM ""Nodes""
WHERE ""NodeId"" = @NodeId
";
        #endregion

        #region LoadNodeHeadByVersionIdScript
        protected override string LoadNodeHeadByVersionIdScript => @"-- PgSqlDataProvider.LoadNodeHeadByVersionId
SELECT
    N.""NodeId"",             -- 0
    N.""Name"",               -- 1
    N.""DisplayName"",        -- 2
    N.""Path""::TEXT,         -- 3
    N.""ParentNodeId"",       -- 4
    N.""NodeTypeId"",         -- 5
    N.""ContentListTypeId"",  -- 6
    N.""ContentListId"",      -- 7
    N.""CreationDate"",       -- 8
    N.""ModificationDate"",   -- 9
    N.""LastMinorVersionId"", -- 10
    N.""LastMajorVersionId"", -- 11
    N.""OwnerId"",            -- 12
    N.""CreatedById"",        -- 13
    N.""ModifiedById"",       -- 14
    N.""Index"",              -- 15
    N.""LockedById"",         -- 16
    N.""Timestamp""           -- 17
FROM ""Nodes"" N JOIN ""Versions"" V ON N.""NodeId"" = V.""NodeId""
WHERE V.""VersionId"" = @VersionId
";
        #endregion

        #region LoadNodeHeadsByIdSetScript
        protected override string LoadNodeHeadsByIdSetScript => @"-- PgSqlDataProvider.LoadNodeHeadsByIdSet
SELECT
    ""NodeId"",             -- 0
    ""Name"",               -- 1
    ""DisplayName"",        -- 2
    ""Path""::TEXT,         -- 3
    ""ParentNodeId"",       -- 4
    ""NodeTypeId"",         -- 5
    ""ContentListTypeId"",  -- 6
    ""ContentListId"",      -- 7
    ""CreationDate"",       -- 8
    ""ModificationDate"",   -- 9
    ""LastMinorVersionId"", -- 10
    ""LastMajorVersionId"", -- 11
    ""OwnerId"",            -- 12
    ""CreatedById"",        -- 13
    ""ModifiedById"",       -- 14
    ""Index"",              -- 15
    ""LockedById"",         -- 16
    ""Timestamp""           -- 17
FROM ""Nodes""
WHERE ""NodeId"" = ANY(string_to_array(@NodeIds, ',')::int[])
";
        #endregion

        // =============================================================================================== Versions

        #region GetVersionNumbersByNodeIdScript
        protected override string GetVersionNumbersByNodeIdScript => @"-- PgSqlDataProvider.GetVersionNumbersByNodeId
SELECT ""VersionId"", ""MajorNumber"", ""MinorNumber"", ""Status"" FROM ""Versions""
WHERE ""NodeId"" = @NodeId ORDER BY ""MajorNumber"", ""MinorNumber""
";
        #endregion

        #region GetVersionNumbersByPathScript
        protected override string GetVersionNumbersByPathScript => @"-- PgSqlDataProvider.GetVersionNumbersByPath
SELECT V.""VersionId"", V.""MajorNumber"", V.""MinorNumber"", V.""Status""
FROM ""Versions"" V JOIN ""Nodes"" N ON V.""NodeId"" = N.""NodeId""
WHERE N.""Path"" = @Path
ORDER BY V.""MajorNumber"", V.""MinorNumber""
";
        #endregion

        // =============================================================================================== NodeQuery

        #region InstanceCountScript
        protected override string InstanceCountScript => @"-- PgSqlDataProvider.InstanceCount
SELECT COUNT(*) FROM ""Nodes"" WHERE ""NodeTypeId"" IN ({0})
";
        #endregion

        #region GetChildrenIdentfiersScript
        protected override string GetChildrenIdentfiersScript => @"-- PgSqlDataProvider.GetChildrenIdentifiers
SELECT ""NodeId"" FROM ""Nodes"" WHERE ""ParentNodeId"" = @ParentNodeId
";
        #endregion

        #region QueryNodesByReferenceScript
        protected override string QueryNodesByReferenceScript => @"-- PgSqlDataProvider.QueryNodesByReference
SELECT V.""NodeId"" FROM ""ReferenceProperties"" R
    JOIN ""Versions"" V ON R.""VersionId"" = V.""VersionId""
    JOIN ""Nodes"" N ON V.""VersionId"" = N.""LastMinorVersionId""
WHERE R.""PropertyTypeId"" = @PropertyTypeId AND R.""ReferredNodeId"" = @ReferredNodeId
";
        #endregion

        #region QueryNodesByReferenceAndTypeScript
        protected override string QueryNodesByReferenceAndTypeScript => @"-- PgSqlDataProvider.QueryNodesByReferenceAndType
SELECT N.""NodeId"" FROM ""ReferenceProperties"" R
    JOIN ""Versions"" V ON R.""VersionId"" = V.""VersionId""
    JOIN ""Nodes"" N ON V.""VersionId"" = N.""LastMinorVersionId""
WHERE R.""PropertyTypeId"" = @PropertyTypeId AND R.""ReferredNodeId"" = @ReferredNodeId
    AND N.""NodeTypeId"" IN ({0})
";
        #endregion

        // =============================================================================================== ContentList

        #region LoadChildTypesToAllowScript
        protected override string LoadChildTypesToAllowScript => @"-- PgSqlDataProvider.LoadChildTypesToAllow
SELECT DISTINCT N.""NodeTypeId"" FROM ""Nodes"" N WHERE N.""ParentNodeId"" = @NodeId
";
        #endregion

        #region GetContentListTypesInTreeScript
        protected override string GetContentListTypesInTreeScript => @"-- PgSqlDataProvider.GetContentListTypesInTree
SELECT DISTINCT N.""ContentListTypeId"" FROM ""Nodes"" N
WHERE N.""ContentListTypeId"" IS NOT NULL
  AND N.""ContentListId"" IS NULL
  AND N.""Path"" LIKE @Path || '/%'
";
        #endregion

        // =============================================================================================== TreeLock

        #region AcquireTreeLockScript
        protected override string AcquireTreeLockScript => @"-- PgSqlDataProvider.AcquireTreeLock
INSERT INTO ""TreeLocks"" (""Path"", ""LockedAt"")
SELECT @Path0, NOW() AT TIME ZONE 'UTC'
WHERE NOT EXISTS (
    SELECT 1 FROM ""TreeLocks""
    WHERE @TimeMin < ""LockedAt"" AND (
        ""Path"" LIKE @Path0 || '/%' OR
        ""Path"" IN ( {0} ))
)
RETURNING ""TreeLockId""
";
        #endregion

        #region IsTreeLockedScript
        protected override string IsTreeLockedScript => @"-- PgSqlDataProvider.IsTreeLocked
SELECT ""TreeLockId"" FROM ""TreeLocks""
WHERE @TimeLimit < ""LockedAt"" AND (
    ""Path"" LIKE @Path0 || '/%' OR
    ""Path"" IN ( {0} ))
LIMIT 1
";
        #endregion

        #region ReleaseTreeLockScript
        protected override string ReleaseTreeLockScript => @"-- PgSqlDataProvider.ReleaseTreeLock
DELETE FROM ""TreeLocks"" WHERE ""TreeLockId"" IN ({0})
";
        #endregion

        #region LoadAllTreeLocksScript
        protected override string LoadAllTreeLocksScript => @"-- PgSqlDataProvider.LoadAllTreeLocks
SELECT ""TreeLockId"", ""Path"" FROM ""TreeLocks""
";
        #endregion

        #region DeleteUnusedLocksScript
        protected override string DeleteUnusedLocksScript => @"-- PgSqlDataProvider.DeleteUnusedLocks
DELETE FROM ""TreeLocks"" WHERE ""LockedAt"" < @TimeMin
";
        #endregion

        // =============================================================================================== IndexDocument

        #region SaveIndexDocumentScript
        protected override string SaveIndexDocumentScript => @"-- PgSqlDataProvider.SaveIndexDocument
UPDATE ""Versions"" SET ""IndexDocument"" = @IndexDocument WHERE ""VersionId"" = @VersionId
RETURNING ""Timestamp""
";
        #endregion

        #region LoadIndexDocumentsCommonColumns
        // GetIndexDocumentDataFromReader reads by name:
        //   NodeTypeId, VersionId, NodeId, ParentNodeId, Path, IsSystem,
        //   LastMinorVersionId, LastMajorVersionId, Status, IndexDocument,
        //   NodeTimestamp, VersionTimestamp
        private string LoadIndexDocumentsCommon => @"SELECT N.""NodeTypeId"", V.""VersionId"", V.""NodeId"",
    N.""ParentNodeId"", N.""Path""::TEXT AS ""Path"", N.""IsSystem"",
    N.""LastMinorVersionId"", N.""LastMajorVersionId"",
    V.""Status"", V.""IndexDocument"",
    N.""Timestamp"" AS ""NodeTimestamp"", V.""Timestamp"" AS ""VersionTimestamp""
FROM ""Versions"" V
    INNER JOIN ""Nodes"" N ON V.""NodeId"" = N.""NodeId""
";
        #endregion

        #region LoadIndexDocumentsByVersionIdScript
        // Base class params: @VersionIds (comma-separated string)
        protected override string LoadIndexDocumentsByVersionIdScript => @"-- PgSqlDataProvider.LoadIndexDocumentsByVersionId
" + LoadIndexDocumentsCommon + @"WHERE V.""VersionId"" = ANY(string_to_array(@VersionIds, ',')::int[])
";
        #endregion

        #region LoadIndexDocumentCollectionBlockByPathScript
        // Base class params: @Path, @Offset, @Count
        protected override string LoadIndexDocumentCollectionBlockByPathScript => @"-- PgSqlDataProvider.LoadIndexDocumentCollectionBlockByPath
" + LoadIndexDocumentsCommon + @"WHERE (N.""Path"" = @Path OR N.""Path"" LIKE @Path || '/%')
ORDER BY N.""Path""
OFFSET @Offset ROWS FETCH NEXT @Count ROWS ONLY
";
        #endregion

        #region LoadIndexDocumentCollectionBlockByPathAndTypeScript
        // Base class params: @Path, @Offset, @Count
        // NOTE: MsSql uses NOT IN for type exclusion
        protected override string LoadIndexDocumentCollectionBlockByPathAndTypeScript => @"-- PgSqlDataProvider.LoadIndexDocumentCollectionBlockByPathAndType
" + LoadIndexDocumentsCommon + @"WHERE N.""NodeTypeId"" NOT IN ({0})
    AND (N.""Path"" = @Path OR N.""Path"" LIKE @Path || '/%')
ORDER BY N.""Path""
OFFSET @Offset ROWS FETCH NEXT @Count ROWS ONLY
";
        #endregion

        #region LoadNotIndexedNodeIdsScript
        // Base class params: @FromId, @ToId
        protected override string LoadNotIndexedNodeIdsScript => @"-- PgSqlDataProvider.LoadNotIndexedNodeIds
SELECT ""NodeId"" FROM ""Versions""
WHERE ""NodeId"" >= @FromId AND ""NodeId"" <= @ToId AND ""IndexDocument"" IS NULL
";
        #endregion

        // =============================================================================================== Indexing Activity

        #region GetLastIndexingActivityIdScript
        protected override string GetLastIndexingActivityIdScript => @"-- PgSqlDataProvider.GetLastIndexingActivityId
SELECT COALESCE(MAX(""IndexingActivityId""), 0) FROM ""IndexingActivities""
";
        #endregion

        #region DeleteRestorePointsScript
        protected override string DeleteRestorePointsScript => @"-- PgSqlDataProvider.DeleteRestorePoints
DELETE FROM ""IndexingActivities"" WHERE ""ActivityType"" = 'Restore'
";
        #endregion

        #region GetCurrentIndexingActivityStatusScript
        // Base class reads: column 0 = Int32 (Id), column 1 = String (State)
        // First row = last done activity, remaining rows = gap activities
        protected override string GetCurrentIndexingActivityStatusScript => @"-- PgSqlDataProvider.GetCurrentIndexingActivityStatus
WITH LastDone AS (
    SELECT ""IndexingActivityId""
    FROM ""IndexingActivities""
    WHERE ""RunningState"" = 'Done'
    ORDER BY ""CreationDate"" DESC
    LIMIT 1
)
SELECT ld.""IndexingActivityId"", 'Done'::TEXT AS ""RunningState""
FROM LastDone ld
UNION ALL
SELECT ia.""IndexingActivityId"", ia.""RunningState""
FROM ""IndexingActivities"" ia, LastDone ld
WHERE ia.""RunningState"" != 'Done' AND ia.""IndexingActivityId"" < ld.""IndexingActivityId""
";
        #endregion

        #region RestoreIndexingActivityStatusScript
        // Base class: ExecuteScalar, expects string result: 'AlreadyRestored', 'Restored', or 'NotNecessary'
        // Base class params: @LastActivityId (int), @Gaps (string, comma-separated)
        // This complex logic requires PL/pgSQL. Create the function in schema install script,
        // then call it here.
        protected override string RestoreIndexingActivityStatusScript => @"-- PgSqlDataProvider.RestoreIndexingActivityStatus
SELECT sn_restore_indexing_activity_status(@LastActivityId, @Gaps)
";
        #endregion

        #region LoadIndexingActivitiesSkeletonScript
        // GetIndexingActivitiesFromReaderAsync reads by name:
        //   IndexingActivityId, ActivityType, CreationDate, RunningState, LockTime,
        //   NodeId, VersionId, Path, Extension,
        //   IndexDocument, NodeTypeId, ParentNodeId, IsSystem,
        //   LastMinorVersionId, LastMajorVersionId, Status, NodeTimestamp, VersionTimestamp
        private string LoadIndexingActivitiesSkeleton(string trace, string where) => $@"-- PgSqlDataProvider.{trace}
SELECT I.""IndexingActivityId"", I.""ActivityType"", I.""CreationDate"", I.""RunningState"", I.""LockTime"",
    I.""NodeId"", I.""VersionId"", I.""Path"", I.""Extension"",
    V.""IndexDocument"", N.""NodeTypeId"", N.""ParentNodeId"", N.""IsSystem"",
    N.""LastMinorVersionId"", N.""LastMajorVersionId"", V.""Status"",
    N.""Timestamp"" AS ""NodeTimestamp"", V.""Timestamp"" AS ""VersionTimestamp""
FROM ""IndexingActivities"" I
    LEFT OUTER JOIN ""Versions"" V ON V.""VersionId"" = I.""VersionId""
    LEFT OUTER JOIN ""Nodes"" N ON N.""NodeId"" = V.""NodeId""
{where}
ORDER BY I.""IndexingActivityId""
LIMIT @Top
";
        #endregion

        #region LoadIndexingActivitiesPageScript
        // Base class params: @From, @To, @Top
        protected override string LoadIndexingActivitiesPageScript =>
            LoadIndexingActivitiesSkeleton("LoadIndexingActivitiesPage",
                @"WHERE I.""IndexingActivityId"" >= @From AND I.""IndexingActivityId"" <= @To");
        #endregion

        #region LoadIndexingActivitiyGapsScript
        // Base class params: @Gaps (comma-separated string), @Top
        protected override string LoadIndexingActivitiyGapsScript =>
            LoadIndexingActivitiesSkeleton("LoadIndexingActivityGaps",
                @"WHERE I.""IndexingActivityId"" = ANY(string_to_array(@Gaps, ',')::int[])");
        #endregion

        #region LoadExecutableIndexingActivitiesScript
        // Base class params: @Top, @TimeLimit
        // MsSql does UPDATE+OUTPUT atomically. For PgSql, we use UPDATE...RETURNING with subquery.
        protected override string LoadExecutableIndexingActivitiesScript => @"-- PgSqlDataProvider.LoadExecutableIndexingActivities
WITH executable AS (
    SELECT I.""IndexingActivityId""
    FROM ""IndexingActivities"" I
    WHERE (I.""RunningState"" = 'Waiting' OR (I.""RunningState"" = 'Running' AND I.""LockTime"" < @TimeLimit))
        AND NOT EXISTS (
            SELECT 1 FROM ""IndexingActivities"" OLD
            WHERE OLD.""IndexingActivityId"" < I.""IndexingActivityId""
                AND (OLD.""RunningState"" = 'Waiting' OR OLD.""RunningState"" = 'Running')
                AND (
                    I.""NodeId"" = OLD.""NodeId"" OR
                    (I.""VersionId"" <> 0 AND I.""VersionId"" = OLD.""VersionId"") OR
                    I.""Path"" LIKE OLD.""Path"" || '/%' OR
                    OLD.""Path"" LIKE I.""Path"" || '/%'
                )
        )
    ORDER BY I.""IndexingActivityId""
    LIMIT @Top
),
updated AS (
    UPDATE ""IndexingActivities"" SET ""RunningState"" = 'Running', ""LockTime"" = NOW() AT TIME ZONE 'UTC'
    FROM executable e
    WHERE ""IndexingActivities"".""IndexingActivityId"" = e.""IndexingActivityId""
    RETURNING ""IndexingActivities"".""IndexingActivityId"", ""IndexingActivities"".""ActivityType"",
        ""IndexingActivities"".""CreationDate"", ""IndexingActivities"".""RunningState"",
        ""IndexingActivities"".""LockTime"", ""IndexingActivities"".""NodeId"",
        ""IndexingActivities"".""VersionId"", ""IndexingActivities"".""Path"",
        ""IndexingActivities"".""Extension""
)
SELECT u.""IndexingActivityId"", u.""ActivityType"", u.""CreationDate"", u.""RunningState"", u.""LockTime"",
    u.""NodeId"", u.""VersionId"", u.""Path"", u.""Extension"",
    V.""IndexDocument"", N.""NodeTypeId"", N.""ParentNodeId"", N.""IsSystem"",
    N.""LastMinorVersionId"", N.""LastMajorVersionId"", V.""Status"",
    N.""Timestamp"" AS ""NodeTimestamp"", V.""Timestamp"" AS ""VersionTimestamp""
FROM updated u
    LEFT OUTER JOIN ""Versions"" V ON V.""VersionId"" = u.""VersionId""
    LEFT OUTER JOIN ""Nodes"" N ON N.""NodeId"" = u.""NodeId""
ORDER BY u.""IndexingActivityId""
";
        #endregion

        #region LoadExecutableAndFinishedIndexingActivitiesScript
        // Same as above + second result set with finished activity IDs
        // Base class params: @Top, @TimeLimit, @WaitingIds (comma-separated string)
        protected override string LoadExecutableAndFinishedIndexingActivitiesScript =>
            LoadExecutableIndexingActivitiesScript + @"
;SELECT ""IndexingActivityId"" FROM ""IndexingActivities""
WHERE ""RunningState"" = 'Done' AND ""IndexingActivityId"" = ANY(string_to_array(@WaitingIds, ',')::int[])
";
        #endregion

        #region RegisterIndexingActivityScript
        // Base class params: @ActivityType, @CreationDate, @RunningState, @LockTime,
        //   @NodeId, @VersionId, @Path, @VersionTimestamp, @Extension
        // Return: ExecuteScalar -> IndexingActivityId
        protected override string RegisterIndexingActivityScript => @"-- PgSqlDataProvider.RegisterIndexingActivity
INSERT INTO ""IndexingActivities""
    (""ActivityType"", ""CreationDate"", ""RunningState"", ""LockTime"",
     ""NodeId"", ""VersionId"", ""Path"", ""VersionTimestamp"", ""Extension"")
VALUES (@ActivityType, @CreationDate, @RunningState, @LockTime,
    @NodeId, @VersionId, @Path, @VersionTimestamp, @Extension)
RETURNING ""IndexingActivityId""
";
        #endregion

        #region UpdateIndexingActivityRunningStateScript
        protected override string UpdateIndexingActivityRunningStateScript => @"-- PgSqlDataProvider.UpdateIndexingActivityRunningState
UPDATE ""IndexingActivities"" SET ""RunningState"" = @RunningState, ""LockTime"" = NOW() AT TIME ZONE 'UTC'
WHERE ""IndexingActivityId"" = @IndexingActivityId
";
        #endregion

        #region RefreshIndexingActivityLockTimeScript
        // Base class params: @Ids (comma-separated string), @LockTime
        protected override string RefreshIndexingActivityLockTimeScript => @"-- PgSqlDataProvider.RefreshIndexingActivityLockTime
UPDATE ""IndexingActivities"" SET ""LockTime"" = @LockTime
WHERE ""IndexingActivityId"" = ANY(string_to_array(@Ids, ',')::int[])
";
        #endregion

        #region DeleteFinishedIndexingActivitiesScript
        // Base class params: @Minutes (int)
        protected override string DeleteFinishedIndexingActivitiesScript => @"-- PgSqlDataProvider.DeleteFinishedIndexingActivities
DELETE FROM ""IndexingActivities""
WHERE ""RunningState"" = 'Done' AND (""LockTime"" < NOW() AT TIME ZONE 'UTC' - (@Minutes || ' minutes')::INTERVAL OR ""LockTime"" IS NULL)
";
        #endregion

        #region DeleteAllIndexingActivitiesScript
        protected override string DeleteAllIndexingActivitiesScript => @"-- PgSqlDataProvider.DeleteAllIndexingActivities
DELETE FROM ""IndexingActivities""
";
        #endregion

        // =============================================================================================== Schema

        #region LoadSchemaScript
        // Base class expects 4 result sets:
        //   1. SchemaModification: Timestamp (bytes -> long)
        //   2. PropertyTypes: PropertyTypeId, Name, DataType, Mapping, IsContentListProperty
        //   3. NodeTypes: NodeTypeId, ParentId, Name, ClassName, Properties
        //   4. ContentListTypes: ContentListTypeId, Name, Properties
        protected override string LoadSchemaScript => @"-- PgSqlDataProvider.LoadSchema
SELECT ""Timestamp"" FROM ""SchemaModification"";
SELECT ""PropertyTypeId"", ""Name"", ""DataType"", ""Mapping"", ""IsContentListProperty"" FROM ""PropertyTypes"";
SELECT ""NodeTypeId"", ""ParentId"", ""Name"", ""ClassName"", ""Properties"" FROM ""NodeTypes"";
SELECT ""ContentListTypeId"", ""Name"", ""Properties"" FROM ""ContentListTypes"";
";
        #endregion

        #region StartSchemaUpdateScript
        protected override string StartSchemaUpdateScript => @"-- PgSqlDataProvider.StartSchemaUpdate
WITH ins AS (
  INSERT INTO ""SchemaModification"" (""ModificationDate"", ""LockToken"")
  SELECT NOW() AT TIME ZONE 'UTC', @LockToken
  WHERE NOT EXISTS (SELECT 1 FROM ""SchemaModification"")
  RETURNING 0 AS r
), upd AS (
  UPDATE ""SchemaModification""
  SET ""LockToken"" = @LockToken, ""ModificationDate"" = NOW() AT TIME ZONE 'UTC'
  WHERE ""Timestamp"" = @Timestamp
    AND (""LockToken"" IS NULL OR ""LockToken"" = @LockToken)
    AND NOT EXISTS (SELECT 1 FROM ins)
  RETURNING 0 AS r
)
SELECT CASE
  WHEN EXISTS (SELECT 1 FROM ins) THEN 0
  WHEN EXISTS (SELECT 1 FROM upd) THEN 0
  WHEN EXISTS (SELECT 1 FROM ""SchemaModification""
               WHERE ""LockToken"" IS NULL OR ""LockToken"" = @LockToken) THEN -1
  ELSE -2
END AS ""Result"";
";
        #endregion

        #region FinishSchemaUpdateScript
        protected override string FinishSchemaUpdateScript => @"-- PgSqlDataProvider.FinishSchemaUpdate
UPDATE ""SchemaModification"" SET ""LockToken"" = NULL, ""ModificationDate"" = NOW() AT TIME ZONE 'UTC'
WHERE ""LockToken"" = @LockToken
RETURNING ""Timestamp""
";
        #endregion

        // =============================================================================================== Logging

        #region WriteAuditEventScript
        protected override string WriteAuditEventScript => @"-- PgSqlDataProvider.WriteAuditEvent
INSERT INTO ""LogEntries""
    (""EventId"", ""Category"", ""Priority"", ""Severity"", ""Title"",
     ""ContentId"", ""ContentPath"", ""UserName"", ""LogDate"",
     ""MachineName"", ""AppDomainName"", ""ProcessID"", ""ProcessName"",
     ""ThreadName"", ""Win32ThreadId"", ""Message"", ""FormattedMessage"")
VALUES
    (@EventId, @Category, @Priority, @Severity, @Title,
     @ContentId, @ContentPath, @UserName, @LogDate,
     @MachineName, @AppDomainName, @ProcessID, @ProcessName,
     @ThreadName, @Win32ThreadId, @Message, @FormattedMessage)
RETURNING ""LogId""
";
        #endregion

        #region LoadLastAuditEventsScript
        protected override string LoadLastAuditEventsScript => @"-- PgSqlDataProvider.LoadLastAuditEvents
SELECT ""LogId"", ""EventId"", ""Category"", ""Priority"", ""Severity"", ""Title"",
    ""ContentId"", ""ContentPath"", ""UserName"", ""LogDate"",
    ""MachineName"", ""AppDomainName"", ""ProcessID"", ""ProcessName"",
    ""ThreadName"", ""Win32ThreadId"", ""Message"", ""FormattedMessage""
FROM ""LogEntries""
ORDER BY ""LogId"" DESC LIMIT @Count
";
        #endregion

        // =============================================================================================== Tools

        #region GetNameOfLastNodeWithNameBaseScript
        protected override string GetNameOfLastNodeWithNameBaseScript => @"-- PgSqlDataProvider.GetNameOfLastNodeWithNameBase
SELECT ""Name"" FROM ""Nodes""
WHERE ""ParentNodeId"" = @ParentId AND (
    ""Name"" LIKE @NameEscaped || '(%)' || @Extension
)
ORDER BY LENGTH(""Name"") DESC, ""Name"" DESC
LIMIT 1
";
        #endregion

        #region GetTreeSizeScript
        protected override string GetTreeSizeScript => @"-- PgSqlDataProvider.GetTreeSize
SELECT SUM(F.""Size"") FROM ""Files"" F
    JOIN ""BinaryProperties"" B ON F.""FileId"" = B.""FileId""
    JOIN ""Versions"" V ON B.""VersionId"" = V.""VersionId""
    JOIN ""Nodes"" N ON V.""NodeId"" = N.""NodeId""
WHERE (N.""Path"" = @NodePath OR N.""Path"" LIKE @NodePath || '/%')
    AND F.""Staging"" IS NULL
";
        #endregion

        #region GetNodeCountScript / GetNodeCountInSubtreeScript
        protected override string GetNodeCountScript => @"-- PgSqlDataProvider.GetNodeCount
SELECT COUNT(*) FROM ""Nodes""
";
        protected override string GetNodeCountInSubtreeScript => @"-- PgSqlDataProvider.GetNodeCountInSubtree
SELECT COUNT(*) FROM ""Nodes"" WHERE ""Path"" = @Path OR ""Path"" LIKE @Path || '/%'
";
        #endregion

        #region GetVersionCountScript / GetVersionCountInSubtreeScript
        protected override string GetVersionCountScript => @"-- PgSqlDataProvider.GetVersionCount
SELECT COUNT(*) FROM ""Versions""
";
        protected override string GetVersionCountInSubtreeScript => @"-- PgSqlDataProvider.GetVersionCountInSubtree
SELECT COUNT(*) FROM ""Versions"" V JOIN ""Nodes"" N ON V.""NodeId"" = N.""NodeId""
WHERE N.""Path"" = @Path OR N.""Path"" LIKE @Path || '/%'
";
        #endregion

        // =============================================================================================== Entity Tree / Database Usage

        #region LoadEntityTreeScript
        protected override string LoadEntityTreeScript => @"-- PgSqlDataProvider.LoadEntityTree
SELECT ""NodeId"", ""ParentNodeId"", ""OwnerId"" FROM ""Nodes""
ORDER BY ""Path""
";
        #endregion

        #region LoadDatabaseUsageScript
        protected override string LoadDatabaseUsageScript => @"-- PgSqlDataProvider.LoadDatabaseUsage
SELECT N.""NodeId"", V.""VersionId"", N.""ParentNodeId"", N.""NodeTypeId"",
    V.""MajorNumber"", V.""MinorNumber"", V.""Status"",
    CASE V.""VersionId"" WHEN N.""LastMajorVersionId"" THEN 1 ELSE 0 END AS ""LastPub"",
    CASE V.""VersionId"" WHEN N.""LastMinorVersionId"" THEN 1 ELSE 0 END AS ""LastWork"",
    N.""OwnerId"",
    COALESCE(octet_length(""DynamicProperties""::TEXT), 0) AS ""DynamicPropertiesSize"",
    COALESCE(octet_length(""ContentListProperties""::TEXT), 0) AS ""ContentListPropertiesSize"",
    COALESCE(octet_length(""ChangedData""::TEXT), 0) AS ""ChangedDataSize"",
    COALESCE(octet_length(""IndexDocument""::TEXT), 0) AS ""IndexSize""
FROM ""Nodes"" N
    JOIN ""Versions"" V ON V.""NodeId"" = N.""NodeId"";

SELECT ""VersionId"", COALESCE(octet_length(""Value""), 0) AS ""Size"" FROM ""LongTextProperties"";

SELECT ""VersionId"", ""FileId"" FROM ""BinaryProperties"";

SELECT ""FileId"", ""Size"", 0 AS ""StreamSize"" FROM ""Files"";

SELECT COUNT(1)::INT AS ""Rows"",
    COALESCE(SUM(pg_column_size(L.*))::BIGINT, 0) AS ""Metadata"",
    COALESCE(SUM(COALESCE(octet_length(""FormattedMessage""), 0))::BIGINT, 0) AS ""Text""
FROM ""LogEntries"" L;
";
        #endregion

        // =============================================================================================== AppModel
        // GetAppModelScript is defined in PgSqlDataProvider.cs
    }
}
