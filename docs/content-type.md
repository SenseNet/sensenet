---
title: "Content Type"
source_url: 'https://github.com/SenseNet/sensenet/docs/content-type.md'
category: Concepts
version: v6.0
tags: [content type, content type definition, inheritance, type system]
---

# Content Type

The [Content Repository](content-repository.md) contains many different types of content. Content vary in structure and even in function. Different types of content contain different fields, are displayed with different views, and may also implement different business logic. The fields, views and business logic of a content is defined by its type - the Content Type.

Content Types are defined in a type hierarchy: a Content Type may be inherited from another Content Type - thus automatically inheriting its fields. Multiple inheritance is not allowed so the Content Types are arranged in a simple tree.

A Content Type is a special content in the [Content Repository](content-repository.md). Content Types define the structure and functioning of content:

- name, description of content types and available set of fields are defined with an xml configuration ([Content Type Definition](ctd.md) or CTD),
- optional custom business logic is implemented via a custom [Content Handler](content-handler.md), custom [Fields](field.md) and custom [Field Controls](field-controls.md).
For example a User has a name, e-mail address, etc. - these fields of the User Content Type are defined by its Content Type Definition. When saving a User it can be synchronized into an Active Directory - this logic is implemented in its Content Handler.

## Content Type hierarchy

Content Types can inherit fields from their ancestors. For example a *Domain* type inherits all the fields of the basic *Folder* type. A Content Type may only inherit fields from a single type thus the Content Types are arranged in a simple tree hierarchy. Inherited field configuration can be overridden in derived types. Field inheritance and overriding is defined in the [Content Type Definition](ctd.md) of the type.

## Example

Let's examine on of the built-on Content Types, the *User*. Log in as an Administrator and go to Explore. Navigate to */Root/System/Schema/ContentTypes*. Here you can see the *Content Type hierarchy*. The root element is the Content (GenericContent). Navigate to */Root/System/Schema/ContentTypes/GenericContent/User*. An administrative surface appears with child Content Types and field set available on Car content:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/ctd/ctd-in-contentexplorer.png" style="margin: 20px auto" />

You can edit the [Content Type Definition](ctd.md) of the User Content Type by clicking on the edit [Action](action.md) link.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/ctd/ctd-xml.png" style="margin: 20px auto" />