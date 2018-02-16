---
title:  "XSS Protection"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/xss-protection.md'
category: Development
version: v6.0
tags: [xxs, sanitaze, scripts]
description: XSS Protection
---

## XSS Protection

sensenet comes with built-in XSS (Cross-site scripting) protection features to enable users to sanitize user inputs making sure no harmful scripts or code is executed when user input is displayed on sensenet pages. The features include the standard sensenet content data displaying mechanisms using XSS protection and also an easy-to-use API for custom solution.

XSS (or Cross-site scripting) is a common form of web attacks that exploit vulnerabilities related to user inputs being displayed on web pages in unmodified form. An attacker could place for example *script* blocks into the page that are executed in browsers of page visitors if user input is not validated/handled correctly. sensenet sanitizes user inputs before displaying them on pages and also provides an API to use for similar purposes in custom controls.

### Field Controls and Field Data

Fields will always store data in the same format as received from input. The following applies when displaying Field data in [Field Controls](field-controls.md):

- the following sanitization levels are defined in the system:
    - **Text**: the output is HTML encoded, so *script* tag will appear as a human readable *script* text in the output, and not as a processed html tag,
    - **Html**: *script* and other harmful tags are removed from the output,
    - **Raw**: raw data is sent to the output.
- default sanitization level of Field Controls is Text. Field Controls define their levels of sanitization. E.g.: Richtext uses *Html*, *Binary* uses *Raw*.
- when using server controls (like asp:Label) in Field Controls the developer should take precautions to sanitize user data before setting it to a displayed value (like *Text*), see API functions later.
- the following property bindings are defined for data output:

```csharp
<%# DataBinder.Eval(Container, "RawData") %>
<%# DataBinder.Eval(Container, "TextData") %>
<%# DataBinder.Eval(Container, "HtmlData") %>
```

Where RawData outputs raw data, TextData uses full encoding of data and HtmlData use sanitization of Field data. You can also use the simple *Data* accessor:

```csharp
<%# DataBinder.Eval(Container, "Data") %>
```

This will use the OutputMethod defined on the Field, or will use Text (ie full encoding) if Default is in effect on the Field. The OutputMethod of any Field can be set at CTD level with the following configuration:

```xml
      <Configuration>
        <OutputMethod>Html</OutputMethod>
      </Configuration>
```

Here OutputMethod can be one of the following:

- **Text**: output is to be fully HTML encoded,
- **Html**: output is to be stripped of harmful tags,
- **Raw**: output is to be displayed as is,
- **Default**: output will be displayed with the default output method of the Field Control. Field Controls' default output is Text, Richtext uses Html, and Binary uses Raw.

### ContentViews

It is possible to display input data in [Content Views](content-views.md) using the *GetValue* function:

```csharp
<%= GetValue("FieldName") %>
```

The function returns the raw Field data. To use sanitized output use the function with the following parameters:

```csharp
<%= GetValue("FieldName", this.Content, SenseNet.ContentRepository.Schema.OutputMethod.Html) %>
```

To get full encoded output use the function with the following parameters:

```csharp
<%= GetValue("FieldName", this.Content, SenseNet.ContentRepository.Schema.OutputMethod.Text) %>
```

### Using the API

When displaying user inputs in custom controls it is the developer's responsibility to sanitize the displayed data to exclude XSS vulnerabilities. To help developers sensenet comes with a built-in API that can be easily used to sanitize user inputs:

```csharp
var sanitizedString = SenseNet.Portal.Security.Sanitize(userInput);
```

For full html encoding you can simply use built-in .Net functions, like:

```csharp
var encodedString = HttpUtility.HtmlEncode(userInput);
```

### Javascript

It's also possible to sanitize a text in javascript with the SN.Util.Sanitize(text) function.

```js
var sanitizedString = SN.Util.Sanitize("<script>alert('Lorem ipsum')</script>");
```

The function removes the 'script' tags and returns the inner text 'alert('Lorem ipsum')'.

## Examples

The following is an excerpt from the CTD of the HTMLContent type:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType name="HTMLContent" parentType="WebContent" handler="SenseNet.ContentRepository.GenericContent" xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
 
 ...
   <Field name="HTMLFragment" type="LongText">
      ...
      <Configuration>
        <OutputMethod>Html</OutputMethod>
      </Configuration>
 
 ...
```

The above setting will instruct the CMS to use *Html* sanitization level if the data of the Field is displayed using the [LongText Field Control](long-text-fieldcontrol.md) in browse mode, since the default fieldcontroltemplate of the LongText Field Control is the following:

```csharp
<%@  Language="C#" %>
<%# DataBinder.Eval(Container, "Data") %>
```

...and the *Data* accessor will use the settings of the CTD for the given Field, as discussed above.
