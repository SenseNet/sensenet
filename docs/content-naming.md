---
title: "Content Naming"
source_url: 'https://github.com/SenseNet/sensenet/docs/content-naming.md'
category: Guides
version: v6.0
tags: [content, naming, content type]
---

# Content Naming

In the sensenet [Content Repository](content-repository.md) every Content has a Name [Field](field.md) that together with its location identifies the Content. The Content Name is part of the Content Path, and since the path can be used to address the Content via url, there are certain restrictions against the Name. The Content in the Repository also have a DisplayName that is the user-friendly human readable name of Content and can contain any kind of characters without restrictions. This article summarizes the connection between these two fields and other common aspects of Content naming.

> If you are interested in customizing how the name of a *downloaded* file looks like, please check out the [Document binary provider article](document-binary-provider.md) for developers.

## Name and DisplayName

All content in the sensenet [Content Repository](content-repository.md) is identified by the following [Fields](field.md):

|Field|Description|Example|
|-----|-----------|-------|
|**Name**|identifier of the Content|	Examples-tutorials|
|**Path**|	link to the Content in Content Repository|/Root/DemoContents/Examples-tutorials|
|**DisplayName**|a legible name of the Content for better human readability|Examples & tutorials|

The **Name** is the main identifier of the [Content](content.md). Its value is also included in the Path property which acts as a permalink to the Content. Thus changing a Content's Name (aka. renaming a Content) also changes the Path and therefore renaming operations should be carried out carefully. A path change may result in a lengthy operation (paths of child content are also changed respectively) and may also result in broken links in the [Content Repository](content-repository.md) (if another content refers to the changed one through its path - e.g. an article containing a link in its text). These two properties are used when the Content is referred to via a url link and therefore may not contain special characters.

> Do not worry about [referenced content](reference-field.md): those are connected by content ids instead of paths, so renaming a referenced content will not brake reference fields.

The **DisplayName** is the main display name of the Content. It acts as a legible, human readable name and may contain punctuations and accented characters as well. Generally, when a Content is displayed on the front-end of the portal, the value of the DisplayName property is shown. Changing the DisplayName is a simple operation and does not cause broken links (because changing only the DisplayName does not change the Url Name).

## Naming conventions of different types

Although all content types contain the 3 properties above there are some special cases when the Name or DisplayName of a Content is not shown when editing or browsing a Content. The following table shows the different types:

<table class="sn-table">
<tbody><tr class="dark">
<td><b>Type</b>
</td>
<td><b>Name and DisplayName importance</b>
</td>
<th width="550"><b>Description</b>
</th>
<td><b>Example Content Types</b>
</td></tr>
<tr>
<td><b>File</b>
</td>
<td>only <b>Name</b> is important
</td>
<td>The <i>File</i> types are identified by their <i>file names</i> in general file systems. When a Content of this naming type is uploaded the <i>Name</i> will act as its <i>file name</i>. When listing this Content the value of its <i>Name</i> will be shown. The path of the Content that acts as a permalink will also contain the <i>file name</i>. A simple .txt file for example does not have a legible <i>DisplayName</i>, only a file name (<i>Name</i> in sensenet <a href="/Content_Repository" title="Content Repository">Content Repository</a>).
</td>
<td>File, Image
</td></tr>
<tr>
<td><b>Item</b>
</td>
<td>only <b>DisplayName</b> is important
</td>
<td>The <i>Item</i> types are Content that are created frequently but permalinks to the Content are rarely used. When a Content of this type is created only the human readable <i>DisplayName</i> is specified and the <i>Name</i> is auto-generated (sometimes a Guid or a contenttype-date value). A memo or a meeting request for example has a subject (the <i>DisplayName</i> will act as a legible subject in this case) but its <i>Name</i> or the permalink to the Content is indifferent.
</td>
<td>Memo, Comment
</td></tr>
<tr>
<td><b>Regular</b>
</td>
<td>both <b>Name</b> and <b>DisplayName</b> are important
</td>
<td>The <i>Regular</i> types are Content that have a legible <i>DisplayName</i> and they also act as common links and organizing units in the Content Repository - therefore their <i>Name</i> is also important. An Organizational Unit for example has a legible DisplayName (like 'Marketing department') and also acts as an organizing folder with identifiable permalink (like '/Root/IMS/marketing-department'). The <i>Name</i> appears in its children content's permalink (like '/Root/IMS/marketing-department/exampleuser')
</td>
<td>Folder, OrganizationalUnit
</td></tr></tbody></table>

