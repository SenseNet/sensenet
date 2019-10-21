---
title: "Content Handler"
source_url: 'https://github.com/SenseNet/sensenet/docs/content-handler.md'
category: Development
version: v6.0
tags: [content type, content type definition, inheritance, type system, handler, business logic]
---

# Content Handler

The Content Handler defines custom "code behind" business logic of a [Content Type](content-type.md) implemented in .Net code (ie. C#). Content Handlers can override mapping between object properties and [Content Repository](content-repository.md) fields, and they may also define properties that are not stored in the Content Repository (properties that are either calculated or retrieved from another data source).

> The Content Handler is considered to be on the lower layer of the sensenet ECM API. You may choose to work with the upper layer, represented by the unified [Content](content.md) class that you can use to work with all kinds of content stored in the Content Repository. In that case, you will have access to [fields](field.md) (higher-level metadata) but without the strongly typed properties of the Content Handler layer.

## When to create a custom Content Handler

A Content Handler should be created in one of the following cases:

- **Custom business logic** has to be added to the object at either loading or saving properties or when saving the object itself. *Example*: You want to check the validity of an attribute based on other attributes of the object or assign value to an attribute based on other attributes of the object.
- The object has **properties that are not stored in the Content Repository**. A content can have properties that are retrieved or saved from or to web services or derived some other way. A Content Handler has to be implemented for this scenario. *Example*: You have a Content Type with a "CreationDate" DateTime property, and want to define an Age property which is calculated dynamically.
- You wish to work with *strongly typed* objects. When using the default GenericContent Content Handler, object binding will not be strongly typed. In this case, attributes can only be accessed and set through the *GetProperty()* and *SetProperty()* methods of the GenericContent class. Creating a custom Content Handler helps you overcome this limitation.

## Reference Content Handler

The following code acts as a reference snippet that can be used as a common starting point when implementing a Content Handler. It defines the necessary **constructors**, examples for all types of properties and the property routing methods for these properties.

```csharp
namespace MyNamespace
{
    [ContentHandler]
    public class MyContentHandler : GenericContent
    {
        // =================================================================================================== Constructors
        // for initialize a new MyContentHandler instance
        public MyContentHandler(Node parent) : this(parent, null) { }
        // for initialize a new MyContentHandler inherited instance
        public MyContentHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        // for initialize instance in the loading operation
        protected MyContentHandler(NodeToken nt) : base(nt) { }
 
 
        // =================================================================================================== Properties
        private const string STRINGPROPERTY = "StringProperty";
        [RepositoryProperty(STRINGPROPERTY, RepositoryDataType.String)] // Text, Int, DateTime
        public virtual string StringProperty
        {
            get { return base.GetProperty<string>(STRINGPROPERTY); }
            set { base.SetProperty(STRINGPROPERTY, value); }
        }
 
        private const string BOOLPROPERTY = "BoolProperty";
        [RepositoryProperty(BOOLPROPERTY, RepositoryDataType.Int)]
        public virtual bool BoolProperty
        {
            get { return base.GetProperty<int>(BOOLPROPERTY) != 0; }
            set { base.SetProperty(BOOLPROPERTY, value ? 1 : 0); }
        }
 
        private const string DECIMALPROPERTY = "DecimalProperty";
        [RepositoryProperty(DECIMALPROPERTY, RepositoryDataType.Currency)]
        public virtual Decimal DecimalProperty
        {
            get { return Convert.ToDecimal(this[DECIMALPROPERTY]); }
            set { this[DECIMALPROPERTY] = Convert.ToDecimal(value); }
        }
 
        private const string REFERENCEPROPERTY = "ReferenceProperty";
        [RepositoryProperty(REFERENCEPROPERTY, RepositoryDataType.Reference)]
        public virtual Node ReferenceProperty
        {
            get { return base.GetReference<Node>(REFERENCEPROPERTY); }
            set { base.SetReference(REFERENCEPROPERTY, value); }
        }
 
        private const string BINARYPROPERTY = "BinaryProperty";
        [RepositoryProperty(BINARYPROPERTY, RepositoryDataType.Binary)]
        public virtual BinaryData BinaryProperty
        {
            get { return this.GetBinary(BINARYPROPERTY); }
            set { this.SetBinary(BINARYPROPERTY, value); }
        }
 
 
        // =================================================================================================== Property routing
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case STRINGPROPERTY:
                    return this.StringProperty;
                case BOOLPROPERTY:
                    return this.BoolProperty;
                case DECIMALPROPERTY:
                    return this.DecimalProperty;
                case REFERENCEPROPERTY:
                    return this.ReferenceProperty;
                case BINARYPROPERTY:
                    return this.BinaryProperty;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case STRINGPROPERTY:
                    this.StringProperty = (string)value;
                    break;
                case BOOLPROPERTY:
                    this.BoolProperty = (bool)value;
                    break;
                case DECIMALPROPERTY:
                    this.DecimalProperty = (Decimal)value;
                    break;
                case REFERENCEPROPERTY:
                    this.ReferenceProperty = (Node)value;
                    break;
                case BINARYPROPERTY:
                    this.BinaryProperty = (BinaryData)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}
```

## Assigning a Content Handler to a content

When you create a [Content Type Definition](ctd.md) you have to assign a Content Handler to the type so that the content will be saved, displayed, loaded in the appropriate manner. Here below is an extract from the Content Type Definition of the Car content type. As you can see the handler is assigned in the first line of the code.

```xml
<ContentType name="Car" parentType="GenericContent"
 handler="SenseNet.ContentRepository.GenericContent"
 xmlns="http://schemas.sensenet.hu/SenseNet/ContentRepository/ContentTypeDefinition">
    {...}
</ContentType>
```

As you can see, the *Car* Content Type is derived from the *GenericContent* [Content Type](content-type.md), and its Content Handler is *SenseNet.ContentRepository.GenericContent*. You may develop new handlers to any [Content Type](content-type.md), but if you don't, *GenericContent* Content Handler is capable of handling your type.

> It is very important to note that the *Content Type hierarchy* in the repository and the *Content Handler class hierarchy* **has to be the same**. This means whenever you want to create a new Content Type (e.g. CustomFolder as a child type of Folder) with a code-behind handler, your c# class has to **inherit from the content handler class of the parent Content Type** (in this case the Folder class).

## Example

The following xml is the [Content Type Definition](ctd.md) of the *Domain* [Content Type](content-type.md). As you can see a custom Content Handler is assigned to the type:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType name="Domain" parentType="Folder" handler="SenseNet.ContentRepository.Domain" xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
  <DisplayName>Domain</DisplayName>
  <Description>A centrally-managed group of users and/or computers. sensenet has a built-in domain (BuiltIn), but you can syncronyze external LDAP directories as well.</Description>
  <Icon>Domain</Icon>
  <Fields>
    <Field name="Name" type="ShortText">
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Show</VisibleEdit>
        <VisibleNew>Show</VisibleNew>
      </Configuration>
    </Field>
    <Field name="Title" type="ShortText">
      <Title>Title</Title>
      <Configuration>
        <VisibleBrowse>Hide</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
    <Field name="Description" type="LongText">
      <Configuration>
        <VisibleBrowse>Advanced</VisibleBrowse>
        <VisibleEdit>Advanced</VisibleEdit>
        <VisibleNew>Advanced</VisibleNew>
      </Configuration>
    </Field>
    <Field name="SyncGuid" type="ShortText">
      <Title>SyncGuid</Title>
      <Description>GUID of corresponding AD object.</Description>
      <Configuration>
        <VisibleBrowse>Advanced</VisibleBrowse>
        <VisibleEdit>Advanced</VisibleEdit>
        <VisibleNew>Advanced</VisibleNew>
      </Configuration>
    </Field>
    <Field name="LastSync" type="DateTime">
      <Title>LastSync</Title>
      <Description>Date of last synchronization.</Description>
    </Field>
  </Fields>
</ContentType>
```

The assigned Content Handler is a simple class:

```csharp
using  SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
 
namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class Domain : Folder
    {
        public static readonly string NodeTypeName = "Domain";
        public static readonly string BuiltInDomainName = "BuiltIn";
 
        public Domain(Node parent) : this(parent, Domain.NodeTypeName) { }
        public Domain(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Domain(NodeToken token) : base(token) { }
 
        //////////////////////////////////////// Public Properties ////////////////////////////////////////
 
        public bool IsBuiltInDomain
        {
            get { return Name == BuiltInDomainName; }
        }
    }
}
```

The above code extends the [Content Type Definition](ctd.md) with the following logic:

- an extra derived property is added
- various constructors are provided to allow easy instance creation
- some string constants are defined

> Please note that the Content Handler in the above example only extends the *Domain* [Content Type](content-type.md)'s default logic and does not override it. This means that the default behavior still applies to the *Domain* [Content Type](content-type.md) just as if no Content Handler was assigned:

- *Domain* content can be created and managed on the portal
- *Domain* content fields can be set and are persisted to [Content Repository](content-repository.md)
- every *Domain* content is a Folder (the *Domain* [Content Type](content-type.md) derives from *Folder*) so everything that applies to a *Folder* applies to a *Domain*, too (may contain child content, etc.)

To use the above defined Content Handler to create a new *Domain* content the following code can be written in C#:

```csharp
var parent = Node.LoadNode("/Root/IMS");
var domain = new Domain(parent);
domain.Name = "MyCustomDomain";
domain.Save();
```

The following example shows how to use the extended logic of the Content Handler:

```csharp
public bool CheckValidDomain(Domain domain)
{
    if (domain == null)
        return false;
 
    if (domain.ParentName != "IMS")
        return false;
 
    // domain is a built in domain
    if (domain.IsBuiltInDomain)
        return false;
 
    return true;
}
```
