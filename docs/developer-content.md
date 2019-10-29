---
title:  "Developer Content"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/developer-content.md'
category: Concepts
version: v6.0
tags: [developer content, content operations, content versioning]
---

# Developer Content

The content class is a high level object model for representing content repository content. It provides high level data manipulation features like field data accessing and field validation, provides a generalized interface to be used on the UI and wraps a node object which provides basic storage-related features.

## Details

### Loading content

There are two ways to load content from the content repository with the content API. The easiest is to use the Load function:

```
    var content = Content.Load("/Root/Sites/Default_Site/myContent");
```

> If the specified content does not exist Content.Load will return with null.

You can also create the content wrapper over a node object:

```
    var node = Node.LoadNode("/Root/Sites/Default_Site/myContent");
    var content = Content.Create(node);
```

### Operations with content

##### Modifying field data

You can use an indexer to access field data:

```
var content = Content.Load("/Root/IMS/Builtin/Portal/Admin");
content["Email"]="admin@example.com";
content.Save();
```

> Please note that in an enterprise environment you need to take [concurrency](/concurrency-control) into account when saving content.

It is also possible to access fields and use the retrieved field object to manipulate data:

```
var content = Content.Load("/Root/IMS/Builtin/Portal/Admin");
content.Fields["Email"].SetData("admin@example.com");
content.Save();
```

##### Copying, moving

Copying and moving is not available from the content API. Use the underlying content handler for these operations:

```
var content = Content.Load("/Root/Sites/Default_Site/myContent");var target = Node.LoadNode("/Root/Sites/Default_Site/myFolder");
content.ContentHandler.MoveTo(target);
```

##### Deleting

To delete a content using the content API use the following syntax:

```
var content = Content.Load("/Root/Sites/Default_Site/myContent");
content.Delete();
```

To move to trash instead of permanently delete the content use the following overload:

```
content.Delete(false);
```

### Versioning and approval

For versioned saving of a content you can use the following functions:

```
content.CheckOut();// checks out the content to the current user
content.UndoCheckOut();// undos changes by the current user
content.ForceUndoCheckOut();// undos changes made by another user
content.CheckIn();// checks in the content
content.Publish();// publishes the content (only in major&minor versioning)
content.Approve();// approves the content
content.Reject();// rejects the content
```

Calling _content.Save()_ will save the content in the appropriate version according to the versioning of the content. You can use the SaveSameVersion in order to save changes on the current version:

```
// versioning: none
content.Save();// from 1.0A to 1.0A
content.SaveSameVersion();// from 1.0A to 1.0A
 
// versioning: major
content.Save();// from 1.0A to 2.0A
content.SaveSameVersion();// from 1.0A to 1.0A
 
// versioning: major and minor
content.Save();// from 1.0A to 1.1D, from 1.1D to 1.2D, etc..
content.SaveSameVersion();// from 1.0A to 1.0A, from 1.1D to 1.1D, etc..
```

Calling _content.Save()_ on a locked content will always save on the current version:

```
// content.ContentHandler.Locked == true
content.Save();// from 2.0L to 2.0L, from 1.1L to 1.1L, etc..
```

There are also some helper properties to determine the current state of the content:

```
content.Approvable;// true if content is approvable
content.Publishable;// true if content is publishable
content.IsLatestVersion;// true if content object corresponds to the latest version
content.IsLastPublicVersion;// true if content object corresponds to the last public version
```

### Creating content

To create a new content using the content API, use the following syntax:

```
var content = Content.CreateNew("Car", parent, "MyCar");
content.Save();
```

It is also possible to [create a node using the node API or content handler](/node-for-developers#Creating_nodes) and create the content wrapper afterwards:

```
var parent = Node.LoadNode("/Root/IMS/Builtin/Portal");
var user =new User(parent);
var content = Content.Create(user);
content.Save();
```

### Content and the UI

Content is the general object model for UI-related functions. For example you need a content object to create a contentview and visualize it on a page (eg.: in a portlet):

```
var content = Content.Load("/Root/Sites/Default_Site/myList/myContent");
var contentview = ContentView.Create(content, this.Page, ViewMode.InlineEdit);
this.Controls.Add(contentview);
```

You can also reach the visualized content from the markup of a contentview:

```
<%= this.Content["Email"]%>
```

or:

```
<%=GetValue("Email")%>
```

To learn more about contentviews, read the following article:

- [Content View - for Developers](/contentview-for-developers)

### Content and node

Content is a high-level object model wrapping the low-level storage model node. The wrapped node object is always accessible from the content interface:

```
var underlyingnode = content.ContentHandler;
```

To learn more about the low-level object and its capabilities, read the following article:

- [Node - for Developers](/node-for-developers)