### Content naming on surface

For the Name and DisplayName Fields come two special [Field Controls](field-control.md) that provide the users handy interfaces to set the Names and DisplayNames of Content: the [Name Field Control](name-field-control.md) and the [DisplayName Field Control](displayname-field-control.md). In situations where both or only the DisplayName is visible by default the Name is automatically generated from the DisplayName typed in by the user. The following screenshot shows the general layout of the two controls in a common scenario:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/NameAndDisplayNameControls.png" style="margin: 20px auto" />

The user always has the possibility to change the Name by clicking the pencil to switch the Name Field Control to edit mode:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/EditableNameControl.png" style="margin: 20px auto" />

### Automatic name generation

Since it can be very time consuming to provide a Name and a DisplayName for a Content at the same time - especially in cases when the Name can be derived from the DisplayName - sensenet provides automatic name generation mechanisms both on client and on server side. A Name can always be automatically generated from a given DisplayName by encoding or removing invalid characters. When the Content is created using a [Content View](content-view.md) name generation is done using two special Field Controls: the [Name Field Control](name-field-control.md) and the [DisplayName Field Control](displayname-field-control.md).

> Content naming behavior is customizable. Please check out the [ContentNamingProvider](content-naming-provider.md) article (for developers) that describe the built-in naming providers and other details.

#### Autonaming on client side

The [DisplayName Field Control](displayname-field-control.md) automatically fills the value of the [Name Field Control](name-field-control.md) visible in the same [Content View](content-view.md), from the value entered to the DisplayName Field Control. The entered DisplayName is processed so that invalid characters are encoded - the resulting string will be the automatically created Name for the Content. The Name however does not change automatically in the following cases:

- the user edits the Name manually,
- the underlying Content already exists in the Content Repository, so it is not a new scenario but an edit scenario.

#### Autonaming on server side

In case no [Name Field Control](name-field-control.md) is visible in the same [Content View](content-view.md), the Name for the Content is automatically generated from the entered DisplayName on the server side, using the same algorithm to generate the Name as on client side.

#### Rename and autoname

Generally speaking it is not desired to change the Name of a Content automatically when changing its DisplayName as it would be considered renaming of the Content, and would possibly cause broken links (since the Name is part of the Path that also acts as a permalink to the Content). Therefore the following rules apply to Name autogeneration:

- for new Content the Name is autogenerated from DisplayName,
- for existing Content if the Content is opened in edit mode, the Name is NOT autogenerated from DisplayName,
- for existing Content if the Content is opened with the Rename action, the Name is autogenerated from DisplayName,
- in any scenario if the AlwaysUpdateName property of the DisplayName control is set to true, the Name is autogenerated from the DisplayName.

#### Configuration of autonaming

You can fine-adjust autonaming with the contentNaming section in the web.config:

```xml
 <sensenet>
    <contentNaming>
      <!-- Regex pattern defining invalid name characters. Escape (\) character can be used as is (ie.: "[^a-zA-Z0-9.()[\]]"). 
Pattern must start with '[' and end with ']'. -->
      <add key="InvalidNameCharsPattern" value="[^a-zA-Z0-9.()[\]\-_ ]" />
      <add key="ReplacementChar" value="-" />
    </contentNaming>
  </sensenet>
```
- **InvalidNameCharsPattern**: a regular expression that defines the invalid characters a Content Name may not contain. These invalid characters are automatically encoded (or replaced) during converting a display name to a name, depending on the configured [ContentNamingProvider](content-naming-provider.md). If the Name is not autogenerated (for example for Files when DisplayName Field Control is not visible in the Content View) and the user inputs a name that contains invalid characters, an error message will be displayed upon trying to save the Content. The regular expression may contain characterset with a negating clause ([^...]) thus defining allowed characters instead of invalid ones, or define invalid characters simply ([...]). In both cases the regular expression MUST start with '[' and end with ']'.
- **ReplacementChar**: defines the character used to replace invalid characters.

