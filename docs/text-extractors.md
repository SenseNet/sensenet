---
title: "Text extractors"
source_url: 'https://github.com/SenseNet/sensenet/docs/text-extractors.md'
category: Development
version: v6.0
tags: [content, ctd, child types, field]
---

# Text extractors

In an enterprise-grade content management solution one of the most important features is indexing documents to let users search for (and find) uploaded files. sensenet is able to index not only metadata of documents (like creation date or author) but the **binary content** itself. The latter is called text extracting. This article was written for developers who want to understand the mechanism behind binary indexing and want to create a custom text extractor for a particular file type.

## Text extractors

The algorithm used for extracting text from a binary depends on the type of the file - e.g. we need a different algorithm for extracting text from docx files than from pdfs. sensenet uses a provider approach to solve this task: every file type (extension) has its own text extractor class.

> At application start the system creates an event log entry that contains the loaded text extractors. You may check that list whether the system successfully loaded your custom text extractor or not.

## ITextExtractor

Text extractor classes should implement a simple interface to expose their functionality to the system. The interface contains the following methods and properties:

```csharp
public interface ITextExtractor
{
    /// <summary>
    /// Extracts all relevant text information from a stream.
    /// </summary>
    /// <param name="stream">Input stream</param>
    /// <param name="context">Context information (e.g. version id)</param>
    /// <returns>Extracted text</returns>
    string Extract(Stream stream, TextExtractorContext context);
 
    /// <summary>
    /// If the text extractor is considered slow, it will be executed outside of the
    /// main indexing database transaction to make the database server more responsive.
    /// It will mean an additional database request when the extracting is finished.
    /// </summary>
    bool IsSlow { get; }
}
```

## Built-in text extractors

The following list contains the list of built-in text extractors:

- **doc**: SenseNet.Search.DocTextExtractor
- **docm**: SenseNet.Search.DocxTextExtractor
- **docx**: SenseNet.Search.DocxTextExtractor
- **msg**: SenseNet.Search.MsgTextExtractor
- **pdf**: SenseNet.Search.PdfTextExtractor 
   - In the **Enterprise Edition**: AsposePreviewProvider.AsposePdfTextExtractor
- **pptx**: SenseNet.Search.PptxTextExtractor
- **rtf**: SenseNet.Search.RtfTextExtractor
- **txt**: SenseNet.Search.PlainTextExtractor
- **xls**: SenseNet.Search.XlsTextExtractor
- **xlb**: SenseNet.Search.XlbTextExtractor
- **xlsm**: SenseNet.Search.XlsxTextExtractor
- **xlsx**: SenseNet.Search.XlsxTextExtractor
- **xml**: SenseNet.Search.XmlTextExtractor
- **contenttype**: SenseNet.Search.XmlTextExtractor

## Custom text extractor

The following example shows how you can create a custom text extractor for the imaginary 'abc' file type. In the *Extract* method you can read the binary and extract the text using the appropriate algorithm. For deploying the plug-in please check the *Settings* section below.

```csharp
public class ABCTextExtractor : TextExtractor
{
    public override string Extract(Stream stream, TextExtractorContext context)
    {
        var text = string.Empty;
 
        using (var sr = new StreamReader(stream))
        {
            // read the stream and extract text
        }
 
        return text;
    }
}
```

## Slow text extraction

There are cases when the text extracting operation is relatively slow. To make the system more robust we allowed these kind of slow extractors to work outside of the main indexing transaction. This means all other fields of the content will be indexed and the index document will be saved into the database inside a transaction, and when the text extractor finished its work, another database request will be made to update the index document.

To control this behaviour, please override the **IsSlow** property in your custom text extractor. The default value of this property in the base TextExtractor class is TRUE.

```diff
Please note that this will not make the content save operation faster, as everything still happens synchronously. The gain is shorter locks on the SQL server, the drawback is an additional SQL request. See the next section for other possibilities.
```

## Asynchronous text extraction

If the text extractor can be considered really slow (this is the case with pdf files for example), you may decide to execute the extraction completely asynchronously. In this case you should return an empty string in the Extract method, and instead of making the extraction synchronously, you should simply start a new Task with the extractor code. As the last step of that task you will need to inject the extracted text into the system manually using the following code:

```csharp
IndexingTools.AddTextExtract(context.VersionId, text);
```

The code snippet above does all the necessary database, index and cache operations that are needed to complete the indexing of the content.

> In this case please state that your text extractor is NOT slow (using the IsSlow property). This may sound contradictory but it will tell the system that we do not want to execute a second indexdocument update automatically, we want to do it manually as shown above.

```csharp
public class ABCTextExtractor : TextExtractor
{
    public override bool IsSlow { get { return false; } }
 
    public override string Extract(Stream stream, TextExtractorContext context)
    {
        Task.Run(() =>
        {
            var text = string.Empty;
 
            using (var sr = new StreamReader(stream))
            {
                // read the stream and extract text
            }
 
            IndexingTools.AddTextExtract(context.VersionId, text);
        });
 
        return string.Empty;
    }
}
```

## Settings

To add a new text extractor (like the custom one above) or override an existing one you will need to configure the file type and the extractor class in the following [settings content](settings.md).

- */Root/System/Settings/Indexing.settings*

Every extension should have its own extractor entry (full class name) but of course different extensions may use the same extractor.

```json
{
	TextExtractors: {
		"abc": "MyNamespace.ABCTextExtractor",
		"def": "MyNamespace.DEFTextExtractor"
	}
}
```
