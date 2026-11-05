---
title: "ContentNamingProvider"
source_url: 'https://github.com/SenseNet/sensenet/docs/content-naming-provider.md'
category: Development
version: v6.0
tags: [content naming, naming, provider]
---

# ContentNamingProvider

As sensenet has a web interface, handling content names that appear in URLs is crucial. Document naming is also important in a system that provides document management as one of its main features. To learn more about content naming, please check out the [main article](content-naming.md). This article is for developers about how to customize the default naming behavior in sensenet.

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

### Optional name normalization

The following independent `sensenet:contentNaming` switches all default to `false`.
They affect `CharReplacementContentNamingProvider` only.

| Setting | Effect |
| --- | --- |
| `ApplyCustomReplacements` | Applies the configured literal replacement pairs in array order. |
| `RemoveDiacritics` | Removes decomposable Unicode diacritics using canonical decomposition. |
| `Transliterate` | Uses AnyAscii 0.3.3 to transliterate Unicode text to ASCII. |
| `Lowercase` | Converts letters using invariant casing. |

The pipeline is custom replacements, diacritic removal, transliteration, lowercase,
then the existing invalid-character replacement. Each step can be enabled separately.
When all four switches are absent or `false`, name generation remains unchanged.
Transliteration itself removes many diacritics. To preserve accents, disable both
`RemoveDiacritics` and `Transliterate` and avoid custom replacements that remove them.
A Settings veto disables its step; other enabled steps can still have overlapping effects.

The Docker webapps include prepared replacement pairs in their base `appsettings.json`
files. All four switches are explicitly `false`; the prepared list has no effect until
`ApplyCustomReplacements` is enabled. These are configuration values, not built-in
fallbacks in the provider or the configuration reader:

```json
{
  "sensenet": {
    "contentNaming": {
      "RemoveDiacritics": false,
      "Lowercase": false,
      "Transliterate": false,
      "ApplyCustomReplacements": false,
      "ReplacementSources": [
        "ä", "Ä", "ö", "Ö", "ü", "Ü", "ß", "ẞ",
        "æ", "Æ", "œ", "Œ", "ø", "Ø", "ł", "Ł",
        "đ", "Đ", "ð", "Ð", "þ", "Þ", "ħ", "Ħ",
        "ı", "ĳ", "Ĳ", "ŋ", "Ŋ", "ŧ", "Ŧ", "ſ", "ŀ", "Ŀ",
        "&", " "
      ],
      "ReplacementTargets": [
        "ae", "AE", "oe", "OE", "ue", "UE", "ss", "SS",
        "ae", "AE", "oe", "OE", "o", "O", "l", "L",
        "d", "D", "d", "D", "th", "TH", "h", "H",
        "i", "ij", "IJ", "n", "N", "t", "T", "s", "l", "L",
        "and", "-"
      ]
    }
  }
}
```

The list covers common additional Latin letters used in European languages and
includes German-style `ä -> ae`, `ö -> oe`, `ü -> ue`, `ß -> ss`, with uppercase
counterparts, plus `& -> and` and spaces to hyphens. It is a starting point to
customize for the repository's languages, not automatic language detection.
Every configured pair applies to every input when custom replacement is enabled.

- For Hungarian text, enable `RemoveDiacritics` and leave
  `ApplyCustomReplacements` disabled. No custom Hungarian accent pairs are needed.
- For German text, enable `ApplyCustomReplacements`. `RemoveDiacritics` can also
  be enabled for other decomposable accents: custom replacement runs first, so
  the German `ae`/`oe`/`ue` results are preserved.
- For mixed-language content, the operator chooses the switches and customizes
  the list. For example, the prepared `ö -> oe` and `ü -> ue` pairs also change
  Hungarian `tükör` to `tuekoer` when custom replacement is enabled.

With only `ApplyCustomReplacements` enabled, `Fähre & Straße schön kühl`
becomes `Faehre-and-Strasse-schoen-kuehl`. The uppercase input `ÄÖÜẞ` becomes
`AEOEUESS` with the prepared pairs.

