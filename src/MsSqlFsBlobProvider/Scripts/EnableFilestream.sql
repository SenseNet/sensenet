/**************************************************************/
/**************** Enable FILESTREAM on database ***************/
/**************************************************************/

-- set access level
EXEC sp_configure filestream_access_level, 2
RECONFIGURE

DECLARE @errorHappened tinyint;
DECLARE @Db_Data_Path	nvarchar(1000);
DECLARE @Db_Name	nvarchar(200);

SET @errorHappened = 0;

-- get current database name
SELECT @Db_Name = DB_NAME();

-- find physical path of the database and create a folder there
SELECT @Db_Data_Path =   
(  
    SELECT  LEFT(physical_name, LEN(physical_name) - CHARINDEX('\',REVERSE(physical_name)) + 1) + @Db_Name + 'Files'
    FROM sys.master_files mf  
    INNER JOIN sys.[databases] d  
        ON mf.[database_id] = d.[database_id]  
    WHERE d.[name] = @Db_Name AND type = 0  
)

BEGIN TRY
IF NOT EXISTS (SELECT * FROM sys.filegroups WHERE name = 'SNFileGroup')
	BEGIN
		DECLARE @sql			varchar(1000);
		SET @sql = N'
		ALTER DATABASE [' + @Db_Name + ']
		ADD FILEGROUP SNFileGroup CONTAINS FILESTREAM

		ALTER DATABASE [' + @Db_Name + '] 
		ADD FILE ( NAME = ''SenseNetContentRepository_files'', FILENAME = '''+ @Db_Data_Path + N''') TO FILEGROUP SNFileGroup

		PRINT(''FILEGROUP and FILE added to database.'');';

		EXEC(@sql);

	END
ELSE
	BEGIN
		PRINT('FILEGROUP already exists, filegroup and file creation skipped.');
	END
END TRY
BEGIN CATCH
	SET @errorHappened = 1;
	RAISERROR('FILEGROUP could not be created.', 1, 1);
	GOTO SKIPPED;
END CATCH

/**************************************************************/
/*** Add UNIQUE constraint to the binary table if necessary ***/
/**************************************************************/

DECLARE @GuidConstraint int;

SELECT @GuidConstraint = COUNT(CONSTRAINT_NAME)
FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE 
WHERE TABLE_NAME = 'Files' AND COLUMN_NAME = 'RowGuid'

IF  (@GuidConstraint = 0)
BEGIN
	ALTER TABLE Files
	ADD UNIQUE (RowGuid)

	PRINT('UNIQUE constraint added to RowGuid column.');
END
ELSE
BEGIN
	PRINT('RowGuid column already has a UNIQUE constraint.');
END


/**************************************************************/
/********* Add FILESTREAM column to the Files table **********/
/**************************************************************/

IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'FileStream' and Object_ID = Object_ID(N'Files'))    
	BEGIN
		PRINT('FILESTREAM column already exists.');
	END
ELSE
	BEGIN
		--This needs to be a dynamic SQL, otherwise compilation would 
		--fail if the Filestream were already set and added to the table...
		execute ('ALTER TABLE dbo.Files SET (FILESTREAM_ON = SNFileGroup)
		ALTER TABLE dbo.Files ADD [FileStream] VARBINARY(MAX) FILESTREAM NULL');

		PRINT('FILESTREAM column added.');		
	END

/**************************************************************/
/******************** Drop stored procedures ******************/
/**************************************************************/

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_Update]') AND type in (N'P', N'PC')) ---OK
DROP PROCEDURE [dbo].[proc_BinaryProperty_Update]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_GetPointer]') AND type in (N'P', N'PC')) ---OK
DROP PROCEDURE [dbo].[proc_BinaryProperty_GetPointer]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_WriteStream]') AND type in (N'P', N'PC')) --OK
DROP PROCEDURE [dbo].[proc_BinaryProperty_WriteStream]

PRINT('Old stored procedures dropped.');

/**************************************************************/
/****************** Re-create stored procedures ***************/
/**************************************************************/

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
	INSERT INTO Files (ContentType, FileNameWithoutExtension, Extension, [Size], [BlobProvider], [BlobProviderData], [Checksum], [Stream], [FileStream])
	    VALUES (@ContentType, @FileNameWithoutExtension, @Extension, @Size, @BlobProvider, @BlobProviderData,
			CASE WHEN (@Size <= 0) THEN NULL ELSE @Checksum END,
			NULL,
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
		[Checksum]   = CASE WHEN (@Size <= 0) THEN NULL ELSE @Checksum END,
		[Stream]     = NULL,
		[FileStream] = CASE WHEN (@Size <= 0) THEN NULL ELSE CONVERT(varbinary, '''') END
	WHERE FileId = @FileId

END
		
SELECT @FileId, FileStream.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT() 
FROM Files WHERE FileId = @FileId' 
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_GetPointer]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_GetPointer]
(
	@VersionId int,
	@PropertyTypeId int,
	@FileId int OUTPUT,
	@Length int OUTPUT,
	@TransactionContext varbinary(max) OUTPUT,
	@FilePath nvarchar(4000) OUTPUT
)
AS
	SELECT @FileId = F.FileId,
		@Length = CASE WHEN F.FileStream IS NULL THEN DATALENGTH(F.Stream) ELSE DATALENGTH(F.FileStream) END,
		@TransactionContext = GET_FILESTREAM_TRANSACTION_CONTEXT(),
		@FilePath = F.FileStream.PathName()				  
	FROM BinaryProperties B
		JOIN Files F ON F.FileId = B.FileId
	WHERE B.VersionId = @VersionId AND B.PropertyTypeId = @PropertyTypeId AND F.Staging IS NULL
' 
END


IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_WriteStream]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_WriteStream]
(
	@Id int,
	@Offset int,
	@Value varbinary(max),
	@UseFileStream tinyint = 0
)
AS
	IF(@UseFileStream = 1)
		UPDATE Files SET FileStream = @Value, Stream = NULL WHERE FileId = @Id;
	ELSE
		UPDATE Files SET Stream = @Value, FileStream = NULL WHERE FileId = @Id;
'
END


PRINT('New stored procedures created.');

SKIPPED:
IF (@errorHappened > 0)
BEGIN
	PRINT('Script stopped with error.');
END

GO