# Built-in OData actions and functions

sensenet ECM has a powerful feature for defining and accessing content operations called the [Smart Application Model](smart-application-model.md). The basic building blocks of this model are [Actions](action.md) and [Applications](application.md). 

The articles above contain an overview of the ideas behind the application model and how [Content](content.md) are displayed using application pages. In this article we are discussing actions in sensenet ECM that can be invoked through our [OData REST API](odata-rest-api.md), the most important service in sensenet ECM. 

This article lists the **built-in actions and functions** that are accessible through OData.

- **OData action**: causes state change in the [Content Repository](content-repository.md)
- **OData function**:  does not cause changes, only provides data

If you want to build your own custom OData action or function, please visit [this tutorial](http://community.sensenet.com/tutorials/how-to-create-a-custom-odata-action). The following sections contain descriptions of our built-in operations with their parameters listed, and a couple of examples.

> Please note that in sensenet ECM there is a restriction in the OData implementation: actions and functions can be invoked **only on entities** and not collections.

## OData actions

> Please note that OData actions can be called with a POST request only.

### Delete action

Deletes one content.

- name: **Delete**
- parameters:
  - `permanent` (bool; optional, default: false): if the content should be deleted permanently, bypassing the Trash
- required permissions: Delete.

#### Example

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('DocumentLibrary')/Delete_
- request body:

```js
{"permanent":true}
```

### Delete batch action

Deletes multiple content.

- name: **DeleteBatch**
- parameters:
  - `paths` (array; required): array of content paths or ids
  - `permanent` (bool; optional, default: false): if the content should be deleted permanently, bypassing the Trash

#### Example

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/DeleteBatch_
- request body:

```js
{"paths":["/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library/Aenean semper.doc",
"/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library/Duis et lorem.doc"],
"permanent":false}
```

### Move to action

Moves one content to another container.

- name: **MoveTo**
- parameters:
  - `targetPath` (string; required): Path of the target container, where the requested content will be moved.
- required permissions: Open minor, Save.

#### Example

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/MoveTo_
- request body:

```js
{"targetPath":"/Root/Sites/Default_Site/workspaces/Project/madridprojectworkspace/Document_Library"}
```

### Move batch action

Moves multiple content to another container.

- name: **MoveBatch**
- parameters:
  - `targetPath` (string; required): Path of the target container, where all the content will be moved.
  - `paths` (string array; required): list of content to move

#### Example

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/MoveBatch_
- request body:

```js
{"targetPath":"/Root/Sites/Default_Site/workspaces/Project/pragueprojectworkspace/Document_Library",
"paths":["/Root/Sites/Default_Site/workspaces/Project/madridprojectworkspace/Document_Library/Aenean semper.doc",
"/Root/Sites/Default_Site/workspaces/Project/madridprojectworkspace/Document_Library/Duis et lorem.doc"]}
```

### Copy to action

Copies one content to another container.

- name: **CopyTo**
- parameters:
  - `targetPath` (string; required): Path of the target container, where the requested content will be copied.
- required permissions: Save.

#### Example

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/CopyTo_
- request body:

```js
{"targetPath":"/Root/Sites/Default_Site/workspaces/Project/madridprojectworkspace/Document_Library"}
```

### Copy batch action

Copies multiple content to another container.

- name: **CopyBatch**
- parameters:
  - `targetPath` (string; required): Path of the target container, where all the content will be copied.
  - `paths` (string array; required): list of content to copy

#### Example

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/CopyBatch_
- request body:

```js
{"targetPath":"/Root/Sites/Default_Site/workspaces/Project/pragueprojectworkspace/Document_Library",
"paths":["/Root/Sites/Default_Site/workspaces/Project/madridprojectworkspace/Document_Library/Aenean semper.doc",
"/Root/Sites/Default_Site/workspaces/Project/madridprojectworkspace/Document_Library/Duis et lorem.doc"]}
```

### Add types to Allowed Child Types action

Adds the given content types to the Allowed content Type list.

- name: **AddAllowedChildTypes**
- parameters:
  - `contentTypes` (string array; required): a list of the case sensitive content type names.

#### Example

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/AddAllowedChildTypes_
- request body:

```js
{"contentTypes":["Car"]}
```

### Remove types from Allowed Child Types action

Removes the given content types from the Allowed content Type list. If the list after removing and the list on the matching CTD are the same, the local list will be removed.

- name: **RemoveAllowedChildTypes**
- parameters:
  - `contentTypes` (string array; required): a list of the case sensitive content type names.

#### Example

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/AddAllowedChildTypes_
- request body:

```js
{"contentTypes":["Car"]}
```

### Set permissions action

Sets permissions on the requested content. You can add or remove permissions for one ore more users or groups using this action or even break/unbreak permission inheritance.
- name: **SetPermissions**
- parameters: this action expects a special kind of request body, a serialized version of an object of the SetPermissionsRequest type.
- required permissions: Open, See permissions, Set permissions.

One and only one of the following parameters must be provided in the request body:
- permission entry list: array of permission entry objects, containing an identity Id or Path and one or more permission settings for permission types (see examples below).
- _inheritance_: _break_ or _unbreak_

#### Example 1

This example sets permissions for the Visitor user and the Creators group.

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/SetPermissions_
- request body:

```js
{r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},
{identity:"/Root/IMS/BuiltIn/Portal/Creators", Custom16:"A", Custom17:"1"}]}
```

As you can see in the example above, the values of the permission settings can be the following:

- _undefined_ or _U_ or _0_
- _allow_ or _A_ or _1_
- _deny_ or _D_ or _2_

#### Example 2

This example sets permissions for the Visitor user and the Creators group. But the visitor's entry is not inheritable (see the "localOnly" property in the request body).

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/SetPermissions_
- request body:

```js
{r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", localOnly:true, OpenMinor:"allow", Save:"deny"},
{identity:"/Root/IMS/BuiltIn/Portal/Creators", Custom16:"A", Custom17:"1"}]}
```

#### Example 3

This example breaks permission inheritance on the document library content.

- URL: _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/SetPermissions_
- request body:

```js
{inheritance:"break"}
```

### Upload action

Uploads a stream or text to a content binary field (e.g. a file).

- name: **Upload**
- parameters:
  - `create` (URL parameter, required in the first request): this parameter should be added to the initial upload request
  - `ContentType` (string, optional): specific content type name for the uploaded content. If not provided, the system will try to determine it from the current environment: the upload content types configured in the `web.config` and the allowed content types in the particular folder. In most cases, this will be File.
  - `FileName` (string): name of the uploaded file.
  - `Overwrite` (bool, optional, default is True): whether the upload action should overwrite a content if it already exist with the same name. If false, a new file will be created with a similar name containing an incremental number (e.g. _sample(2).docx_).
  - `UseChunk` (bool, optional, used in the first request, default is False): determines whether the system should start a chunk upload process instead of saving the file in one round. Usually this is determined by the size of the file.
  - `PropertyName` (string, optional): appoints the binary field of the content where the data should be saved. Default: _Binary_.
  - `ChunkToken` (string, mandatory, except in the first request): the response of first request returns this token. It must be posted in all of the subsequent requests without modification. It is used for executing the chunk upload operation.
  - `FileText`: in case you do not have the file as a real file in the file system but a text in the browser, you can provide the raw text in this parameter. See an example here.
- required permissions: See, Open Add new.

#### Example

- URL (first request): _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/Upload?create=1_
- request body:

```js
{
"ContentType": "File",
"FileName": "sampledata.xlsx",
"Overwrite": true,
"UseChunk": true,
"PropertyName": "Binary"
}
```

**Response**: 5062\*3196\*True (or similar). This value **must be passed to subsequent requests** as the _ChunkToken_.
- URL (subsequent requests): _/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/Upload_
- request (properties in the Content-Disposition header):

```js
{
"ContentType": "File",
"FileName": "sampledata.xlsx",
"Overwrite": true,
"ChunkToken": "5062*3196*True",
"PropertyName": "Binary"
}
```

In addition, subsequent request bodies should contain the uploaded file in chunks or the whole file content.

> For more information about how uploading works see the [Upload action](upload-action.md) article. You can find a C# source code example there that demonstrates the usage of the [Upload action](upload-action.md) from a **console application**.

### Restore action
*(from version 6.3)*

Restores a deleted content from the Trash. You can call this action only on a TrashBag content that contains the deleted content itself.

- name: **Restore**
- parameters:
  - `destination` (string; optional): Path of the target container, where the deleted content will be restored. If it is not provided, the system uses the original path stored on the trash bag content.
  - `newname` (bool; optional): whether to generate a new name automatically if a content with the same name already exists in the desired container (e.g. mydocument(1).docx).
- errors:
  - A content with this name already exists in the selected target folder.
  - Cannot restore this type of content to the selected target.
  - Destination folder does not exist.
  - You do not have enough permissions to complete the restore operation.

#### Example

```js
$.ajax({
     url: "/OData.svc/Root/Trash('TrashBag-20130403112917')/Restore",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
          'destination':"/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library", 
          'newname': true
     }),
     success: function () {
		//...
     }
});
```

### SaveQuery action
*(from version 6.3)*

Creates or modifies a Query content. Use this action instead of creating query content directly using the basic OData create method, because query content can be saved under a workspace or to the user's profile as a private query.

- name: **SaveQuery**
- parameters:
  - `query` (string; required): query text, composed in [Query Builder](query-builder.md) or written manually (see [Query syntax](query-syntax.md) for more details).
  - `displayName` (string; required): desired display name for the query content. Can be empty.
  - `queryType` (string; ): type of the saved query. If an empty value is posted, the default is Public. Possible values are:
    - _Public_: (default) the query will be saved to the _Queries_ content list of the current workspace.
    - _Private_: the query will be saved to the  _Queries_ content list under the profile of the current user.
    - _NonDefined_: use this value only when you are editing an existing query content.

```js
$.ajax({
     url: "/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/SaveQuery",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
          'query':"%2BTypeIs:WebContentDemo %2BInTree:/Root",
          'displayName': "",
          'queryType': "Private"
     }),
     success: function () {
		//...
     }
});
```

### Approve action
*(from version 6.3)*

Performs an approve operation on a content, the equivalent of calling `Approve()` on the Content instance in .NET. Also checks whether the content handler of the subject content inherits GenericContent (otherwise it does not support this operation). This action has no parameters.

#### Example

```js
$.ajax({
     url: "/OData.svc/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library('Aenean semper.doc')/Approve",
     dataType: "json",
     type: 'POST',
     success: function () {
		//...
     }
});
```

### Reject action
*(from version 6.3)*

Performs a reject operation on a content, the equivalent of calling `Reject()` on the Content instance in .NET. Also checks whether the content handler of the subject content inherits `GenericContent` (otherwise it does not support this operation). The reject reason can be supplied in an optional parameter called `rejectReason`.

#### Example

```js
$.ajax({
     url: "/OData.svc/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library('Aenean semper.doc')/Reject",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
	'rejectReason': "Reject reason"
     }),
     success: function () {
		//...
     }
});
```

### Publish action
*(from version 6.3)*

Performs a publish operation on a content, the equivalent of calling `Publish()` on the Content instance in .NET. Also checks whether the content handler of the subject content inherits GenericContent (otherwise it does not support this operation). This action has no parameters.

#### Example

```js
$.ajax({
     url: "/OData.svc/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library('Aenean semper.doc')/Publish",
     dataType: "json",
     type: 'POST',
     success: function () {
		//...
     }
});
```

### Check in action
*(from version 6.3)*

Performs a check in operation on a content, the equivalent of calling `CheckIn()` on the Content instance in .NET. This action enforces the check in comments mode of the content. Also checks whether the content handler of the subject content inherits GenericContent (otherwise it does not support this operation). The check-in comments can be supplied in an optional parameter called checkInComments.

#### Example

```js
$.ajax({
     url: "/OData.svc/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library('Aenean semper.doc')/CheckIn",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
        'checkInComments': "Adding new contract"
     }),
     success: function () {
		//...
     }
});
```

### Check out action
*(from version 6.3)*

Performs a check out operation on a content, the equivalent of calling `CheckOut()` on the Content instance in .NET. Also checks whether the content handler of the subject content inherits GenericContent (otherwise it does not support this operation). This action has no parameters.

#### Example

```js
$.ajax({
     url: "/OData.svc/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library('Aenean semper.doc')/CheckOut",
     dataType: "json",
     type: 'POST',
     success: function () {
		//...
     }
});
```

### Undo check out action
*(from version 6.3)*

Performs an undo check out operation on a content, the equivalent of calling `UndoCheckOut()` on the Content instance in .NET. Also checks whether the content handler of the subject content inherits GenericContent (otherwise it does not support this operation). This action has no parameters.

#### Example

```js
$.ajax({
     url: "/OData.svc/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library('Aenean semper.doc')/UndoCheckOut",
     dataType: "json",
     type: 'POST',
     success: function () {
		//...
     }
});
```

### Force undo check out action
*(from version 6.3)*

Performs a force undo check out operation on a content, the equivalent of calling `ForceUndoCheckOut()` on the Content instance in .NET. Also checks whether the content handler of the subject content inherits GenericContent (otherwise it does not support this operation). This action has no parameters.

#### Example

```js
$.ajax({
     url: "/OData.svc/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library('Aenean semper.doc')/ForceUndoCheckOut",
     dataType: "json",
     type: 'POST',
     success: function () {
		//...
     }
});
```

### Restore version action
*(from version 6.3)*

Restores an old version of the content. Also checks whether the content handler of the subject content inherits GenericContent (otherwise it does not support this operation). This action has a single parameter called version where the caller can specify which old version to restore.

#### Example

```js
$.ajax({
     url: "/OData.svc/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library('Aenean semper.doc')/RestoreVersion",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
         'version': "V1.0.A"
     }),
     success: function () {
		//...
     }
});
```

### Finalize content action
*(from version 6.3)*

Closes a [Multistep saving](multistep-saving.md) operation and sets the saving state of a content to Finalized. Can be invoked only on content that are not already finalized.

#### Example

```js
$.ajax({
    url: "/OData.svc/workspaces/Project/budapestprojectworkspace/Document_Library('sampledocument.docx')/FinalizeContent",
    dataType: "json",
    type: 'POST',
    success: function () {
        console.log('Content is successfully finalized');
    }
});
```

### Take lock over action
*(from version 6.3.2)*

Lets administrators take over the lock of a checked out document from another user. A new locker user can be provided using the 'user' parameter (user path or id as string). If left empty, the current user will take the lock.

#### Example

```js
$.ajax({
     url: "/OData.svc/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library('Aenean semper.doc')/TakeLockOver",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
         'user': "12345"
     }),
     success: function () {
		//...
     }
});
```

### Indexing actions
*(from version 6.4 Patch 1)*

These actions perform an [indexing operation](http://community.sensenet.com/tutorials/how-to-reindex-content) on a single content or a whole subtree.

#### RebuildIndex

Rebuilds or just refreshes the Lucene index document of a content and optionally of all documents in the whole subtree.

```js
$.ajax({
    url: "/OData.svc/workspaces/Project/budapestprojectworkspace/Document_Library('sampledocument.docx')/RebuildIndex",
    dataType: "json",
    type: 'POST',
    data: JSON.stringify({
        'recursive': true, 
        'rebuildLevel': 1
    }),
    success: function () {     
    }
});
```

#### RebuildIndexSubtree

Performs a full reindex operation on the content and the whole subtree.

```js
$.ajax({
    url: "/OData.svc/workspaces/Project/budapestprojectworkspace/Document_Library('sampledocument.docx')/RebuildIndexSubtree",
    dataType: "json",
    type: 'POST',
    success: function () {     
    }
});
```

#### RefreshIndexSubtree

Refreshes the index document of the content and the whole subtree using the already existing index data stored in the database.

```js
$.ajax({
    url: "/OData.svc/workspaces/Project/budapestprojectworkspace/Document_Library('sampledocument.docx')/RefreshIndexSubtree",
    dataType: "json",
    type: 'POST',
    success: function () {     
    }
});
```

### Add members action
*(from version 6.5)*

Administrators can add new members to a group using this action. The list of new members can be provided using the 'contentIds' parameter (list of user or group ids).

```js
$.ajax({
     url: "/OData.svc/Root/IMS/BuiltIn/Portal('Editors')/AddMembers",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
         'contentIds': [ 123, 456, 789 ]
     }),
     success: function () {
		//...
     }
});
```

### Remove members action
*(from version 6.5)*

Administrators can remove members from a group using this action. The list of removable members can be provided using the 'contentIds' parameter (list of user or group ids).

```js
$.ajax({
     url: "/OData.svc/Root/IMS/BuiltIn/Portal('Editors')/RemoveMembers",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
         'contentIds': [ 123, 456, 789 ]
     }),
     success: function () {
		//...
     }
});
```

### Take ownership action
*(from version 6.5)*

Users who have _TakeOwnership_ permission for the current content can modify the Owner of this content. The new owner is provided using the 'userOrGroup' parameter that accepts the path or the id of the new owner (that can be a Group or a User). The input parameter also supports empty or null string, in this case the new owner will be the current user.

```js
$.ajax({
     url: "/Odata.svc/Root/Sites('Default_Site')/TakeOwnership",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
          'userOrGroup': "/Root/IMS/BuiltIn/Portal/Admin"
     }),
     success: function () {
               //....
     }
});
```

### Login action
*(from version 6.5.3)*

It is possible to send authentication requests using this action. You provide the username and password and will get the User object as the response if the login operation was successful or HTTP 403 Forbidden message if it wasn’t. If the username does not contain a domain prefix, the configured default domain will be used. After you logged in the user successfully, you will receive a standard ASP.NET auth cookie which will make sure that your subsequent requests will be authorized correctly.

```diff
- Before sensenet ECM version 6.5.4.9375 after you provide the username and password, you will get a response 
- of a boolean value depending on whether the login operation was successful or not.
```

```diff
- As the username and password is sent in clear text, always send these kinds of requests throuigh HTTPS.
```

```js
$.ajax({
     url: "/Odata.svc/('Root')/Login",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
          'username': "domain\\myusername",
          'password': "1234"
     }),
     success: function (d) {
               //....user content
     }
});
```

### Logout action
*(from version 6.5.3)*

Similarly to the Login action above, you can send a logout action to the portal.

```js
$.ajax({
     url: "/Odata.svc/('Root')/Logout",
     dataType: "json",
     type: 'POST',
     success: function () {
     }
});
```

### Preview actions

These actions were designed to support the client-side preview plugin that displays content preview images.

#### CheckPreviews

Returns the number of currently existing preview images. If necessary, it can make sure that all preview images are generated and available for a document.

```js
$.ajax({
     url: "/Odata.svc/workspaces/Project/budapestprojectworkspace/Document_Library('MyDocument.docx')/CheckPreviews?metadata=no",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
          'generateMissing': true
     }),
     success: function () {
               //....
     }
});
```

#### RegeneratePreviews

It clears all existing preview images for a document and starts a task for generating new ones. This can be useful in case the preview status of a document has been set to 'error' before for some reason and you need to force the system to re-generate preview images.

```js
$.ajax({
     url: "/Odata.svc/workspaces/Project/budapestprojectworkspace/Document_Library('MyDocument.docx')/RegeneratePreviews?metadata=no",
     dataType: "json",
     type: 'POST',
     success: function () {
               //....
     }
});
```

#### GetPageCount

Returns the number of pages in a document. If there is no information about page count on the content, it **starts a preview generation task** to determine the page count.

```js
$.ajax({
     url: "/Odata.svc/workspaces/Project/budapestprojectworkspace/Document_Library('MyDocument.docx')/GetPageCount?metadata=no",
     dataType: "json",
     type: 'POST',
     success: function () {
               //....
     }
});
```

### Field editor actions

These actions were designed to let you create, edit or delete metadata fields of a Content List. We represent these fields as virtual Contents that can be managed in a similar way as regular content items.

#### Adding a field

The following example demonstrates how can you add a new metadata field to a Content List, along with setting configuration values of the field (e.g. _Compulsory_ or _MaxValue_).

```js
$.ajax({
    url: "/OData.svc/workspaces/Project/pragueprojectworkspace('Memos')",
    dataType: "json",
    type: 'POST',
    data: "models=[" + JSON.stringify({ '__ContentType':'IntegerFieldSetting' , Name: 'MyField1', 'DisplayName': 'My Field 1', 'Compulsory': true, 'MinValue': 10 }) + "]",
    success: function () {
        //...
    }
});
```

#### Editing a field

You may edit the configuration values of a field in two different ways: treating the field as a virtual child of the content list (see the first example below), or using a dedicated action, as in the second example.

##### Virtual child using a PATCH request

```js
$.ajax({
    url: "/OData.svc/workspaces/Project/pragueprojectworkspace/Memos('MyField1')",
    dataType: "json",
    type: 'PATCH',
    data: "models=[" + JSON.stringify({ 'MinValue': 5, 'MaxValue': 20 }) + "]",
    success: function () {
        //...
    }
});
```

##### Virtual child using a PUT request

Please note that a _PUT_ request will erase all previous properties of the field (reset them to their default values).

```js
$.ajax({
    url: "/OData.svc/workspaces/Project/pragueprojectworkspace/Memos('MyField1')",
    dataType: "json",
    type: 'PUT',
    data: "models=[" + JSON.stringify({ 'MinValue': 5, 'DisplayName': 'My field 1' }) + "]",
    success: function () {
        //...
    }
});
```

##### Dedicated action (EditField) using a POST request

```js
$.ajax({
    url: "/OData.svc/workspaces/Project/pragueprojectworkspace('Memos')/EditField",
    dataType: "json",
    type: 'POST',
    data: "models=[" + JSON.stringify({ Name: 'MyField1', 'MinValue': 3, 'MaxValue': 19 }) + "]",
    success: function () {
        //...
    }
});
```

#### Deleting a field

You can delete a field in two different ways: treating the field as a virtual child of the content list (see the first example below), or using a dedicated action, as in the second example.

##### Virtual child using a DELETE request

```js
$.ajax({
    url: "/OData.svc/workspaces/Project/pragueprojectworkspace/Memos('MyField1')",
    dataType: "json",
    type: 'DELETE',
    success: function () {
        //...
    }
});
```

##### Dedicated action (DeleteField) using a POST request

```js
$.ajax({
    url: "/OData.svc/workspaces/Project/pragueprojectworkspace('Memos')/DeleteField",
    dataType: "json",
    type: 'POST',
    data: "models=[" + JSON.stringify({ name: 'MyField1' }) + "]",
    success: function () {
        //...
    }
});
```

## OData functions

> Please note that OData functions can be called with POST or GET request.

### GetQueries
*(from version 6.3)*

Gets Query content that are relevant in the current context. The result set will contain two types of content:

- **Public queries**: query content in the Queries content list of the current workspace.
- **Private queries**: query content in the Queries content list under the profile of the current user.

Action details:

- name: **GetQueries**
- parameters:
  - `onlyPublic` (bool; compulsory): if true, only public queries are returned from the current workspace.

#### Example

```js
"http://example.com/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/GetQueries?onlyPublic=false"
```

### Has permission

Gets if the given user (or if it is not given than the current user) has the specified permissions for the requested content.

- name: **HasPermission**
- parameters:
  - `user` (string; optional, default: current user): path of the user
  - `permissions` (string array; required): list of permission names (e.g. Open, Save)
- required permissions to call this action: See permissions.

#### Example

Permissions for the library for the current user:

```js
"http://example.com/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission?permissions=AddNew,Save"
```

### Permission queries
*(from version 6.3)*

There are a couple of OData functions that can be used for getting aggregated security information about a content subtree. These are the following:

- **GetRelatedIdentities**
- **GetRelatedItems**
- **GetRelatedPermissions**
- **GetRelatedItemsOneLevel**
- **GetAllowedUsers** (from version 6.5.2)
- **GetParentGroups** (from version 6.5.2)

> For more details, please visit the [Permission queries](permission-queries.md) article.

### Get metadata
*(from version 6.3)*

OData function for collecting all fields of all types in the system. The content parameter (the resource you call it on, in this example the library) is ignored.

- name: **GetMetadata**
- parameters: none

#### Example

```js
"http://example.com/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/GetMetadata"
```

### Check allowed child types
*(from version 6.3)*

Checks all IFolder objects in the Content Repository and returns all paths where AllowedChildTypes is empty. Paths are categorized by content type names. This is a helper function that can be used to get an overview of your system.

- name: **CheckAllowedChildTypesOfFolders**
- parameters: none

#### Example

Permissions for the library for the current user:

```js
"http://example.com/OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/CheckAllowedChildTypesOfFolders"
```

### Get VersionInfo
*(from version 6.3.1)*

Gets the complete version information about the core product and the installed applications. This function is accessible only for administrators by default. You can learn more about the subject in the [SnAdmin](snadmin.md) article. You can read detailed description of the [function result](snadmin.md#Getting_package_information).

#### Example

```js
"http://example.com/OData.svc('root')/GetVersionInfo"
```

### Preview functions

These functions were designed to support the client-side preview plugin that displays content preview images.

#### PreviewAvailable

Gets information about a preview image generated for a specific page in a document. It returns with the path and the dimensions (width/height) of the image. If the image does not exist yet, it returns with an empty object but it **starts a background task** to generate that image if a valid page count number was determined. If page count is -1 you need to call GetPageCount action first. It is OK to call this method periodically for checking if an image is already available.

```js
$.ajax({
     url: "/Odata.svc/workspaces/Project/budapestprojectworkspace/Document_Library('MyDocument.docx')/PreviewAvailable",
    dataType: "json",
     type: 'POST',
     data: JSON.stringify({
          ‘page’:1
     }),
     success: function () {
        //...
     }
});
```

#### GetPreviewImagesForOData

Returns the full list of preview images as content items. This methods **synchronously** generates all missing preview images.

```js
$.ajax({
     url: "/Odata.svc/workspaces/Project/budapestprojectworkspace/Document_Library('MyDocument.docx')/GetPreviewImagesForOData?metadata=no",
     dataType: "json",
     type: 'GET',
     success: function () {
        //....
     }
});
```

#### GetExistingPreviewImagesForOData

Returns the list of **existing** preview images (only the first consecutive batch) as objects with a few information (image path, dimensions). It does not generate any new images.

```js
$.ajax({
     url: "/Odata.svc/workspaces/Project/budapestprojectworkspace/Document_Library('MyDocument.docx')/GetExistingPreviewImagesForOData?metadata=no",
     dataType: "json",
     type: 'GET',
     success: function () {
      //....
     }
});
```

#### GetAllContentTypes

Returns the list of all ContentTypes in the system.

```js
$.ajax({
     url: "/Odata.svc/Root/Sites('Default_Site')/GetAllContentTypes?metadata=no",
     dataType: "json",
     type: 'GET',
     success: function () {
      //....
     }
});
```

#### GetAllowedChildTypesFromCTD

Returns the list of the AllowedChildTypes which are set on the current Content.

```js
$.ajax({
     url: "/Odata.svc/Root/Sites('Default_Site')/GetAllowedChildTypesFromCTD?metadata=no",
     dataType: "json",
     type: 'GET',
     success: function () {
               //....
     }
});
```