// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{
    public partial class PgSqlBlobMetaDataProvider
    {
        #region GetBlobStorageContextScript
        private const string GetBlobStorageContextScript = @"-- PgSqlBlobMetaDataProvider.GetBlobStorageContext
SELECT ""Size"", ""BlobProvider"", ""BlobProviderData""
FROM ""Files"" WHERE ""FileId"" = @FileId
";
        #endregion
        #region ClearStreamByFileIdScript
        private const string ClearStreamByFileIdScript = @"-- PgSqlBlobMetaDataProvider.ClearStreamByFileId
UPDATE ""Files"" SET ""Stream"" = NULL WHERE ""FileId"" = @FileId;
";
        #endregion

        #region DeleteBinaryPropertyScript
        internal const string DeleteBinaryPropertyScript = @"-- PgSqlBlobMetaDataProvider.DeleteBinaryProperty
DELETE FROM ""BinaryProperties"" WHERE ""VersionId"" = @VersionId AND ""PropertyTypeId"" = @PropertyTypeId;
";
        #endregion
        #region InsertBinaryPropertyScript
        private const string InsertBinaryPropertyScript = @"-- PgSqlBlobMetaDataProvider.InsertBinaryProperty
WITH inserted_file AS (
    INSERT INTO ""Files"" (""ContentType"", ""FileNameWithoutExtension"", ""Extension"", ""Size"", ""BlobProvider"", ""BlobProviderData"", ""Checksum"")
    VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, @BlobProvider, @BlobProviderData,
        CASE WHEN @Size = 0 THEN NULL ELSE @Checksum END)
    RETURNING ""FileId"", ""Timestamp""
), inserted_bp AS (
    INSERT INTO ""BinaryProperties"" (""VersionId"", ""PropertyTypeId"", ""FileId"")
    SELECT @VersionId, @PropertyTypeId, f.""FileId"" FROM inserted_file f
    RETURNING ""BinaryPropertyId"", ""FileId""
)
SELECT bp.""BinaryPropertyId"", bp.""FileId"", f.""Timestamp""
FROM inserted_bp bp
JOIN inserted_file f ON bp.""FileId"" = f.""FileId"";
";
        #endregion
        #region DeleteAndInsertBinaryPropertyScript = DeleteBinaryPropertyScript + InsertBinaryPropertyScript
        private const string DeleteAndInsertBinaryPropertyScript = DeleteBinaryPropertyScript + InsertBinaryPropertyScript;
        #endregion

        #region InsertBinaryPropertyWithKnownFileIdScript
        private const string InsertBinaryPropertyWithKnownFileIdScript = @"-- PgSqlBlobMetaDataProvider.InsertBinaryPropertyWithKnownFileId
INSERT INTO ""BinaryProperties""
    (""VersionId"", ""PropertyTypeId"", ""FileId"") VALUES (@VersionId, @PropertyTypeId, @FileId)
RETURNING ""BinaryPropertyId""
";
        #endregion
        #region DeleteAndInsertBinaryPropertyWithKnownFileIdScript
        private const string DeleteAndInsertBinaryPropertyWithKnownFileIdScript = DeleteBinaryPropertyScript + InsertBinaryPropertyWithKnownFileIdScript;
        #endregion

        #region UpdateBinaryPropertyNewFilerowScript
        private const string UpdateBinaryPropertyNewFilerowScript = @"-- PgSqlBlobMetaDataProvider.UpdateBinaryPropertyNewFilerow
WITH inserted_file AS (
    INSERT INTO ""Files"" (""ContentType"", ""FileNameWithoutExtension"", ""Extension"", ""Size"", ""BlobProvider"", ""BlobProviderData"", ""Checksum"", ""Stream"")
    VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, @BlobProvider, @BlobProviderData,
        CASE WHEN @Size <= 0 THEN NULL ELSE @Checksum END,
        CASE WHEN @Size <= 0 THEN NULL ELSE ''::bytea END)
    RETURNING ""FileId""
)
UPDATE ""BinaryProperties"" SET ""FileId"" = f.""FileId""
FROM inserted_file f
WHERE ""BinaryPropertyId"" = @BinaryPropertyId
RETURNING f.""FileId"";
";
        #endregion
        #region UpdateBinaryPropertyScript
        // Check whether an existing built-in binary needs a new Files row.
        // Returns TRUE when the file row must be replaced (shared FileId or blob-provider mismatch).
        private const string UpdateBinaryPropertyNeedNewFileRowScript = @"-- PgSqlBlobMetaDataProvider.UpdateBinaryPropertyNeedNewFileRow
