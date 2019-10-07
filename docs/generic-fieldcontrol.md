---
title: "Generic Field Control"
source_url: 'https://github.com/SenseNet/sensenet/docs/generic-fieldcontrol.md'
category: Development
version: v6.0
tags: [field control, field, generic]
---

# Generic Field Control

> Although this feature is supported in sensenet ECM 7, it is built on the old Web Forms technology that you **should not use for new projects**. We encourage you to use a more modern UI solution using our [client-side packages](https://www.npmjs.com/org/sensenet).

The Generic Field Control is a special [Field Control](field-control.md) that renders the default Field Controls for the Fields of the displayed Content. The default Field Control for a Field is defined by the Field implementation (see supported Field Controls section for each [Field](field.md)) and can be overridden in the [Content Type Definition](ctd.md) of the displayed Content.

The Generic Field Control automatically resolves the default Field Control for a given Field and renders it. Not only does it render a single Field Control, but Field Controls for all Fields defined in the CTD of the Content - except when explicitly given to skip specific Field Controls. This makes it a useful tool when creating [Content Views](content-view.md) that use a generic layout for the Field Controls.

### Field controls

The Generic Field Control will list all defined Fields of the Content using the default Field Control. The default Field Control for a Field is defined by the Field itself , from code (see supported Field Controls section for each Field) - but can be set with the ControlHint property in the [Field Setting](field-setting.md).

### Field order

Fields are listed according to the order of the Fields defined in the CTD. Please note that Field order can be changed by overriding a Field in a child Content Type. Above that, Field order can be configured using the *FieldsOrder* property.

### Field visibility

Visible Fields are controlled by the visibility settings in the [CTD](ctd.md) (*VisibleBrowse*, *VisibleEdit*, *VisibleNew*). Fields marked as Hide are not listed; *Advanced* Fields are put under the *Show advanced fields* section and are hidden by default.

### Generic Content View

There is a special Content View type defined in the base system called the Generic Content View. It has similar functionality except that it is less configurable than the Generic Field Control and strictly relies upon CTD settings.

## Properties

- **ContentListFieldsOnly**: lists only the Fields that were defined with a List. See Content Views for FormItem Content Type.
- **FieldsOrder**: space separated list of Fields that will be displayed in the exact order that is given by this list.
- **ReadonlyFields**: space spearated list of Fields that will be rendered read only.
- **ExcludedFields**: space separated list of Fields that will NOT be rendered.

## Example

The following code is an excerpt from the User Content Type's *Edit* Content View. It includes a couple of manually defined Field Controls and a Generic Field Control.

```csharp
<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<div class="sn-user">
 
    <div class="sn-user-prop">
            <div class="sn-user-edit"><sn:ShortText runat="server" ID="ShortTextFullName" FieldName="FullName" /></div>
            <div class="sn-user-edit"><sn:ShortText runat="server" ID="ShortTextDomain" FieldName="Domain" /></div>
            <div class="sn-user-edit"><sn:ShortText runat="server" ID="ShortTextName" FieldName="Name" /></div>
            <div class="sn-user-edit"><sn:ShortText runat="server" ID="ShortTextEmail" FieldName="Email" /></div>
            <div class="sn-user-edit"><sn:GenericFieldControl runat="server" ID="GenericFieldControl1"
                ExcludedFields="Avatar FullName Domain Name Email Version Index Password" /></div>
            <div class="sn-user-edit"><sn:CommandButtons ID="CommandButtons1" runat="server" /></div>
    </div>
</div>
```

The following example shows how to give Field Control ordering:

```html
<sn:GenericFieldControl runat="server" ID="GenericFieldcontrol1" FieldsOrder="DisplayName Description FirstLevelApprover FirstLevelTimeFrame 
SecondLevelApprover SecondLevelTimeFrame WaitForAll" />
```

The following line will list the Fields of the current item Content that are defined in a parent List - Fields that start with '#'.

```html
<sn:GenericFieldControl runat=server ID="GenericFieldControl1" ContentListFieldsOnly="true" />
```
