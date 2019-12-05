---
title: "DropDown Field Control"
source_url: 'https://github.com/SenseNet/sensenet/docs/dropdown-fieldcontrol.md'
category: Development
version: v6.0
tags: [dropdowm, select, choice, field control, field]
---

# DropDown Field Control

> Although this feature is supported in sensenet 7, it is built on the old Web Forms technology that you should not use for new projects. We encourage you to use a more modern UI solution using our [client-side packages](https://www.npmjs.com/org/sensenet).

The DropDown Field Control displays a drop-down list for selecting an option from a single-selection Choice field.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/DropDown.png" style="margin: 20px auto" />

When the underlying [Choice Field](choice-field.md) is configured to allow extra value an extra textbox is rendered next to the dropdown control.

## Supported Field types

- [Choice Field](choice-field.md)

## Properties

- **SelectedValueType** (optional): Sets the display type of the selected options in *Browse* mode. Possible values are *Value* and *Text*, default is *Text*.

Example:

```html
   <sn:DropDown ID="DropDown1" runat="server" FieldName="Style" SelectedValueType="Value" />
```

## Templates

The DropDown Field Control renders a Label control in Browse mode and a DropDownList accompanied with a TextBox - the latter for an optional extra value - in Edit mode.

### Browse view template

```csharp
<%@  Language="C#" %>
<asp:Label ID="InnerControl" runat="server" />
```

### Edit view template

```csharp
<%@  Language="C#" %>
<asp:DropDownList CssClass="sn-ctrl sn-ctrl-select" ID="InnerControl" runat="server" />
<asp:TextBox CssClass="sn-ctrl sn-ctrl-text sn-ctrl-extravalue" ID="ExtraTextBox" runat="server" />
```

## Examples

### Simple example

```html
   <sn:DropDown ID="DropDown1" runat="server" FieldName="Style" />
```

### Templated example

```html
   <sn:DropDown ID="DropDown1" runat="server" FieldName="Style">
      <EditTemplate>
         <asp:DropDownList ID="InnerControl" runat="server" />
         <asp:TextBox ID="ExtraTextBox" runat="server" />
      </EditTemplate>
   </sn:DropDown>
```