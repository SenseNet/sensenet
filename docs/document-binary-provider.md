---
title: "Document binary provider"
source_url: 'https://github.com/SenseNet/sensenet/docs/document-binary-provider.md'
category: Development
version: v6.0
tags: [document, binary, provider]
---

# Document binary provider

sensenet ECM is able to store huge amounts of documents in the [Content Repository](content-repository.md) and serve them to clients. In most cases, these are simple files with one binary field - e.g. Office documents or PDF files. There are cases however when the stored binary is not enough or certain adjustments must be made on the binary before serving it. This article describes how can you customize the binary content before serving it to the client.

## Document binary provider

When a client sends a request to the server for a file, the server first finds the content (the file) in the Content Repository. Then passes it to the Document binary provider along with the name of the requested field - which is in most cases the default Binary field. The binary provider can decide how the binary value should be served or what additional tasks should be completed when accessing the binary (e.g. logging). This always happens on-the-fly, when the binary is accessed, so please make these operations efficient and fast.

## Built-in default binary provider

sensenet ECM has a default binary provider which is sufficient in most cases. It simply serves the stored binary value of the specified field.

## Custom binary provider

You can create your own binary provider if you want to change the default behavior. This module is provider-based, which means you can create your own provider by inheriting from the *DefaultDocumentBinaryProvider* class and simply placing your dll into the bin directory of the website. The system will recognize your provider automatically and will use it to serve binaries.

In your custom binary provider you have to implement the following methods:

- **GetStream**: returns a binary stream that the system will serve and two out parameters (the content type and file name for the file). In this method, you may check for certain parameters or environment status and serve a custom binary (e.g. contents of another field or a modified stream) or **fall back to the default behavior**.
- **GetFileName**: returns the file name to put into the *Content-Disposition* response header.

## Example

```csharp
public class CustomBinaryProvider : DefaultDocumentBinaryProvider
{
    internal static bool IsFeatureEnabled { get; set; }
 
    public override Stream GetStream(Node node, string propertyName, out string contentType, out BinaryFileName fileName)
    {
        var file = node as File;
 
        if (!IsFeatureEnabled || file == null || file.Binary.FileName.Extension != "jpg")
            return base.GetStream(node, propertyName, out contentType, out fileName);
 
        fileName = file.Binary.FileName;
        contentType = file.Binary.ContentType;
 
        return ModifyStream(file.Binary);
    }
 
    public override BinaryFileName GetFileName(Node node, string propertyName = DEFAULTBINARY_NAME)
    {
        // Custom file name conversion logic goes here
        //...    
    }
 
    private static Stream ModifyStream(BinaryData binaryData)
    {
        //TODO: modify the stored stream or return a different one
        //...
    }
}
```
