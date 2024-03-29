------------------------------------------------                        --------------------------------------------------------------
------------------------------------------------  DROP EXISTING TABLES  --------------------------------------------------------------
------------------------------------------------                        --------------------------------------------------------------

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_Nodes_ContentListId]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_Nodes_ContentListId]
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_Nodes_CreatedById]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_Nodes_CreatedById]
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_Nodes_ModifiedById]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_Nodes_ModifiedById]
GO

/****** Object:  ForeignKey [FK_BinaryProperties_PropertyTypes]    Script Date: 10/25/2007 15:49:18 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BinaryProperties_PropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
ALTER TABLE [dbo].[BinaryProperties] DROP CONSTRAINT [FK_BinaryProperties_PropertyTypes]
GO
/****** Object:  ForeignKey [FK_BinaryProperties_Versions]    Script Date: 10/25/2007 15:49:19 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BinaryProperties_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
ALTER TABLE [dbo].[BinaryProperties] DROP CONSTRAINT [FK_BinaryProperties_Versions]
GO
/****** Object:  ForeignKey [FK_BinaryProperties_Files] ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BinaryProperties_Files]') AND parent_object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
ALTER TABLE [dbo].[BinaryProperties] DROP CONSTRAINT [FK_BinaryProperties_Files]
GO

/****** Object:  ForeignKey [FK_Nodes_LockedBy]    Script Date: 10/25/2007 15:50:16 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_LockedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_LockedBy]
GO
/****** Object:  ForeignKey [FK_Nodes_Parent]    Script Date: 10/25/2007 15:50:16 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_Parent]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_Parent]
GO
/****** Object:  ForeignKey [FK_Nodes_NodeTypes]    Script Date: 10/25/2007 15:50:17 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_NodeTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_NodeTypes]
GO
/****** Object:  ForeignKey [FK_ReferenceProperties_PropertyTypes]    Script Date: 10/25/2007 15:50:19 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ReferenceProperties_PropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]'))
ALTER TABLE [dbo].[ReferenceProperties] DROP CONSTRAINT [FK_ReferenceProperties_PropertyTypes]
GO
/****** Object:  ForeignKey [FK_NodeTypes_NodeTypes]    Script Date: 10/25/2007 15:50:23 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_NodeTypes_NodeTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[NodeTypes]'))
ALTER TABLE [dbo].[NodeTypes] DROP CONSTRAINT [FK_NodeTypes_NodeTypes]
GO
/****** Object:  ForeignKey [FK_LongTextProperties_PropertyTypes]    Script Date: 10/25/2007 15:50:32 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_LongTextProperties_PropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[LongTextProperties]'))
ALTER TABLE [dbo].[LongTextProperties] DROP CONSTRAINT [FK_LongTextProperties_PropertyTypes]
GO
/****** Object:  ForeignKey [FK_LongTextProperties_Versions]    Script Date: 10/25/2007 15:50:32 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_LongTextProperties_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[LongTextProperties]'))
ALTER TABLE [dbo].[LongTextProperties] DROP CONSTRAINT [FK_LongTextProperties_Versions]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes]    Script Date: 10/25/2007 15:50:36 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions] DROP CONSTRAINT [FK_Versions_Nodes]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes_CreatedBy]    Script Date: 10/25/2007 15:50:37 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes_CreatedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions] DROP CONSTRAINT [FK_Versions_Nodes_CreatedBy]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes_ModifiedBy]    Script Date: 10/25/2007 15:50:37 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes_ModifiedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions] DROP CONSTRAINT [FK_Versions_Nodes_ModifiedBy]
GO

/****** Object:  View [dbo].[NodeInfoView]  ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[NodeInfoView]'))
DROP VIEW [dbo].[NodeInfoView]
GO
/****** Object:  View [dbo].[PermissionInfoView]  ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[PermissionInfoView]'))
DROP VIEW [dbo].[PermissionInfoView]
GO
/****** Object:  View [dbo].[ReferencesInfoView]    Script Date: 08/07/2007 14:50:18 ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[ReferencesInfoView]'))
DROP VIEW [dbo].[ReferencesInfoView]
GO
/****** Object:  View [dbo].[MembershipInfoView]    ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[MembershipInfoView]'))
DROP VIEW [dbo].[MembershipInfoView]
GO

/****** Object:  Table [dbo].[PropertyTypes]    Script Date: 10/25/2007 15:50:28 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PropertyTypes]') AND type in (N'U'))
DROP TABLE [dbo].[PropertyTypes]
GO
/****** Object:  Table [dbo].[NodeTypes]    Script Date: 10/25/2007 15:50:23 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NodeTypes]') AND type in (N'U'))
DROP TABLE [dbo].[NodeTypes]
GO
/****** Object:  Table [dbo].[ContentListTypes]  ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ContentListTypes]') AND type in (N'U'))
DROP TABLE [dbo].[ContentListTypes]
GO
/****** Object:  Table [dbo].[ReferenceProperties]    Script Date: 10/25/2007 15:50:19 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]') AND type in (N'U'))
DROP TABLE [dbo].[ReferenceProperties]
GO
/****** Object:  Table [dbo].[BinaryProperties]    Script Date: 10/25/2007 15:49:18 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BinaryProperties]') AND type in (N'U'))
DROP TABLE [dbo].[BinaryProperties]
GO
/****** Object:  Table [dbo].[Files] ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Files]') AND type in (N'U'))
DROP TABLE [dbo].[Files]
GO
/****** Object:  Table [dbo].[LongTextProperties]    Script Date: 10/25/2007 15:50:32 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LongTextProperties]') AND type in (N'U'))
DROP TABLE [dbo].[LongTextProperties]
GO
/****** Object:  Table [dbo].[Nodes]    Script Date: 10/25/2007 15:50:16 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Nodes]') AND type in (N'U'))
DROP TABLE [dbo].[Nodes]
GO
/****** Object:  Table [dbo].[Versions]    Script Date: 10/25/2007 15:50:36 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Versions]') AND type in (N'U'))
DROP TABLE [dbo].[Versions]
GO
/****** Object:  Table [dbo].[JournalItems]    Script Date: 07/08/2009 07:22:07 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[JournalItems]') AND type in (N'U'))
DROP TABLE [dbo].[JournalItems]
GO

/****** Object:  Table [dbo].[LogEntries]    Script Date: 10/09/2009 10:01:54 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogEntries]') AND type in (N'U'))
DROP TABLE [dbo].[LogEntries]
GO

/****** Object:  Table [dbo].[IndexingActivities]    Script Date: 09/01/2010 05:28:15 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IndexingActivities]') AND type in (N'U'))
DROP TABLE [dbo].[IndexingActivities]
GO

/****** Object:  Table [dbo].[WorkflowNotification]   ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WorkflowNotification]') AND type in (N'U'))
DROP TABLE [dbo].[WorkflowNotification]
GO

/****** Object:  Table [dbo].SchemaModification]   ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaModification]') AND type in (N'U'))
DROP TABLE [dbo].[SchemaModification]
GO

/****** Object:  Table [dbo].[Packages]  ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Packages]') AND type in (N'U'))
DROP TABLE [dbo].[Packages]
GO

/****** Object:  Table [dbo].[TreeLocks]  ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TreeLocks]') AND type in (N'U'))
DROP TABLE [dbo].[TreeLocks]
GO

/****** Object:  Table [dbo].[AccessTokens]  ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AccessTokens]') AND type in (N'U'))
DROP TABLE [dbo].[AccessTokens]
GO

/****** Object:  Table [dbo].[SharedLocks]  ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SharedLocks]') AND type in (N'U'))
DROP TABLE [dbo].[SharedLocks]
GO

------------------------------------------------                           --------------------------------------------------
------------------------------------------------ ENABLE SNAPSHOT ISOLATION --------------------------------------------------
------------------------------------------------                           --------------------------------------------------

DECLARE @dbName sysname,
        @cmd0 nvarchar(max),
        @cmd1 nvarchar(max),
        @cmd2 nvarchar(max),
        @cmd3 nvarchar(max);

SET @dbName = DB_NAME()

------ Put the db into single user mode and close all open connections
------ so that we can alter the database during installation.

SET @cmd0 = N'ALTER DATABASE ' + quotename(@dbName) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;'
SET @cmd1 = N'ALTER DATABASE ' + quotename(@dbName) + N' SET ALLOW_SNAPSHOT_ISOLATION on'
SET @cmd2 = N'ALTER DATABASE ' + quotename(@dbName) + N' SET READ_COMMITTED_SNAPSHOT on'
SET @cmd3 = N'ALTER DATABASE ' + quotename(@dbName) + N' SET MULTI_USER;'

BEGIN TRY
	EXEC(@cmd0)
	EXEC(@cmd1)
	EXEC(@cmd2)
	EXEC(@cmd3)
END TRY
BEGIN CATCH
   	print '!!! Can not enable snapshot isolation mode. (Warning).'
END CATCH;
GO

------------------------------------------------               --------------------------------------------------------------
------------------------------------------------ CREATE TABLES --------------------------------------------------------------
------------------------------------------------               --------------------------------------------------------------

/****** Object:  Table [dbo].[ReferenceProperties]    Script Date: 10/25/2007 15:50:19 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ReferenceProperties](
	[ReferencePropertyId] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NOT NULL,
	[PropertyTypeId] [int] NOT NULL,
	[ReferredNodeId] [int] NOT NULL,
	--[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	--[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_ReferenceProperties] PRIMARY KEY CLUSTERED 
(
	[ReferencePropertyId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]') AND name = N'IX_VersionIdPropertyTypeId')
CREATE NONCLUSTERED INDEX [IX_VersionIdPropertyTypeId] ON [dbo].[ReferenceProperties]
(
	[VersionId] ASC,
	[PropertyTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]') AND name = N'IX_ReferenceProperties_ReferredNodeId')
CREATE NONCLUSTERED INDEX [IX_ReferenceProperties_ReferredNodeId]
    ON ReferenceProperties (ReferredNodeId);
GO

/****** Object:  Table [dbo].[LongTextProperties]    Script Date: 10/25/2007 15:50:32 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LongTextProperties]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[LongTextProperties](
	[LongTextPropertyId] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NOT NULL,
	[PropertyTypeId] [int] NOT NULL,
	[Length] [int] NULL,
	[Value] [nvarchar](MAX) NULL,
 CONSTRAINT [PK_LongTextProperties] PRIMARY KEY CLUSTERED 
(
	[LongTextPropertyId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[BinaryProperties]    Script Date: 10/25/2007 15:49:18 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BinaryProperties]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[BinaryProperties](
	[BinaryPropertyId] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NULL,
	[PropertyTypeId] [int] NULL,
	[FileId] [int] NOT NULL
 CONSTRAINT [PK_BinaryProperties] PRIMARY KEY CLUSTERED 
(
	[BinaryPropertyId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY];
END
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Files]    Script Date: 10/25/2007 15:49:18 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Files]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Files](
	[FileId] [int] IDENTITY(1,1) NOT NULL,
	[ContentType] [nvarchar](450) NOT NULL,
	[FileNameWithoutExtension] [nvarchar](450) NULL,
	[Extension] [nvarchar](50) NOT NULL,
	[Size] [bigint] NOT NULL,
	[Checksum] [varchar](200) NULL,
	[Stream] VARBINARY(MAX) NULL,
	[CreationDate] [datetime2] NOT NULL CONSTRAINT [DF_Files_CreationDate]  DEFAULT (getutcdate()),
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL unique DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
	[Staging] bit NULL,
	[StagingVersionId] int NULL,
	[StagingPropertyTypeId] int NULL,
	[IsDeleted] bit NULL,
	[BlobProvider] [nvarchar](450) NULL,
	[BlobProviderData] [nvarchar](MAX) NULL,
 CONSTRAINT [PK_Files] PRIMARY KEY CLUSTERED 
(
	[FileId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY];
END
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Nodes]    Script Date: 10/25/2007 15:50:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Nodes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Nodes](
	[NodeId] [int] IDENTITY(1,1) NOT NULL,
	[NodeTypeId] [int] NOT NULL,
	[ContentListTypeId] [int] NULL,
	[ContentListId] [int] NULL,
	[CreatingInProgress] [tinyint] NOT NULL,
	[IsDeleted] [tinyint] NOT NULL,
	[IsInherited] [tinyint] NOT NULL CONSTRAINT [DF_Nodes_IsInherited]  DEFAULT ((1)),
	[ParentNodeId] [int] NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Path] [nvarchar](450) COLLATE Latin1_General_CI_AS NOT NULL,
	[Index] [int] NOT NULL,
	[Locked] [tinyint] NOT NULL,
	[LockedById] [int] NULL,
	[ETag] [varchar](50) NOT NULL,
	[LockType] [int] NOT NULL,
	[LockTimeout] [int] NOT NULL,
	[LockDate] [datetime2] NOT NULL,
	[LockToken] [varchar](50) NOT NULL,
	[LastLockUpdate] [datetime2] NOT NULL,
	[LastMinorVersionId] [int] NULL,
	[LastMajorVersionId] [int] NULL,
	[CreationDate] [datetime2] NOT NULL,
	[CreatedById] [int] NOT NULL,
	[ModificationDate] [datetime2] NOT NULL,
	[ModifiedById] [int] NOT NULL,
	[DisplayName] [nvarchar](450) NULL,
	[IsSystem] [tinyint] NULL,
	[OwnerId] [int] NOT NULL,
	[SavingState] [int] NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_tblFpsNodes] PRIMARY KEY CLUSTERED 
(
	[NodeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
ALTER TABLE [dbo].[Nodes] ADD  CONSTRAINT [DF_Nodes_CreatingInProgress]  DEFAULT ((0)) FOR [CreatingInProgress]
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Nodes]') AND name = N'IX_Nodes_Path')
CREATE UNIQUE NONCLUSTERED INDEX [IX_Nodes_Path] ON [dbo].[Nodes] 
(
	[Path] ASC
) Include([NodeId])  WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Nodes]') AND name = N'IX_Nodes_ParentNodeId')
CREATE NONCLUSTERED INDEX [IX_Nodes_ParentNodeId] ON [dbo].[Nodes]
(
	[ParentNodeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Nodes]') AND name = N'IX_Nodes_NodeTypeId')
CREATE NONCLUSTERED INDEX [IX_Nodes_NodeTypeId] ON [dbo].[Nodes]
(
	[NodeTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Versions]    Script Date: 10/25/2007 15:50:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Versions]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Versions](
	[VersionId] [int] IDENTITY(1,1) NOT NULL,
	[NodeId] [int] NOT NULL,
	[MajorNumber] [smallint] NOT NULL,
	[MinorNumber] [smallint] NOT NULL,
	[CreationDate] [datetime2] NOT NULL,
	[CreatedById] [int] NOT NULL,
	[ModificationDate] [datetime2] NOT NULL,
	[ModifiedById] [int] NOT NULL,
	[Status] [smallint] NOT NULL CONSTRAINT [DF_Versions_Status]  DEFAULT ((1)),
	[IndexDocument] NVARCHAR(MAX) NULL,
	[ChangedData] NVARCHAR(MAX) NULL,
	[DynamicProperties] NVARCHAR(MAX) NULL,
	[ContentListProperties] NVARCHAR(MAX) NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_Versions] PRIMARY KEY CLUSTERED 
(
	[VersionId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[NodeTypes]    Script Date: 10/25/2007 15:50:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NodeTypes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[NodeTypes](
	[NodeTypeId] [int] IDENTITY(1,1) NOT NULL,
	[ParentId] [int] NULL,
	[Name] [varchar](450) NOT NULL,
	[ClassName] [varchar](450) NULL,
	[Properties] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_NodeTypes] PRIMARY KEY CLUSTERED 
(
	[NodeTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[NodeTypes]') AND name = N'ix_parentid')
CREATE NONCLUSTERED INDEX [ix_parentid] ON [dbo].[NodeTypes]
(
	[ParentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[ContentListTypes]    Script Date: 10/25/2007 15:50:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ContentListTypes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ContentListTypes](
	[ContentListTypeId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](450) NOT NULL,
	[Properties] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_ContentListTypes] PRIMARY KEY CLUSTERED 
(
	[ContentListTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[PropertyTypes]    Script Date: 10/25/2007 15:50:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PropertyTypes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PropertyTypes](
	[PropertyTypeId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](450) NOT NULL,
	[DataType] [varchar](10) NOT NULL,
	[Mapping] [int] NOT NULL,
	[IsContentListProperty] [tinyint] NOT NULL DEFAULT 0,
 CONSTRAINT [PK_PropertyTypes] PRIMARY KEY CLUSTERED 
(
	[PropertyTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO

------------------------------------------------              --------------------------------------------------------------
------------------------------------------------ CREATE VIEWS --------------------------------------------------------------
------------------------------------------------              --------------------------------------------------------------

/******  Object:  View [dbo].[NodeInfoView]  ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[NodeInfoView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[NodeInfoView]
AS
SELECT     N.NodeId, T.Name AS Type, N.Name, N.Path, N.LockedById, V.VersionId, CONVERT(Varchar, V.MajorNumber) + ''.'' + CONVERT(Varchar, V.MinorNumber) 
                      + ''.'' + CASE [Status] WHEN 1 THEN ''A'' WHEN 2 THEN ''L'' WHEN 4 THEN ''D'' WHEN 8 THEN ''R'' WHEN 16 THEN ''P'' ELSE '''' END AS Version, 
                      CASE V.VersionId WHEN N .LastMajorVersionId THEN ''TRUE'' ELSE ''false'' END AS LastPub, 
                      CASE V.VersionId WHEN N .LastMinorVersionId THEN ''TRUE'' ELSE ''false'' END AS LastWork
FROM         dbo.Versions AS V INNER JOIN
                      dbo.Nodes AS N ON V.NodeId = N.NodeId INNER JOIN
                      dbo.NodeTypes AS T ON N.NodeTypeId = T.NodeTypeId
'
GO
/****** Object:  View [dbo].[ReferencesInfoView]    Script Date: 08/07/2007 14:50:18 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[ReferencesInfoView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[ReferencesInfoView]
AS
--SELECT     Nodes.Name AS SrcName, ''V'' + CAST(Versions.MajorNumber AS nvarchar(20)) + ''.'' + CAST(Versions.MinorNumber AS nvarchar(20)) AS SrcVer, 
--                      Slots.Name AS RelType, RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, 
--                      RefNodes.Path AS TargetPath
--FROM         dbo.ReferenceProperties AS Refs INNER JOIN
--                      dbo.Versions AS Versions ON Refs.VersionId = Versions.VersionId INNER JOIN
--                      dbo.Nodes AS Nodes ON Versions.NodeId = Nodes.NodeId INNER JOIN
--                      dbo.Nodes AS RefNodes ON Refs.ReferredNodeId = RefNodes.NodeId INNER JOIN
--                      dbo.PropertyTypes AS Slots ON Refs.PropertyTypeId = Slots.PropertyTypeId

-- ReferenceProperties
	SELECT     Nodes.Name AS SrcName, ''V'' + CAST(Versions.MajorNumber AS nvarchar(20)) + ''.'' + CAST(Versions.MinorNumber AS nvarchar(20)) AS SrcVer, 
						  Slots.Name AS RelType, RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, 
						  RefNodes.Path AS TargetPath
	FROM         dbo.ReferenceProperties AS Refs INNER JOIN
						  dbo.Versions AS Versions ON Refs.VersionId = Versions.VersionId INNER JOIN
						  dbo.Nodes AS Nodes ON Versions.NodeId = Nodes.NodeId INNER JOIN
						  dbo.Nodes AS RefNodes ON Refs.ReferredNodeId = RefNodes.NodeId INNER JOIN
						  dbo.PropertyTypes AS Slots ON Refs.PropertyTypeId = Slots.PropertyTypeId
UNION ALL
-- Parent
	SELECT     Nodes.Name AS SrcName, ''V*.*'' AS SrcVer, ''Parent'' AS RelType, RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, 
						  RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, RefNodes.Path AS TargetPath
	FROM         dbo.Nodes AS Nodes INNER JOIN
						  dbo.Nodes AS RefNodes ON Nodes.ParentNodeId = RefNodes.NodeId
UNION ALL
-- LockedById
	SELECT     Nodes.Name AS SrcName, ''V*.*'' AS SrcVer, ''LockedById'' AS RelType, RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, 
						  RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, RefNodes.Path AS TargetPath
	FROM         dbo.Nodes AS Nodes INNER JOIN
						  dbo.Nodes AS RefNodes ON Nodes.LockedById = RefNodes.NodeId
UNION ALL
-- CreatedById
	SELECT     Nodes.Name AS SrcName, ''V'' + CAST(Versions.MajorNumber AS nvarchar(20)) + ''.'' + CAST(Versions.MinorNumber AS nvarchar(20)) AS SrcVer, 
						  ''CreatedById'', RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, 
						  RefNodes.Path AS TargetPath
	FROM         dbo.Nodes AS Nodes INNER JOIN
		                  dbo.Versions AS Versions ON Nodes.NodeId = Versions.NodeId INNER JOIN
			              dbo.Nodes AS RefNodes ON Versions.CreatedById = RefNodes.NodeId
UNION ALL
-- ModifiedById
	SELECT     Nodes.Name AS SrcName, ''V'' + CAST(Versions.MajorNumber AS nvarchar(20)) + ''.'' + CAST(Versions.MinorNumber AS nvarchar(20)) AS SrcVer, 
						  ''ModifiedById'', RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, 
						  RefNodes.Path AS TargetPath
	FROM         dbo.Nodes AS Nodes INNER JOIN
		                  dbo.Versions AS Versions ON Nodes.NodeId = Versions.NodeId INNER JOIN
			              dbo.Nodes AS RefNodes ON Versions.ModifiedById = RefNodes.NodeId
'
GO
/****** Object:  View [dbo].[PermissionInfoView] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[PermissionInfoView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[PermissionInfoView]
AS
SELECT n.Path, e1.IsInherited, i.Path IdentityPath, e.LocalOnly,
	(SELECT CASE (DenyBits & 0x8000000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x8000000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x4000000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x4000000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x2000000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x2000000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x1000000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x1000000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0800000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0800000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0400000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0400000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0200000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0200000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0100000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0100000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0080000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0080000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0040000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0040000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0020000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0020000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0010000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0010000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0008000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0008000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0004000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0004000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0002000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0002000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0001000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0001000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000800000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000800000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000400000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000400000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000200000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000200000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000100000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000100000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000080000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000080000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000040000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000040000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000020000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000020000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000010000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000010000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000008000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000008000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000004000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000004000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000002000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000002000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000001000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000001000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000000800000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000000800000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000000400000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000000400000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000000200000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000000200000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x0000000100000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000000100000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS CustomBits,
	(SELECT CASE (DenyBits & 0x80000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x80000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x40000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x40000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x20000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x20000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x10000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x10000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x08000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x08000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x04000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x04000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x02000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x02000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x01000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x01000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00800000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00800000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00400000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00400000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00200000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00200000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00100000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00100000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00080000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00080000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00040000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00040000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00020000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00020000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00010000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00010000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00008000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00008000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00004000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00004000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00002000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00002000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00001000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00001000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000800) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000800) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000400) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000400) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000200) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000200) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000100) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000100) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000080) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000080) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000040) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000040) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000020) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000020) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000010) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000010) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000008) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000008) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000004) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000004) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000002) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000002) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) +
	(SELECT CASE (DenyBits & 0x00000001) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000001) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) As SystemBits,
	e.EFEntityId, e.IdentityId, e.AllowBits, e.DenyBits,
	(SELECT CASE (DenyBits & 0x00000001) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000001) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS See,
	(SELECT CASE (DenyBits & 0x00000002) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000002) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS Pre,
	(SELECT CASE (DenyBits & 0x00000004) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000004) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS PWa,
	(SELECT CASE (DenyBits & 0x00000008) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000008) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS PRd,
	(SELECT CASE (DenyBits & 0x00000010) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000010) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS Opn,
	(SELECT CASE (DenyBits & 0x00000020) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000020) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS OpM,
	(SELECT CASE (DenyBits & 0x00000040) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000040) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS Sav,
	(SELECT CASE (DenyBits & 0x00000080) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000080) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS Pub,
	(SELECT CASE (DenyBits & 0x00000100) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000100) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS Chk,
	(SELECT CASE (DenyBits & 0x00000200) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000200) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS [Add],
	(SELECT CASE (DenyBits & 0x00000400) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000400) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS Apr,
	(SELECT CASE (DenyBits & 0x00000800) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00000800) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS Del,
	(SELECT CASE (DenyBits & 0x00001000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00001000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS ReV,
	(SELECT CASE (DenyBits & 0x00002000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00002000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS DeV,
	(SELECT CASE (DenyBits & 0x00004000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00004000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS ReP,
	(SELECT CASE (DenyBits & 0x00008000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00008000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS WrP,
	(SELECT CASE (DenyBits & 0x00010000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00010000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS Run,
	(SELECT CASE (DenyBits & 0x00020000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00020000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS LST,
	(SELECT CASE (DenyBits & 0x00040000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00040000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS Own,
	(SELECT CASE (DenyBits & 0x00080000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00080000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U13,
	(SELECT CASE (DenyBits & 0x00100000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00100000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U12,
	(SELECT CASE (DenyBits & 0x00200000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00200000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U11,
	(SELECT CASE (DenyBits & 0x00400000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00400000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U10,
	(SELECT CASE (DenyBits & 0x00800000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x00800000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U09,
	(SELECT CASE (DenyBits & 0x01000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x01000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U08,
	(SELECT CASE (DenyBits & 0x02000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x02000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U07,
	(SELECT CASE (DenyBits & 0x04000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x04000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U06,
	(SELECT CASE (DenyBits & 0x08000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x08000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U05,
	(SELECT CASE (DenyBits & 0x10000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x10000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U04,
	(SELECT CASE (DenyBits & 0x20000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x20000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U03,
	(SELECT CASE (DenyBits & 0x40000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x40000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U02,
	(SELECT CASE (DenyBits & 0x80000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x80000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS U01,

	(SELECT CASE (DenyBits & 0x0000000100000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000000100000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C01,
	(SELECT CASE (DenyBits & 0x0000000200000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000000200000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C02,
	(SELECT CASE (DenyBits & 0x0000000400000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000000400000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C03,
	(SELECT CASE (DenyBits & 0x0000000800000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000000800000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C04,
	(SELECT CASE (DenyBits & 0x0000001000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000001000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C05,
	(SELECT CASE (DenyBits & 0x0000002000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000002000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C06,
	(SELECT CASE (DenyBits & 0x0000004000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000004000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C07,
	(SELECT CASE (DenyBits & 0x0000008000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000008000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C08,
	(SELECT CASE (DenyBits & 0x0000010000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000010000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C09,
	(SELECT CASE (DenyBits & 0x0000020000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000020000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C10,
	(SELECT CASE (DenyBits & 0x0000040000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000040000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C11,
	(SELECT CASE (DenyBits & 0x0000080000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000080000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C12,
	(SELECT CASE (DenyBits & 0x0000100000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000100000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C13,
	(SELECT CASE (DenyBits & 0x0000200000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000200000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C14,
	(SELECT CASE (DenyBits & 0x0000400000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000400000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C15,
	(SELECT CASE (DenyBits & 0x0000800000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0000800000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C16,
	(SELECT CASE (DenyBits & 0x0001000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0001000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C17,
	(SELECT CASE (DenyBits & 0x0002000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0002000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C18,
	(SELECT CASE (DenyBits & 0x0004000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0004000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C19,
	(SELECT CASE (DenyBits & 0x0008000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0008000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C20,
	(SELECT CASE (DenyBits & 0x0010000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0010000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C21,
	(SELECT CASE (DenyBits & 0x0020000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0020000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C22,
	(SELECT CASE (DenyBits & 0x0040000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0040000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C23,
	(SELECT CASE (DenyBits & 0x0080000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0080000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C24,
	(SELECT CASE (DenyBits & 0x0100000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0100000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C25,
	(SELECT CASE (DenyBits & 0x0200000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0200000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C26,
	(SELECT CASE (DenyBits & 0x0400000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0400000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C27,
	(SELECT CASE (DenyBits & 0x0800000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x0800000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C28,
	(SELECT CASE (DenyBits & 0x1000000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x1000000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C29,
	(SELECT CASE (DenyBits & 0x2000000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x2000000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C30,
	(SELECT CASE (DenyBits & 0x4000000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x4000000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C31,
	(SELECT CASE (DenyBits & 0x8000000000000000) WHEN 0 THEN (SELECT CASE (AllowBits & 0x8000000000000000) WHEN 0 THEN ''_'' ELSE ''+'' END) ELSE ''-'' END) AS C32

FROM EFEntries e
	join EFEntities e1 on e.EFEntityId = e1.Id
	join Nodes n on e.EFEntityId = n.NodeId
	join Nodes i on e.IdentityId = i.NodeId
'
GO

------------------------------------------------                    --------------------------------------------------------------
------------------------------------------------ CREATE CONSTRAINTS --------------------------------------------------------------
------------------------------------------------                    --------------------------------------------------------------


/****** Object:  ForeignKey [FK_BinaryProperties_PropertyTypes]    Script Date: 10/25/2007 15:49:18 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BinaryProperties_PropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
ALTER TABLE [dbo].[BinaryProperties]  WITH CHECK ADD  CONSTRAINT [FK_BinaryProperties_PropertyTypes] FOREIGN KEY([PropertyTypeId])
REFERENCES [dbo].[PropertyTypes] ([PropertyTypeId])
GO
ALTER TABLE [dbo].[BinaryProperties] CHECK CONSTRAINT [FK_BinaryProperties_PropertyTypes]
GO
/****** Object:  ForeignKey [FK_BinaryProperties_Versions]    Script Date: 10/25/2007 15:49:19 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BinaryProperties_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
ALTER TABLE [dbo].[BinaryProperties]  WITH CHECK ADD  CONSTRAINT [FK_BinaryProperties_Versions] FOREIGN KEY([VersionId])
REFERENCES [dbo].[Versions] ([VersionId])
GO
ALTER TABLE [dbo].[BinaryProperties] CHECK CONSTRAINT [FK_BinaryProperties_Versions]
GO
/****** Object:  ForeignKey [FK_BinaryProperties_Files] ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BinaryProperties_Files]') AND parent_object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
ALTER TABLE [dbo].[BinaryProperties]  WITH CHECK ADD  CONSTRAINT [FK_BinaryProperties_Files] FOREIGN KEY([FileId])
REFERENCES [dbo].[Files] ([FileId])
GO
ALTER TABLE [dbo].[BinaryProperties] CHECK CONSTRAINT [FK_BinaryProperties_Files]
GO
/****** Object:  ForeignKey [FK_Nodes_LockedBy]    Script Date: 10/25/2007 15:50:16 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_LockedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_LockedBy] FOREIGN KEY([LockedById])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_LockedBy]
GO
/****** Object:  ForeignKey [FK_Nodes_Parent]    Script Date: 10/25/2007 15:50:16 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_Parent]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_Parent] FOREIGN KEY([ParentNodeId])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_Parent]
GO
/****** Object:  ForeignKey [FK_Nodes_NodeTypes]    Script Date: 10/25/2007 15:50:17 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_NodeTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_NodeTypes] FOREIGN KEY([NodeTypeId])
REFERENCES [dbo].[NodeTypes] ([NodeTypeId])
GO
ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_NodeTypes]
GO
/****** Object:  ForeignKey [FK_ReferenceProperties_PropertyTypes]    Script Date: 10/25/2007 15:50:19 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ReferenceProperties_PropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]'))
ALTER TABLE [dbo].[ReferenceProperties]  WITH CHECK ADD  CONSTRAINT [FK_ReferenceProperties_PropertyTypes] FOREIGN KEY([PropertyTypeId])
REFERENCES [dbo].[PropertyTypes] ([PropertyTypeId])
GO
ALTER TABLE [dbo].[ReferenceProperties] CHECK CONSTRAINT [FK_ReferenceProperties_PropertyTypes]
GO
/****** Object:  ForeignKey [FK_NodeTypes_NodeTypes]    Script Date: 10/25/2007 15:50:23 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_NodeTypes_NodeTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[NodeTypes]'))
ALTER TABLE [dbo].[NodeTypes]  WITH CHECK ADD  CONSTRAINT [FK_NodeTypes_NodeTypes] FOREIGN KEY([ParentId])
REFERENCES [dbo].[NodeTypes] ([NodeTypeId])
GO
ALTER TABLE [dbo].[NodeTypes] CHECK CONSTRAINT [FK_NodeTypes_NodeTypes]
GO
/****** Object:  ForeignKey [FK_LongTextProperties_PropertyTypes]    Script Date: 10/25/2007 15:50:32 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_LongTextProperties_PropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[LongTextProperties]'))
ALTER TABLE [dbo].[LongTextProperties]  WITH CHECK ADD  CONSTRAINT [FK_LongTextProperties_PropertyTypes] FOREIGN KEY([PropertyTypeId])
REFERENCES [dbo].[PropertyTypes] ([PropertyTypeId])
GO
ALTER TABLE [dbo].[LongTextProperties] CHECK CONSTRAINT [FK_LongTextProperties_PropertyTypes]
GO
/****** Object:  ForeignKey [FK_LongTextProperties_Versions]    Script Date: 10/25/2007 15:50:32 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_LongTextProperties_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[LongTextProperties]'))
ALTER TABLE [dbo].[LongTextProperties]  WITH CHECK ADD  CONSTRAINT [FK_LongTextProperties_Versions] FOREIGN KEY([VersionId])
REFERENCES [dbo].[Versions] ([VersionId])
GO
ALTER TABLE [dbo].[LongTextProperties] CHECK CONSTRAINT [FK_LongTextProperties_Versions]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes]    Script Date: 10/25/2007 15:50:36 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions]  WITH CHECK ADD  CONSTRAINT [FK_Versions_Nodes] FOREIGN KEY([NodeId])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Versions] CHECK CONSTRAINT [FK_Versions_Nodes]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes_CreatedBy]    Script Date: 10/25/2007 15:50:37 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes_CreatedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions]  WITH CHECK ADD  CONSTRAINT [FK_Versions_Nodes_CreatedBy] FOREIGN KEY([CreatedById])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Versions] CHECK CONSTRAINT [FK_Versions_Nodes_CreatedBy]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes_ModifiedBy]    Script Date: 10/25/2007 15:50:37 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes_ModifiedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions]  WITH CHECK ADD  CONSTRAINT [FK_Versions_Nodes_ModifiedBy] FOREIGN KEY([ModifiedById])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Versions] CHECK CONSTRAINT [FK_Versions_Nodes_ModifiedBy]
GO

ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_Nodes_CreatedById] FOREIGN KEY([CreatedById])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_Nodes_CreatedById]
GO

ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_Nodes_ModifiedById] FOREIGN KEY([ModifiedById])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_Nodes_ModifiedById]
GO

/****** Object:  Table [dbo].[JournalItems]    Script Date: 07/08/2009 07:22:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JournalItems](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[When] [datetime2] NOT NULL,
	[Wherewith] [nvarchar](450) NOT NULL,
	[What] [nvarchar](100) NOT NULL,
	[Who] [nvarchar](200) NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
	[NodeId] [int] NOT NULL,
	[DisplayName] [nvarchar](450) NOT NULL,
	[NodeTypeName] [nvarchar](100) NOT NULL,
	[SourcePath] [nvarchar](450) NULL,
	[TargetPath] [nvarchar](450) NULL,
	[TargetDisplayName] [nvarchar](450) NULL,
	[Hidden] [bit] NOT NULL,
	[Details] [nvarchar](450) NULL	
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_JournalItems] ON [dbo].[JournalItems] 
(
	[When] DESC,
	[Wherewith] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[LogEntries]    Script Date: 10/09/2009 10:01:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogEntries]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[LogEntries](
	[LogId] [int] IDENTITY(1,1) NOT NULL,
	[EventId] [int] NOT NULL,
	[Category] [nvarchar](50) NULL,
	[Priority] [int] NOT NULL,
	[Severity] [varchar](30) NOT NULL,
	[Title] [nvarchar](256) NULL,
	[ContentId] [int] NULL,
	[ContentPath] [nvarchar](450) NULL,
	[UserName] [nvarchar](450) NULL,
	[LogDate] [datetime2] NOT NULL,
	[MachineName] [varchar](32) NULL,
	[AppDomainName] [varchar](512) NULL,
	[ProcessID] [varchar](256) NULL,
	[ProcessName] [varchar](512) NULL,
	[ThreadName] [varchar](512) NULL,
	[Win32ThreadId] [varchar](128) NULL,
	[Message] [nvarchar](1500) NULL,
	[FormattedMessage] [ntext] NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_Log] PRIMARY KEY CLUSTERED 
(
	[LogId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[IndexingActivities]    Script Date: 08/27/2010 07:54:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[IndexingActivities](
	[IndexingActivityId] [int] IDENTITY(1,1) NOT NULL,
	[ActivityType] [varchar](50) NOT NULL,
	[CreationDate] [datetime2] NOT NULL,
	[RunningState] varchar(10) NOT NULL,
	[LockTime] [datetime2] NULL,
	[NodeId] [int] NOT NULL,
	[VersionId] [int] NOT NULL,
	[Path] [nvarchar](450) NOT NULL,
	[VersionTimestamp] [bigint] NULL,
	[Extension] [varchar](max) NULL
 CONSTRAINT [PK_IndexingActivity] PRIMARY KEY CLUSTERED 
(
	[IndexingActivityId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[WorkflowNotification] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[WorkflowNotification](
	[NotificationId] [int] IDENTITY(1,1) NOT NULL,
	[NodeId] [int] NOT NULL,
	[WorkflowInstanceId] [uniqueidentifier] NOT NULL,
	[WorkflowNodePath] [nvarchar](450) NOT NULL,
	[BookmarkName] [varchar](50) NOT NULL,
 CONSTRAINT [PK_WorkflowNotification] PRIMARY KEY CLUSTERED 
(
	[NotificationId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 50) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[SchemaModification]    Script Date: 01/06/2011 12:08:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SchemaModification](
	[SchemaModificationId] [int] IDENTITY(1,1) NOT NULL,
	[ModificationDate] [datetime2] NOT NULL,
	[LockToken] [varchar](50) NULL,
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_SchemaModification] PRIMARY KEY CLUSTERED 
(
	[SchemaModificationId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Packages]  ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Packages](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PackageType] [varchar](50) NOT NULL,
	[ComponentId] [nvarchar](450) NULL,
	[ComponentVersion] [varchar](50) NULL,
	[ReleaseDate] [datetime2] NOT NULL,
	[ExecutionDate] [datetime2] NOT NULL,
	[ExecutionResult] [varchar](50) NOT NULL,
	[ExecutionError] [nvarchar](max) NULL,
	[Description] [nvarchar](1000) NULL,
	[Manifest] [nvarchar](max) NULL,
 CONSTRAINT [PK_Packages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[TreeLocks] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TreeLocks](
	[TreeLockId] [int] IDENTITY(1,1) NOT NULL,
	[Path] [nvarchar](450) NOT NULL,
	[LockedAt] [datetime2] NOT NULL,
 CONSTRAINT [PK_TreeLocks] PRIMARY KEY CLUSTERED 
(
	[TreeLockId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[AccessTokens] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AccessTokens](
	[AccessTokenId] [int] IDENTITY(1,1) NOT NULL,
	[Value] [nvarchar](1000) NOT NULL,
	[UserId] [int] NOT NULL,
	[ContentId] [int] NULL,
	[Feature] [nvarchar](1000) NULL,
	[CreationDate] [datetime2] NOT NULL,
	[ExpirationDate] [datetime2] NOT NULL,
 CONSTRAINT [PK_AccessTokens] PRIMARY KEY CLUSTERED 
(
	[AccessTokenId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[SharedLocks] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SharedLocks](
	[SharedLockId] [int] IDENTITY(1,1) NOT NULL,
	[ContentId] [int] NOT NULL,
	[Lock] [nvarchar](1000) NOT NULL,
	[CreationDate] [datetime2] NOT NULL,
 CONSTRAINT [PK_SharedLocks] PRIMARY KEY CLUSTERED 
(
	[SharedLockId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object: Index [ix_version_id] Script Date: 05/03/2011 15:21:41 ******/
