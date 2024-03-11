USE [ReinstallationTransfer]

select * from SavedAccessTokens
select * from SavedClientApps
select * from SavedClientSecrets
select * from SavedVersions

select * from [SnWebApplication.Api.Sql.TokenAuth].[dbo].AccessTokens
select * from [SnWebApplication.Api.Sql.TokenAuth].[dbo].ClientApps
select * from [SnWebApplication.Api.Sql.TokenAuth].[dbo].ClientSecrets
select
	(select CreationDate from [SnWebApplication.Api.Sql.TokenAuth].[dbo].Nodes where NodeId = 1) CreationDate,
	(select CreationDate from [SnWebApplication.Api.Sql.TokenAuth].[dbo].Versions where VersionId = 1) VersionCreationDate,
	(select DynamicProperties from [SnWebApplication.Api.Sql.TokenAuth].[dbo].Versions where VersionId = 1) DynamicProperties

select * from [SnWebApplication.Api.Sql.TokenAuth2].[dbo].AccessTokens
select * from [SnWebApplication.Api.Sql.TokenAuth2].[dbo].ClientApps
select * from [SnWebApplication.Api.Sql.TokenAuth2].[dbo].ClientSecrets
select
	(select CreationDate from [SnWebApplication.Api.Sql.TokenAuth2].[dbo].Nodes where NodeId = 1) CreationDate,
	(select CreationDate from [SnWebApplication.Api.Sql.TokenAuth2].[dbo].Versions where VersionId = 1) VersionCreationDate,
	(select DynamicProperties from [SnWebApplication.Api.Sql.TokenAuth2].[dbo].Versions where VersionId = 1) DynamicProperties

select * from [SnWebApplication.Api.Sql.TokenAuth3].[dbo].AccessTokens
select * from [SnWebApplication.Api.Sql.TokenAuth3].[dbo].ClientApps
select * from [SnWebApplication.Api.Sql.TokenAuth3].[dbo].ClientSecrets
select
	(select CreationDate from [SnWebApplication.Api.Sql.TokenAuth3].[dbo].Nodes where NodeId = 1) CreationDate,
	(select CreationDate from [SnWebApplication.Api.Sql.TokenAuth3].[dbo].Versions where VersionId = 1) VersionCreationDate,
	(select DynamicProperties from [SnWebApplication.Api.Sql.TokenAuth3].[dbo].Versions where VersionId = 1) DynamicProperties

USE [SnWebApplication.Api.Sql.TokenAuth]
PRINT db_name()
DBCC checkident ('AccessTokens')

USE [SnWebApplication.Api.Sql.TokenAuth2]
PRINT db_name()
DBCC checkident ('AccessTokens')

USE [SnWebApplication.Api.Sql.TokenAuth3]
PRINT db_name()
DBCC checkident ('AccessTokens')
