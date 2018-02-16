---
title:  "Binary Field"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/binary-field.md'
category: Concepts
version: v6.0
tags: [fields, binary]
---

# Binary Field

Binary Field is used for storing binary data. This is the most important Field defined on the File Content Type: after uploading a file the binary content of your file will be stored in a Binary field.

In a Binary field any kind of binary data can be stored without length restrictions. This is very useful, when you want to store uploaded files on your portal. The following apply to the behavior of the field:

- **Import/Export**: binary Field data can be exported to a single file / imported from a single file.

## Field handler

- handler: *SenseNet.ContentRepository.Fields.BinaryField*
- short name: **Binary**

Usage in CTD:

```xml
   <Field name="BinaryData" type="Binary">
   ...
   </Field>
```

## Supported Field Controls

- [Binary Field Control](binary-fieldcontrol.md): a complex field control that provides interface to upload/download binary content or edit textual content in a textarea.

## Configuration

The following properties can be set in the Field's [Field Setting](field-setting.md) configuration:

- **IsText**: a bool property that defines the way the binary is presented - when displayed with [Binary Field Control](binary-fieldcontrol.md). If set to true, the Field data can be inserted manually as a text, otherwise a file can be uploaded. In the former case a textarea, in the latter case a fileupload control is rendered.

> The **IsText** configuration property value is only a hint and the [Binary Field Control](binary-fieldcontrol.md) displaying the control can override this setting in cases when the Content has a special extension or it is a Content Type.

## Example

Fully featured example:

```xml
<Field name="BinaryData" type="Binary">
   <DisplayName>Binary content.</DisplayName>
   <Description>This field contains the binary of a file.</Description>
   <Configuration>
      <ReadOnly>false</ReadOnly>
      <Compulsory>false</Compulsory>
      <IsText>true</IsText>
   </Configuration>
</Field>
```

The above example configures the Binary field so that:

- the field is editable (not read-only)
- filling the field is not necessary
- the Field is rendered with a textarea by default and binary content can be inserted as a text
