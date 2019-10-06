---
title: "Allowed Child Types"
source_url: 'https://github.com/SenseNet/sensenet/docs/allowed-child-types.md'
category: Guides
version: v6.0
tags: [content, ctd, child types, field]
---

# Allowed Child Types

The sensenet [Content Repository](content-repository.md) stores different [Content Types](content-type.md). One of the major differences between a file system and the Content Repository, is that in a file system you can store any tpye (file or folder) anywhere, whereas in the sensenet Content Repository it is possible to define restrictions on what Content Types the different containers can contain. This allows portal builders to create a much more precisely defined [Content](content.md) structure and provide the users a better user experience when creating new content under different places in the Content Repository.

You can configure Allowed Child Types in the Content Type Definition of the different types. For example, a MemoList can only contain Memos, a Document Library can only contain Folders and Files, etc. These settings can be overridden on the specific Content, for example you can modify any of your Document Libraries to contain Images, too. There are also some special types that behave differently: a Folder for example, can never define child types, it will always inherit its parent settings. A SystemFolder will allow every type by default and can be created anywhere in the repository.

## CTD settings

To set the default allowed child types for a specific Content Type, go to its CTD and define the AllowedChildTypes element. If it does not exist yet, create it right before the *Fields* element:

```xml
  <AllowedChildTypes>
    Folder,File
  </AllowedChildTypes>
```

The above settings will ensure that whenever you create a new Content of this specific type, only Files and Folders will be allowed to be created under it. This setting can be overridden on the created Content as explained in the next section.

## Content settings

Allowed Child Types can also be defined on Content instances. When types are locally defined for a specific Content it means that the CTD settings of its type will no longer be in effect. This way you can freely modify Allowed Child Type settings for a specific Content, and modifications in CTD will not affect the child type settings of that Content in any ways. The local allowed child type settings of a Content are stored in the *AllowedChildTypes* field.

To modify allowed child types of a Content, simply open the edit page and modify the *Content Types* list:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/allowed-child-types/AllowedChildTypes1.png" style="margin: 20px auto" />

By default it shows the allowed types according to the CTD settings. You can remove any enlisted type and also add new allowed types using the dropdown under the list.

> You cannot remove all of the specified types. If you do this and save the content, settings will automatically be inherited from the CTD.

If you make any changes, a toolbar at the top of the control will indicate that these changes differ from that of the CTD and therefore and handled as local settings:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/allowed-child-types/AllowedChildTypes2.png" style="margin: 20px auto" />

To undo these changes and remove local settings so that allowed child types list is directly determined by the CTD, simply press the Inherit from CTD button on the toolbar. If settings are inherited from CTD then any changes in CTD will influence the allowed child types settings of this content.

### Explicit and effective allowed child types

The **AllowedChildTypes** field itself does not always store the values you see in the user interface. For example ,  Folders and Pages cannot have their own setting (see below), they always inherit from their parent. Other containers may inherit their allowed child types list from their content type (CTD). If you as a developer need the actual list of types that your users will be able to create in a container, use the **EffectiveAllowedChildTypes** read only field based on the read only property with the same name.

## Creating new content

The allowed child types definition on a content (whether it comes from CTD or from local settings) influences the New menu and the Add new portlet in a way that only those types appear in lists that have been configured as allowed child types.

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/allowed-child-types/AllowedChildTypes4.png" style="margin: 20px auto" />

## Permissions for creating a type

A Content Type in the new menu will only show up if the user has See permissions on the Content Type node.  , to create a new Car anywhere you need to have **See** permissions on the */Root/System/Schema/ContentTypes/GenericContent/ListItem/Car* content.

## Content allowing all types

It might happen that a certain content does not impose a restriction on allowed types. In this case any type is allowed to be created under that content. Since this however imposes a security risk as executable types can also be created at these locations, creating a content of any type under such locations is only allowed for users of specified groups. This permission is configured with the following web.config key:

```xml
<add key="AdminGroupPathsForAllowedContentTypes" value="/Root/IMS/BuiltIn/Portal/Administrators,/Root/IMS/Demo/Developers"/>
```

If the user is not a member of any enlisted group he/she will not be able to create anything under locations where allowed child types list is empty. An error message will be shown:

<img src="https://raw.githubusercontent.com/SenseNet/sensenet/master/docs/images/allowed-child-types/AllowedChildTypes5.png" style="margin: 20px auto" />

## Inheriting types: Folder, Page

A Folder or a Page always inherits these settings from its parent Content. You cannot set the allowed child types property of any Folder or Page (even if you make the corresponding control visible on the surface, it will have no effect), not even in CTD. Therefore , whatever allowed child types settings you specify for a list containing a Folder, these settings will also apply for the children of the Folder placed in this list. This ensures that if you build structures in a content list using folders, your list settings will never be overridden on a lower level.

## Special type: SystemFolder

A SystemFolder can be created anywhere in the Content Repository, regardless of its parent content's allowed child types settings, if the user is granted see permissions on the SystemFolder Content Type. This ensures that developers can create (apps) SystemFolders and other system-used SystemFolders without having to modify the allowed child types of the individual content. An (apps) folder (containing [Applications](application.md)) can basically be created anywhere in the Content Repository and this applies to SystemFolders in general. Also, a SystemFolder does not specify any allowed types by default, so all types are allowed under a new SystemFolder. Allowed types however can be configured for a SystemFolder instance.

## for Developers

Developers can use functions defined on the GenericContent API to get and set allowed child type settings.

To get the allowed child types, use the *GetAllowedChildTypes* function:

```csharp
var gc = Node.Load<GenericContent>("/Root/Sites/Default_Site/MyContent");
 
// get IEnumerable<ContentType>
var types = gc.GetAllowedChildTypes();
 
// get IEnumerable<string>
var typeNames = gc.GetAllowedChildTypeNames();
```

Adding a Content Type to the child types can be done using the AllowChildType function:

```csharp
gc.AllowChildType("Car");
gc.Save();
```

To check if a type is allowed, use the *IsAllowedChildType* function:

```csharp
if (gc.IsAllowedChildType("Car"))
{
   // TODO
}
```

If you need more advanced operations (like clearing the allowed types list meaning to inherit from CTD, or explicitly defining a list of Content Types) simply use the *AllowedChildTypes* property:

```csharp
// inherit from CTD
gc.AllowedChildTypes = null;
gc.Save();
 
// set types explicitly
gc.AllowedChildTypes = new List<ContentType> { ContentType.GetByName("Car"), ContentType.GetByName("Image") };
gc.Save();
```