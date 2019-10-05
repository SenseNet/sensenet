---
title:  "Most common API calls "
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/most-common-api-calls.md'
category: Development
version: v6.0
tags: [API, .NET API, API calls, OData Rest API]
---

# Most Common API Calls

## Overview 
This article shows how to solve common scenarios using **Sense/Net ECMS** APIs. It lists only the most widely used development tools, but it is a good place to start if you want to work with sensenet.
> Please note that this article is about **server-side** programming. If you are interested in client-side code, you have the following options:
> - There is a constantly growing [OData REST API](http://wiki.sensenet.com/OData_REST_API) that you may use to access any content in the [Content Repository](http://wiki.sensenet.com/Content_Repository) from **JavaScript**.
> - There is a *client SDK* ([Client library](http://wiki.sensenet.com/Client_library)) for *.Net developers* to manage content stored in the Content Repository. This is built on top of the OData REST API above.)


## Details
Before you start developing, please check the following article for the basic concepts of sensenet development.

- [Getting started - developing applications](http://wiki.sensenet.com/Getting_started_-_developing_applications)

To build your development environment, please follow the steps listed in this article:
- [How to set up development environment](http://wiki.sensenet.com/How_to_set_up_development_environment)

### Where can I write code?
When you have a convenient development environment, you may try one of the following development use cases. You can use the code samples below in several scenarios:
- in a [custom portlet](http://wiki.sensenet.com/How_to_create_and_install_a_Portlet)
- in a [content view](http://wiki.sensenet.com/Content_View_-_for_Developers)
- in a [content handler](http://wiki.sensenet.com/How_to_create_a_ContentHandler)
- write a [user control](http://wiki.sensenet.com/How_to_use_ASCX_controls_in_portlets) and display it using the [User control Portlet](http://wiki.sensenet.com/User_control_Portlet)
- simply upload an ASPX page and place inline code into it


## Authentication
If you execute your code in the context of your sensenet web application (which usually is the case) **you will not need this functionality**, since we will perform user authentication for you, and the *ContentRepository.User.Current* object will hold the authenticated user. However when you are using sensenet API from a console application or a web application that is not sensenet but a third party host app, then the following API calls could be useful.

### Using the Membership provider
The easiest way to authenticate users is when you use our built-in membership provider (which is the default). In that case you can validate the user with a single line that uses the common ASP.NET API for checking users:

```c#
if (Membership.ValidateUser(username, password))
{
   //...
}
```

### Manually
To authenticate the user against the password stored in the Content Repository manually create the following function. First we load the given user, and check if it exists and is enabled. Then we check the provided password and set the User.Current object, so that from here onwards every API call will be executed on behalf of this user. Note the usage of *using (new SystemAccount())*: this will ensure that we are executing the enclosed logic in a security elevated mode, so no permission checks are running inside the block:

```c#
private static bool Login(string domainName, string username, string password)
{
    User user;
    using (new SystemAccount())
    {
        user = User.Load(domainName, username);
        if (user == null || !user.Enabled)
            return false;
    }
    var match = User.CheckPasswordMatch(password, user.PasswordHash);
    if (match)
        User.Current = user;
 
    return match;
}
```

This is how you would call it to login with *Builtin\Admin*:

```c#
if (!Login("Builtin", "Admin", "admin"))
    throw new Exception("The user could not be authenticated!");
```


## Loading content
Development usually starts with loading one or more content items. In this section you will see a couple of examples for basic operations. For more examples please visit this article:
- [Content - for Developers](http://wiki.sensenet.com/Content_-_for_Developers)

### Single content
Loading a single content from the [Content Repository](http://wiki.sensenet.com/Content_Repository) looks like this:

```c#
var content = Content.Load(path);
```

As a result, you will get a [Content](http://wiki.sensenet.com/Content) object that you can use to display its metadata or load or create other related content.

### Children

```c#
foreach(var child in parent.Children)
{
   // process child element
}
```

### Referenced content
Any content may have a [Reference Field](http://wiki.sensenet.com/Reference_Field) (e.g. *Author* of a book or *Members* of a group). If you want to load referenced content, you would do so like this:

```c#
var members = groupContent["Members"] as IEnumerable<Node>;
foreach(var member in members)
{
   // process members
}
```

> Please take a look at [this article](http://wiki.sensenet.com/Node_-_for_Developers#Operations_with_nodes) for detailed examples on how to work with strongly typed objects and reference fields.


## Creating a Folder
Creating a folder (or any simple type for that matter) is easy in sensenet. A function creating a folder could look like this:

```c#
private static Content CreateFolder(Content parent, string name)
{
    var folder = Content.CreateNew("Folder", parent.ContentHandler, name);
    folder["Name"] = name;
    folder.Save();
    return folder;
}
```

This is how you would call it to create *MyFolder* under the document library in the workspace *londondocumentworkspace*:

```c#
var library = Content.Load("/Root/Sites/Default_Site/workspaces/Document/londondocumentworkspace/Document_Library");
var folder = CreateFolder(library, "MyFolder");
```

> Note that the examples here work with the Content API. If you want to work with strongly typed objects and properties (e.g. *Folder*), take a look at the [Content Handler](http://wiki.sensenet.com/Content_Handler) article that contains an example of how to load and create a Folder object.


## Creating a User
To create a user we first load the parent Content in the Content Repository. We will now place the user directly under the specified domain, so we load the domain using *Node.LoadNode* and its path. Then we use the Content API to create a new user and set its Field values.

```c#
private static Content CreateUser(string domainName, string username, string password, string fullname, bool enabled, Dictionary<string, object> properties = null)
{
    var domainPath = RepositoryPath.Combine(Repository.ImsFolderPath, domainName);
    var domain = Node.LoadNode(domainPath);
    var user = Content.CreateNew("User", domain, username);
    user["Name"] = username;
	user["LoginName"] = username;
    user["Password"] = password;
    user["FullName"] = fullname;
    user["Enabled"] = enabled;
 
    if (properties != null)
    {
        foreach (var key in properties.Keys)
        {
            user[key] = properties[key];
        }
    }
 
    user.Save();
 
    return user;
}
```

This is how you would call it to create *MyUser*:

```c#
var user = CreateUser("Builtin", "MyUser", "MyUserPass", "My user", true);
```


## Creating a Workspace
Creating a Workspace is a bit different from what you have seen at creating a simple user, since a Workspace usually consists of a main content - the Workspace content - and a couple of child elements like Document Libraries, MemoLists, etc. Therefore we will use a [Content Template](http://wiki.sensenet.com/Content_Template) to create a Workspace. You can define your own templates at /Root/ContentTemplates.

A possible implementation of a function that creates a workspace could look like the following. We recieve the target path including the name in the first parameter. We can use the *RepositoryPath* object to get the parent path and the name from this string. We will load the specified template and create a Workspace from this template using the [Content Template](http://wiki.sensenet.com/Content_Template) API. We will set the name and other Field values, and save the created Workspace:

```c#
private static Content CreateWorkspace(string targetPath, string templatePath, Dictionary<string, object> properties = null)
{
    var parentPath = RepositoryPath.GetParentPath(targetPath);
    var name = RepositoryPath.GetFileName(targetPath);
    var parent = Node.LoadNode(parentPath);
    var template = Node.LoadNode(templatePath);
    var workspace = ContentTemplate.CreateTemplated(parent, template, name);
    workspace["Name"] = name;
    if (properties != null)
    {
        foreach (var key in properties.Keys)
        {
            workspace[key] = properties[key];
        }
    }
 
    workspace.Save();
    return workspace;
}
```

This is how you would call it to create *MyDocumentWorkspace* using the *Document_Workspace* template:

```c#
var workspace = CreateWorkspace("/Root/Sites/Default_Site/workspaces/Document/MyDocumentWorkspace", "/Root/ContentTemplates/DocumentWorkspace/Document_Workspace");
```


## Creating a File
To create a file you will have to create a new *BinaryData* object, and set the file stream using the *SetStream* function. Apart from that it is very similar to User creation, but we are using the *File ContentHandler* instead of the Content API to get a more developer-friendly API-set:

```c#
private static Content CreateFile(Content folder, string fileSystemPath)
{
    var name = System.IO.Path.GetFileName(fileSystemPath);
 
    using (var stream = System.IO.File.OpenRead(fileSystemPath))
    {
        var binaryData = new BinaryData();
        binaryData.SetStream(stream);
        binaryData.FileName = name;
 
        var file = new File(folder.ContentHandler);
        file.Name = name;
        file.Binary = binaryData;
        file.Save();
        return Content.Create(file);
    }
}
```

This is how you would call it to create *temp.txt* under *MyFolder* (if you want to call this right after creating MyFolder like in the previous example, you don't have to load the folder from the Content Repository, since you already have a reference to it):

```c#
var folder = Content.Load("/Root/Sites/Default_Site/workspaces/Document/londondocumentworkspace/Document_Library/MyFolder");
var file = CreateFile(folder, "c:\\temp.txt");
```


## Working with permissions
For examples and details about how to work with permissions and how to edit group membership please check the [Permission API](http://wiki.sensenet.com/Permission_API) article.


## Searching workspaces
We can use the [Query API](http://wiki.sensenet.com/Query_API) to search workspaces. This function will return with the list of all workspaces as Nodes. We switch off the AutoFilters option during the query execution, so even workspaces under SystemFolders will be returned:

```c#
var workspaces = ContentQuery.Query("TypeIs:Workspace", new QuerySettings { EnableAutofilters = false }).Nodes;
```

> Please also take a look at the [LINQ to sensenet](http://wiki.sensenet.com/LINQ_to_
) API that offers a similar way to query the repository using well-known developer techniques.
