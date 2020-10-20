// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    public partial class MsSqlBlobMetaDataProvider
    {
        #region GetBlobStorageContextScript
        private const string GetBlobStorageContextScript = @"-- MsSqlBlobMetaDataProvider.GetBlobStorageContext
SELECT Size, BlobProvider, BlobProviderData
FROM  dbo.Files WHERE FileId = @FileId
";
        #endregion
        #region ClearStreamByFileIdScript
        private const string ClearStreamByFileIdScript = @"-- MsSqlBlobMetaDataProvider.ClearStreamByFileId
UPDATE Files SET Stream = NULL WHERE FileId = @FileId;
";
        #endregion

        #region DeleteBinaryPropertyScript
        internal const string DeleteBinaryPropertyScript = @"-- MsSqlBlobMetaDataProvider.DeleteBinaryProperty
DELETE BinaryProperties WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId
";
        #endregion
        #region InsertBinaryPropertyScript
        private const string InsertBinaryPropertyScript = @"-- MsSqlBlobMetaDataProvider.InsertBinaryProperty
INSERT INTO Files (ContentType, FileNameWithoutExtension, Extension, [Size], [BlobProvider], [BlobProviderData], [Checksum])
VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, @BlobProvider, @BlobProviderData,
	CASE @Size WHEN 0 THEN NULL ELSE @Checksum END);
DECLARE @FileId int; SELECT @FileId = @@IDENTITY;

INSERT INTO BinaryProperties (VersionId, PropertyTypeId, FileId) VALUES (@VersionId, @PropertyTypeId, @FileId);
DECLARE @BinPropId int; SELECT @BinPropId = @@IDENTITY;

SELECT @BinPropId, @FileId, [Timestamp] FROM Files WHERE FileId = @FileId;
";
        #endregion
        #region DeleteAndInsertBinaryPropertyScript = DeleteBinaryPropertyScript + InsertBinaryPropertyScript
        private const string DeleteAndInsertBinaryPropertyScript = DeleteBinaryPropertyScript + InsertBinaryPropertyScript;
        #endregion

        #region InsertBinaryPropertyWithKnownFileIdScript
        private const string InsertBinaryPropertyWithKnownFileIdScript = @"-- MsSqlBlobMetaDataProvider.InsertBinaryPropertyWithKnownFileId
INSERT INTO BinaryProperties
    (VersionId, PropertyTypeId, FileId) VALUES (@VersionId, @PropertyTypeId, @FileId)
SELECT CAST(@@IDENTITY AS int)
";
        #endregion
        #region DeleteAndInsertBinaryPropertyWithKnownFileIdScript
        private const string DeleteAndInsertBinaryPropertyWithKnownFileIdScript = DeleteBinaryPropertyScript + InsertBinaryPropertyWithKnownFileIdScript;
        #endregion

        #region UpdateBinarypropertyNewFilerowScript
        private const string UpdateBinaryPropertyNewFilerowScript = @"-- MsSqlBlobMetaDataProvider.UpdateBinarypropertyNewFilerow
DECLARE @FileId int
INSERT INTO Files (ContentType, FileNameWithoutExtension, Extension, [Size], [BlobProvider], [BlobProviderData], [Checksum], [Stream])
    VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, @BlobProvider, @BlobProviderData,
		CASE WHEN (@Size <= 0) THEN NULL ELSE @Checksum END,
		CASE WHEN (@Size <= 0) THEN NULL ELSE CONVERT(varbinary, '') END)
SELECT @FileId = @@IDENTITY
UPDATE BinaryProperties SET FileId = @FileId WHERE BinaryPropertyId = @BinaryPropertyId
SELECT @FileId
";
        #endregion
        #region UpdateBinaryPropertyScript
        private const string UpdateBinaryPropertyScript = @"-- MsSqlBlobMetaDataProvider.UpdateBinaryProperty
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
			CASE WHEN (@Size <= 0) THEN NULL ELSE CONVERT(varbinary, '') END)

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
		-- [Stream]   = IIF (@Size <= 0, NULL, CONVERT(varbinary, '')
		[Checksum] = CASE WHEN (@Size <= 0) THEN NULL ELSE @Checksum END,
		[Stream]   = CASE WHEN (@Size <= 0) THEN NULL ELSE CONVERT(varbinary, '') END
	WHERE FileId = @FileId
END
SELECT @FileId
";
        #endregion

        #region DeleteBinaryPropertiesScript
        private const string DeleteBinaryPropertiesScript = @"-- MsSqlBlobMetaDataProvider.DeleteBinaryProperties
DECLARE @VersionIdTable AS TABLE(Id INT)
INSERT INTO @VersionIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@VersionIds, ',');
DELETE FROM BinaryProperties WHERE VersionId IN (SELECT Id FROM @VersionIdTable)
";
        #endregion

        #region LoadBinaryPropertyScript
        private const string LoadBinaryPropertyScript = @"-- MsSqlBlobMetaDataProvider.LoadBinaryProperty
SELECT B.BinaryPropertyId, B.VersionId, B.PropertyTypeId, F.FileId, F.ContentType, F.FileNameWithoutExtension,
    F.Extension, F.[Size], F.[Checksum], NULL AS Stream, 0 AS Loaded, F.[Timestamp], F.[BlobProvider], F.[BlobProviderData] 
