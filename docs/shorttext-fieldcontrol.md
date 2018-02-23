---
title: "ShortText Field Control"
source_url: 'https://github.com/SenseNet/sensenet/docs/shorttext-fieldcontrol.md'
category: Development
version: v6.0
tags: [shorttext, text, field control, field]
---

# ShortText Field Control

> Although this feature is supported in sensenet ECM 7, it is built on the old Web Forms technology that you **should not use for new projects**. We encourage you to use a more modern UI solution using our [client-side packages](https://www.npmjs.com/org/sensenet).

The ShortText [Field Control](field-control.md) is a Field Control that handles [ShortText Fields](shorttext-field.md) and provides an interface to display/modify short text data.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/ShortTextFieldControl_editmode.png" style="margin: 20px auto" />

With ShortText field control textual data can be displayed or edited up to 450 characters. (Longer text will be truncated on save!) Ideal for storing simple descriptions, but also can contain complex HTML formatted text to display custom visualized data.

## Supported Field types

- [ShortText Field](shorttext-field.md)

## Templates

The ShortText Field Control is a simple Field Control that renders a single TextBox. In *Browse* mode simply the value of the Field is rendered.

### Browse view template

```csharp
<%@  Language="C#" %>
<%# DataBinder.Eval(Container, "Data") %>
```

### Edit view template

```csharp
<%@  Language="C#" %>
<asp:TextBox ID="InnerShortText" CssClass="sn-ctrl sn-ctrl-text" runat="server"></asp:TextBox>
```

## Examples

### Simple example

```html
   <sn:ShortText ID="ShortText1" runat="server" FieldName="Data1" />
```

### Templated example

```html
   <sn:ShortText ID="ShortText2" runat="server" FieldName="Data1">
      <EditTemplate>
         <asp:TextBox ID="InnerShortText" runat="server"></asp:TextBox>
      </EditTemplate>
   </sn:ShortText>
```