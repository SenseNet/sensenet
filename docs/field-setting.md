---
title: "Field Setting"
source_url: 'https://github.com/SenseNet/sensenet/docs/field-setting.md'
category: Concepts
version: v6.0
tags: [field, field, setting, content type, content type definition]
---

# Field Setting

The behavior of [Fields](field.md) can be controlled by their configuration - or as we call it in our API: their Field Setting. The Field Setting of a Field contains properties that define the behavior of the Field - for example a Field can be configured as *read only* or *compulsory to fill*. The Field Setting of Fields can be adjusted in the [Content Type Definition (CTD)](ctd.md), using the *Configuration* element.

As [Fields](field.md) build up [Content](content.md), the set of Fields are defined when [Content Types](content-type.md) are defined. The [CTD](ctd.md) holds the Field definition information (besides a couple of Content Type-related configuration settings). When defining a Content Type the contained Fields can be defined in the CTD with *XML fragments* describing the type of the Field, metadata and Field Setting.

### Field Setting and Field types

Different Field types have different Field Settings.
Besides Field type-specific settings there are a couple common settings that are available for all Fields. For a complete list of common Field Setting configuration properties see [CTD Field definition](ctd.md)