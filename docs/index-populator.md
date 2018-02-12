---
title: "Index Populator"
source_url: 'https://github.com/SenseNet/sensenet/docs/index-populator.md'
category: Development
version: v7.0
tags: [indexing, search, tools]
---

# Index Populator

Making full index of content repository content is one of the most important tasks of the installation procedure of the sensenet Content Repository. This feature is used frequently during development too. The tool that can be used to create a full index is the Index Populator.

Index Populator is a standalone tool that has two files:

- IndexPopulator.exe
- IndexPopulator.exe.config

## Usage

The tool has three optional parameters:

- **SOURCE**: sensenet Content Repository path as the root. Default: "/Root".
- **INDEX**: Location of Lucene index directory. Default: depends on the configuration (web.config/app.config).
- **ASM**: FileSystem directory containig the required assemblies. Default: location of IndexPopulator.exe.
- **NOBACKUP**: Disables the backup of the new index to the database.

```diff
Do not use the -SOURCE parameter. This tool can work properly only in the full index population mode.
```

```diff
If you have a huge Lucene index (the compressed folder is bigger than 500MB), you should consider switching off automatic index backup. In this case you should use the NOBACKUP parameter above to prevent the index populator to make a database backup. For index backup strategy for huge index, please visit the [Backup tool article](backup-tool.md).
```

## How it works

The tool starts a new repository instance in a new .NET appdomain. It uses the last subdirectory in the configured LuceneIndex directory and does not create a new one. In the starting sequence the configured index directory will be released, which means writer.lock file will be deleted. The writer.lock file can be successfully deleted if its directory currently is not used by another sensenet instance or any Lucene IndexWriter. After the tool is started, all files in the directory will be deleted. After that the tool starts the index generating with reading the database. When it is finished, all indexing activities will be deleted in the database, the index will be optimized and backed up into the database.

```diff
You must ensure that the database does not change during the index generation. After the generation is finished the full index must be copied to all NLB nodes.
```

## Configuration

The configuration of the Index populator tool (*IndexPopulator.exe.config*) is similar to the web.config but there is an important difference: MSMQ must be switched off (*ClusterChannelProvider* and *MsmqChannelQueueName* keys) but indexing must be switched on (*EnableOuterSearchEngine* and *IndexDirectoryPath* keys).

## Example

Index population if the IndexPopulator.exe is started from the web folder's bin directory and index directory is correctly configured:

```bash
IndexPopulator.exe
```

Use the -INDEX parameter if the target index is different from the configured:

```bash
IndexPopulator.exe -INDEX C:\MovedIndex
```

Use the -ASM parameter if the sensenet's assemblies are not in the starting directory:

```bash
IndexPopulator.exe -ASM C:\Dev10\SenseNet\Development\Budapest\Source\SenseNet\WebSite\bin
```