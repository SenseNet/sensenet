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
- short name: **Image*

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

The above example is an excerpt from the User Content Type CTD. The *Avatar* field incorporates the hidden *ImageRef* and ImageData technical fields. Builders only have to deal with the Avatar field when presenting the content via .xslt or .ascx.

### XSLT example

When the content containing an Image field is rendered with .xslt the Image field is rendered as an element containing the path that can be used as an accessor to the image data. The current image mode is indicated with the imageMode attribute:

```xml
<Fields>
   ...
   <Avatar imageMode="BinaryData">/binaryhandler.ashx?nodeid=2846&propertyname=ImageData</Avatar>
   ...
</Fields>
```

is the rendered xml fragment when used in Binary mode. Likewise,

```xml
<Fields>
   ...
   <Avatar imageMode="Reference">/Root/YourContents/Avatars/cio.jpg</Avatar>
   ...
</Fields>
```

is the rendered xml fragment when used in Reference mode.

The rendered node values can be used as input to an html img tag's src attribute. It is useful to resize large images before transmitting them to client side - you may use extra *height* and *width* url parameters in both modes. These parameters can easily be added using the CreateThumbnailPath xslt function available in SenseNet.Portal.UI.ContentTools:

```csharp
CreateThumbnailPath(path, imageMode, width, height)
```

The url created this way will give back the resized image data in response. For example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<xsl:transform version="1.0" 
   xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
   exclude-result-prefixes="xsl snc" 
   xmlns:snc="sn://SenseNet.Portal.UI.ContentTools">
   <xsl:output method="html" indent="yes" />
 
   <xsl:template match="/">
      <img>
         <xsl:attribute name="src">
            <xsl:value-of select="snc:CreateThumbnailPath(/Content/Fields/Avatar, /Content/Fields/Avatar/@imageMode, 20, 20)" />
         </xsl:attribute>
      </img>
   </xsl:template>
</xsl:transform>
```

the above code generates the following:

- reference mode:

```html
   <img src="/Root/YourContents/Avatars/cio.jpg?dynamicThumbnail=1&width=20&height=20"/>
```

- binarydata mode:

```html
   <img src="/binaryhandler.ashx?nodeid=2846&propertyname=ImageData&width=20&height=20"/>
```

### ASCX example

When used in an .ascx view an image can be rendered using the *Avatar* field's value according to the following example:

```xml
<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.Portlets.ContentCollectionView" %>
 
<div>
    <%foreach (var content in this.Model.Items)
      {
          var imgSrc = "/Root/Global/images/orgc-missinguser.png?dynamicThumbnail=1&width=64&height=64";
          var imgField = content.Fields["Avatar"] as SenseNet.ContentRepository.Fields.ImageField;
          imgField.GetData(); // initialize image field
          var param = SenseNet.ContentRepository.Fields.ImageField.GetSizeUrlParams(imgField.ImageMode, 64, 64);
          if (!string.IsNullOrEmpty(imgField.ImageUrl))
              imgSrc = imgField.ImageUrl + param;
      %>
 
        <img src="<%= imgSrc %>" />
 
    <%} %>
</div>
```

The above example will render the collection's users' avatars to the output. For each user the *Avatar* Image Field's ImageUrl property holds the basic url information for the image. The Image Field's *GetSizeUrlParams* static function is used to get additional request parameters for resizing. The above example creates the following output for a collection containing two users:

```html
<div>
   <img src="/Root/YourContents/Avatars/cio.jpg?dynamicThumbnail=1&amp;width=64&amp;height=64"> 
   <img src="/binaryhandler.ashx?nodeid=2846&amp;propertyname=ImageData&amp;width=64&amp;height=64"> 
</div>
```

Also, check out the Image Field Control for more information on how to present Image Field data in an .ascx ContentView.
