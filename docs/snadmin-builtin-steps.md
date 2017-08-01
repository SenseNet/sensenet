# Built-in SnAdmin steps
**sensenet ECM** evolves constantly and we release new versions of the product from time to time. To make the upgrade easier we offer **upgrade packages** for *Enterprise* customers. *Community* customers can take advantage of this packaging framework also, to create their own upgrade packages or aid their **build processes**. This article lists all the available built-in steps that are shipped with the product and can be used to build a package.

>To learn more about the tool we use to execute these packages, please check out the main [SnAdmin](https://github.com/SenseNet/sn-admin/blob/master/docs/snadmin.md) article.
> 
>Developers may create their own [custom steps](http://community.sensenet.com/docs/tutorials/snadmin-create-custom-step) to extend the built-in API.

## Basic steps
### Assign
- Full name: *SenseNet.Packaging.Steps.Assign*
- Default property: *Value*
- Additional properties: *Name*

>This step can be placed in ForEach steps' Block sections.

Assigns a phase variable with the given string value. Use this step to declare or modify a phase-level variable that can be used in other steps (not every step can use a phase variable). The Name must start with an **@** sign. The declared variable can be seen for the consecutive steps in the whole phase. A variable declared this way cannot be deleted or hidden in any way.

Example:
``` xml
<Assign Name="@path">Admin\Rename4\contents\features1.Content</Assign>
<Assign Name="@newName">features.Content</Assign>
<Rename Source="@path" SourceIsRelativeTo="TargetDirectory">@newName</Rename>
```

### Copy
- Full name: `SenseNet.Packaging.Steps.Copy`
- Default property: `Source`
- Additional properties: `NewName, SourceIsRelativeTo, TargetDirectory`

>This step can be placed in ForEach steps' Block sections.

Copies files, usually from the package to the web folder. Use this step to update or add new libraries or other files to the *web* folder. If the given target parent directory structure does not exist in the file system, this step creates them.

The valid values for SourceIsRelativeTo are the following:
-   *Package*: the source path is relative to the package root folder (*default*)
-   *TargetDirectory*: the source path is relative to the web site root folder (this is the mandatory value if the step is put into a ForEach step)

### Delete
- Full name: `SenseNet.Packaging.Steps.Delete`
- Default property: `Path`
- Additional properties: -

>This step can be placed in ForEach steps' Block sections.

Deletes a file/directory from the file system or the Content Repository, depending on the given path. Path is relative to the package's target directory or an absolute repository path.
``` xml
<Delete>App_Data\MyFolder</Delete>
... or ...
<Delete>/Root/MyFolder</Delete>
```
>If the given path is a Content Repository path, please make sure that a **StartRepository** step (see below) is placed into the manifest before this one to make sure that the Content Repository is started and accessible.

### ExecuteDatabaseScript
- Full name: `SenseNet.Packaging.Steps.ExecuteDatabaseScript`
- Default property: `Query`
- Additional properties: `ConnectionName, InitialCatalog`

Executes a database script on the Content Repository database. The Query property is two-faced:
- Can be a path of the file in the package that contains a valid database script.
``` xml
<ExecuteDatabaseScript query="dbscripts\longest-tables.sql" />
```
- Can contain a valid database script.
``` xml
<ExecuteDatabaseScript>
  <![CDATA[
    SELECT TOP 5
      t.NAME AS TableName, SUM(a.total_pages) * 8 AS TotalSpaceKB
    FROM sys.tables t
      INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
      INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
      INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
    WHERE
      t.NAME NOT LIKE 'dt%'
        AND t.is_ms_shipped = 0
        AND i.OBJECT_ID > 255
    GROUP BY
      t.Name, p.Rows
    ORDER BY
      TotalSpaceKB desc
  ]]>
</ExecuteDatabaseScript>
```
The result of this step depends on the query result. If it contains a table, a very simply formatted table will be displayed. The result of the query above is something like this (five largest tables in the database in descendant order):
``` text
================================================== #0/1 ExecuteDatabaseScript
TableName       TotalSpaceKB
LogEntries      36176
Versions        27832
TextPropertiesNText     4328
JournalItems    1488
BinaryProperties        1344
Script is successfully executed.
-------------------------------------------------------------
```
The scalar result will be displayed too. The step:
``` xml
<ExecuteDatabaseScript>
  SELECT COUNT(1) FROM Nodes
</ExecuteDatabaseScript>
```
The result:
``` text
================================================== #0/1 ExecuteDatabaseScript
1689
Script is successfully executed.
-------------------------------------------------------------
```
In any other cases (e.g. update / delete / mode change) the step does not write back extra information but the “Script is successfully executed.” will be shown.

>If the script contains XML characters ( '<', '>', '&' etc.), do not escape the individual characters but place the whole query into a **CDATA** section.

### Export
- Full name: `SenseNet.Packaging.Steps.Export`
- Default property: `Source`
- Additional properties: `Target, Filter`

>This step can be placed in ForEach steps' Block sections.

Extracts contents from the given Source repository path, filtered by the given Filter content query into the given Target path.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

Either Source or Filter are mandatory arguments. If you omit Source it defaults to /Root and the Filter content query will be executed globally. If you omit Filter, the step extracts every content dwells in the repository tree pointed by Source into the target folder. Target is mandatory and its value is a file system path that must be writable for the user running the package. This path can be absolute or relative. If it is relative, the step will replenish it by the path of the App\_Data folder. If the final path points to a not existing folder, it will be created.

Examples:
``` xml
<StartRepository/>
<Export Target="export_target" >/Root/Sites/Default_Site/Cars</Export>
```
``` xml
<StartRepository/>
<Assign Name="@filter">+InFolder:/Root/Sites/Default_Site/Cars +Type:Car .REVERSESORT:ContentName</Assign>
<Export Target="C:\SenseNet\WebSite\App_Data\export_target" Filter="@filter"></Export>
```
``` xml
<StartRepository/>
<Assign Name="@path">/Root/Sites/Default_Site/Cars</Assign>
<Assign Name="@filter">+Type:Car</Assign>
<Export Target="export_target" Filter="@filter">@path</Export>
```

## ForEach
- Full name: `SenseNet.Packaging.Steps.ForEach`
- Properties: `Item, ContentQuery, Files`

>This step can be placed in ForEach steps' Block sections.

Implements a loop structure inside the package, that iterates through the collection having collected by the given source. Use this step to define a recurring loop of steps on the resources obtained by the source property. The source can be two kinds: *Files* or *ContentQuery*. Exactly one of them must be present at once. Files property is used to give a file system path (wildcards are allowed) relative to the web root. ContentQuery on the other hand is used to define a repository query expression. 

A ForEach step must contain exactly one *Block* XML tag. The Block tag is not an independent step, it serves only one aim: to contain the steps that are executed subsequently on each and every resource. The Block tag can contain indefinite number of steps (not every step is allowed to be put inside a ForEach step). The Item property contains the identifier of an inner package variable, that holds the value of the current resource, while the loop is executing. The Item's value must contain only English alphabetic characters preceded by an **@** sign.

Examples:
``` xml
<Steps>
  <StartRepository />
  <ForEach item="@path" files="Tools\*.config">
    <Block>
      <Copy TargetDirectory="App_Data\copytest" SourceIsRelativeTo="TargetDirectory">@path</Copy>
    </Block>
  </ForEach>
</Steps>
```
``` xml
<Steps>
  <StartRepository />
    <ForEach item="@filePath" files="Admin\ForEachAndImport\contents\*.Content">
      <Block>
    <Import source="@filePath" target="/Root/Folder1" SourceIsRelativeTo="TargetDirectory" />
      </Block>
    </ForEach>
</Steps>
```
``` xml
<Steps>
  <StartRepository/>
  <ForEach item="@content" ContentQuery="/Root/MyFolder/MyContent">
    <Block>
    <Delete>@content.Path</Delete>
    </Block>
  </ForEach>
</Steps>
```

### StartRepository
- Full name: `SenseNet.Packaging.Steps.StartRepository`
- Default property: -
- Additional properties: `StartLuceneManager, StartWorkflowEngine, PluginsPath, IndexPath, RestoreIndex, BackupIndexAtTheEnd`

Starts the Content Repository in the SnAdmin tool. Use this step before accessing content in the Content Repository from any step.
``` xml
<StartRepository startLuceneManager="false" />
```

### Import
- Full name: `SenseNet.Packaging.Steps.Import`
- Default property: `Source`
- Additional properties: `AbortOnError, LogLevel, SourceIsRelativeTo, Target, ResetSecurity`

>This step can be placed in ForEach steps' Block sections.

Imports content from the file system (usually from inside the package) to the Content Repository.

The valid values for SourceIsRelativeTo are the following:

- *Package*: the source path is relative to the package root folder (*default*)
- *TargetDirectory*: the source path is relative to the web site root folder (this is the mandatory value if the step is put into a ForEach step)

The valid values for LogLevel are the following:

- *Info*: minimal information (*default*)
- *Progress*: displays a status bar of the ongoing import process
- *Verbose*: displays everything (one line per every imported content)

The source is always a file system structure in the package or in the target web folder.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.
``` xml
<Steps>
  <StartRepository />
  <Import target="/Root/YourContents/MyFolder" logLevel="Info" abortOnError="true">./import</Import>
</Steps>
```
``` xml
<Steps>
  <StartRepository />
  <Import target="/Root/YourContents/MyFolder" logLevel="Info" abortOnError="true" SourceIsRelativeTo="TargetDirectory">App_Data\TempImport</Import>
</Steps>
```
If the “import” directory contains a content “MyContent”, after the execution this content could be accessed on the “/Root/YourContents/MyFolder/MyContent” path.

### ImportSchema
>This step is deprecated, use the more generic **Import** step to import all kinds of content, even content types to the repository.

### SetField
- Full name: `SenseNet.Packaging.Steps.SetField`
- Default property: `Value`
- Additional properties: `Content, Name, Fields, Overwrite`

> This step can be placed in ForEach steps' Block sections.

Sets one or more field values on the provided content. By default the field values will be overwritten unconditionally, but if you set the *overwrite* property to false, fields that already contain a value will be skipped.

```xml
<SetField name="Description" content="/Root/MyContent"><![CDATA[New description]]></SetField>

<SetField name="IncomingEmailWorkflow" content="/Root/ContentTemplates/DocumentLibrary/Document_Library" overwrite="@overwrite">
   <Value>
      <Path>/Root/System/Schema/ContentTypes/GenericContent/Workflow/MailProcessorWorkflow</Path>
   </Value>
</SetField>

<SetField content="/Root/ContentTemplates/EventList/Calendar" overwrite="@overwrite">
   <Fields>
      <Field name="IncomingEmailWorkflow">
         <Value>
            <Path>/Root/System/Schema/ContentTypes/GenericContent/Workflow/MailProcessorWorkflow</Path>
         </Value>
      </Field>
   </Fields>   	
</SetField>
```

### AddReference
- Full name: `SenseNet.Packaging.Steps.AddReference`
- Default property: -
- Additional properties: `Content, Name, Value, Fields`

> This step can be placed in ForEach steps' Block sections.

Adds one or more content as a reference to a reference field. Previous list is preserved, this is an addition. Both path and id work.

```xml
<AddReference name="Members" content="/Root/IMS/BuiltIn/Portal/HR">
   <Value>
      <Path>/Root/IMS/BuiltIn/johnsmiths</Path>
      <Id>12345</Id>
   </Value>
</AddReference>
```

### RemoveReference
- Full name: `SenseNet.Packaging.Steps.RemoveReference`
- Default property: -
- Additional properties: `Content, Name, Value, Fields`

> This step can be placed in ForEach steps' Block sections.

Removes one or more content from a reference field. All other referenced values remain untouched. Both path and id work.

```xml
<RemoveReference name="Members" content="/Root/IMS/BuiltIn/Portal/HR">
   <Value>
      <Path>/Root/IMS/BuiltIn/johnsmiths</Path>
   </Value>
</RemoveReference>
```

### Rename
-   Full name: `SenseNet.Packaging.Steps.Rename`
-   Default property: `NewName`
-   Additional properties: `Source, SourceIsRelativeTo`

Renames a repository content or a file. NewName property holds the value of the object's new name. Source can be either a repository path or a file system path.

The valid values for SourceIsRelativeTo are the following:
-   *Package*: the source path is relative to the package root folder (*default*)
-   *TargetDirectory*: the source path is relative to the web site root folder

>This step can use a phase variable in *Source* and in *NewName* properties.

Examples:
``` xml
<Rename Source="WebRoot\Insertable\Contents\some.Content" SourceIsRelativeTo="TargetDirectory">another.Content</Rename>
```
``` xml
<Rename Source="Contents\feature1.Content" SourceIsRelativeTo="Package" NewName="feature.Content"></Rename>
```
``` xml
<Assign Name="@path">WebRoot\Insertable\Contents\some.Content</Assign>
<Assign Name="@newName">another.Content</Assign>
<Rename Source="@path" SourceIsRelativeTo="TargetDirectory">@newName</Rename>
```

### ReplaceText
- Full name: `SenseNet.Packaging.Steps.ReplaceText`
- Default property: `Value`
- Additional properties: `Path, Field, Template, Regex, PathIsRelativeTo` 

Loads a content or a text file defined by the Path property (on all configured web servers) and replaces a pattern defined by the Template property with the Value.

If the Path is a Content Repository path, than you can define a Field (optionally) to target any content field.

You can define either the *Template* or the *Regex* property for searching replaceable text, but **not both of them**.

### SetUrl
- Full name: `SenseNet.Packaging.Steps.SetUrl`
- Default property: `Url`
- Additional properties: `Site, AuthenticationType` 

Sets a url on a site content in the Content Repository. If the url is already assigned to another site, this step will fail.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

## JSON text
### EditJson
- Full name: `SenseNet.Packaging.Steps.EditJson`
- Default property: `Value`
- Additional properties: `Path, Field`

This step allows you to modify an existing file or content that contains JSON-formatted data (for example [Settings] files). The Value default property is a JSON fragment that may contain simple properties or a whole json object tree. It will be merged into the target JSON defined by the usual Path and (the optional) Field properties.
``` xml
<EditJson Path="/Root/System/Settings/Logging.settings">
{ 
    Trace: {
        TaskManagement: false
    }
}
</EditJson>
```

## Xml manipulation
The following steps help you create, remove or edit xml fragments either in the file system (e.g. config files) or in the Content Repository.
#### Handling namespaces
It is important to take care of namespaces when editing xml files. You can provide namespace definitions for the xml steps below by defining an attribute that has the following form: *namespace-\[prefixname\]*. You can use this prefix in your XPath expressions defined for the step. For example:
``` xml
<DeleteXmlNodes Content="/Root/MyFolder/MyCustom.xml"
   xpath="/x:rootelement/x:subelemenet/x:field[@name = 'Test']"
   namespace-x="http://schemas.example.com/a/b/c" />
```

### AppendXmlFragment
- Full name: `SenseNet.Packaging.Steps.AppendXmlFragment`
- Default property: -
- Additional properties: `Source, Xpath, File, Content, Field`

Appends the given xml fragment (from the *Source* property) as a child (or children) under the xml elements determined by the Xpath property. The target xml can be in the file system (usually a .config file) or in the Content Repository (a field value of a content).

The default property model does not work in this step by design (because the source is an xml fragment itself), so you will need to provide the *Source* tag explicitely as shown below. The Source value cannot appear as an xml attribute either.

>If the target is a content, please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

For example we extend the “test.xml” in the App\_Data directory. First here is the package step:
``` xml
<AppendXmlFragment file="App_Data\test.xml" xpath="/testxml/element[2]">
  <Source>
    <testElement attr="any"/>
    <testElement attr="42">
      <nestedElement>42</nestedElement>
    </testElement>
  </Sourc e>
</AppendXmlFragment>
```
The target XML before execution:
``` xml
<?xml version="1.0" encoding="utf-8"?>
<testxml>
  <element>one</element>
  <element>
    <nestedElement />
    <nestedElement />
  </element>
  <element />
</testxml>
```
Target XML after execution:
``` xml
<?xml version="1.0" encoding="utf-8"?>
<testxml>
  <element>one</element>
  <element>
    <nestedElement />
    <nestedElement />
    <testElement attr="any" />
    <testElement attr="42">
      <nestedElement>42</nestedElement>
    </testElement>
  </element>
  <element />
</testxml>
```

### AppendXmlAttributes
- Full name: `SenseNet.Packaging.Steps.AppendXmlAttributes`
- Default property: Source
- Additional properties: `Xpath, File, Content, Field`

Appends or overwrites the given attributes (from the *Source* property) to the xml elements determined by the Xpath property. The target xml can be in the file system (usually a .config file) or in the Content Repository (a field value of a content).

The source property describes the appended or edited attributes. The valid format is a JSON object.

>If the target is a content, please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

For example we extend the “test.xml” in the App\_Data directory. First here is the package step:
``` xml
<AppendXmlAttributes file="App_Data\test.xml" xpath="//element">
  { attr: "value", attr1: "value1" }
</AppendXmlAttributes>
```
The target XML before execution:
``` xml
<testxml>
  <element />
  <element attr="oldValue"/>
</testxml>
```
Target XML after execution:
``` xml
<testxml>
  <element attr="value" attr1="value1" />
  <element attr="value" attr1="value1" />
</testxml>
```

### EditXmlNodes
- Full name: `SenseNet.Packaging.Steps.EditXmlNodes`
- Default property: -
- Additional properties: `Source, Xpath, File, Content, Field`

Overwrites the content of the selected element or value of the selected attributes. The selector is the Xpath property. If the selection is attributes, their value will be overwritten. In case of elements the full inner xml will be overwritten. The Source property contains the new value or fragment. This step does not have default property similarly for AppendXmlFragment step.

The target xml can be in the file system (usually a .config file) or in the Content Repository (a field value of a content).

>If the target is a content, please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

For example we change the value of the “attr” on the “element” when the value is “42”:
``` xml
<EditXmlNodes file="App_Data\test.xml" xpath="//element[@attr='42']/@attr">
  <Source>Forty two</Sour ce>
</EditXmlNodes>
```
The target XML before execution:
``` xml
<testxml>
  <element attr="42"/>
  <element attr="43"/>
</testxml>
```
Target XML after execution:
``` xml
<testxml>
  <element attr="Forty two"/>
  <element attr="43"/>
</testxml>
```

### DeleteXmlNodes
- Full name: `SenseNet.Packaging.Steps.DeleteXmlNodes`
- Default property: -
- Additional properties: `Xpath, File, Content, Field`

Deletes the xml nodes determined by the Xpath property.

The target xml can be in the file system (usually a .config file) or in the Content Repository (a field value of a content).

>If the target is a content, please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

For example we delete all of the “nestedElement” in any level of the document. The step:
``` xml
<DeleteXmlNodes file="App_Data\test.xml" xpath="//nestedElement" />
```
The target XML before execution:
``` xml
<testxml>
  <element>one</element>
  <element>
    <nestedElement />
    <nestedElement />
    <testElement attr="any" />
    <testElement attr="42">
      <nestedElement>42</nestedElement>
    </testElement>
  </element>
  <element />
</testxml>
```
Target XML after execution:
``` xml
<testxml>
  <element>one</element>
  <element>
    <testElement attr="any" />
    <testElement attr="42"></testElement>
  </element>
  <element />
</testxml>
```

### EditConnectionString
- Full name: `SenseNet.Packaging.Steps.EditConnectionString`
- Default property: -
- Additional properties: `File, ConnectionName, DataSource, InitialCatalogName, DbUserName, DbPassword`

Modifies the connection string (selected by the *ConnectionName* property) in the provided config *File*. At least a database name (InitialCatalogName) *or* datasource should be provided.

```xml
<EditConnectionString ConnectionName="SnCrMsSql" InitialCatalogName="@initialCatalog" DataSource="@dataSource" DbUserName="@dbUserName" DbPassword="@dbPassword" File="Web.config" />
```

## Content type manipulation
The following steps are designed specifically to modify content types. You can choose to use the generic *xml manipulation steps* instead (see above), if you do not find the particular CTD step that you need here.
### AddField
- Full name: `SenseNet.Packaging.Steps.AddField`
- Default property: `FieldXml`
- Additional properties: `ContentType`

Adds a non-existing field to a content type. The ContentType property must refer to an existing content type name. FieldXml is the xml fragment that will be inserted. One step is able to add only one field. Because of the manifest parsing and XML processing specialities there are two ways to use this step:

- The default-property model requires a CDATA section.
``` xml
<AddField contentType="MyCustomizedContent">
  <![CDATA[
    <Field name="OurNewField" type="LongText">
      <DisplayName>Our new field</DisplayName>
      <Description>For test purposes only.</Description>
      <Configuration>
        <MaxLength>1000</MaxLength>
      </Configuration>
    </Field>
  ]]>
</AddField>
```
- In case of explicit property the field must be written in raw XML format. To keep it simple we *ignore the ContentType's xml namespace* so the field node must be written as if it is in the *null* namespace. After inserting the field it gets the correct namespace.
``` xml
<AddField contentType="MyCustomizedContent">
  <FieldXml>
    <Field name="OurNewField" type="LongText">
      <DisplayName>Our new field</DisplayName>
      <Description>For test purposes only.</Description>
      <Configuration>
        <MaxLength>1000</MaxLength>
      </Configuration>
    </Field>
  </FieldXml>
</AddField>
```
>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

### EditField
- Full name: `SenseNet.Packaging.Steps.EditField`
- Default property: `FieldXml`
- Additional properties: `ContentType, InsertBefore, InsertAfter, FieldName, PropertyName`

Adds or modifies a field property in a CTD. This step is able to add new xml fragments to the root of the field only. For changes in the *Configuration* section please use the *EditFieldConfiguration* step below.

As the CTD schema is very strict, sometimes it is necessary to provide the previous (or next) xml node to insert the new one after (or before).

>There are two ways to provide the new field xml: as a named xml node or as a default property in a **CDATA** section. See *AddField* step above for details.

The following sample will add or override an existing display name value for the Field1 field in the File CTD.
``` xml
<EditField contentType="File" fieldName="Field1" propertyName="DisplayName" insertBefore="Description">New displayname</EditField>
```
>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

### EditFieldConfiguration
- Full name: `SenseNet.Packaging.Steps.EditFieldConfiguration`
- Default property: `FieldXml`
- Additional properties: `ContentType, InsertBefore, InsertAfter, FieldName, PropertyName`

Adds or modifies a field configuration value in a CTD. This step is able to add new xml fragments to the *Configuration* xml node of the field. For main field property changes please use the *EditField* step above.

As the CTD schema is very strict, sometimes it is necessary to provide the previous (or next) xml node to insert the new one after (or before).

>There are two ways to provide the new xml: as a named xml node or as a default property in a **CDATA** section. See *AddField* step above for details.

The following sample will add or override an existing configuration value for the Field1 field in the File CTD.
``` xml
<EditFieldConfiguration contentType="File" fieldName="Field1" propertyName="Conf1" insertBefore="Conf2">new value</EditFieldConfiguration>
```
>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

### ChangeFieldType
- Full name: `SenseNet.Packaging.Steps.ChangeFieldType`
- Default property: `FieldXml`
- Additional properties: `ContentType, FieldName, FieldType, ArchiveFieldName`

This step is able to change the type of a metadata field defined on a content type. The old field will be removed first than the new one will be added. Old field values will be migrated only if it is possible (e.g. from a *Number* field to a *Currency* field or from a *ShortText* field to a *LongText* field). If the conversion does not make sense (e.g. changing a short text field to a reference field), you may provide an archive field for storing old values (that archive field must be created first of course, with the add field step above).

The step also makes possible migrating field values to a field with a different name, if you provide both the FieldName property and a FieldXml that contains a different name for the new field.

- **ContentType**: must refer to an existing content type.
- **FieldXml**: the xml fragment for the new field. Can be omitted if both FieldName and FieldType are provided. In that case the original xml fragment will be used.
- **FieldName**: name of the field to be changed (original name).
- **FieldType**: new field type. If you provide FieldName and FieldType, you do not have to provide the full FieldXml.
- **ArchiveFieldName**: name of the archive field that has the same type that the original field. Provide this if you need to preserve old field values in an archive field instead of the new one.

>Because of the nature of the manifest XML parsing there are two ways to provide the new field xml: as a named xml node and as a default property in a **CDATA** section. See *AddField* step above for details.

Changing a number field to a currency field:
``` xml
<ChangeFieldType contentType="File" fieldName="Field1" fieldType="Currency" />
```
Changing a field type and providing new settings for the new field:
``` xml
<ChangeFieldType contentType="File">
    <FieldXml>
      <Field name="Field1" type="Currency">
          <DisplayName>My field</DisplayName>
          <Configuration>
            <MinValue>0</MinValue>
            <Format>en-US</Format>
            <Digits>0</Digits>
          </Configuration>
        </Field>
    </FieldXml>
</ChangeFieldType>
```
Migrating a field to a new one with an incompatible type and preserving the old values in an archive field:
``` xml
<AddField contentType="File">
    <FieldXml>
        <Field name="Field1Archive" type="ShortText">
          <DisplayName>My field Archive</DisplayName>
        </Field>
    </FieldXml>
</AddField>
<ChangeFieldType contentType="File" fieldName="Field1" fieldType="Reference" archiveFieldName="Field1Archive" />
```
>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

### ChangeFieldTypeInCTD
- Full name: `SenseNet.Packaging.Steps.ChangeFieldTypeInCTD`
- Default property: `FieldXml`
- Additional properties: `ContentType, FieldName, FieldType, ArchiveFieldName`

Simple step for changing a field type in a ContentType definition xml. It is able to handle only compatible field types as it replaces the field type *without* making any migration or field remove/add operations (as the more complex *ChangeFieldType* step above does).

For details about the properties, see the **ChangeFieldType** step above.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

```xml
<ChangeFieldTypeInCTD ContentType="Application" FieldName="RequiredPermissions" FieldType="PermissionChoice" />
```

### EditContentTypeHeader
- Full name: *SenseNet.Packaging.Steps.EditContentTypeHeader*
- Default property: *InnerXml*
- Additional properties: *PropertyName*

Adds or modifies a property in the header of a CTD, For example *Allowed child types*, *Icon*, etc.

As the CTD schema is very strict, sometimes it is necessary to provide the previous (or next) xml node to insert the new one after (or before).

The following sample will add or override the allowed child types list in the Survey list CTD.
``` xml
<EditContentTypeHeader contentType="SurveyList" propertyName="AllowedChildTypes" InsertAfter="AllowIncrementalNaming">SurveyListItem,Folder</EditContentTypeHeader>
```
If you want to modify the **content handler** of the content type, use this step with the property name 'handler'.
``` xml
<EditContentTypeHeader contentType="SurveyList" propertyName="handler">MyNamespace.MySurveyHandler</EditContentTypeHeader>
```
>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

## Permissions and security
Although it is possible to modify content permissions by importing .Content files containing security entries, sometimes it is easier to define permission changes using these specialized steps.

### SetPermissions
- Full name: `SenseNet.Packaging.Steps.SetPermissions`
- Default property: `Path`
- Additional properties: `Identity, Allow, Deny, LocalOnly`

Sets permissions on a content, defined by the Path property for the user, group or org unit defined by the Identity property. Permission type names are provided as a comma separated list. Not enumerated permissions will be cleared.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

For example the following step adds the read permission to the user but clears any other existing permissions:
``` xml
<SetPermissions
    path="/Root/Sites/MySite/MyPage"
    identity="/Root/IMS/MyDomain/Users/readerRobert"
    allow="Open"
    />
```

### EditPermissions
- Full name: `SenseNet.Packaging.Steps.EditPermissions`
- Default property: `Path`
- Additional properties: `Identity, Allow, Deny, Clear, LocalOnly`

Sets or clears permissions on a content, defined by the Path property for the user, group or org unit defined by the Identity property. Permission type names are provided as a comma separated list. Only the enumerated permissions will be changed.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

For example the following step extends the user permissions with Publish and Approve and removes AddNew:
``` xml
<EditPermissions
    path="/Root/Sites/MySite/MyPage"
    identity="/Root/IMS/MyDomain/Users/intranetMike"
    allow="Publish,Approve"
    clear="AddNew"
    />
```

### RemovePermissionEntries
- Full name: `SenseNet.Packaging.Steps.RemovePermissionEntries`
- Default property: `Path`
- Additional properties: `Identity`

Removes all permissions from a content defined by the Path property, for the user, group or org unit defined by the Identity property. If the Identity property is missing, all explicit entries will be removed.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

For example the first step removes all explicit permissions from the content, while the second only deletes the defined user's permissions:
``` xml
<RemovePermissionEntries>/Root/Sites/MySite/workspaces<RemovePermissionEntries>
<RemovePermissionEntries
  path="/Root/Sites/MySite/samples"
  identity="/Root/IMS/MyDomain/Users/internetJohn"
  />
```

### BreakPermissionInheritance
- Full name: `SenseNet.Packaging.Steps.BreakPermissionInheritance`
- Default property: `Path`
- Additional properties: -

Breaks permission inheritance on a content. If the content does not inherit permissions, this step does nothing.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

### RemoveBreakPermissionInheritance
- Full name: `SenseNet.Packaging.Steps.RemoveBreakPermissionInheritance`
- Default property: `Path`
- Additional properties: -

Removes break permission inheritance from a content. If the content already inherits permissions, this step does nothing.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

### AddMembers
- Full name: `SenseNet.Packaging.Steps.AddMembers`
- Default property: `Members`
- Additional properties: `Group`

Adds one or more members to the selected Group. The group can be selected by a path or a content query. In case of query only the first content will be used. If the selected group does not exist, the step execution *terminates silently, without throwing an error*. The *Members* property is a comma or semicolon separated list of selectors. Every selector can be one of the following:
- path
- content query
- user name (only the domain\\username format is allowed e.g.: BuiltIn\\Admin)

The result of selectors are aggregated into a single distinct list. Every element of the result list will become a member of the selected group if the element is a *group or user* (and is not already a member). Any other elements are skipped and logged.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.
``` xml
<AddMembers group="/Root/IMS/BuiltIn/Portal/VIP">
  /Root/IMS/BuiltIn/Demo/Developers;
  BuiltIn\mike;
  /Root/IMS/BuiltIn/Demo/Managers/robspace;
  +InFolder:"/Root/IMS/BuiltIn/Demo/ProjectManagers";
</AddMembers>
```

## Resources
### AddResource
- Full name: `SenseNet.Packaging.Steps.AddResource`
- Default property: -
- Additional properties: `ContentName, ClassName, Resources`

Adds or edits string resources. It is able to create the necessary string resource content if it does not exist.

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.
``` xml
<AddResource contentName='MyResourceFile.xml' className='Action'>
  <Resources>
    <add key="res1" lang="en">Resume upload</add>
    <add key="res1" lang="hu">Feltöltés folytatása</add>
    <add key="res2" lang="en">sample</add>
    <add key="res2" lang="hu">példa</add>
  </Resources>
</AddResource>
```

## Conditional steps
One of the most powerful features of the packaging framework is the ability to **execute steps based on a condition**. These are the built-in conditional steps, but developers may create their own conditional steps if necessary (see details on the previous link).

### IfCondition
- Full name: `SenseNet.Packaging.Steps.IfCondition`
- Default property: -
- Additional properties: *Condition*

General conditional step. It executes steps in its *Then* block in case the provided *Condition* evaluates to True. The string condition can be a string representation of a boolean value ('true' or 'false'), or a **variable** of a boolean or string type.

```xml
<If condition="@needToDoSomething">
  <Then>
    <DoSomething />
  </Then>
</If>
```

### IfFileExists
- Full name: `SenseNet.Packaging.Steps.IfFileExists`
- Default property: -
- Additional properties: `Path`

Checks if the file on the provided path exists in the file system. The path is relative to the target directory.
``` xml
<IfFileExists path="App_Data\MyFolder\MyFile.txt">
  <Then>
    ...steps if the file exists...
  </Then>
  <Else>
    ...steps if the file does not exist...
  <Else>
</IfFileExists>
```

>Please note that in the current version this step checks only the **local file system**, where the package is executed; it does not check other (configured) web folders. We assume that all the web folders are synchronized.

### IfDirectoryExists
- Full name: `SenseNet.Packaging.Steps.IfDirectoryExists`
- Default property: -
- Additional properties: `Path`

Checks if the directory on the provided path exists in the file system. The path is relative to the target directory.
``` xml
<IfDirectoryExists path="App_Data\MyFolder">
  <Then>
    ...steps if the directory exists...
  </Then>
  <Else>
    ...steps if the directory does not exist...
  <Else>
</IfDirectoryExists>
```
>Please note that in the current version this step checks only the local file system, where the package is executed; it does not check other (configured) web folders. We assume that all the web folders are synchronized.

### IfContentExists
- Full name: `SenseNet.Packaging.Steps.IfContentExists`
- Default property: -
- Additional properties: `Path`

Checks if the content with the provided path exists in the Content Repository. Path is an absolute repository path.
``` xml
<IfContentExists path="/Root/MyFolder/MyContent">
  <Then>
    ...steps if the content exists...
  </Then>
  <Else>
    ...steps if the content does not exist...
  <Else>
</IfContentExists>
```
>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

### IfFieldExists
- Full name: `SenseNet.Packaging.Steps.IfFieldExists`
- Default property: -
- Additional properties: `ContentType, Field, LocalOnly`

Checks if the given field already exists on the provided content type in the Content Repository. If the *LocalOnly* boolean property is set to *True* (the default is *False*), we check only the given CTD xml and do not care about a field defined in a parent type.
``` xml
<IfFieldExists contentType="Folder" field="Annotation">
  <Then>
    ...steps if the Folder has an Annotation field...
  </Then>
  <Else>
    ...steps if the Folder does not have an Annotation field...
  </Else>
</IfFieldExists>
```
>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

### IfXmlNodeExists
- Full name: `SenseNet.Packaging.Steps.IfXmlNodeExists`
- Default property: -
- Additional properties: `File, Content, Field, Xpath`

Checks if the given xpath already exists in the provided xml file in the files system or in a content in the Content Repository.
``` xml
<IfXmlNodeExists file="./web.config" xpath="/configuration/appSettings/add[@key='ClusterChannelProvider']">
  <Then>
    ...steps if the given xpath exists...
  </Then>
  <Else>
    ...steps if the given xpath does not exist...
  </Else>
</IfXmlNodeExists>
```

### IfDatabaseValue
- Full name: `SenseNet.Packaging.Steps.IfDatabaseValue`
- Default property: -
- Additional properties: `Query`

Executes the given SQL script (with *ExecuteScalar*) and checks the result. If it is null, 0 or empty string, the conditional result will be FALSE and the steps defined in the *Else* branch will be executed. If the result is 'positive' (a number greater than 0 or a non-empty string), the *Then* branch will get executed.
``` xml
<IfDatabaseValue query="SELECT COUNT(0) FROM MyTable WHERE RelatedId IN (3, 4, 5)">
  <Then>
    ...steps if the result is positive...
  </Then>
  <Else>
    ...steps if the result is 0 or negative...
  </Else>
</IfDatabaseValue>
```
The query argument can also point to a SQL script file in the package (e.g. ```scripts\MyPatchScript.sql```).

### IfDatabaseExists
- Full name: `SenseNet.Packaging.Steps.IfDatabaseExists`
- Default property: -
- Additional properties: `DataSource, Name, UserName, Password`

Checks if the database with the provided *Name* exists.

If the current user (who executes SnAdmin) does not have enough permissions to access the *master* db on the server (necessary for db existence check), a username and password must be provided.

```xml
<IfDatabaseExists Name="@initialCatalog" DataSource="@dataSource" UserName="@userName" Password="@password">
  <Then>
    <If condition="@recreateDbIfExists">
      <Then>
        <Trace>Database exists, re-creating it...</Trace>        
      </Then>
    </If>
  </Then>
</IfDatabaseExists>
```

### IfEquals
- Full name: `SenseNet.Packaging.Steps.IfEquals`
- Default property: -
- Additional properties: `LeftOperand, RightOperand`

Executes the steps defined the *Then* section if the two provided values (one of them is usually a *variable*) are equal.

```xml
<IfEquals leftOperand="@siteUrl" rightOperand="localhost">
   <Then>
   </Then>
   <Else>
   </Else>
</IfEquals>	
```

### IfMatch
- Full name: `SenseNet.Packaging.Steps.IfMatch`
- Default property: -
- Additional properties: `Value, Pattern`

Evaluates a Regex expression (the *Pattern* property) on the provided *Value* (usually a string *variable*). If the result matches, it executes the steps in the *Then* section.

```xml
<IfMatch Value="@dataSource" Pattern="\w+\.database\.example\.net">
  <Then>
    <Assign Name="@createScript">scripts\Create_Example_Database.sql</Assign>
  </Then>
  <Else>
    <Assign Name="@createScript">scripts\Create_SenseNet_Database.sql</Assign>
  </Else>
</IfMatch>
```

## Advanced steps
### DisableIndexing
- Full name: `SenseNet.Packaging.Steps.DisableIndexing`
- Default property: -
- Additional properties: -

Switches off the indexing. This step is experimental, we use it only in internal scenarios.

### EnableIndexing
- Full name: `SenseNet.Packaging.Steps.EnableIndexing`
- Default property: -
- Additional properties: -

Switches on the indexing. This step is experimental, we use it only in internal scenarios.

### PopulateIndex
- Full name: `SenseNet.Packaging.Steps.PopulateIndex`
- Default property: `Path`
- Additional properties: -

Populates the index of the whole content tree, or a subtree, provided by the Path property.

### CheckIndexIntegrity
- Full name: `SenseNet.Packaging.Steps.CheckIndexIntegrity`
- Default property: `Path`
- Additional properties: Recursive, OutputLimit

Checks the index integrity by comparation the index and database. This step needs running repository. All parameters are optional and their meanings are the following:
- **Path**: Defines the integrity check's scope if there is. If empty, the whole repository tree will be checked.
- **Recursive**: Defines whether check only one content or the whole tree or subtree. Default: true.
- **OutputLimit**: Limits the output line count. 0 means all lines. Default: 1000. If this limit is reached the “...truncated...” will be displayed.

Usage on a subtree and show all difference:
``` xml
<CheckIndexIntegrity outputLimit="0">
    /Root/Sites/Default_Site
</CheckIndexIntegrity>
```
Fore example here is a possible output if there is some inconsistence:
``` text
================================================== #1/2 CheckIndexIntegrity
Recursive integrity check. Scope: /Root
Integrity check finished. Count of differences: 5
  MoreDocument: DocId: 3576, VersionId: 3979, NodeId: 4967, Version: v1.0.a, DbNodeTimestamp: 3713243, IxNodeTimestamp: 3713243, DbVersionTimestamp: 3713247, IxVersionTimestamp: 3713247, Path: /root/sites/default_site/workspaces/project/pragueprojectworkspace/document_library/aenean semper.doc
  NotInIndex: VersionId: 3980, NodeId: 4968, Version: V1.0.A, DbNodeTimestamp: 3713251, DbVersionTimestamp: 3713255, Path: /Root/Sites/Default_Site/workspaces/Project/pragueprojectworkspace/Document_Library/Aliquam porta suscipit ante.doc
  MoreDocument: DocId: 3578, VersionId: 3981, NodeId: 4969, Version: v1.0.a, DbNodeTimestamp: 3713259, IxNodeTimestamp: 3713259, DbVersionTimestamp: 3713263, IxVersionTimestamp: 3713263, Path: /root/sites/default_site/workspaces/project/pragueprojectworkspace/document_library/duis et lorem.doc
  NotInDatabase: DocId: 3577, VersionId: 3979, NodeId: 4967, Version: v1.0.a, IxNodeTimestamp: 3713243, IxVersionTimestamp: 3713247, Path: /root/sites/default_site/workspaces/project/pragueprojectworkspace/document_library/aenean semper.doc
  NotInDatabase: DocId: 3579, VersionId: 3981, NodeId: 4969, Version: v1.0.a, IxNodeTimestamp: 3713259, IxVersionTimestamp: 3713263, Path: /root/sites/default_site/workspaces/project/pragueprojectworkspace/document_library/duis et lorem.doc
-------------------------------------------------------------
Time: 00:00:00.2456550
```

### ChangeContentType
- Full name: `SenseNet.Packaging.Steps.ChangeContentType`
- Default property: -
- Additional properties: `ContentQuery, ContentTypeName`

Changes the content type of one or more content, collected by the ContentQuery property. The new type is defined by the ContentTypeName property. Limitations:
- only *leaf* content (that have no children) can be changed (this is because the step actually creates a new content based on the source content)
- only *child types* are allowed, meaning the new type must be inherited from the original type of the content

>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

### EditAllowedChildTypes
- Full name: `SenseNet.Packaging.Steps.EditAllowedChildTypes`
- Default property: `Add`
- Additional properties: `Remove, Path, ContentType`

Changes the *AllowedContentTypes* list of the defined content type or content by the provided new and retirement list. Step parameters:
- **Add**: Comma separated content type names that *extend* the original AllowedChildTypes list.
- **Remove**: Comma separated content type names that will be *removed* from the original AllowedChildTypes list.
- **Path**: Full path of the existing content to edit. If provided, the *ContentType* parameter is prohibited.
- **ContentType**: Name of the existing content type to edit. If provided, the *Path* parameter is prohibited.

Examples:
``` xml
<EditAllowedChildTypes path="/Root/IMS" add="SystemFolder" />
<EditAllowedChildTypes path="/Root/Sites/MySite/Test1">ImageLibrary,Image,Page</EditAllowedChildTypes>
<EditAllowedChildTypes path="/Root/Sites/MySite/Test2" add="ImageLibrary,Image" remove="Page"/>
<EditAllowedChildTypes contentType="Domain" add="RegisteredUser" remove="SystemFolder"/>
```
>Please make sure that a **StartRepository** step precedes this one to make sure that the repository is started.

### Trace
- Full name: `SenseNet.Packaging.Steps.Trace`
- Default property: `Text`
- Additional properties: -

Writes the given text to the console and to the package log. You can annotate the execution with this step. For example:
``` xml
<Trace>The main structure is installed.</Trace>
```

### StopSite
- Full name: `SenseNet.Packaging.Steps.StopSite`
- Default property: `Site`
- Additional properties: `MachineName`

Stops an IIS site on a local or a remote machine. Useful in automatic build scenarios, when you need to update files in a web folder (e.g. NLB test sites).
``` xml
<StopSite machineName="\\MYVPC01">test.example.com</StopSite>
```
>This step uses the **PsExec** tool to execute appcmd.exe on remote machines. Please download [PsTools](https://technet.microsoft.com/en-us/sysinternals/bb897553.aspx) and copy PsExec.exe to one of the well-known Windows folders (System32 or SysWOW64, depending on the environment) to let the system access the tool.

### StartSite
- Full name: `SenseNet.Packaging.Steps.StartSite`
- Default property: `Site`
- Additional properties: `MachineName`

Starts an IIS site on a local or a remote machine. Useful in automatic build scenarios, when you need to update files in a web folder (e.g. NLB test sites).
``` xml
<StartSite machineName="\\MYVPC01">test.example.com</StartSite>
```
>This step uses the **PsExec** tool to execute appcmd.exe on remote machines. Please download [PsTools](https://technet.microsoft.com/en-us/sysinternals/bb897553.aspx) and copy PsExec.exe to one of the well-known Windows folders (System32 or SysWOW64, depending on the environment) to let the system access the tool.

### Terminate
- Full name: `SenseNet.Packaging.Steps.Terminate`
- Default property: `Message`
- Additional properties: `Reason`. The type is *SenseNet.Packaging.TerminationReason* enumeration that has two options:
    - `Successful`: the package will terminate with success
    - `Warning`: the package execution result will be Faulty

Writes the given message to the console and to the package log. If the Reason is Warning, the package result will be faulty.

#### Examples
In case of all conditions are fulfilled and there are no actions left, the execution can be interrupted.
``` xml
<IfDirectoryExists path="App_Data\MyFolder">
  <Then>
    <Terminate reason="Successful">No need to execute other steps.</Terminate>
  </Then>
</IfDirectoryExists>
... other relevant steps ...
```
In the opposite case, when an error is detected in the environment and any further action can be harmful use the Terminate step with Warning and enter compensation-strategy instructions into the element (or into the Message property).
``` xml
<IfDirectoryExists path="App_Data\MyFolder">
  <Then>
    <Terminate reason="Warning">
      Cannot continue the execution because MyFolder exists in the App_Data.
      Please delete it and rerun this package.
    </Terminate>
  </Then>
</IfDirectoryExists>
... other relevant steps ...
```

### CreateEventLog
- Full name: `vSenseNet.Packaging.Steps.CreateEventLog`
- Default property: -
- Additional properties: `LogName, Machine, Sources`

System step for creating the provided log and source in Windows Event log.

>There is predefined SnAdmin [tool package](snadmin-tools.md) that contains this step, you can execute it from the command line.

```xml
<CreateEventLog LogName="@logName" Machine="@machine" Sources="@sources" />
```
### DeleteEventLog
- Full name: `SenseNet.Packaging.Steps.CreateEventLog`
- Default property: -
- Additional properties: `LogName, Machine, Sources`

System step for deleting the provided log and source from the Windows Event log.

>There is predefined SnAdmin [tool package](snadmin-tools.md) that contains this step, you can execute it from the command line.

```xml
<DeleteEventLog LogName="@logName" Machine="@machine" Sources="@sources" />
```