> Note that changing InvalidNameCharsPattern will affect path validation logic in the whole Content Repository. It is therefore desired to change validation messages when changing invalid characters pattern, see [Invalid names and error messages](#Invalid-names-and-error-messages) below.

#### Autonaming rules

> Please note that the automatic algorithm changes to lessen the possibility of name collisions: we provide a short list of invalid characters (see example below) that will be encoded or replaced (depending on the configured [ContentNamingProvider](content-naming-provider.md)) and everything else is allowed. You may still make the regular expression less permissive if you need to.

We opened our content naming API so that you can provide your own naming algorithm. See the default providers and customization options in the [ContentNamingProvider](content-naming-provider.md) article.

## Invalid names and error messages

The value of the Name Field falls under special validation according to certain restrictions. An auto-generated name by default is always correct but the user always has the possibility to override any auto-generated name and provide a name manually. If the name (hence the path) of the Content does not satisfy the requirements an error message is displayed upon saving the Content. These can be the following:

- Name cannot be empty.
    - Cause: the user did not provide name, it is 0 characters long. A valid name should contain at least 1 character.
    - Resource key: Portal, EmptyNameMessage
- Path too long. Max length is 450.
    - Cause: The overall path length of the Content with the provided name exceeds the maximum allowed number of characters. The maximum length is determined by the data provider.
    - Resource key: Portal, PathTooLongMessage
- Content path may only contain characters allowed in configuration.
    - Cause: the provided path contains invalid characters. Invalid name characters are defined using the InvalidNameCharsPattern web.config key. The '/' character in a path is always considered to be valid.
    - Resource key: Portal, InvalidPathMessage
- Content name may only contain characters allowed in configuration.
    - Cause: the provided name contains invalid characters. Invalid name characters are defined using the InvalidNameCharsPattern web.config key.
    - Resource key: Portal, InvalidNameMessage
- Name cannot start with whitespace.
    - Cause: the provided name starts with space, which is not allowed.
    - Resource key: Portal, NameStartsWithWhitespaceMessage
- Name cannot end with whitespace.
    - Cause: the provided name ends with a space, which is not allowed.
    - Resource key: Portal, NameEndsWithWhitespaceMessage
- Path must start with '/' character.
    - Cause: the resulting/provided path does not start with '/'.
    - Resource key: Portal, PathFirstCharMessage
- Path cannot end with '.' character.
    - Cause: the provided name ends with a '.' character, which is not allowed.
    - Resource key: Portal, PathEndsWithDotMessage

## Incremental naming

Since the path identifies the Content, it has to be unique. Therefore a Content cannot be saved with a name that another Content already uses in the same Folder. By default in this case an error message is shown upon saving the Content:

**Cannot create new content. A content with the name you specified already exists.**

It is possible to set up a Content Type so that when saving instances of that type no error message is shown when a Content with the same name already exists - but the name is automatically suffixed with a number until it does not collide with any name in the same folder. This setting is controlled using the **AllowIncrementalNaming** element in the CTD of the type. Possible settings:

```xml
<AllowIncrementalNaming>false</AllowIncrementalNaming>
```

if another Content with the same name exists in the same Folder, an error message is shown and the Content is not saved.

```xml
<AllowIncrementalNaming>true</AllowIncrementalNaming>
```

if another Content with the same name exists in the same Folder, the Content is saved with the provided name suffixed with a number. Ie. *My-Content* will be saved as *My-Content(1)* if the Folder already contains a Content named *My-Content*. If *My-Content(1)* is also occupied, it will be saved as *My-Content(2)*, etc.

> The incremental naming behavior is also customizable using the [ContentNamingProvider](content-naming-provider.md) feature.

## Example

**Default configuration: invalid characters for Content name**

The following web.config settings marks the characters that are not allowed in a URL as invalid characters. Everything else is considered a valid character:

```xml
<add key="InvalidNameCharsPattern" value="[\$&amp;\+,/:;=?@&quot;&lt;&gt;\#%{}|\\^~\[\]'’`\*\t\r\n]]" />
```

**Defining allowed characters for Content name**

The following web.config settings allows alphanumeric characters, the '.', '(', ')', '[', ']', '-', '_' characters and the space for names. Everything else is considered an invalid character:

```xml
<add key="InvalidNameCharsPattern" value="[^a-zA-Z0-9.()[\]\-_ ]" />
```

**Defining invalid characters for Content name**

The following web.config settings marks the '%' and the '/' as invalid characters for names. Everything else is considered a valid character:

```xml
<add key="InvalidNameCharsPattern" value="[%/]" />
```