FROM dbo.BinaryProperties B
    JOIN dbo.Files F ON B.FileId = F.FileId
WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId AND Staging IS NULL
";
        #endregion

        #region LoadBinaryCacheEntityScript
        private const string LoadBinaryCacheEntityScript = @"-- MsSqlBlobMetaDataProvider.LoadBinaryCacheEntity
SELECT F.Size, B.BinaryPropertyId, F.FileId, F.BlobProvider, F.BlobProviderData, CASE  WHEN F.Size < @MaxSize THEN F.Stream ELSE null END AS Stream
FROM dbo.BinaryProperties B
    JOIN Files F ON B.FileId = F.FileId
WHERE B.VersionId = @VersionId AND B.PropertyTypeId = @PropertyTypeId AND F.Staging IS NULL
";
        #endregion

        #region InsertStagingBinaryScript
        private const string InsertStagingBinaryScript = @"-- MsSqlBlobMetaDataProvider.InsertStagingBinary
DECLARE @ContentType varchar(50);
DECLARE @FileNameWithoutExtension varchar(450);
DECLARE @Extension varchar(50);
DECLARE @FileId int;

BEGIN TRAN
                
-- select existing stream metadata values
SELECT TOP(1) @ContentType = F.ContentType, @FileNameWithoutExtension = F.FileNameWithoutExtension, @Extension = F.Extension
FROM BinaryProperties B JOIN Files F ON B.FileId = F.FileId
WHERE B.VersionId = @VersionId AND B.PropertyTypeId = @PropertyTypeId;

-- no existing binary/file relation
IF (@ContentType IS NULL)
BEGIN
    SET @ContentType = '';
    SET @FileNameWithoutExtension = '';
    SET @Extension = '';
END

INSERT INTO Files ([ContentType],[FileNameWithoutExtension],[Extension],[Size],[Checksum],[CreationDate], [Staging], [StagingVersionId], [StagingPropertyTypeId], [BlobProvider], [BlobProviderData])
VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, NULL, GETUTCDATE(), 1, @VersionId, @PropertyTypeId, @BlobProvider, @BlobProviderData);            

SET @FileId = @@IDENTITY;

-- lazy binary row creation
IF NOT EXISTS (SELECT 1 FROM BinaryProperties WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId)
BEGIN
    INSERT INTO BinaryProperties ([VersionId],[PropertyTypeId], [FileId])
    VALUES (@VersionId, @PropertyTypeId, @FileId);
END

SELECT BinaryPropertyId, @FileId
FROM BinaryProperties
WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId

COMMIT TRAN
";
        #endregion

        #region UpdateStreamWriteChunkSecurityCheckScript
        internal static readonly string UpdateStreamWriteChunkSecurityCheckScript = @"-- MsSqlBlobMetaDataProvider.UpdateStreamWriteChunkSecurityCheck
-- security check: if the versionid in the token matches the version that this staging row belongs to
IF NOT EXISTS (SELECT 1 FROM Files WHERE FileId = @FileId AND StagingVersionId = @VersionId AND StagingPropertyTypeId = @PropertyTypeId)
BEGIN
    RAISERROR (N'FileId and versionid and propertytypeid mismatch.', 12, 1);
END
";
        #endregion
        #region CommitChunkScript
        private static readonly string CommitChunkScript = UpdateStreamWriteChunkSecurityCheckScript +
@"-- MsSqlBlobMetaDataProvider.CommitChunk
UPDATE Files SET [Size] = @Size, [Checksum] = @Checksum, ContentType = @ContentType, FileNameWithoutExtension = @FileNameWithoutExtension, Extension = @Extension, Staging = NULL, StagingVersionId = NULL, StagingPropertyTypeId = NULL
    WHERE FileId = @FileId
UPDATE BinaryProperties SET FileId = @FileId
    WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId;";
        #endregion

        #region CleanupFileSetIsdeletedScript
        // this is supposed to be faster than using LEFT JOIN
        private const string CleanupFileSetIsDeletedScript = @"-- MsSqlBlobMetaDataProvider.CleanupFileSetIsdeleted
UPDATE [Files] SET IsDeleted = 1
WHERE [Staging] IS NULL AND CreationDate < DATEADD(minute, -30, GETUTCDATE()) AND FileId NOT IN (SELECT FileId FROM [BinaryProperties])
";
        #endregion
        #region CleanupFileSetIsdeletedImmediatelyScript
        // this is supposed to be faster than using LEFT JOIN
        private const string CleanupFileSetIsDeletedImmediatelyScript = @"-- MsSqlBlobMetaDataProvider.CleanupFileSetIsdeleted
UPDATE [Files] SET IsDeleted = 1
WHERE [Staging] IS NULL AND FileId NOT IN (SELECT FileId FROM [BinaryProperties])
";
        #endregion

        #region CleanupFileScript
        private const string CleanupFileScript = @"-- MsSqlBlobMetaDataProvider.CleanupFile
DELETE TOP(1) FROM Files
OUTPUT DELETED.FileId, DELETED.Size, DELETED.BlobProvider, DELETED.BlobProviderData 
WHERE IsDeleted = 1
";
        #endregion
    }
}