---
title:  "Node"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/developer-node.md'
category: Concepts
version: v6.0
tags: [node, node operations, node versioning, node contenthandler]
---

# Developer Node

Node is the storage-layer representation of Content. It is the lowest level complex data entity corresponding to a content in the [Sense/Net Content Repository](/content-repository). It holds properties that are stored in the db. For low-level content repository operations, like manipulating stored data, moving, deleting nodes it is the ideal object model.

## Details

### Loading nodes

To load a node from the content repository in order to work with it in-memory or to carry out certain repository operations on it you can use the following syntax:

```
var node = Node.LoadNode("/Root/Sites/Default_Site/mycontent");
```

You can also use the node's Id to load it:

```
var node = Node.LoadNode(3411);
```

> If the specified node does not exist Node.LoadNode will return with null.

A node can be cast to its specific content handler if the corresponding content handler type exists in CLR, and thus access specific attached logic available on its content handler:

```
var node = Node.LoadNode("/Root/IMS/BuiltIn/Portal/Administrators");
var group= node as Group;
 
// or using shortcut..
var group2 = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Administrators");
```

All the above functions load the appropriate node for the current user. For example, if the current user is allowed to open minor versions the last minor version will be loaded (otherwise the last public version is loaded). If the current user is the author of the current draft version of a content then the last draft version will be loaded (otherwise the last minor/major version is loaded). You can load a specific version with the following:

```
// here we provide versionid and not nodeid!
var node = Node.LoadNodeByVersionId(6542);
```

To load multiple nodes all at once using an optimal database operation, use the following:

```
var nodes = Node.LoadNodes(newint[] { 2342, 2343, 2344 });
```

Note: we usually load multiple nodes using [content query](/query-API).

> If the specified nodes do not exist Node.LoadNodes will return with a list of 0 items.

### Operations with nodes

The range of accessible operations on a node include repository-related actions, like modifying properties, moving nodes, deleting nodes, etc. Here is a list of available operations and examples of usage.

##### Modifying properties

You can use an indexer to access/modify properties:

```
var node = Node.LoadNode("/Root/IMS/Builtin/Portal/Admin");
var oldemail = node["Email"];
node["Email"]="admin@example.com";
node.Save();
```

> Please note that in an enterprise environment you need to take [concurrency](/concurrency-control) into account when saving content.

The above property setting is equal to the following form:

```
node[PropertyType.GetByName("Email")]="admin@example.com";
```

If the loaded node has a contenthandler, you can access properties more easily:

```
var user = Node.LoadNode("/Root/IMS/Builtin/Portal/Admin") as User;
user.Email="admin@example.com";
user.Save();
```

###### Reference properties

There are a couple of helper methods that you can use to read or modify reference properties on a node. The following methods are available on the Node class that you may find useful:

- **AddReference**(string propertyName, Node refNode): add one reference
- **AddReferences&lt;T&gt;**(string propertyName, IEnumerable&lt;T&gt; refNodes): add multiple references
- **ClearReference**(string propertyName): remove all references
- **GetReference&lt;T&gt;**(string propertyName): shortcut for getting the first or only reference
- **GetReferenceCount**(string propertyName): count of referenced nodes
- **GetReferences**(string propertyName): get all references
- **HasReference**(string propertyName, Node refNode): check for existing references
- **RemoveReference**(string propertyName, Node refNode): remove one node from the reference list
- **SetReference**(string propertyName, Node node): set one node as a reference value
- **SetReferences&lt;T&gt;**(string propertyName, IEnumerable&lt;T&gt; nodes): set multiple nodes as references

Please use these methods for editing reference fields instead of reading and setting referenced nodes manually as they are designed to work efficiently even if there are hundreds of references. Please note that these changes are made in memory only, you have to save the node manually later!

##### Copying

Use the following syntax to copy a node. You don't have to call Save() in this case.

