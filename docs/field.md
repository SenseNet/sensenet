---
title: "Field"
source_url: 'https://github.com/SenseNet/sensenet/docs/field.md'
category: Concepts
version: v6.0
tags: [field, content type, content type definition, inheritance, type system]
---

# Field

Fields are single data storing units that build up [Content Types](content-type.md) - in other words: Fields are attributes of Content. A single Field represents a single chunk of information, such as a name or a date. Fields are mapped to [Content Repository](content-repository.md) data slots in the storage layer, and can be displayed and accessed through controls on the UI.

Fields however not only store data, but also carry metadata and configuration settings. Metadata includes attributes like *Title* and *Description*; configuration includes attributes like Compulsory and ReadOnly. For a complete list of available metadata and configuration attributes see [CTD Field definition](ctd.md).

## Field types

There are numerous Field types defined in the base system. Different Field types store different types of data and have different configuration. A *Number* Field for example stores a number and - among others - the *MaxValue* property can be adjusted to limit the maximum value stored, whereas a *ShortText* Field stores text data and has a *MaxLength* configuration property that can be set to limit the maximum allowed number of characters stored in the Field. New Field types can be created by implementing a Field handler (see later) in C#.

## Field Setting

The behavior of Fields can be controlled by their configuration - or as we call it in our API: their [Field Setting](field-setting.md). The Field Setting of a Field contains properties that define the behavior of the Field - for example, a Field can be configured as read only or compulsory to fill. The Field Setting of a Field can be adjusted using its Field definition, read on!

## Field definition

As Fields build up [Content](content.md), the set of contained Fields are defined when Content Types are defined. The [Content Type Definition](ctd.md) (CTD) holds the Field definition information (besides a couple of Content Type-related configuration settings). When defining a Content Type the contained Fields can be defined in the CTD using *XML fragments* describing the type of the Field, metadata and Field configuration (see [Field Setting](field-setting.md) for details):

- For a complete list of common Field Setting configuration properties see [CTD Field definition](ctd.md).
- Different Field types have different Field Settings. Check out the reference documentation of Fields to learn about Field Settings of the different Fields (for example the [Reference Field](reference-field.md)).

## Fields and data slots

As stated above the Field is a data storing unit of Content. For most simple Fields the stored Field value is stored in the database in a single slot (single value in a db table, also referred to as a storage property) and there exists a one-to-one correspondence between the data slot and the Field. A Field, however, can be a composite Field meaning that it is able to represent different storage properties in one Field unit (configured using the Bind Field Setting property). The [Image Field](image-field.md) is a good example for this, that can represent an image either via a Reference Field referencing an Image Content or a Binary Field storing image data. Moreover, not only multiple slots can be assigned to a single Field, but the Field itself is capable of representing a complex datatype built upon a simple storage property. For example, it is possible to store an XML or JSON value in a text property and create a Field that parses the text and provides a simple interface to manage it via complex programmable objects - enabling the implementation of simple Field Controls that can display complex data. Summarizing, the Field is considered as the basic building block of Content, but it is important to keep in mind that the Field is already a higher level representation of data that builds upon (and can be split to) storage properties.

## Validation

When the Content is saved the input Field data falls under validation according to the [Field Setting](field-setting.md). This includes non-empty value check for compulsory Fields and some Field Setting-dependent checks. An [Integer Field](integer-field.md) for example validates input data against data-type check - the entered value should be an Integer value - and also checks whether input data falls between the range of *MinValue* and *MaxValue* as given in the Field definition in the Content Type's CTD.

## Inheritance

[Content Types](content-type.md) can be inherited from other Content Types meaning that the child Content Type inherits all Fields that are defined on the *parent* Content Type. Field metadata and configuration **can be overridden** in child types - for example when a parent type defines a ShortText Field displayed as *my shorttext* and allowing a maximum number of 10 characters to be entered, a child Content Type may redefine the exact same Field as having *my redefined shorttext* text displayed and allowing a maximum number of 5 characters entered. Thus when creating Content of the child Content Type the inherited Field will behave different than as if a Content of the parent type were created. Inheritance and override - just like Field definition - can be adjusted in the Content Type Definition of the Content Types:

- For more info on Field inheritance see [CTD Field inheritance](ctd.md).

## Field handlers

Field handlers define the underlying logic of the Field including validation against configuration settings and data providing built upon storage properties. Whenever a Field is defined in a CTD its handler has to be referenced either by using the type or the handler attributes (more info at [CTD Field definition](ctd.md)). New Field types can be created by implementing a new Field handler in C# derived from the Field base class (see [Field - for Developers](field-for-developers.md)).

## Indexing

For every Content, the Field values can be indexed so that when searching for a value the corresponding Content will appear in the result set. It is also possible to search in Fields by explicitly defining the Field to search for in a query. The portal uses the [Lucene search engine](http://lucene.apache.org/lucene.net/) by default for indexing the Content Repository and to provide a fast mechanism for returning query results. You can read more about Field indexing [here](field-indexing.md).

## Deleting a field

When you remove a field from a CTD, we delete all the values for that field immediately from the database, so please be careful about it! On the other hand, content are not reindexed (it would take too much time), which means you will still be able to find content by their previous field value using a [Content Query](content-query.md) - even after that field has been removed from the CTD. If that does not suit your business needs, you may rebuild the index (of the whole Content Repository or just a subtree) using the [index SnAdmin tool](snadmin-tools.md).
