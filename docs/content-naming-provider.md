---
title: "ContentNamingProvider"
source_url: 'https://github.com/SenseNet/sensenet/docs/content-naming-provider.md'
category: Development
version: v6.0
tags: [content naming, naming, provider]
---

# ContentNamingProvider

As sensenet ECM has a web interface, handling content names that appear in URLs is crucial. Document naming is also important in a system that provides document management as one of its main features. To learn more about content naming, please check out the [main article](content-naming.md). This article is for developers about how to customize the default naming behavior in sensenet ECM.

All content naming operations are done through a content naming provider. The base provider is the abstract *SenseNet.ContentRepository.ContentNamingProvider* class. It has the following customizable features:

- creating and validating content names
- converting a display name to a 'url-friendly' name
- handling incremental suffixes in names

These features are called by system methods and can be customized by developers by simply inheriting from the base class. The active provider is selected in the configuration (see below).

> If you are interested in customizing how the name of a *downloaded* file looks like, please check out the [Document binary provider](document-binary-provider.md) article.

## Built-in naming providers

We offer the following predefined naming providers. You may choose either one of them or create a custom one based on their behavior (and source code :)).

## CharReplacementContentNamingProvider

**This is the default naming provider.** Contains a customized *GenerateNameFromDisplayName* method that replaces invalid characters with a **single replacement character**. Invalid characters and the replacement character are configurable in the **sensenet/contentNaming** section in the web.config.

- **InvalidNameCharsPattern** (see details in the [Content naming article](content-naming.md))
- **ReplacementChar**: a single character that will be used as a replacement character. Default is the '-' character.

Duplicated 'ReplacementChar' characters are replaced by a single character (so after the conversion the name may contain less characters than the display name). This conversion is very simple but there is a chance of non-unique name creation. The original file extension will be kept.

## Underscore5FContentNamingProvider

Contains a customized *GenerateNameFromDisplayName* method that encodes the display name. The encoding works with the standard UrlEncode .Net method, but the percent sign ('%') will be replaced with an underscore ('_') character. The names that are generated from unique names are guaranteed to remain unique. After the conversion, the name may contain more characters than the input name. The original file extension will be kept. Invalid characters are configurable in the **sensenet/contentNaming** section in the web.config.

- **InvalidNameCharsPattern** (see details in the [Content naming article](content-naming.md))

## Custom naming provider

In this section, we list all the API methods that you may override in your custom provider.

### Creating a new name

This method should create a valid name from the base name and an associated ContentType that may describe the expected extension. In our implementation, the name base will be supplemented by the extension that is described in the provided content type. In inherited custom providers it is possible to write a more sophisticated name generation algorithm using the content type and the parent content instance.

```csharp
protected virtual string GenerateNewName(string nameBase, ContentType contentType, Node parent)
```

### Conversion from displayname

This is the only abstract method of the provider. Generates a valid name from a human-readable display name. The generated name is only a hint, because the user may overwrite it on the UI. The conversion needs to be as fast as possible because the [DisplayName field control](displayname-field-control.md) uses it frequently from the client-side.

```csharp
protected abstract string GenerateNameFromDisplayName(string originalName, string displayName);
```

### Validating name

When a content gets saved, this method is responsible for validating the content name. In our implementations, this method checks the forbidden characters in the content's name and if finds one it throws an InvalidPathException. This check uses a regex pattern that is configured in the web.config (see the main [Content naming article](content-naming.md) for examples). Inherited providers can customize this behavior in the following method:

```csharp
protected virtual void AssertNameIsValid(string name)
```

### Incremental suffix handling

The provider can create and increment a numbered suffix in the name (e.g. MyContent(12).doc) in case the name is already taken (see [incremental naming](content-naming.md) for details. There are two customizable methods for this feature:

#### Getting name and suffix separately

This method recognizes the existing suffix (an integer in parentheses) and separates the name and the number.

```csharp
protected virtual int GetNameBaseAndSuffix(string name, out string nameBase)
```

The return value is the number and the output parameter contains the name base. The passed name must not contain the file extension: it needs to be cut off before. The number will be 0 if the suffix does not exist.

#### Increasing suffix

There are two ways to increment a suffix. If the parent content is not known, the next suffix value is calculated based on the existing value so it will be the old value + 1. This value may not be valid because the target content may have a child with such a name. A known parent helps the algorithm because (in our implementation) it uses a database query to explore the highest suffix with the same name.

```csharp
protected virtual string GetNextNameSuffix(string currentName, int parentNodeId = 0)
```

## Configuring the naming provider

The active naming provider class needs to be selected in the configuration (e.g. web.config) file. In the *sensenet/providers* section you may use the *ContentNamingProvider* key for providing the fully qualified name of the target class. If the key does not exist, the default provider is the *SenseNet.ContentRepository.CharReplacementContentNamingProvider*.

```csharp
<sensenet>
  <providers>
    ...
    <add key="ContentNamingProvider" value="SenseNet.ContentRepository.Underscore5FContentNamingProvider" />
    ...
  </providers>
</sensenet>
```