With `RemoveDiacritics` and `Lowercase` enabled and custom replacement disabled,
`Árvíztűrő tükörfúrógép` becomes `arvizturo tukorfurogep`. Spaces remain under the
default invalid-character pattern; the existing `InvalidNameCharsPattern` and
`ReplacementChar` can be configured to replace whitespace with hyphens separately.
If custom replacement is enabled as well, the prepared German pairs instead
produce `arvizturo-tuekoerfurogep`.

`RemoveDiacritics` alone maps `ä/ö/ü` to `a/o/u`; it does not change `ß` to `ss`.
Other scripts can be handled by the separate `Transliterate` option.
The original stored `DisplayName` is preserved.

The replacement arrays are paired by index. Entries are case-sensitive literal
strings, not regular expressions, and can contain multiple characters. An empty
target string deletes a match. Sources must be non-empty strings and both arrays
must have the same length; invalid pairs throw when custom replacement is enabled.
If no replacement entries are loaded, the custom step does nothing.
Use string values, not JSON null values.
Pairs run in order and may cascade: `a -> b`, then `b -> c`, turns `ab` into `cc`.
Custom replacement runs first, so `ä -> ae` can override the usual `ä -> a`.

For Docker, the four switches are environment variables such as
`sensenet__contentNaming__Transliterate=true`. Array elements use consecutive,
zero-based indexes, for example `sensenet__contentNaming__ReplacementSources__0=ä`
and `sensenet__contentNaming__ReplacementTargets__0=ae`. Do not leave gaps: array
loading stops at the first missing index. Configuration providers merge array
entries by index: a shorter array in a later provider does not clear remaining
entries from the base configuration. Edit the complete paired list in the base
`appsettings.json` when removing prepared pairs. The host package/image must contain
these features; changing host configuration or replacement arrays requires a restart.

### Runtime Settings overrides

Operators can disable any configured step without restarting the repository by
saving the global `/Root/System/Settings/ContentNaming.settings` content. Use the
existing Settings content editing/management APIs and permissions. For example,
this disables only transliteration:

```json
{
  "Transliterate": false
}
```

Use the same four switch names in the settings file. Settings are checked during
name generation on the running repository and use the existing Settings cache
invalidation. The rules are:

| Host config | Settings value | Effective behavior |
| --- | --- | --- |
| `false` | Any value or absent | Disabled |
| `true` | Absent or `true` | Enabled |
| `true` | `false` | Disabled |

Settings cannot enable a feature disabled by host configuration. Removing the
override restores the configured behavior. Replacement arrays and the existing
`InvalidNameCharsPattern`/`ReplacementChar` are not overridden by these settings.
This is a global override; the naming API does not receive a content context path.

### Preserved behavior and Unicode scope

The stored `DisplayName`, input parameters, original extension handling, and name
validation are unchanged. An extension retained from `originalName` is appended
unchanged, even with normalization enabled (`NEW.TXT` with original name `old.TXT`
becomes `new.TXT` with `Lowercase`). Changing naming configuration or Settings does
not rename existing content; this is the standard content naming behavior.

The existing `InvalidNameCharsPattern` and `ReplacementChar` still control invalid
character and whitespace replacement. Custom replacements can replace a space
with `-` without changing the global validation pattern. Changing the existing
invalid-character pattern separately still affects repository-wide Name/Path validation.

`RemoveDiacritics` removes combining marks in all scripts after canonical
decomposition. It leaves `æ`, `ø`, `ß` and non-Latin letters that have no relevant
decomposition. `Transliterate` additionally handles such Latin letters (`Æ -> Ae`,
`Œ -> Oe`, `ß -> ss`) and other writing systems and symbols. AnyAscii works without
language context; its output is not guaranteed to match language-specific spelling.
It can expand symbols, introduce punctuation, or remove unsupported characters.
The existing invalid-character replacement runs afterward. Transliteration does
not lowercase ASCII letters; the separate `Lowercase` option controls that.

Different input names can produce the same result or an empty name. Existing
duplicate-name handling and validation apply. AnyAscii is distributed under the
ISC license; its notice is included in the package and copied to output/publish.

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
