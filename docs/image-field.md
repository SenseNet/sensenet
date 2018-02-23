---
title:  "Image Field"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/image-field.md'
category: Concepts
version: v6.0
tags: [fields, image]
---

# Image Field

Image Field is a field type that is used to handle an image attached to the content. It handles images either as references or binary field content.

The field has two modes depending on how the image is stored in relation to the content:

- **reference mode**
    - image data is stored as a node in Content Repository (under the content)
    - the field acts as a [Reference Field](reference-field.md) referencing the image node
- **binarydata mode**
    - the field acts as a [Binary Field](binary-field.md) storing image data
    - image is therefore not visible in Content Repository

Image can be uploaded and attached to a content using the Image Field Control. Switching between modes can also be done through the Image Field Control.

## Field handler

- handler: *SenseNet.ContentRepository.Fields.ImageField*
- short name: *Image*

Usage in CTD:

```xml
<Field name="ImageRef" type="Reference">
...
</Field>
<Field name="ImageData" type="Binary">
...
</Field>
<Field name="Avatar" type="Image">
  <Bind property="ImageRef" />
  <Bind property="ImageData" />
  ...
</Field>
```

> Note that the Image field is a composite field and it embodies a [Reference Field](reference-field.md) and a [Binary Field](binary-field.md). These two fields are technical fields and do not have to be presented by contentviews.

## Supported Field Controls

- Image Field Control: an image placeholder with an upload control for uploading images and a checkbox for switching between binary/reference modes.

## Configuration

- The Field does not have any special configuration settings in the [Content Type Definition](ctd.md).

## Example

### CTD example

```xml
<Field name="ImageRef" type="Reference">
  <DisplayName>Avatar image (reference)</DisplayName>
  <Configuration>
    <VisibleBrowse>Hide</VisibleBrowse>
    <VisibleEdit>Hide</VisibleEdit>
    <VisibleNew>Hide</VisibleNew>
    <AllowMultiple>false</AllowMultiple>
  </Configuration>
</Field>
<Field name="ImageData" type="Binary">
  <DisplayName>Avatar image (binarydata)</DisplayName>
  <Configuration>
    <VisibleBrowse>Hide</VisibleBrowse>
    <VisibleEdit>Hide</VisibleEdit>
    <VisibleNew>Hide</VisibleNew>
  </Configuration>
</Field>
<Field name="Avatar" type="Image">
  <DisplayName>Avatar</DisplayName>
  <Description>Avatar image of user.</Description>
  <Bind property="ImageRef" />
  <Bind property="ImageData" />
  <Configuration>
    <Visible>true</Visible>
  </Configuration>
</Field>
```

The above example is an excerpt from the User Content Type CTD. The *Avatar* field incorporates the hidden *ImageRef* and ImageData technical fields.