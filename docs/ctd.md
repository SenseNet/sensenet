---
title: "Content Type Definition"
source_url: 'https://github.com/SenseNet/sensenet/docs/ctd.md'
category: Development
version: v6.0
tags: [content type, content type definition, inheritance, type system]
---

# Content Type Definition

A Content Type Definition is an xml-format configuration file for defining Content Types. The xml configuration (CTD) holds information about 
- the type name, description 
- properties that control how content of this type look and behave (icon, preview generation, indexing)
- parent content type
- set of fields (name, displayname and configuration of fields)
- content handler

The Content Type Definition xml of a [Content Type](content-type.md) can be edited by editing the [Content Type](content-type.md) in Content Explorer (see [How to create a Content Type](https://community.sensenet.com/docs/tutorials/how-to-create-a-content-type) for details).

## Default template

This is the default template with the most common options:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType name="" parentType="GenericContent" handler="SenseNet.ContentRepository.GenericContent" xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
  <DisplayName></DisplayName>
  <Description></Description>
  <Icon>Content</Icon>
  <Preview>false</Preview>
  <AllowIndexing>True</AllowIndexing>
  <AllowIncrementalNaming>true</AllowIncrementalNaming>
  <AllowedChildTypes>ContentTypeName1,ContentTypeName2</AllowedChildTypes>
  <Fields>
    <Field name="ShortTextField" type="ShortText">
      <DisplayName>ShortTextField</DisplayName>
      <Description></Description>
      <Configuration>
        <MaxLength>100</MaxLength>
        <MinLength>0</MinLength>
        <Regex>[a-zA-Z0-9]*$</Regex>
        <ReadOnly>false</ReadOnly>
        <Compulsory>false</Compulsory>
        <DefaultValue></DefaultValue>
        <VisibleBrowse>Show|Hide|Advanced</VisibleBrowse>
        <VisibleEdit>Show|Hide|Advanced</VisibleEdit>
        <VisibleNew>Show|Hide|Advanced</VisibleNew>
      </Configuration>
    </Field>
    <Field name="LongTextField" type="LongText">
      <DisplayName>LongTextField</DisplayName>
      <Description></Description>
      <Configuration>
        <MaxLength>100</MaxLength>
        <MinLength>0</MinLength>
        <TextType>LongText|RichText|AdvancedRichText</TextType>
        <ReadOnly>false</ReadOnly>
        <Compulsory>false</Compulsory>
        <DefaultValue></DefaultValue>
        <VisibleBrowse>Show|Hide|Advanced</VisibleBrowse>
        <VisibleEdit>Show|Hide|Advanced</VisibleEdit>
        <VisibleNew>Show|Hide|Advanced</VisibleNew>
      </Configuration>
    </Field>
    <Field name="NumberField" type="Number">
      <DisplayName>NumberField</DisplayName>
      <Description></Description>
      <Configuration>
        <MinValue>0</MinValue>
        <MaxValue>100.5</MaxValue>
        <Digits>2</Digits>
        <ReadOnly>false</ReadOnly>
        <Compulsory>false</Compulsory>
        <DefaultValue></DefaultValue>
        <VisibleBrowse>Show|Hide|Advanced</VisibleBrowse>
        <VisibleEdit>Show|Hide|Advanced</VisibleEdit>
        <VisibleNew>Show|Hide|Advanced</VisibleNew>
      </Configuration>
    </Field>
    <Field name="IntegerField" type="Integer">
      <DisplayName>IntegerField</DisplayName>
      <Description></Description>
      <Configuration>
        <MinValue>0</MinValue>
        <MaxValue>100</MaxValue>
        <ReadOnly>false</ReadOnly>
        <Compulsory>false</Compulsory>
        <DefaultValue></DefaultValue>
        <VisibleBrowse>Show|Hide|Advanced</VisibleBrowse>
        <VisibleEdit>Show|Hide|Advanced</VisibleEdit>
        <VisibleNew>Show|Hide|Advanced</VisibleNew>
      </Configuration>
    </Field>
    <Field name="BooleanField" type="Boolean">
      <DisplayName>BooleanField</DisplayName>
      <Description></Description>
      <Configuration>
        <ReadOnly>false</ReadOnly>
        <Compulsory>false</Compulsory>
        <DefaultValue></DefaultValue>
        <VisibleBrowse>Show|Hide|Advanced</VisibleBrowse>
        <VisibleEdit>Show|Hide|Advanced</VisibleEdit>
        <VisibleNew>Show|Hide|Advanced</VisibleNew>
      </Configuration>
    </Field>
    <Field name="ChoiceField" type="Choice">
      <DisplayName>ChoiceField</DisplayName>
      <Description></Description>
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <AllowExtraValue>false</AllowExtraValue>
        <Options>
          <Option selected="true">1</Option>
          <Option>2</Option>
        </Options>
        <ReadOnly>false</ReadOnly>
        <Compulsory>false</Compulsory>
        <DefaultValue></DefaultValue>
        <VisibleBrowse>Show|Hide|Advanced</VisibleBrowse>
        <VisibleEdit>Show|Hide|Advanced</VisibleEdit>
        <VisibleNew>Show|Hide|Advanced</VisibleNew>
      </Configuration>
    </Field>
    <Field name="DateTimeField" type="DateTime">
      <DisplayName>DateTimeField</DisplayName>
      <Description></Description>
      <Configuration>
        <DateTimeMode>DateAndTime</DateTimeMode>
        <Precision>Second</Precision>
        <ReadOnly>false</ReadOnly>
        <Compulsory>false</Compulsory>
        <DefaultValue></DefaultValue>
        <VisibleBrowse>Show|Hide|Advanced</VisibleBrowse>
        <VisibleEdit>Show|Hide|Advanced</VisibleEdit>
        <VisibleNew>Show|Hide|Advanced</VisibleNew>
      </Configuration>
    </Field>
    <Field name="ReferenceField" type="Reference">
      <DisplayName>ReferenceField</DisplayName>
      <Description></Description>
      <Configuration>
        <AllowMultiple>true</AllowMultiple>
        <AllowedTypes>
          <Type>Type1</Type>
          <Type>Type2</Type>
        </AllowedTypes>
        <SelectionRoot>
          <Path>/Root/Path1</Path>
          <Path>/Root/Path2</Path>
        </SelectionRoot>
        <DefaultValue>/Root/Path1,/Root/Path2</DefaultValue>
        <ReadOnly>false</ReadOnly>
        <Compulsory>false</Compulsory>
        <VisibleBrowse>Show|Hide|Advanced</VisibleBrowse>
        <VisibleEdit>Show|Hide|Advanced</VisibleEdit>
        <VisibleNew>Show|Hide|Advanced</VisibleNew>
      </Configuration>
    </Field>
    <Field name="BinaryField" type="Binary">
      <DisplayName>BinaryField</DisplayName>
      <Description></Description>
      <Configuration>
        <IsText>true</IsText>
        <ReadOnly>false</ReadOnly>
        <Compulsory>false</Compulsory>
        <DefaultValue></DefaultValue>
        <VisibleBrowse>Show|Hide|Advanced</VisibleBrowse>
        <VisibleEdit>Show|Hide|Advanced</VisibleEdit>
        <VisibleNew>Show|Hide|Advanced</VisibleNew>
      </Configuration>
    </Field>
  </Fields>
</ContentType>
```

> Please note that in case of a *Binary field* the *Compulsory* configuration option currently does not work.

Below you can see a fully featured skeleton of a Content Type Definition xml:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType name="" handler="" parentType="" xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
  <DisplayName></DisplayName>
  <Description></Description>
  <Icon></Icon>
  <Preview></Preview>
  <AllowIndexing></AllowIndexing>
  <AppInfo></AppInfo>
  <AllowIncrementalNaming></AllowIncrementalNaming>
  <AllowedChildTypes></AllowedChildTypes>
  <Fields>
    <Field name="" type="">
      <DisplayName></DisplayName>
      <Description></Description>
      <AppInfo></AppInfo>
      <Bind property="" />
      <Indexing>
         <Mode></Mode>
         <Store></Store>
         <TermVector></TermVector>
         <Analyzer></Analyzer>
         <IndexHandler></IndexHandler>
      </Indexing>
      <Configuration>
        <ReadOnly></ReadOnly>
        <Compulsory></Compulsory>
        <DefaultValue></DefaultValue>
        <VisibleBrowse></VisibleBrowse>
        <VisibleEdit></VisibleEdit>
        <VisibleNew></VisibleNew>
        <FieldIndex></FieldIndex>
        <ControlHint></ControlHint>
      </Configuration>
    </Field>
  </Fields>
</ContentType>
```

## General structure

The following elements make up the general structure of the xml:

- **ContentType**: root element, holds basic information in attributes
    - **name** attribute (required): unique name of the [Content Type](content-type.md) - this identifies the [Content Type](content-type.md)
    - **handler** attribute (required): the type name of the [Content Handler](content-handler.md) for the [Content Type](content-type.md). The referred class implements attached background programmed logic.

    ```diff
    Warning! If you need to extend or modify a content type that has code behind, you must use the parent class as content handler in the "handler" attribute or must develop code behind class and use it in the "handler" attribute. Use *SenseNet.ContentRepository.GenericContent* only if parent Content Type uses it, too!
    ```

    - **parentType** attribute: name of the parent [Content Type](content-type.md). Parent is the direct ancestor [Content Type](content-type.md) of this type in the Content Type hierarchy. Not required but it is suggested that all Content Types inherit from *SenseNet.ContentRepository.GenericContent* or any of its descendants.
    - **xmlns** attribute: xml namespace of the xml, always use *"http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"*.

- **DisplayName**: the displayed user friendly name of the type
- **Description**: the short description of the type
- **Icon**: icon of the type. Icons reside in the /Root/Global/images/icons folder. An icon is referred with its name here.
- **Preview**: whether the system needs to generate preview images for this type of content. Currently this works only for file types. This setting is not inheritable, you should set it explicitly in every CTD where you want to have preview images. Possible values are *yes/no* or *true/false*.
- **AllowIndexing**: whether the instances of this type should be indexed or not. Possible values are *yes/no* or *true/false*. The default is true. If set to false, the index will be smaller, but nobody will be able to find the items of this type using content query. They will still be accessible through a direct request of course. In the default installation preview images are not indexed, for example.
- **AppInfo**: custom text or xml fragment for CTD extensibility
- **AllowIncrementalNaming**: boolean property for allowing the incremental name suffix generation during content creation when a Content with the same name already exists. Default is false - in this case an error message is shown when saving the Content with an existing name. See [Content naming](content-naming.md#Incremental-naming) for details.
- **AllowedChildTypes**: a comma, semicolon or space separated list of Content Type names defining the allowed Content Types that can be created under any content of this type. See [Allowed Child Types](allowed-child-types.md) for details.
- **Fields**: this is the container element for the Field definitions. It should include all Fields of the type that are not defined in any ancestor type and also fields that are defined in an ancestor but overridden in this type.

## <a name="fielddefinition"></a>Field definition

The following elements build up the field definition:

- **Field**: root element, holds basic information in attributes
    - **name** attribute (required): name of the field. If a field with the given name is already defined in the system (on another content type), field types must match.
    - **type** attribute (this or **handler** is required): the short name of the field type. This is defined with the *ShortName* attribute of the corresponding [Field](field.md) class (field handler implementation).
    - **handler** attribute (this or **type** is required): the fully qualified type name of field handler class. Prefer using type attribute instead of handler, for example:

    ```xml
    <Field name="Make" type="ShortText">
    ```
    is equivalent to

    ```xml
    <Field name="Make" handler="SenseNet.ContentRepository.Fields.ShortTextField">
    ```
    For more information read [Field - for Developers](field-for-developers.md).

- **DisplayName**: displayed name of the field
- **Description**: description of the field
- **AppInfo**: custom text or xml fragment for CTD extensibility
- **Bind**: name of the storage property the field is bound to. By default the bound property name is the name of the field. You can define composite fields by binding them to multiple properties. An example for this is Image Field. Within a content multiple fields can be bound to the same property. For more info read [Field - for Developers](field-for-developers.md#Connection) between Field and Property.
- **Indexing**: indexing settings of the field. Please refer to [Field Indexing](field-indexing.md) for detailed information.
- **Configuration**: configuration settings of the field. This varies with the actual [Field Setting](field-setting.md). See detailed information on field configuration articles of specific Fields. The following optional elements are available in all types of Field Settings:
    - **ReadOnly**: indicates if the field is read-only
    - **Compulsory**: indicates if the field is compulsory
    - **OutputMethod**: defines the field output type. It can be Default, Raw, Text, Html. Plays an important role in [XSS Protection](xss-protection.md).
    - **DefaultValue**: default value of the field. When someone creates new content this value will be written to the field. Users can create new content with either through a GUI where content fields can be set by the user (e.g. a webform) or other methods (e.g. WebDAV drag and drop or new document created and saved to the sensenet Content Repository from Microsoft Office). 
    The value will be written to the field upon saving the new content if:
        - the field is compulsory,
        - has no value set by the user or any other logic,
        - and the Default Value is not null.
        - jScript functions can be used to define dynamic default values (see [examples](#examples)).
    - **VisibleBrowse**: indicates if the field is visible in generic browse Content Views
    - **VisibleEdit**: indicates if the field is visible in generic edit Content Views
    - **VisibleNew**: indicates if the field is visible in generic new Content Views
    - **FieldIndex**: defines order of field in generic Content Views
    - **ControlHint**: default field control name (ie. sn:ShortText) for generic Content Views and Generic Field Control

## Content Type inheritance

Content Types are organized into hierarchy according to inheritance. Any Content Type may inherit from another one. The topmost Content Type in the inheritance hierarchy is the GenericContent (with handler SenseNet.ContentRepository.GenericContent), every Content Type must inherit from this, or any of its descendant. When a child Content Type is inherited from a parent Content Type it means that the child Content Type contains all the Fields of the parent, even if they are not defined in the child CTD (see [Field inheritance](#fieldinheritance)) - and also they share common implemented logic.

```diff
Warning! The child Content Type must use the parent's handler, or use a custom content handler derived from it. The child cannot use *SenseNet.ContentRepository.GenericContent* unless the parent also uses that as the handler.
```

## <a name="fieldinheritance"></a>Field inheritance

A Content Type inherits its fields from its parent Content Type (defined by the **parentType** attribute). This means that only additional fields have to be defined in the type's CTD. The inherited fields apply to the Content Type as defined on the parent type, but may also be overridden. The following apply to field inheritance:

- fields of all ancestors are inherited: ie. fields of parent type of the direct parent are also available in the current type
- all fields of the parent type are inherited, deleting a field that has been defined on an ancestor type is not possible
- a field is inherited from parent when it is not defined in the current type's CTD
- if a field is defined in a CTD it overrides parent's field of the same name
- if a field is defined in a CTD with empty markup the parent's field of the same name is overridden with empty data
- when inheriting a field the first order elements of the configuration element are inherited (these are defined by the [Field Definition](#fielddefinition))

## Examples

### Simple example

Below you can see an extract of the *Car* Content Type Definition:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType name="Car" parentType="ListItem" handler="SenseNet.ContentRepository.GenericContent" 
xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
  <DisplayName>Car</DisplayName>
  <Description>Car is a leaf content type to demonstrate the ECMS features of sensenet</Description>
  <Icon>Car</Icon>
  <Fields>
    <Field name='Make' type='ShortText'>
      <DisplayName>Make</DisplayName>
      <Description>e.g. Mazda, Ferrari etc.</Description>
    </Field>
    <Field name='Model' type='ShortText'>
      <DisplayName>Model</DisplayName>
      <Description>e.g. RX-8, F-40 etc.</Description>
    </Field>
    <Field name='Style' type='Choice'>
      <DisplayName>Style</DisplayName>
      <Description>Select one</Description>
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <AllowExtraValue>true</AllowExtraValue>
        <Options>
          <Option selected='true'>Sedan</Option>
          <Option>Coupe</Option>
          <Option>Cabrio</Option>
          <Option>Roadster</Option>
          <Option>SUV</Option>
          <Option>Van</Option>
        </Options>
      </Configuration>
    </Field>  
  </Fields>
</ContentType>
```

The above configuration defines a [Content Type](content-type.md) that is inherited from the ListItem type and defines two [ShortText Fields](shortext-field.md) and a [Choice Field](choice-field.md).

### Example for field inheritance

The following CTD defines a simple type:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType name="MyType" parentType="GenericContent" handler="SenseNet.ContentRepository.MyTypeHandler" 
xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
  <DisplayName>My type</DisplayName>
  <Description>My base type</Description>
  <Icon>Content</Icon>
  <Fields>
    <Field name='MyField' type='ShortText'>
      <DisplayName>My field</DisplayName>
      <Description>My field description</Description>
      <Configuration>
        <MaxLength>10</MaxLength>
      </Configuration>
    </Field>
  </Fields>
</ContentType>
```

#### Extending base Content Type field set

This second CTD derives from the above one and defines an additional [Field](field.md).

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType name="MyDerivedType" parentType="MyType" handler="SenseNet.ContentRepository.MyTypeHandler" 
xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
  <DisplayName>My derived type</DisplayName>
  <Description>My derived type</Description>
  <Icon>Content</Icon>
  <Fields>
    <Field name='MyOtherField' type='ShortText'>
      <DisplayName>My other field</DisplayName>
      <Description>My other field description</Description>
    </Field>
  </Fields>
</ContentType>
```

The following table sums up field sets of the two types:

| MyType  | MyDerivedType |
|---------|---------------|
| MyField | MyField       |
|         | MyOtherField  |

> Please note that *MyDerivedType* uses its **parent's handler** to use the same implemented content handler logic. This is the correct setting if the derived type does not have its own custom handler.

#### Overriding a field of base Content Type

The following definition of *MyDerivedType* overrides *MyField*.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType name="MyDerivedType" parentType="MyType" handler="SenseNet.ContentRepository.MyTypeHandler" 
xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
  <DisplayName>My derived type</DisplayName>
  <Description>My derived type</Description>
  <Icon>Content</Icon>
  <Fields>
    <Field name='MyField' type='ShortText'>
      <DisplayName>My field overridden</DisplayName>
      <Description>My field description</Description>
      <Configuration>
        <MaxLength>15</MaxLength>
      </Configuration>
    </Field>
    <Field name='MyOtherField' type='ShortText'>
      <DisplayName>My other field</DisplayName>
      <Description>My other field description</Description>
    </Field>
  </Fields>
</ContentType>
```

The following table sums up field sets of the two types:

| MyType                                         | MyDerivedType                                                   |
|------------------------------------------------|-----------------------------------------------------------------|
| MyField (DisplayName='My field', MaxLength=10) | MyField (DisplayName='My field overridden', MaxLength=15)       |
|                                                | MyOtherField                                                    |

> Please note that *MyDerivedType* uses its **parent's handler** to use the same implemented content handler logic. This is the correct setting if the derived type does not have its own custom handler.

### Example for CTD with composite field

The CTD below shows you how to define a field that is built up of two other fields. Note the *Bind* element.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType name="MyType" parentType="GenericContent" handler="SenseNet.ContentRepository.GenericContent" 
xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
  <DisplayName>My type</DisplayName>
  <Description>My base type</Description>
  <Icon>Content</Icon>
  <Fields>
    <Field name="ImageRef" type="Reference">
      <DisplayName>Image content</DisplayName>
    </Field>
    <Field name="ImageData" type="Binary">
      <DisplayName>Image binarydata</DisplayName>
    </Field>
    <Field name="Image" type="Image">
      <DisplayName>Composite image</DisplayName>
      <Bind property='ImageRef' />
      <Bind property='ImageData' />
    </Field>    
  </Fields>
</ContentType>
```

The above-defined *Image* field is a composite field that references *ImageRef* and *ImageData* fields. The type of the field is [Image Field](image-field.md) - which has a custom implementation that handles the corresponding logic: an Image is either persisted to the [Content Repository](content-repository.md) as a separate content (in this case the image content is referenced via a [Reference Field](reference-field.md)) or it is part of the content as a binary (in this case the image is a binary saved to a [Binary Field](binary-field.md) on the content).

### <a name="examples">Example for Default value setting with JScript.NET

It is possible to set a default value for fields, and even use dynamically executed code which will be evaluated **on the fly**. You can use a variant of JavaScript for this purpose which is called JScript.NET - which is a JavaScript runtime on top of the .NET Framework. **Expressions that you define this way will be evaluated in runtime on the server side**.

To use jScripts in CTDs you have to write your code between *[Script:jScript]* and *[/Script]* tags. You can use more than one function in a DefaultValue tag, the appropriate script parts will be evaluated and concatenated.

The following example is an excerpt from the Memo Content Type. The *Date* Field here is configured to include the current time as the default value. This means that when a new Memo Content is created the Date Field will always show the current date - when the Date Field is visible. If the Date Field is not visible in the Content View this setting does not affect the value of the Field.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType name="Memo" parentType="ListItem" handler="SenseNet.ContentRepository.GenericContent" xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
  <DisplayName>Memo</DisplayName>
  <Description>A content type for short memos or posts on a subject</Description>
  <Icon>Document</Icon>
  <Fields>
    <Field name="Date" type="DateTime">
      <DisplayName>Date</DisplayName>
      <Description>Please set the due date of the memo. Date format: '2010.09.17', time format: '14:30:00'</Description>
      <Configuration>
        <DateTimeMode>DateAndTime</DateTimeMode>
        <DefaultValue>[Script:jScript] DateTime.Now; [/Script]</DefaultValue>
      </Configuration>
    </Field>
  </Fields>
</ContentType>
```

This next CTD source-fragment shows another example for using jScript functions for default values:

```xml
<DefaultValue>12[Script:jScript]"apple".IndexOf("l")+8[/Script]8[Script:jScript]"pear".IndexOf("a")+2[/Script]85</DefaultValue>
```

The result will be 12118485, because the first script returns *11* and the second *4*. These are concatenated to the static values.
