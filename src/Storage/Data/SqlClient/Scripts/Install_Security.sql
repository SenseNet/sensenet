IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_dbo.EFEntries_dbo.EFEntities_EFEntityId]') AND parent_object_id = OBJECT_ID(N'[dbo].[EFEntries]'))
ALTER TABLE [dbo].[EFEntries] DROP CONSTRAINT [FK_dbo.EFEntries_dbo.EFEntities_EFEntityId]
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_dbo.EFEntities_dbo.EFEntities_ParentId]') AND parent_object_id = OBJECT_ID(N'[dbo].[EFEntities]'))
ALTER TABLE [dbo].[EFEntities] DROP CONSTRAINT [FK_dbo.EFEntities_dbo.EFEntities_ParentId]
GO
/****** Object:  Table [dbo].[EFMessages]    Script Date: 2018. 11. 20. 8:25:04 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EFMessages]') AND type in (N'U'))
DROP TABLE [dbo].[EFMessages]
GO
/****** Object:  Table [dbo].[EFMemberships]    Script Date: 2018. 11. 20. 8:25:04 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EFMemberships]') AND type in (N'U'))
DROP TABLE [dbo].[EFMemberships]
GO
/****** Object:  Table [dbo].[EFEntries]    Script Date: 2018. 11. 20. 8:25:04 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EFEntries]') AND type in (N'U'))
DROP TABLE [dbo].[EFEntries]
GO
/****** Object:  Table [dbo].[EFEntities]    Script Date: 2018. 11. 20. 8:25:04 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EFEntities]') AND type in (N'U'))
DROP TABLE [dbo].[EFEntities]
GO


/****** Object:  Table [dbo].[EFEntities]    Script Date: 10/9/2015 11:57:27 AM ******/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EFEntities]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[EFEntities](
	[Id] [int] NOT NULL,
	[OwnerId] [int] NULL,
	[ParentId] [int] NULL,
	[IsInherited] [bit] NOT NULL,
 CONSTRAINT [PK_dbo.EFEntities] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END


/****** Object:  Table [dbo].[EFEntries]    Script Date: 10/9/2015 11:57:27 AM ******/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EFEntries]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[EFEntries](
	[EFEntityId] [int] NOT NULL,
	[EntryType] [int] NOT NULL,
	[IdentityId] [int] NOT NULL,
	[LocalOnly] [bit] NOT NULL,
	[AllowBits] [bigint] NOT NULL,
	[DenyBits] [bigint] NOT NULL,
 CONSTRAINT [PK_dbo.EFEntries] PRIMARY KEY CLUSTERED 
(
	[EFEntityId] ASC,
    [EntryType] ASC,
    [IdentityId] ASC,
	[LocalOnly] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/****** Object:  Table [dbo].[EFMemberships]    Script Date: 10/9/2015 11:57:27 AM ******/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EFMemberships]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[EFMemberships](
	[GroupId] [int] NOT NULL,
	[MemberId] [int] NOT NULL,
	[IsUser] [bit] NOT NULL,
 CONSTRAINT [PK_dbo.EFMemberships] PRIMARY KEY CLUSTERED 
(
	[GroupId] ASC,
	[MemberId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/****** Object:  Table [dbo].[EFMessages]    Script Date: 10/9/2015 11:57:27 AM ******/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EFMessages]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[EFMessages](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SavedBy] [nvarchar](max) NULL,
	[SavedAt] [datetime] NOT NULL,
	[ExecutionState] [nvarchar](max) NULL,
	[LockedBy] [nvarchar](max) NULL,
	[LockedAt] [datetime] NULL,
	[Body] [varbinary](max) NULL,
 CONSTRAINT [PK_dbo.EFMessages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END

/****** Object:  Index [IX_ParentId]    Script Date: 10/9/2015 11:57:27 AM ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[EFEntities]') AND name = N'IX_ParentId')
CREATE NONCLUSTERED INDEX [IX_ParentId] ON [dbo].[EFEntities]
(
	[ParentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

/****** Object:  Index [IX_EFEntityId]    Script Date: 10/9/2015 11:57:27 AM ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[EFEntries]') AND name = N'IX_EFEntityId')
CREATE NONCLUSTERED INDEX [IX_EFEntityId] ON [dbo].[EFEntries]
(
	[EFEntityId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_dbo.EFEntities_dbo.EFEntities_ParentId]') AND parent_object_id = OBJECT_ID(N'[dbo].[EFEntities]'))
ALTER TABLE [dbo].[EFEntities]  WITH CHECK ADD  CONSTRAINT [FK_dbo.EFEntities_dbo.EFEntities_ParentId] FOREIGN KEY([ParentId])
REFERENCES [dbo].[EFEntities] ([Id])

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_dbo.EFEntities_dbo.EFEntities_ParentId]') AND parent_object_id = OBJECT_ID(N'[dbo].[EFEntities]'))
ALTER TABLE [dbo].[EFEntities] CHECK CONSTRAINT [FK_dbo.EFEntities_dbo.EFEntities_ParentId]

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_dbo.EFEntries_dbo.EFEntities_EFEntityId]') AND parent_object_id = OBJECT_ID(N'[dbo].[EFEntries]'))
ALTER TABLE [dbo].[EFEntries]  WITH CHECK ADD  CONSTRAINT [FK_dbo.EFEntries_dbo.EFEntities_EFEntityId] FOREIGN KEY([EFEntityId])
REFERENCES [dbo].[EFEntities] ([Id])

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_dbo.EFEntries_dbo.EFEntities_EFEntityId]') AND parent_object_id = OBJECT_ID(N'[dbo].[EFEntries]'))
ALTER TABLE [dbo].[EFEntries] CHECK CONSTRAINT [FK_dbo.EFEntries_dbo.EFEntities_EFEntityId]