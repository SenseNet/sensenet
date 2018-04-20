---
title: "RadioButtonGroup Field Control"
source_url: 'https://github.com/SenseNet/sensenet/docs/radiobuttongroup-fieldcontrol.md'
category: Development
version: v6.0
tags: [radiobutton, radiobuttongroup, choice, field control, field]
---

# RadioButtonGroup Field Control

> Although this feature is supported in sensenet ECM 7, it is built on the old Web Forms technology that you **should not use for new projects**. We encourage you to use a more modern UI solution using our [client-side packages](https://www.npmjs.com/org/sensenet).

The RadioButtonGroup Field Control displays a list of radiobuttons for selecting an option from a single-selection Choice field.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/Radiobuttongroup.png" style="margin: 20px auto" />

When the underlying [Choice Field](choice-field.md) is configured to allow extra value an extra textbox is rendered next to the radiobuttongroup control.

## Supported Field types

- [Choice Field](choice-field.md)

## Properties

- **SelectedValueType** (optional): Sets the display type of the selected options in *Browse* mode. Possible values are *Value* and *Text*, default is *Text*.

Example:

```html
<sn:RadioButtonGroup ID="RadioButtonGroup1" runat="server" FieldName="Style" SelectedValueType="Value" />
```

## Templates

The RadioButtonGroup Field Control renders a Label control in Browse mode and a RadioButtonList accompanied with a TextBox - the latter for an optional extra value - in Edit mode.

### Browse view template

```csharp
<%@  Language="C#" %>
<asp:Label ID="InnerControl" runat="server" />
```

### Edit view template

```csharp
<%@  Language="C#" %>
<asp:RadioButtonList CssClass="sn-ctrl sn-ctrl-radiogroup" ID="InnerControl" runat="server" />
<asp:TextBox CssClass="sn-ctrl sn-ctrl-text sn-ctrl-extravalue" ID="ExtraTextBox" runat="server" />
```

## Examples

### Simple example

```html
<sn:RadioButtonGroup ID="RadioButtonGroup1" runat="server" FieldName="Style" />
```

### Templated example

```html
<sn:RadioButtonGroup ID="RadioButtonGroup1" runat="server" FieldName="Style">
   <EditTemplate>
      <asp:RadioButtonList ID="InnerControl" runat="server" />
      <asp:TextBox ID="ExtraTextBox" runat="server" />
   </EditTemplate>
</sn:RadioButtonGroup>
```