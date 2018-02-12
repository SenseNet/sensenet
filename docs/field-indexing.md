---
title: "Field Indexing"
source_url: 'https://github.com/SenseNet/sensenet/docs/field-indexing.md'
category: Development
version: v6.0
tags: [field, indexing, index, content type, content type definition]
---

# Field Indexing

For every [Content](content.md) the [Field](field.md) values can be indexed so that when searched for a Field value the corresponding Content will appear in the result set. It is also possible to search in Fields by explicitely defining the Field whose values are to be searched within a query. The way a specific Field of a Content is indexed is defined in the [CTD Field Definition](ctd.md).

> It is possible to switch off indexing for certain content types. In that case nobody will be able to find the instances of those content types using [Content Query](content-query.md), but the index will be smaller. For more details, see the Index description in the Content Type Definition article.

The portal uses the [Lucene search engine](http://lucenenet.apache.org/) for indexing of the [Content Repository](content-repository.md) and to provide a fast mechanism for returning query results. Apart from the indexing of some basic built-in properties every Field can be configured to be indexed separately.

### Indexing and storing

There are two ways to put Field data information in the index: by indexing and by storing. Indexing means that an analyzer processes Field data, it resolves to data to terms and the Content ID is stored under the corresponding term making it possible to search for terms to get the Content. Storing means that Field data itself can be stored in the index for a Content (for example the base system stores path in the index to allow convenient programming). Indexing and storing is independent of each other, they can both be switched on and off regardless of the state of the other.

### Analyzers

The goal of an analyzer is to extract all relevant terms from a text, filtering stopwords etc. It is important that the same analyzer is used in the indexing process and the query building. For example your document contains the following text: „Writing Sentences” and your query text is „writing”. After analysis the indexed text and search text will be these: „writing, sentences” and „writing”. This method ensures that the original text can be found even if the query word typed in and the word in the original text do not match exactly char-by-char. We use a *PerFieldAnalyzerWrapper* that can support a unique analyzer for every Field. Analyzer-Field bindings are defined in the CTD. Field without analyzer-binding will be analyzed with the default analyzer: *KeywordAnalyzer*.

### Stop-word dictionary

Some of the built-in analyzers (*StandardAnalyzer* and *StopAnalyzer*) use a stop-word dictionary to exclude certain words that will not be indexed as terms. For example when indexing written English texts it is useful not to index the word the, as it is usually irrelevant in relation to the text content. Besides, searching for *the* would come up with results including Content containing any written English text. The built-in stop-word dictionary uses the following words:

```txt
"a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", 
"on", "or", "such", "that", "the", "their", "then", "there", "these", "they", "this", "to", "was", "will", "with"
```

> Custom stop-word dictionaries are not yet supported.

### Indexing definition

Below you can see the skeleton of a Field definition with indexing definition included:

```xml
    <Field name="" type="">
      ...
      <Indexing>
         <Mode></Mode>
         <Store></Store>
         <TermVector></TermVector>
         <Analyzer></Analyzer>
         <IndexHandler></IndexHandler>
      </Indexing>
    </Field>
```

You can configure the indexing and storing mode, analyzer, and the association of Field IndexHandler in every Field. Indexing configuration is an optional xml element, with name Indexing, under the Field element after the Bind element (if defined) and before the Configuration element (if defined). Indexing element can contain the following sub elements in this order: *Mode*, *Store*, *TermVector*, *Analyzer*, *IndexHandler*. All elements are optional because all elements have default values.

### Mode

Indexing mode settings (refer to [http://lucene.apache.org/java/2_9_4/api/core/org/apache/lucene/document/Field.Index.html](http://lucene.apache.org/java/2_9_4/api/core/org/apache/lucene/document/Field.Index.html)). Available values:

- **Analyzed** (default): Field will be analyzed with the given analyzer (see later)
- **AnalyzedNoNorms**
- **No**: Field is not indexed at all
- **NotAnalyzed**: Field is indexed without analyzing
- **NotAnalyzedNoNorms**

> This setting is only available to make it easier to configure the indexing subsystem, default install only uses *Analyzed* and *No* settings.

### Store

The native Field value storage in the index can be switched on or off (refer to [http://lucene.apache.org/java/2_9_4/api/core/org/apache/lucene/document/Field.Store.html](http://lucene.apache.org/java/2_9_4/api/core/org/apache/lucene/document/Field.Store.html)). Available values:

- **No** (default)
- **Yes**

### Term vector

Term vector settings (refer to [http://lucene.apache.org/java/2_9_4/api/core/org/apache/lucene/document/Field.TermVector.html](http://lucene.apache.org/java/2_9_4/api/core/org/apache/lucene/document/Field.TermVector.html)). Available values:

- **No** (default)
- **WithOffsets**
- **WithPositions**
- **WithPositionsOffsets**
- **Yes**

> This setting is only available to make it easier to configure the indexing subsystem, default install only uses default setting.

### Analyzer

You can associate any Lucene Analyzer to a Field (refer to: [http://lucene.apache.org/java/2_9_4/api/core/org/apache/lucene/analysis/Analyzer.html](http://lucene.apache.org/java/2_9_4/api/core/org/apache/lucene/analysis/Analyzer.html)). The element value is the fully qualified type name of the desired Lucene analyzer. Available analyzers:

- Lucene.Net.Analysis.Standard.**StandardAnalyzer**: an analyzer created specifically for text sentences/words. It excludes punctuations, splits up input string to words, gets each word in lower case and uses a stop-word dictionary for exclusions to lower false hit rates (for example you cannot query for articles like 'a' or 'the'). Use this whenever written text is stored in a Field that needs to be queried to individual words.
- Lucene.Net.Analysis.**KeywordAnalyzer**: (default) Trims the input string (removes whitespaces from the beginning and the end) and treats the input as a whole expression, as given - it does not even transform the input to lower case. It is useful for Fields holding processable string data, that needs to be searched as is.
- Lucene.Net.Analysis.**SimpleAnalyzer**: splits input string along whitespaces and punctuations, and even along numbers.
- Lucene.Net.Analysis.**StopAnalyzer**: similar to SimpleAnalyzer but also uses stop-word dictionary to exclude words.
- Lucene.Net.Analysis.**WhitespaceAnalyzer**: splits input string along whitespaces, but not along punctuations.

The built-in standard analyzer is based upon the English language. Please note, that when using the system in different language environments it is reasonable to develop a custom analyzer with stop-word dictionary (and optionally a stemmer) specialized for the given language.

```diff
Warning! Be very careful when providing an analyzer classname for a Field. Misspelled classnames may result in system crash!
```

Only one analyzer can be bound to a specific Field, that is this setting cannot be overridden. Changing an analyzer for a Field can only be done at the topmost level the Field is defined. To change an analyzer first re-register the CTD with omitted analyzer settings:

```xml
    <Field name="MyKeywords" type="LongText">
      <DisplayName>MyKeywords</DisplayName>
      <Indexing>
      </Indexing>
    </Field>
```

After registration you may provide the new analyzer settings and reinstall the CTD:

```xml
    <Field name="MyKeywords" type="LongText">
      <DisplayName>MyKeywords</DisplayName>
      <Indexing>
        <Analyzer>Lucene.Net.Analysis.Standard.StandardAnalyzer</Analyzer>
      </Indexing>
    </Field>
```

```diff
Warning! Changing an analyzer for a Field is only valid in development time, it should not be carried out on live portals! After changing an analyzer the affected Content should be saved and reindexed - a full index repopulation is highly recommended!
```

### IndexHandler

Every content Field has an association with a FieldIndexHandler that generates the indexable value from the Field's value. This association is configurable with the IndexHandler element. The element value is the fully qualified type name of the desired *FieldIndexHandler*. Default depends on the Field's [Field Setting](field-setting.md). The master default is the *LowerStringIndexHandler* (if a Field Setting does not override the CreateDefaultIndexFieldHandler method). Available built-in Field index handlers and their usages:

- SenseNet.Search.**NotIndexedIndexFieldHandler**: Password, UrlList, Color, Image, Lock, Security, SiteRelativeUrl, WhoAndWhen fields. These are not indexed.
- SenseNet.Search.**LowerStringIndexHandler**: this is the default index field handler.
- SenseNet.Search.**BooleanIndexHandler**: Boolean fields.
- SenseNet.Search.**IntegerIndexHandler**: Integer fields (Id, VersionId, Index fields and so on).
- SenseNet.Search.**NumberIndexHandler**: Number fields.
- SenseNet.Search.**DateTimeIndexHandler**: DateTime fields.
- SenseNet.Search.**LongTextIndexHandler**: LongText fields.
- SenseNet.Search.**BinaryIndexHandler**: Binary fields.
- SenseNet.Search.**HyperLinkIndexHandler**: HyperLink fields.
- SenseNet.Search.**ChoiceIndexHandler**: Choice fields.
- SenseNet.Search.**ReferenceIndexHandler**: Reference fields.
- SenseNet.Search.**ExclusiveTypeIndexHandler**: Type field.
- SenseNet.Search.**TypeTreeIndexHandler**: TypeIs field.
- SenseNet.Search.**InFolderIndexHandler**: InFolder field.
- SenseNet.Search.**InTreeIndexHandler**: InTree field.

## Indexing of built-in properties

The following is a list of the properties that are indexed regardless of Field indexing settings:

- **NodeId**: (node.Id) the identifier number of the Content.
- **VersionId**: (node.VersionId) version id of the Content.
- **Version**: (node.Version) version string of the Content (in the form of V*major*.*minor*.*status*).
- **CreatedById**: (node.CreatedById) id of the creator user Content of the Content.
- **ModifiedById**: (node.ModifiedById) id of the last modifier user Content of the Content.
- **NodeTimestamp**: (node.NodeTimestamp) 8 byte auto incremented timestamp for optimistic concurrency control.
- **VersionTimestamp**: (node.VersionTimestamp) 8 byte auto incremented timestamp for optimistic concurrency control.
- **IsInherited**: (node.IsInherited) value indicating whether the default permissions of this instance are inherited from its parent.
- **IsMajor**: (node.Version.IsMajor) true if the instance represents a major version (eg. 2.0).
- **IsPublic**: value indicating that version status (node.Version.Status) is *Approved*.
- **AllText**: the concatenated text extract of Content Field values. Format of a text extract of a Field is defined by the type of IndexHandler (ie.: HyperLinkIndexHandler returns the hyperlink's href, target, text and title attributes' concatenation). This technical Field is analyzed by *StandardAnalyzer*, and query texts are interpreted as queries in this Field when no query Field is selected.
- **Path**: (node.Path) path of the Content.
- **ParentId**: (node.ParentId) id of the parent Content of the Content.
- **IsLastDraft**: value indicating that Content is last public version and it's status is public (node.IsLastPublicVersion && node.Version.Status == VersionStatus.Approved).
- **IsLastPublic**: (node.IsLatestVersion) value indicating that the Content is the last version (version ID equals to the last minor version ID).

## for Developers

The indexing of content is carried out in two steps: first an IndexDocument data is created and stored in the database when the content is saved. After that, this IndexDocument data is used to include the analyzed data in the index. This two-step procedure allows fast creation of the index using the [Index Populator](index-populator.md). Please bear in mind though, that when changing field index configuration the IndexDocuments are not automatically regenerated, so running the Index Populator after configuration change will lead to the index being created according to previous settings. To overcome this you could manually save each affected content or use the following API to regenerate the IndexDocument data:

```csharp
var popu = StorageContext.Search.SearchEngine.GetPopulator();
popu.RefreshIndexDocumentInfo(node, false);
```

## Indexing binaries

Binary fields are special fields that hold the actual content of a file. Indexing these kinds of fields depend on the type of the file (e.g. pdf files need a different algorithm than docx files). For more information about extracting text and the customization possibilities please visit the following article:

- [Text extractors](text-extractors.md)

## Example

### Disabling Field indexing

The following example shows an indexing configuration that disables the indexing of the field:

```xml
  <Field name="Versions" type="Reference">
    <Title>Versions</Title>
    <Description>Content version history</Description>
    <Indexing> <!-- Indexing configuration -->
      <Mode>No</Mode>
      <Store>No</Store>
    </Indexing>
    ...
  </Field>
```

### Using StandardAnalyzer

```xml
    <Field name="MyKeywords" type="LongText">
      <DisplayName>MyKeywords</DisplayName>
      <Indexing>
        <Analyzer>Lucene.Net.Analysis.Standard.StandardAnalyzer</Analyzer>
      </Indexing>
    </Field>
```

Enter the following text into the *MyKeywords* Field:

```txt
the testing tesT2 and test3/test4;test5
```

The following terms will be present in the index:

| Fields | Text |
|--------|------|
| MyKeywords | testing |
| MyKeywords | test2 |
| MyKeywords | test3/test4 |
| MyKeywords | test5 |

Different queries will return the following results:

```txt
MyKeywords:test
Result count: 0
 
MyKeywords:test*
Result count: 1
 
MyKeywords:testing
Result count: 1
 
MyKeywords:testING
Result count: 1
 
MyKeywords:test2
Result count: 1
 
MyKeywords:test3
Result count: 0
 
MyKeywords:test3*
Result count: 1
 
MyKeywords:test3/test4;test5
Result count: 1
 
MyKeywords:test3/test4;testing
Result count: 0
 
MyKeywords:tested
Result count: 0
 
MyKeywords:"testing tesT2 test3/test4;test5"
Result count: 1
```

### Using KeyWordAnalyzer

```xml
    <Field name="MyKeywords" type="LongText">
      <DisplayName>MyKeywords</DisplayName>
      <Indexing>
        <Analyzer>Lucene.Net.Analysis.KeywordAnalyzer</Analyzer>
      </Indexing>
    </Field>
```

Enter the following text into the *MyKeywords* Field:

```txt
the testing tesT2 and test3/test4;test5
```

The following terms will be present in the index:

| Fields | Text |
|--------|------|
| MyKeywords | the testing tesT2 and test3/test4;test5 |

Different queries will return the following results:

```txt
MyKeywords:the
Result count: 0
 
MyKeywords:the*
Result count: 1
 
MyKeywords:testing
Result count: 0
 
MyKeywords:testING
Result count: 0
 
MyKeywords:test2
Result count: 0
 
MyKeywords:test3
Result count: 0
 
MyKeywords:test3*
Result count: 0
 
MyKeywords:*test3*
Result count: 1
 
MyKeywords:test3/test4;test5
Result count: 0
 
MyKeywords:"the testing tesT2 and test3/test4;test5"
Result count: 1
```

### Using SimpleAnalyzer

```xml
    <Field name="MyKeywords" type="LongText">
      <DisplayName>MyKeywords</DisplayName>
      <Indexing>
        <Analyzer>Lucene.Net.Analysis.SimpleAnalyzer</Analyzer>
      </Indexing>
    </Field>
```

Enter the following text into the *MyKeywords* Field:

```txt
the testing tesT2 and test3/test4;test5
```

The following terms will be present in the index:

| Fields | Text |
|--------|------|
| MyKeywords | test |
| MyKeywords | testing |
| MyKeywords | the |
| MyKeywords | and |

Different queries will return the following results:

```txt
MyKeywords:test
Result count: 1
 
MyKeywords:test*
Result count: 1
 
MyKeywords:testing
Result count: 1
 
MyKeywords:testING
Result count: 1
 
MyKeywords:test2
Result count: 1
 
MyKeywords:test4334
Result count: 1
 
MyKeywords:tester
Result count: 0
 
MyKeywords:*test3*
Result count: 0
 
MyKeywords:test3/test4;test5
Result count: 1
 
MyKeywords:"the testing tesT2 and test3/test4;test5"
Result count: 1
```

Just to make it clearer: enter the following text into the *MyKeywords* Field:

```txt
helo12bye
```

The following terms will be present in the index:

| Fields | Text |
|--------|------|
| MyKeywords | helo |
| MyKeywords | bye |

### Using StopAnalyzer

```xml
    <Field name="MyKeywords" type="LongText">
      <DisplayName>MyKeywords</DisplayName>
      <Indexing>
        <Analyzer>Lucene.Net.Analysis.StopAnalyzer</Analyzer>
      </Indexing>
    </Field>
```

Enter the following text into the *MyKeywords* Field:

```txt
the testing tesT2 and test3/test4;test5
```

The following terms will be present in the index:

| Fields | Text |
|--------|------|
| MyKeywords | testing |
| MyKeywords | test |

Different queries will return the following results:

``txt
MyKeywords:test
Result count: 1
 
MyKeywords:test*
Result count: 1
 
MyKeywords:testing
Result count: 1
 
MyKeywords:testING
Result count: 1
 
MyKeywords:test2
Result count: 1
 
MyKeywords:test4334
Result count: 1
 
MyKeywords:tester
Result count: 0
 
MyKeywords:*test3*
Result count: 0
 
MyKeywords:test3/test4;test5
Result count: 1
 
MyKeywords:"the testing tesT2 and test3/test4;test5"
Result count: 1
```

### Using WhitespaceAnalyzer

```xml
    <Field name="MyKeywords" type="LongText">
      <DisplayName>MyKeywords</DisplayName>
      <Indexing>
        <Analyzer>Lucene.Net.Analysis.WhitespaceAnalyzer</Analyzer>
      </Indexing>
    </Field>
```

Enter the following text into the *MyKeywords* Field:

```txt
the testing tesT2 and test3/test4;test5
```

The following terms will be present in the index:

| Fields | Text |
|--------|------|
| MyKeywords | testing |
| MyKeywords | tesT2 |
| MyKeywords | test3/test4;test5 |
| MyKeywords | the |
| MyKeywords | and |

```txt
MyKeywords:test
Result count: 0
 
MyKeywords:test*
Result count: 1
 
MyKeywords:testing
Result count: 1
 
MyKeywords:testING
Result count: 0
 
MyKeywords:test2
Result count: 0
 
MyKeywords:tesT2
Result count: 1
 
MyKeywords:test3
Result count: 0
 
MyKeywords:test3*
Result count: 1
 
MyKeywords:test3/test4;test5
Result count: 1
 
MyKeywords:the
Result count: 1
```