---
title: "Field"
source_url: 'https://github.com/SenseNet/sensenet/docs/field-for-developers.md'
category: Development
version: v6.0
tags: [field, content type, content type definition, inheritance, type system]
---

# Field

[Content](content.md) in the sensenet [Content Repository](content-repository.md) are built up of fields. Everything is represented as a content and all of content data can be accessed via fields. Field in sensenet is the atomic data accessor, however it is more than a simple property. A field describes the represented data, defines its type, domain range and it is also able to validate data. The field specifies how its data is being indexed and is also responsible for data transformations between the storage layer, file system and the index. The field is one of the most important extensibility points in sensenet.

## Field definition in CTD

Fields of a content are defined in the content's content type definition. Any configuration related to fields can be set in the CTD. A typical example of field definition looks like the following:

```xml
<Field name="CheckInComments" type="LongText">
  <DisplayName>Checkin comments</DisplayName>
  <Description>Comments for a new version.</Description>
  <Configuration>
    <Compulsory>false</Compulsory>
    <MaxLength>500</MaxLength>
  </Configuration>
</Field>
```

For more info on field definition and the possible properties/settings, read [CTD](ctd.md#Field-definition).

### Field type setting

The *type* attribute determines the field's type. The name of the field must be unique in the CTD, and the name and the type together must be unique in the system. This means that for example if the *InFolder* field's type is *ShortText*, then it must be a *ShortText* in other CTDs, too. The type attribute is a short name that is declared with the ShortName attribute on the field implementation:

```csharp
[ShortName("ShortText")]
...
public class ShortTextField : Field
```

The field type can also be defined with the *handler* attribute instead of the *type* attribute. The value of the *handler* attribute must be the desired type's fully qualified name. The *type* and *handler* attributes cannot be used together, but at least one of them must be declared. The following declarations are equivalent:

```xml
<Field name="InFolder" type="ShortText">
<Field name="InFolder" handler="SenseNet.ContentRepository.Fields.ShortTextField">
```

### Field configuration

The field configuration element can have a custom implementation, known as FieldSetting. You can read more about the FieldSetting object here:

- [Field Setting - for Developers](field-setting-for-developers.md)

## The Field class

The Field is an abstract type (*SenseNet.ContentRepository.Field*). There are several implementations of it in the base system. Below you can see the built-in field hierarchy:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/FieldHierarchy.png" style="margin: 20px auto" />

The following diagram shows the role of the field with respect to other content repository entities:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/FieldRole.png" style="margin: 20px auto" />

The content type definition specifies the type of the content and its fields. In code this is manifested in the ContentType and FieldSetting objects. The Content wraps the given Node instance (or Content Handler) and has got a Field collection. The field can access the Node object's properties, read, write, validate, export/import them and the field also serves as the data source for the corresponding field control of the content view.

## Connection between Field and Property

When the content is created over a node the most important step is building the connections between the fields of the content and the properties of the node. The field collection of the content is created according to the content type and every field connects to a property of the node according to what's specified in it.

A field connects to a property based on its name. This is called property binding - the default binding is the name of the field. The default binding can be overridden using the *Bind* element in the CTD. For example here is a part of the InTree field definition. The field will be connected to the Path property:

```xml
<Field name="InTree" type="ShortText">
  <Bind property="Path"/>
```

A field can access one ore more properties of the wrapped node. The type of the desired property must be declared in a DataSlot CLR attribute. This attribute needs to be defined on the field implementation, eg.:

```csharp
[DataSlot(0, RepositoryDataType.Int, typeof(Int32), typeof(Byte), typeof(Int16), typeof(SByte), typeof(UInt16)]
...
public class IntegerField : Field
{
    ...
```

The DataSlot attribute receives at least 3 parameters. The first one is an id that is always 0 if the field handles a single property. The second parameter is the type definition for the storage layer - this is necessary if the field is dynamic, that is it handles a property not defined in code (content handler). The third parameter defines the accepted data types for the case when it will handle coded property. It is actually a paramarray, thus an unlimited number of types can be given.

The following table summarizes RepositoryDataType values and closest corresponding CLR types:

|**RepositoryDataType**|**Closest CLR type**|
|----------------------|--------------------|
| RepositoryDataType.String | typeof(string) |
| RepositoryDataType.Text | typeof(string) |
| RepositoryDataType.Int | typeof(Int32) |
| RepositoryDataType.Currency | typeof(decimal) |
| RepositoryDataType.DateTime | typeof(DateTime) |
| RepositoryDataType.Binary | typeof(BinaryData) |
| RepositoryDataType.Reference | typeof(IEnumerable) |
| RepositoryDataType.NotDefined | ... any type, the field does not access any property |

Field property bindings are created when the type system of the sensenet Content Repository is (re)started, which occurs at system startup and when a CTD is changed.

### Binding to dynamic properties

If the CTD contains a field that cannot be associated to any coded property (for example when the CTD is extended with a new field but the underlying content handler is not updated), then a dynamic property will automatically be created. This property will always be stored in the database through the storage layer. The property will get the name of the field's name or the binding name declared in the field definition. The data type of the property will be the type declared in the second (RepositoryDataType) property of the DataSlot attribute. If a field is removed from the CTD whose corresponding property was a dynamic property, the dynamic property is removed by the system. The feature of dynamic creation and removal of properties is what gives the ability to change content types in the sensenet Content Repository in runtime.

### Binding to hard coded properties

If a coded property (in a content handler) exists whose name corresponds to the name of the field or the binding name, then the field will use that property. In this case the field is not responsible for storing data, but not even for converting data to the stored type. When the bindig is created the field will choose the property's type among its own supported types, notes it and will use that type to execute any data transformations afterwards.

## Conversions during accessing the property

This is the most important logic of the field implementation. For any new field both directions must be implemented:

- Converts data from a content handler's property to the field's native data type:

```csharp
protected override object ConvertTo(object[] handlerValues
```

- Converts data from the field's native data type to the data type of the accessed property:

```csharp
protected override object[] ConvertFrom(object value);
```

A good *ConvertFrom/ConvertTo* function pair implements data conversion for all data types accepted by the field (ie.: data types that are declared in the field's implementation), and it always converts to the bound property type from its own type and vica versa.

## Using fields from code

The most common code example related to fields is when we read or write a content's field. The following code creates a new content under *testRoot* with the name *TestFolder*, sets the values of the Index and *Description* fields, and saves the content:

```csharp
    Content content = Content.CreateNew("Folder", testRoot, "TestFolder");
    content["Index"] = 123;
    content["Description"] = "For test purposes only";
    content.Save();
```

The content has a field collection via which all fields can be accessed:

```csharp
    var binaryField = content.Fields["Binary"];
```

This is technically different from the content's indexer, since the latter returns the field data. Thus the following code lines for reading field data are equivalent:

```csharp
    binaryData = content.Fields["Binary"].GetData(); // without indexer
    binaryData = content["Binary"]; // with indexer
```

You can use the following code for writing a field's data. The given code lines are equivalent:

```csharp
    content.Fields["Binary"].SetData(binaryData); // without indexer
    content["Binary"] = binaryData; // with indexer
```

We usually retrieve the Field object from the Field when we need its metadata:

```csharp
    var maxLength = ((LongTextFieldSetting)content.Fields["Description"].FieldSetting).MaxLength;
```

## Field and indexing

For details on field indexing refer to the following article:

- [Field Indexing](field-indexing.md)

## Field related extensibilities

sensenet is highly extensible and customizable. Implementing custom fields is just one option among many possibilities - for most business problems you don't need to create custom fields and the solution can be reached with different techniques. Here are the most common cases when to and when not to develop a custom field:

- If you need to use a new .NET data type in a content handler: implement a custom Field.
- If you have a Field using the appropriate .NET type but you want to configure or validate it differently: implement a custom [FieldSetting](field-setting.md).
- If you want to search in a Field's values differently: implement a custom [FieldIndexHandler](how-to-create).