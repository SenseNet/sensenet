---
title: "Field Control"
source_url: 'https://github.com/SenseNet/sensenet/docs/field-control-for-developers.md'
category: Development
version: v6.0
tags: [field control, field]
---

# Field Control

> Although this feature is supported in sensenet ECM 7, it is built on the old Web Forms technology that you **should not use for new projects**. We encourage you to use a more modern UI solution using our [client-side packages](https://www.npmjs.com/org/sensenet).

**Field Contols** are the main building blocks of [Content views](content-view.md). They generate the HTML controls responsible for the input or output of the displayed [Content](content.md)'s fields. Field controls are implemented as ASP.NET controls, they can be used in the ascx source of the content views.

The field control class is responsible for the data flow between user interface and appropriate Field of the viewed Content.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/FieldControlRole.png" style="margin: 20px auto" />

## Field control implementation

A field control is a class that is inherited from the abstract FieldControl (namespace: SenseNet.Portal.UI.Controls). The most simple field control overrides the GetData and SetData methods to define custom logic:

```csharp
namespace FieldControlSample
{
    public class MyFieldControl : FieldControl
    {
        public override object GetData()
        {
            // return data from UI to field
            return null;
        }
        public override void SetData(object data)
        {
            // field calls this on init, initialize UI from retrieved data here
        }
    }
}
```

## Field controls in contentviews

Field conrols can be embedded in content views just like any arbitrary ASP.NET control. In the following content view fragment every element is a field control that uses the *sn* prefix and has a FieldName attribute:

```html
...
<div class="sn-content sn-content-inlineview">
    <sn:ErrorView ID="ErrorView1" runat="server" />
    <h1><%= GetValue("FullName") %></h1>
    <sn:ShortText ID="ShortText0" runat="server" FieldName="Name" ControlMode="Edit">
      <EditTemplate>
        <asp:TextBox ID="InnerShortText" Class="sn-ctrl sn-ctrl-text sn-ctrl-username" runat="server"></asp:TextBox>
      </EditTemplate>
    </sn:ShortText>
    <sn:ShortText ID="ShortText1" runat="server" FieldName="FullName" ControlMode="Edit" />
</div>
...
```

> Manually registering the *sn* prefix for built-in controls is unnecessary, since it is registered in the web.config. If you create custom field controls you will have to register a tagprefix for the corresponding namespace.

## Built in field controls

There are numerous built-in field controls you can use in content views out-of-the-box. Any built-in field has at least one corresponding built-in field control in the sensenet Content Repository.

## Field control properties

In addition to the standard properties every ASP.NET server control shares (e.g. *ID*, *runat*, etc.), field controls provide several other ways to control their behavior. The most important properties are the following:

- **FieldName (*)** - Gets or sets the fieldname of the [Field Control](field-control.md). This is the name with which the control is mapped to a [Field](field.md) of the underlying [Content](content.md).
- **ControlMode (*)** - Gets or sets the control mode: Browse, Edit or None.
- **FrameMode (*)** - Gets or sets the frame mode: NoFrame, ShowFrame or None.
- **Field** - Gets or sets the underlying field.
- **ContentView** - Gets the content view that is the parent of this control.
- **Content** - Gets the content that belongs to the underlying field.
- **ContentHandler** - Gets the Node that belongs to the underlying field.
- **ReadOnly (*)** - Gets or sets whether the control is read-only. If true control will not be editable on the user interface.
- **InputUnitCssClass (*)** - Gets or sets the class of the wrapper tag of the control.
- **FieldIsCompulsory** - Indicates whether the underlying field is compulsory.
- **HasError** - Gets whether the field control is invalid (has the ErrorMessage set).
- **ErrorMessage** - Gets error messages related to the field control.

(*) Some properties can be set as an attribute of the field control element. For example the following field control is connected to the *FullName* field and it is rendered as an editable control:

```html
<sn:ShortText ID="ShortText1" runat="server" FieldName="FullName" ControlMode="Edit" />
```

## Field control templates

Field controls can be templated, that define the layout of the field control. Global templates can be found in the */Root/Global/fieldcontroltemplates* folder and can be overridden in the content view markup, for example:

```html
    <sn:ShortText ID="ShortText0" runat="server" FieldName="Name" ControlMode="Edit">
      <EditTemplate>
        <asp:TextBox ID="InnerShortText" Class="sn-ctrl sn-ctrl-text sn-ctrl-username" runat="server"></asp:TextBox>
      </EditTemplate>
    </sn:ShortText>
```