SELECT
    CASE
        WHEN (@BlobProvider IS NULL) AND EXISTS (
            SELECT 1 FROM ""Files"" f
            JOIN ""BinaryProperties"" bp ON bp.""FileId"" = f.""FileId""
            WHERE bp.""BinaryPropertyId"" = @BinaryPropertyId AND f.""BlobProvider"" IS NOT NULL
        ) THEN TRUE
        WHEN EXISTS (
            SELECT 1 FROM ""BinaryProperties"" bp1
            JOIN ""BinaryProperties"" bp2 ON bp1.""FileId"" = bp2.""FileId""
            WHERE bp1.""BinaryPropertyId"" = @BinaryPropertyId AND bp2.""BinaryPropertyId"" != @BinaryPropertyId
        ) THEN TRUE
        ELSE FALSE
    END AS ""NeedNewRow"";
";

        // Update an existing Files row in-place (no new row needed).
        private const string UpdateBinaryPropertyInPlaceScript = @"-- PgSqlBlobMetaDataProvider.UpdateBinaryPropertyInPlace
UPDATE ""Files"" f
SET ""ContentType"" = @ContentType,
    ""FileNameWithoutExtension"" = @FileNameWithoutExtension,
    ""Extension"" = @Extension,
    ""Size"" = @Size,
    ""BlobProvider"" = @BlobProvider,
    ""BlobProviderData"" = @BlobProviderData,
    ""Checksum"" = CASE WHEN @Size <= 0 THEN NULL ELSE @Checksum END,
    ""Stream"" = CASE WHEN @Size <= 0 THEN NULL ELSE ''::bytea END
FROM ""BinaryProperties"" bp
WHERE bp.""BinaryPropertyId"" = @BinaryPropertyId AND f.""FileId"" = bp.""FileId""
RETURNING f.""FileId"";
";
        #endregion

        #region DeleteBinaryPropertiesScript
        private const string DeleteBinaryPropertiesScript = @"-- PgSqlBlobMetaDataProvider.DeleteBinaryProperties
DELETE FROM ""BinaryProperties"" WHERE ""VersionId"" = ANY(string_to_array(@VersionIds, ',')::int[])
";
        #endregion

        #region LoadBinaryPropertyScript
        private const string LoadBinaryPropertyScript = @"-- PgSqlBlobMetaDataProvider.LoadBinaryProperty
SELECT B.""BinaryPropertyId"", B.""VersionId"", B.""PropertyTypeId"", F.""FileId"", F.""ContentType"", F.""FileNameWithoutExtension"",
    F.""Extension"", F.""Size"", F.""Checksum"", NULL AS ""Stream"", 0 AS ""Loaded"", F.""Timestamp"", F.""BlobProvider"", F.""BlobProviderData"" 
FROM ""BinaryProperties"" B
    JOIN ""Files"" F ON B.""FileId"" = F.""FileId""
WHERE ""VersionId"" = @VersionId AND ""PropertyTypeId"" = @PropertyTypeId AND ""Staging"" IS NULL
";
        #endregion

        #region LoadBinaryCacheEntityScript
        private const string LoadBinaryCacheEntityScript = @"-- PgSqlBlobMetaDataProvider.LoadBinaryCacheEntity
SELECT F.""Size"", B.""BinaryPropertyId"", F.""FileId"", F.""BlobProvider"", F.""BlobProviderData"",
    CASE WHEN F.""Size"" < @MaxSize THEN F.""Stream"" ELSE null END AS ""Stream""
FROM ""BinaryProperties"" B
    JOIN ""Files"" F ON B.""FileId"" = F.""FileId""
WHERE B.""VersionId"" = @VersionId AND B.""PropertyTypeId"" = @PropertyTypeId AND F.""Staging"" IS NULL
";
        #endregion

        #region InsertStagingBinaryScript
        // Two-step approach: first ensure the BinaryProperties row exists, then insert staging file.
        // Cannot use DO $$ blocks because PostgreSQL anonymous blocks do not support parameter binding.
        private const string InsertStagingBinaryEnsureBinaryPropertyScript = @"-- PgSqlBlobMetaDataProvider.InsertStagingBinaryEnsureBinaryProperty
INSERT INTO ""BinaryProperties"" (""VersionId"", ""PropertyTypeId"", ""FileId"")
SELECT @VersionId, @PropertyTypeId, 0
WHERE NOT EXISTS (
    SELECT 1 FROM ""BinaryProperties""
    WHERE ""VersionId"" = @VersionId AND ""PropertyTypeId"" = @PropertyTypeId
);
";

        private const string InsertStagingBinaryScript = @"-- PgSqlBlobMetaDataProvider.InsertStagingBinary