CREATE NONCLUSTERED INDEX [ix_version_id] ON [dbo].[BinaryProperties] 
(
[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object: Index [ix_file_id] Script Date: 05/03/2011 15:21:41 ******/
CREATE NONCLUSTERED INDEX [ix_file_id] ON [dbo].[BinaryProperties] 
(
[FileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/****** Object: Index [ix_versionid] Script Date: 05/03/2011 15:22:32 ******/
CREATE NONCLUSTERED INDEX [ix_version_id] ON [dbo].[ReferenceProperties] 
(
[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/****** Object: Index [ix_version] Script Date: 05/03/2011 15:23:25 ******/
CREATE NONCLUSTERED INDEX [ix_version_id] ON [dbo].[LongTextProperties] 
(
[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [ix_Versions_NodeId]
ON [dbo].[Versions] ([NodeId])
GO

CREATE NONCLUSTERED INDEX [ix_Versions_NodeId_MinorNumber_MajorNumber_Status]
ON [dbo].[Versions] ([NodeId],[MinorNumber],[Status])
GO


CREATE NONCLUSTERED INDEX [ix_name] ON [dbo].[NodeTypes] 
(
	[Name] ASC
)
INCLUDE([NodeTypeId])
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 50) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [ix_name] ON [dbo].[PropertyTypes] 
(
	[Name] ASC
)
INCLUDE([PropertyTypeId])
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 50) ON [PRIMARY]
GO

--============================== Switch off the foreign keys ==============================

ALTER TABLE [BinaryProperties] NOCHECK CONSTRAINT ALL
ALTER TABLE [Nodes] NOCHECK CONSTRAINT ALL
ALTER TABLE [ReferenceProperties] NOCHECK CONSTRAINT ALL
ALTER TABLE [LongTextProperties] NOCHECK CONSTRAINT ALL
ALTER TABLE [Versions] NOCHECK CONSTRAINT ALL

--=========================================================================================
