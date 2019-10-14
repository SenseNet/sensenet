---
title: "Blob provider"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/blob-provider.md'
category: Development
version: v7.0.0
tags: [enterprise, blob, storage, provider, sn7]
description: This article describes the concept of our blob storage and the customization options.
---

# Blob provider

In **sensenet** all files are stored in the main [Content Repository](content-repository.md) database by default. All binaries, along with their metadata. In larger projects this can lead to a huge database, which requires a *large data storage* and sometimes additional *server licenses*. To aid this scenario sensenet allows you to **store binaries outside of the database**, in an external storage. This article describes the concept of our blob storage and the customization options. Available blob providers for the *Enterprise Edition*:

-   [MongoDB blob provider](https://community.sensenet.com/docs/mongodb-provider)
-   [Azure blob provider](https://community.sensenet.com/docs/azureblob-provider) *(soon to be released)*

## Blob storage concept

sensenet is a lot more than a simple document storage. We offer a rich set of additional features like storing different metadata for different types of content and indexing binaries to aid field-specific or full-text search. All this additional data will have to remain in the main database (and in our index), but we wanted to let customers have the option to store their binaries outside of the database. The sensenet **blob storage** has a very simple purpose: storing binary data that can be linked to our records in the main database. No additional, high-level features, no custom metadata, only raw binaries, so that we can keep 3rd party implementations **simple**.

## Blob providers

A blob provider is the component in sensenet that is responsible for all binary operations. The **built-in blob provider** stores binaries in the database. We offer a simple interface for developers to implement external blob providers. These providers may store binaries in a completely different environment - e.g. in a file system or in a nosql solution (like MongoDB).

Currently there is a simple **selector algorithm** for choosing the appropriate provider when saving a file: if the size of the file is smaller than a certain amount, the default built-in provider will store it into the database. If it is bigger and there is an external blob provider configured, it will use that. This means that currently the system can work with a single external blob provider. Later this selector algorithm will be able to decide using a more complex and customizable algorithm.

Upper layers do not know anything about the underlying storage: the Content binary API is unified, regardless of the blob provider currently in use.

### Migration

By default everything is stored in the database. When you create a custom external blob provider (and define it in the configuration), from then on *new files* will be saved into that external storage. If you want your old files to be moved to the new storage, you either have to wait for them to be migrated when their binary is saved again, or you'll have to create a tool (preferably an SnAdmin package) that iterates through the files and saves them programmatically.

### Backup

Backup strategy depends on the characteristics of the custom external provider, but it is advisable to create a backup mechanism that synchronizes the backup of the index, the main database and the blob storage so that everything remains in sync.

### Writing chunks

One of the few important capabilities of blob providers is that they have to support **writing binaries in chunks**. This means it is possible to write a huge file in chunks into the storage in **random order, even in parallel** requests to speed up the process.

### Multiple web servers

An external blob provider has to support **NLB** environments, when multiple web servers are accessing the same storage at the same time for read and write operations.

### Accessing binaries

The blob storage should not allow read and write operations outside of the sensenet API. All modifications should be performed through the **blob storage API** that we publish (e.g. users must not modify files stored in the file system *manually*).

If you are working in the context of the sensenet Content Repository, you do not have to think about binaries, we handle that in the background (e.g. when you create a File from code). But in case of external tools (e.g. a custom importer or synchronizer tool) developers may use the blob storage API to read and write binaries directly, without having to send them through the REST API of the portal. For details please check the following article:

-   [How to access the blob storage directly](https://community.sensenet.com/docs/tutorials/how-to-access-the-blob-storage-directly)

## Built-in blob provider

The built-in blob provider will always be there as a fallback. Currently it supports storing files in the database in a regular *varbinary* column.

## Custom blob provider

It is possible to implement a custom blob storage provider that sends files to external storage. For a sample implementation (a local file storage provider) check the following article:

- [How to create an external blob provider](https://community.sensenet.com/docs/tutorials/how-to-create-an-external-blob-provider)

### Configuration

When defining a custom provider you have to provide two values in the configuration:

``` xml
<sensenet>
   <blobstorage>
      <add key="BlobProvider" value="MyProject.Data.CustomBlobProvider"/>
      <add key="MinimumSizeForBlobProviderKB" value="500"/>
   </<blobstorage>>
</sensenet>
```

The *BlobProvider* value defines the single external provider that is used when the file size is bigger than the value defined as the *MinimumSizeForBlobProviderKB*. The default minimum size is 500 kbytes.