WITH existing_meta AS (
    SELECT F.""ContentType"", F.""FileNameWithoutExtension"", F.""Extension""
    FROM ""BinaryProperties"" B
    JOIN ""Files"" F ON B.""FileId"" = F.""FileId""
    WHERE B.""VersionId"" = @VersionId AND B.""PropertyTypeId"" = @PropertyTypeId
    LIMIT 1
),
defaults AS (
    SELECT
        COALESCE((SELECT ""ContentType"" FROM existing_meta), '') AS ""ContentType"",
        COALESCE((SELECT ""FileNameWithoutExtension"" FROM existing_meta), '') AS ""FileNameWithoutExtension"",
        COALESCE((SELECT ""Extension"" FROM existing_meta), '') AS ""Extension""
),
inserted_file AS (
    INSERT INTO ""Files"" (""ContentType"", ""FileNameWithoutExtension"", ""Extension"", ""Size"", ""Checksum"",
        ""CreationDate"", ""Staging"", ""StagingVersionId"", ""StagingPropertyTypeId"", ""BlobProvider"", ""BlobProviderData"")
    SELECT d.""ContentType"", d.""FileNameWithoutExtension"", d.""Extension"", @Size, NULL,
        NOW() AT TIME ZONE 'UTC', TRUE, @VersionId, @PropertyTypeId, @BlobProvider, @BlobProviderData
    FROM defaults d
    RETURNING ""FileId""
)
SELECT bp.""BinaryPropertyId"", f.""FileId""
FROM inserted_file f
CROSS JOIN ""BinaryProperties"" bp
WHERE bp.""VersionId"" = @VersionId AND bp.""PropertyTypeId"" = @PropertyTypeId;
";
        #endregion

        #region UpdateStreamWriteChunkSecurityCheckScript
        // Plain SQL security check — uses 1/0 division to raise an error when the staging row is not found.
        // Cannot use DO $$ blocks because PostgreSQL anonymous blocks do not support parameter binding.
        internal static readonly string UpdateStreamWriteChunkSecurityCheckScript = @"-- PgSqlBlobMetaDataProvider.UpdateStreamWriteChunkSecurityCheck
SELECT 1 / (SELECT COUNT(*)::int FROM ""Files""
    WHERE ""FileId"" = @FileId AND ""StagingVersionId"" = @VersionId AND ""StagingPropertyTypeId"" = @PropertyTypeId);
";
        #endregion
        #region CommitChunkScript
        private static readonly string CommitChunkScript = UpdateStreamWriteChunkSecurityCheckScript +
@"-- PgSqlBlobMetaDataProvider.CommitChunk
UPDATE ""Files"" SET ""Size"" = @Size, ""Checksum"" = @Checksum, ""ContentType"" = @ContentType,
    ""FileNameWithoutExtension"" = @FileNameWithoutExtension, ""Extension"" = @Extension,
    ""Staging"" = NULL, ""StagingVersionId"" = NULL, ""StagingPropertyTypeId"" = NULL
WHERE ""FileId"" = @FileId;
UPDATE ""BinaryProperties"" SET ""FileId"" = @FileId
WHERE ""VersionId"" = @VersionId AND ""PropertyTypeId"" = @PropertyTypeId;";
        #endregion

        #region CleanupFileSetIsdeletedScript
        private const string CleanupFileSetIsDeletedScript = @"-- PgSqlBlobMetaDataProvider.CleanupFileSetIsDeleted
UPDATE ""Files"" SET ""IsDeleted"" = TRUE
WHERE ""Staging"" IS NULL AND ""CreationDate"" < (NOW() AT TIME ZONE 'UTC') - INTERVAL '30 minutes'
    AND ""FileId"" NOT IN (SELECT ""FileId"" FROM ""BinaryProperties"")
";
        #endregion
        #region CleanupFileSetIsdeletedImmediatelyScript
        private const string CleanupFileSetIsDeletedImmediatelyScript = @"-- PgSqlBlobMetaDataProvider.CleanupFileSetIsDeletedImmediately
UPDATE ""Files"" SET ""IsDeleted"" = TRUE
WHERE ""Staging"" IS NULL AND ""FileId"" NOT IN (SELECT ""FileId"" FROM ""BinaryProperties"")
";
        #endregion

        #region CleanupFileScript
        private const string CleanupFileScript = @"-- PgSqlBlobMetaDataProvider.CleanupFile
DELETE FROM ""Files""
WHERE ctid = (SELECT ctid FROM ""Files"" WHERE ""IsDeleted"" = TRUE LIMIT 1)
RETURNING ""FileId"", ""Size"", ""BlobProvider"", ""BlobProviderData""
";
        #endregion

        public string GetFirstFileId = @"-- PgSqlBlobMetaDataProvider.GetFirstFileId
SELECT ""FileId"" FROM ""Files"" LIMIT 1
";
    }
}
