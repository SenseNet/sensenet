USE [ReinstallationTransfer]

/********************************************************************** ENSURE SavedAccessTokens TABLE */

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SavedAccessTokens]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SavedAccessTokens](
	[RowId] [int] IDENTITY(1,1) NOT NULL,
	[OperationId] [varchar](128) NOT NULL,
	[AccessTokenId] [int] NOT NULL,
	[Value] [nvarchar](1000) NOT NULL,
	[UserId] [int] NOT NULL,
	[ContentId] [int] NULL,
	[Feature] [nvarchar](1000) NULL,
	[CreationDate] [datetime2](7) NOT NULL,
	[ExpirationDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_SavedAccessTokens] PRIMARY KEY CLUSTERED 
(
	[RowId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/********************************************************************** ENSURE SavedClientApps TABLE */

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SavedClientApps]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SavedClientApps](
	[RowId] [int] IDENTITY(1,1) NOT NULL,
	[OperationId] [varchar](128) NOT NULL,
	[ClientId] [varchar](50) NOT NULL,
	[Name] [nvarchar](450) NULL,
	[Repository] [nvarchar](450) NULL,
	[UserName] [nvarchar](450) NULL,
	[Authority] [nvarchar](450) NULL,
	[Type] [int] NULL,
 CONSTRAINT [PK_SavedClientApps] PRIMARY KEY CLUSTERED 
(
	[RowId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/********************************************************************** ENSURE SavedClientSecrets TABLE */

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SavedClientSecrets]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SavedClientSecrets](
	[RowId] [int] IDENTITY(1,1) NOT NULL,
	[OperationId] [varchar](128) NOT NULL,
	[Id] [varchar](50) NOT NULL,
	[ClientId] [varchar](50) NOT NULL,
	[Value] [nvarchar](450) NOT NULL,
	[CreationDate] [datetime2](7) NOT NULL,
	[ValidTill] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_SavedClientSecrets] PRIMARY KEY CLUSTERED 
(
	[RowId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/********************************************************************** ENSURE SavedVersions TABLE */

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SavedVersions]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SavedVersions](
	[RowId] [int] IDENTITY(1,1) NOT NULL,
	[OperationId] [varchar](128) NOT NULL,
	[NodeId] [int] NOT NULL,
	[Path] [nvarchar](450) NOT NULL,
	[VersionId] [int] NOT NULL,
	[CreationDate] [datetime2](7) NOT NULL,
	[VersionCreationDate] [datetime2](7) NOT NULL,
	[DynamicProperties] [nvarchar](max) NULL,
 CONSTRAINT [PK_SavedVersions] PRIMARY KEY CLUSTERED 
(
	[RowId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
