---
title: "CheckBoxGroup Field Control"
source_url: 'https://github.com/SenseNet/sensenet/docs/checkboxgroup-fieldcontrol.md'
category: Development
version: v6.0
tags: [checkbox, checkboxgroup, choice, field control, field]
---

# CheckBoxGroup Field Control

The CheckBoxGroup Field Control displays a list of checkboxes for selecting an option from a single- or multiple-selection Choice field.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/Checkboxes.png" style="margin: 20px auto" />

When the underlying [Choice Field](choice-field.md) is configured to allow extra value an extra textbox is rendered next to the dropdown control.

## Supported Field types

- [Choice Field](choice-field.md)

## Properties

- **SelectedValueType** (optional): Sets the display type of the selected options in *Browse* mode. Possible values are *Value* and *Text*, default is *Text*.

Example:

```html
   <sn:CheckBoxGroup ID="CheckBoxGroup1" runat="server" FieldName="Style" SelectedValueType="Value" />
```

## Templates

The CheckBoxGroup Field Control renders a Label control in Browse mode and a CheckBoxList accompanied with a TextBox - the latter for an optional extra value - in Edit mode.

### Browse view template

```csharp
<%@  Language="C#" %>
<asp:Label ID="InnerControl" runat="server" />
```

### Edit view template

```csharp
<%@  Language="C#" %>
<asp:CheckBoxList CssClass="sn-ctrl sn-ctrl-checkboxgroup" ID="InnerControl" runat="server" />
<asp:TextBox CssClass="sn-ctrl sn-ctrl-text sn-ctrl-extravalue" ID="ExtraTextBox" runat="server" />
```

## Examples

### Simple example

```html
   <sn:CheckBoxGroup ID="CheckBoxGroup1" runat="server" FieldName="Style" />
```

### Templated example

```html
   <sn:CheckBoxGroup ID="CheckBoxGroup1" runat="server" FieldName="Style">
      <EditTemplate>
         <asp:CheckBoxList ID="InnerControl" runat="server" />
         <asp:TextBox ID="ExtraTextBox" runat="server" />
      </EditTemplate>
   </sn:CheckBoxGroup>
```