```
var node = Node.LoadNode("/Root/Sites/Default_Site/mycontent");
var target = Node.LoadNode("/Root/Sites/Default_Site/myfolder");
node.CopyTo(target);
```

##### Moving

Use the following syntax to move a node. You don't have to call Save() in this case.

```
var node = Node.LoadNode("/Root/Sites/Default_Site/mycontent");
var target = Node.LoadNode("/Root/Sites/Default_Site/myfolder");
node.MoveTo(target);
```

##### Deleting

You can delete a node using the following syntax:

```
var node = Node.LoadNode("/Root/Sites/Default_Site/mycontent");
node.Delete();
```

The above will permanently delete the node from the repository. To move it to the Trash use the following line:

```
TrashBin.DeleteNode(node);
```

### Versioning

Versioning operations (checking content in, checking out, publishing, etc.) can be accessed using the [GenericContent](/genericcontent-for-developers) API. Some helpers are available from the node API though:

```
node.IsLatestVersion; // true if node object corresponds to the latest version
node.IsLastPublicVersion; // true if node object corresponds to the last public version
```

### Creating nodes

The Node API does not provide you means to create nodes. You can use the following syntax:

```
var parent = Node.LoadNode("/Root/IMS/Builtin/Portal");
var node = NodeType.CreateInstance("User", parent);
node.Save();
```

Or use a content handler to create a new instance if one is given:

```
var parent = Node.LoadNode("/Root/IMS/Builtin/Portal");
var user = new User(parent);
user.Save();
```

You can also use the GenericContent API to create nodes:

```
var parent = Node.LoadNode("/Root/IMS/Builtin/Portal");
var user = new GenericContent(parent, "User");
user.Save();
```

> If you want to create nodes from template please refer to [Content Template - for Developers](/content-template-for-developers)

### ContentHandler and node

The node class is a generalized model for any content stored in the content repository, whereas a content handler is a specific object model for one specific content type. This means that every content handler is derived from the node base class, and thus every node functionality is available on content handlers. Content handlers extend the base logic, customize the saving process of the content type and also provide strong properties to provide easy access of data:

```
// use User content handler to save a user
var user = Node.LoadNode("/Root/IMS/MyDomain/MyUser") as User;
user.Email="myuser@example.com"; // strong property for easy access of data
user.Save(); // save also synchronizes user to Active Directory if it is configured
 
// use the Node API to save a user
var node = Node.LoadNode("/Root/IMS/MyDomain/MyUser");
node["Email"]="myuser@example.com"; // use a lookup to set a property - a bit slower than direct accessor
node.Save(); // same as user.Save, also synchronizes user to Active Directory if it is configured
```

For this reason it is recommended to use content handlers when present for a type and not the generalized Node API. Also: if a type takes any role in a programmed business process it is highly recommended to create the corresponding content handler implementation and use it in the business process. To read more on content handlers:

- [Content Handler](/content-handler)

### GenericContent and node

The GenericContent is the most basic [content type](/content-type) in the content repository. The GenericContent class is the content handler for the top-level content type (is derived from the Node base class) and thus all of our content handlers are derived from GenericContent. There are some features like functions related to versioning/versioned saving that are only available via GenericContent objects, so it is always a good idea to learn and use the GenericContent API.

- [GenericContent - for Developers](/genericcontent-for-developers)

### Content and node

The content is a wrapper over the node. It represents a higher level functionality over the storage level functions provided by the node API. Higher level features available from the content API include field validation, field data processing, generalized accessors for view techniques. We mainly use the content object model as a model that drives the UI. For example a contentview can be created over a content and not a node. You can always create the content wrapper over a node and you can always access the node from the content:

```
var node = Node.LoadNode("/Root/Sites/Default_Site/myContent");
var content = Content.Create(node); // create content wrapper
var originalnode = content.ContentHandler; // access the underlying node
```

You can read more about the content API here:

- [Content - for Developers](/content-for-developers)