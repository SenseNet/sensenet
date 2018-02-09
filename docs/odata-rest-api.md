# OData REST API

## Overview of sensenet ECM REST API

The Open Data Protocol (OData) is a Web protocol for querying and updating data that provides a way to unlock your data and free it from silos that exist in applications today. OData is being used to expose and access information from a variety of sources including, but not limited to, relational databases, file systems, content management systems and traditional Web sites. From version 6.2 sensenet ECM Content Repository is an OData producer. Your applications can consume our OData service to create web apps on PCs and mobile devices (e.g. with KendoUI), mobile apps (OData is supported on all major smartphone platforms) or any other type of content based applications. We support CRUD operations on the repository, properties, binary streams, paging options and custom queries. You can expect an even wider coverage of the OData specification in future releases. For more information about OData check the OData website.In this article we explain how we implemented OData V3 and how you can access the sensenet ECM OData producer from OData clients.

> Please note that the structure and philosophy of the sensenet ECM [Content Repository](content-repository.md) prevents publishing an automatically discoverable OData metadata service, which means generic OData client tools will likely not work with sensenet ECM.

## Clients accessing the repository
There are two built-in tools for accessing content in the Content Repository through the REST api, so that you do not have to construct OData requests manually. Please check the following projects, they offer an easy-to-learn client API to access sensenet ECM:

- [JavaScript client SDK](https://github.com/SenseNet/sn-client-js)
- [.Net client library](https://github.com/SenseNet/sn-client-dotnet)

## OData specific requests

An OData HTTP request sent to the Content Repository contains the following parts:

- protocol (http or https)
- host (site URL)
- a service name (e.g. _OData.svc_)
- the path of the requested resource
- optional parameters

For example:

```http://www.example.com/OData.svc/[RESOURCEPATH]```

If the requested resource is empty (i.e. the URI is the service root) the server returns the service document that exposes the available collections of the service (see in [OData documentation](http://www.odata.org/developers/protocols/json-format#ServiceDocuments)). This information now available only in JSON format.

If the resource is _$metadata_, the server returns the Service Metadata Document that exposes the data model of the service in XML and JSON (see in [OData documentation](http://www.odata.org/documentation/overview#ServiceMetadataDocument)). This document is the global (static) metadata that cannot contain content specific information e.g. expando (`ContentList`) fields. Instance specific metadata is available on a collection or entity: in this case the resource URI needs to end with the _/$metadata_ segment. 

For example:

```http://www.example.com/OData.svc/workspaces/$metadata```

> For more info, see the [Metadata requests](#Metadata_requests) section.

The requested resource can be any content in the repository that is permitted for the current user. The resource may be addressed with relative or absolute path. The following requests are equivalent:

- http://www.example.com/OData.svc/Root/Sites/ExampleSite/workspaces
- http://www.example.com/OData.svc/workspaces

Another way to access content is addressing by content-id:

- http://www.example.com/OData.svc/content(42)

In this case the URI must satisfy a strict rule: the service path followed by _"/content"_ (insensitive) and content id wrapped by parenthesis without any whitespace.

> If the requested resource is not found, the server returns with a **404 Error** status code.

There are some cases when the request body should contain additional information beyond the URL parameters. See the examples related to each method below for details.

## HTTP methods

The following HTTP methods can be used in requests to specify the expected operation:

- **GET**: getting one or more entities. The URL contains all request information.
- **PUT/PATCH**: modifying an entity. The URL defines the entity and the request body contains a JSON object. This object describes the properties and new values of the requested entity.
- **POST**: creating an entity. The URL defines the entity and the request body contains a JSON object. The URL determines the place and name of the new entity. The JSON object describes the properties and initial values of the new entity.
- **DELETE**: deleting an entity. The URL determines the entity that will be deleted. Always only one entity (and its children) will be deleted.

[Check out the examples below.](#Examples)

# OData specific responses

``` diff
- Please note that sensenet ECM currently supports only the OData Verbose JSON response format.
```

# Addressing children (collections)

Service path followed by the site relative path of the container

- http://www.example.com/OData.svc/**workspaces**

It returns child content of _/Root/Sites/ExampleSite/workspaces_ as a **collection**. Every child entity contains the following properties:

- **__metadata**: contains the OData URI of the entity and the name of its Content Type
- **Actions**: this property is deferred: a comma separated list of action names
- **IsFile**: if its value is true, the content has a binary property named "Binary"
- **IsFolder**: if its value is true, the content implements **IFolder** interface. Implementing IFolder interface does not mean that the content is inherited from the `Folder` class but it has public Children property.
- **Id**, **Name**, **DisplayName**, **Icon**, **CreationDate**, **ModificationDate**: common properties of type integer, number or datetime.
- **CreatedBy** and **ModifiedBy**: deferred properties (see: [http://www.odata.org/documentation/json-format#DeferredContent](http://www.odata.org/documentation/json-format#DeferredContent)).

**Example: list of workspaces**

```js
$.ajax({
    url: "/OData.svc/workspaces",
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
        //do something with the result
        });
    }
});
```

## Addressing an entity

Service path followed by the site relative path of the parent and an entity name wrapped by aposthrophes and parentheses.

- http://www.example.com/OData.svc/workspaces('Document')

Returns with one entity and all its properties. The content addressed with this OData URI can be accessed on the portal using the following browse URL:

- http://www.example.com/workspaces/Document

Another way: Service path followed by the "content" word and an entity id between parentheses.

- http://www.example.com/OData.svc/content(42)

If the site root contains a content that's name is "content", access it wit absolute path:

- http://www.example.com/OData.svc/Root/Sites/ExampleSite('content')

## Addressing a property

Any property of a content entity can be addressed in the following way:

- {OData service URL}{entity uri}{/}{property name}

> Property request cannot be executed on a collection.

A sample property request:

- http://www.example.com/OData.svc/workspaces('Document')/DisplayName

This returns with the following response:

```js
{
  "d": {
    "DisplayName": "Document Workspaces"
  }
}
```

## Addressing a property value

Raw value of a property can be accessed if the request is extended with the _/$value_ parameter.

- http://www.example.com/OData.svc/workspaces('Document')/DisplayName/$value

This returns with the following response:

`Document Workspaces`

## Addressing the count of a collection

Returns with the count of the requested collection. The value depends on other query string parameters ($top, $skip, $filter, query, etc.) and does not depend on the _$inlinecount_ parameter.

- http://www.example.com/OData.svc/workspaces/Document/$count

This returns with a raw integer value:

`3`

## Addressing a binary stream

Binary data is represented by an OData Named Resource Stream Value (see [OData Verbose JSON Format](http://www.odata.org/media/30002/OData%20JSON%20Verbose%20Format.html)). The _"media_src"_ and _"content_type"_ properties are filled with proper values, while the _"edit_media"_ and _"media_etag"_ properties are not supported. Check out the _"Binary"_ property of the following response as an example:

```js
{
  "d": {
    "__metadata": {
      "uri": "/OData.svc/workspaces/Project/budapestprojectworkspace/Document_Library('Aenean semper.doc')",
      "type": "File"
    },
    ...
    "Binary": {
      "__mediaresource": {
        "edit_media": null,
        "media_src": "/workspaces/Project/budapestprojectworkspace/Document_Library/Aenean semper.doc",
        "content_type": "application/msword",
        "media_etag": null
      }
    },
    ...
```

## Addressing available actions of a content

Every content has many executable actions in the sensenet Content Repository managed by the [Action Framework](action.md). List of these actions are available viathe "Actions"\"deferred" OData property:

```js
{
  "d": {
    "__count": 5,
    "results": [
      {
        "__metadata": {
          "uri": "/OData.svc/workspaces('workspacename')",
          "type": "SystemFolder"
        },
        "Actions": {
          "__deferred": {
            "uri": "/OData.svc/workspaces('workspacename')/Actions",
    ...
```

You can filter the actions with a [scenario name](http://community.sensenet.com/tutorials/how-to-create-a-custom-scenario.md). The filter is the _"scenario"_ URL parameter. Its value can be a case sensitive scenario name.

**Example: get workspace actions**

Request for actions in scenario _ListItem_:

```js
$.getJSON("/OData.svc/workspaces/('workspacename')?$expand=Actions&scenario=ListItem", null, function (o) {
    var content = new SN.Content(o['d']);
    $.each(content.json.Actions, function () {
	  //do something with the result
    });
});
```

Part of the response:

```js
{
  "d": {
    "Actions": [
      {
        "Name": "Edit",
        "DisplayName": "Edit",
        "Index": 0,
        "Icon": "edit",
        "Url": "/workspaces/workspacename?action=Edit"
      },
      {
        "Name": "Browse",
        "DisplayName": "Browse",
        "Index": 0,
        "Icon": "browse",
        "Url": "/workspaces/workspacename"
      },
      ...
```

## Addressing metadata fields of a Content List

It is possible to manage the fields of a Content List through our OData REST API. For details and examples please visit this article:

- [Field editor actions](built-in-odata-actions-and-functions.md#Field Editor Actions)

## System Query Options

See the [OData.org](http://www.odata.org/documentation/uri-conventions#SystemQueryOptions) article about System Query Options.

### $orderby query option

Sorting collection results by one or more properties and forward or reverse direction. See on [OData.org](http://www.odata.org/documentation/uri-conventions#OrderBySystemQueryOption).

- Sorting by one property: http://www.example.com/OData.svc/workspaces?$orderby=Id
- Explicit direction: http://www.example.com/OData.svc/workspaces?$orderby=Name asc
- Reverse sorting: http://www.example.com/OData.svc/workspaces?$orderby=DisplayName desc
- Sorting by more fields: http://www.example.com/OData.svc/workspaces?$orderby=ModificationDate desc, Category, Name

**Example: reverse order by creation date**

```js
$.ajax({
    url: "/OData.svc/workspaces?$orderby=CreationDate desc",
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
		//do something with the result
        });
    }
});
```

### $top query option

Limiting collection results. See on [OData.org](http://www.odata.org/documentation/uri-conventions#TopSystemQueryOption).

- Request only 3 items: http://www.example.com/OData.svc/workspaces?$top=3

Negative value causes error. Zero value means "no top".

### $skip query option

Hiding first elements from the result. See on [OData.org](http://www.odata.org/documentation/uri-conventions#SkipSystemQueryOption).

- Skip first 4 items: http://www.example.com/OData.svc/workspaces?$skip=4
- Paging: http://www.example.com/OData.svc/workspaces?$top3&skip=4

Negative value causes error. Zero value means "no skip".

**Example: get second page of the result set**

```js
$.ajax({
    url: "/OData.svc/workspaces?$orderby=CreationDate desc&$top=5&$skip=5",
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
		    //do something with the result
        });
    }
});
```

### $filter query option

Defines a subset of the Entries from the specified collection. See on [OData.org](http://www.odata.org/documentation/uri-conventions#FilterSystemQueryOption). The filter expression can contain some global functions according to the OData standard:

| **Function**                             | **Description**                                          | **Example**                                       |
| ---------------------------------------- |:--------------------------------------------------------:| -------------------------------------------------:|
| **String functions**                                                                                                                                    |
| bool **substringof**(string a, string b) | Returns true if the second string contains the first.    | $filter=substringof(‘About’, Description) eq true |
| bool **startswith**(string a, string b)  | Returns true if the first string starts with the second. | $filter=startswith(Name, ‘About’) eq true         |
| bool **endswith**(string a, string b)    | Returns true if the first string ends with the second.   | $filter=endswith(Name, ‘About’) eq true           |
| **Type functions**                                                                                                                                      |
| bool **isof**(string type)               | Returns true if the current content is instance of or derived from the given content type. |                 |

> Please take the following sensenet-specific implementations into account.

#### Do not use relational database operations in filter

Our search engine is Lucene.NET which is a text based engine and not a relational engine. For this reason we cannot use two or more fields in one logical operation, and cannot process any operations with fields. For example:

- **Field1 ne Field2**: cannot compare two fields. This results in a run-time exception.
- **Field1 plus 42 eq 85**: cannot execute field operations in terms. Use the **Field1 eq 85 sub 42** form instead.
- **Field1 eq 85 sub 42**: this operation is allowed because our query processor can execute the subtraction so the expression will be a simple logical operation.

#### Filter works on children

Our repository is tree based instead of table based. So our collections are not only tables as typed collections rather children of a tree node. Because a collection request returns a folder's children so the filter works only on it.

#### Filter does not work on reference properties

In this implementation the reference filtering is skipped.

#### Do not use spatial data in filter

Spatial data types and operations are not implemented.

#### Type filtering

There are two ways to filter by content type.

**Type family query**

This is the most frequently used type query. Use **IsOf()** operation to filter by type family (see more on OData.org). The response of the following request will contain every content whose type is _Folder_ or any inherited type under the /Root/MyDocuments folder.

- https://example.com/odata.svc/Root/MyDocuments?$filter=isof('Folder')

Only the version with one parameter is implemented. The parameter is the name of the content type.

**Exact match query**

Sometimes it may be necessary to filter by exact type. In this case use the ContentType equality:

- https://example.com/odata.svc/Root/MyDocuments?$filter=ContentType eq 'Folder'

**Example: filter workspaces by type**

```js
$.ajax({
    url: "/OData.svc/workspaces?$filter=isof('ProjectWorkspace')",
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
		    //do something with the result
        });
    }
});
```

#### Other filtering examples

**Example: filter articles by date**

In this example you can see how to filter for datetime fields:

```js
$.ajax({
    url: "/OData.svc/workspaces/articles?$filter=CreationDate gt datetime'2013-03-26T03:55:00'",
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
        //...
        });
    }
});
```

**Example: filter by aspect field**

The name of an aspect field is the aspect's name concatenated with the field's name and separated by a dot character ('.'). So on the server side in a C# code we can access an aspect field like this:

```js
content["Summarizable.Summary"] = "sample value";
```

In OData filter syntax the dot notation in the member name means a fully qualified type name. Walking down on the properties of the object tree is possible in the OData filter but the members' path must be separated by slash ('/') instead of dot ('.'). In this example you can see how to filter for an aspect field:

```js
$.ajax({
    url: "/OData.svc/workspaces/articles?$filter=Summarizable/Summary eq 'sample value'",
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
            //...
        });
    }
});
```

#### Auto filter and lifespan filter

The following two parameters can be used to switch automatic filtering on or off (true or false).

- enableautofilters
- enablelifespanfilter

Check the [autofilters](#AUTOFILTERS) and [lifespan](#LIFESPAN) filter articles for more information.

Both setting's default value is false. The following query switches on both options:

- http://example.com/odata.svc/workspaces/Project/budapestprojectworkspace?enableautofilters=true&enablelifespanfilter=true

### $expand query option (supported from 6.2.1)

According to the [OData protocol](http://www.odata.org/developers/protocols/uri-conventions#ExpandSystemQueryOption) the _$expand_ option indicates that related items should be represented inline in the response with full content instead of simple links. In our case this means that any [Reference Field](reference-field.md) can be expanded to be able to get metadata of a content and one or more related content with a **single HTTP request**.

The value provided in the _$expand_ option is a **comma separated list of navigational properties** (in sensenet ECM these are reference fields). _$expand_ option works with a collection and a single content request as well. You may indicate that you want to expand **one or more fields** (e.g. `ModifiedBy` and `CreatedBy` at the same time). You may even expand fields of expanded content by providing a 'field name chain', separated by slashes (e.g. `CreatedBy/Manager`).

Additionally, you may expand the following special fields as well:

- **Actions**: list of available HTML actions
- **AllowedChildTypes**: list of available content types in a container

#### $expand and $select

It is possible to specify the list of fields the response should contain (see $select option for more info). This works with expanded properties as well: you may specify wich fields of the expanded content should be added to the response by providing a 'field name chain', separated by slashes (e.g. _$select=CreatedBy/DisplayName_).

If you do not provide a $select option in the request, all the field values will be returned of the requested and the expanded content as well. You do not have to select the property that you want to expand.

#### <a id="Examples"></a>Examples

Expand the Manager user of all document workspaces:

```js
$.ajax({
 url: "/OData.svc/workspaces/document?$expand=Manager",	      
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
		    //...
        });
    }
});
```

Expand the Manager of Managers of all document workspaces (expand two levels):

```js
$.ajax({
 url: "/OData.svc/workspaces/document?$expand=Manager/Manager",	      
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
		    //...
        });
    }
});
```

Filter the workspaces' and the Manager's properties at the same time:

```js
$.ajax({
 url: "/OData.svc/workspaces/document?$expand=Manager&$select=DisplayName,Path,Manager/Domain,Manager/Name,Manager/FullName,Manager/Path",	      
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
		    //...
        });
    }
});
```

Expand more levels with a couple of selected fields:

```js
$.ajax({
 url: "/OData.svc/workspaces/document?$expand=Manager/Manager&$select=DisplayName,Path,Manager/Name,Manager/FullName,Manager/Manager/FullName",	      
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
		    //...
        });
    }
});
```

Request for listing workspaces containing the managers' full name:

```js
$.ajax({
 url: "/OData.svc/workspaces/Project?$select=Manager/FullName&$expand=Manager",	      
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
		    //...
        });
    }
});
```

### $format query option

Our service always returns with verbose json format. Atom and xml formats are not implemented yet. You can specify the format option but every value causes error expect 'json' and 'verbosejson'.

### $select query option

Specifies the displayed properties in a comma separated list of the property names. Property names are case sensitive. See on [OData.org](http://www.odata.org/documentation/uri-conventions#SelectSystemQueryOption).

> Limitation: a select clause can be only a property name. Expressions in select clauses are not supported yet in sensenet ECM.

Without this option the result will contain all available properties. If the request refers only one entity, the available property set is the entity's all properties. In case of collection it is all properties of the available content types in the collection. For example: request only DisplayName, Path and Index properties for a quick list: http://www.example.com/OData.svc/workspaces?$select=DisplayName,Path,Index. Part of the result:

```js
{
  "d": {
    "__count": 5,
    "results": [
      {
        "__metadata": {
          "uri": "/OData.svc/workspaces('(apps)')",
          "type": "SystemFolder"
        },
        "DisplayName": "(apps)",
        "Path": "/Root/Sites/Default_Site/workspaces/(apps)",
        "Index": 5
      },
      {
        "__metadata": {
          "uri": "/OData.svc/workspaces('Document')",
          "type": "DocumentWorkspaceFolder"
        },
        "DisplayName": "Document Workspaces",
        "Path": "/Root/Sites/Default_Site/workspaces/Document",
        "Index": 2
      },
    ...
```

If the $select option contains an expanded navigation property (e.g. Manager/FullName), the navigation property must be included in the $expand option otherwise the response will be an error object. For example: this is a valid request: http://www.example.com/odata.svc/workspaces?$expand=**Manager**&$select=**Manager/FullName**. But this: http://www.example.com/odata.svc/workspaces?$expand=**Manager**&$select=**ModifiedBy/FullName** causes an error:

```js
{
  "error": {
    "code": "InvalidSelectParameter",
    "message": {
      "lang": "en-us",
      "value": "Bad item in $select: Manager/FullName"
    }
  }
}
```

#### Example: workspace list with few fields

```js
$.ajax({
 url: "/OData.svc/workspaces/Project?$select=DisplayName,Path,Icon,CreatedBy/FullName&$expand=CreatedBy", 
    dataType: "json",
    async: false,
    success: function (d) {
        $.each(d.d.results, function () {
		    //...
        });
    }
});
```

### $inlinecount query option

This option controls the "__count" property that can be found every collection response. See on [OData.org](http://www.odata.org/documentation/uri-conventions#InlinecountSystemQueryOption). Valid values are: 'allpages' and 'none'.

- **allpages**: means count of whole set, filter, top, skip options are ignored.
- **none**: result shows the actual count (__count property is not hidden).

Other value causes error. This query option is optional, the default value is none.

If the requested collection contains 25 items, the following request returns with 3, but __count contains 25:

- http://www.example.com/OData.svc/workspaces?$top=3&$skip=4&$inlinecount=allpages

## Custom Query Options

### query

There is a reserved custom query option: "query" (without "$" prefix) that helps to get filtered collection of entities with Content Query. The scope of the query is the requested entity's subtree. Default is the requested site. The whole repository is queryable if the requested entity is the "Root". 

Examples:

- http://www.example.com/OData.svc/?$select=Name,Index,Icon&query=about: returns content that contain "about" from the whole requested site (please note the '/' char after the service!).
- http://www.example.com/OData.svc/workspaces/document?$select=Name,Index,Icon&query=about: returns content that contain "about" from document workspaces under the requested site.
- http://www.example.com/OData.svc/Root?$select=Name,Index,Icon&query=about: returns content that contain "about" from the whole repository.
- http://www.example.com/OData.svc/Root?$select=DisplayName,Index,Icon&query=TypeIs:Article%20AND%20DisplayName:Africa*: returns all articles from the whole repository whose DisplayName starts with Africa.

#### Performance considerations

Do not forget that querying big collections can degrade the server performance. Always use result limiters in the queries. If you use the custom "query" options use result limiters and sorting in the "query" options as in Content Query (.TOP .SKIP) instead of using the OData's query options ($top, $skip, $orderby).

### metadata

This option controls the metadata content in output entities. It is invented for development purposes: the reduced or hidden metadata improves the output readability. There is three value:

- **full** (default): the output contains the whole metadata.
- **minimal**: metadata contains only self URI and type name (actions and functions are hidden).
- **no**: the output does not contain entity metadata.

For example: http://www.example.com/OData.svc/workspaces/Project/budapestprojectworkspace/Document_Library/?$select=Name&**metadata=no**

And the result is:

```js
{
  "d": {
    "__count": 4,
    "results": [
      { "Name": "Aenean semper.doc" },
      { "Name": "Aliquam porta suscipit ante.doc" },
      { "Name": "Duis et lorem.doc" },
      { "Name": "Views" }
    ]
  }
```

See more about custom query options on [odata.org](http://www.odata.org/documentation/uri-conventions#CustomQueryOptions).

## Other implementation details

- Deferred properties are extended with _title_ and _text_ to make link generation easier.
- Only JSON (verbose) format is implemented. $format option does not take effect.
- We are using the [Json.NET](http://james.newtonking.com/projects/json-net.aspx) component for serializing/deserializing objects to/from JSON format.
- Circular reference exception may occur during serialization if the content structure contains a circular reference. To avoid this we will create JsonConverters when needed.
- Exceptions are caught and logged on top level of the service. Default log target is the windows event log. All exceptions are wrapped in a `SenseNet.Portal.Odata.ODataException`.

## Operations

OData operations (see on [OData.org](http://www.odata.org/media/30002/OData.html#operations)) are integrated into the sensenet ECM via the _Action Framework_. Our actions can have two faces: they may control server generated HTML GUI (represented as applications) and may behave as OData operations. The action in _Action Framework_ is an extensibility point: every 3rd party action appears automatically in OData metadata if the current user has enough permissions.

For the list of built-in OData operations see the following article:

- [Built-in OData actions and functions](built-in-odata-actions-and-functions.md)

About developing custom OData see the following article:

- [How to create a custom OData action](http://community.sensenet.com/tutorials/how-to-create-a-custom-odata-action)

## Metadata requests

**Service Metadata Document**

If the request URI is the service root extended by "/$metadata", the server returns the Service Metadata Document that exposes the data model of the service in XML and JSON (see in [OData Documentation](http://www.odata.org/documentation/overview#ServiceMetadataDocument)). This document is the global (static) metadata that cannot contain content specific information e.g. expando (Content List) fields. For example:

- http://www.example.com/OData.svc/$metadata

**Instance Metadata Document**

Instance specific metadata is available on a collection or simple entity: the resource URI needs to end with the "/$metadata" segment. This metadata format is equivalent to the service metadata: it contains types, associations and EntityContainer. Every entity type is exdended with the expando fields of the current Content List if it exists and really contains expando fields. URI examples:

- Collection: http://www.example.com/OData.svc/workspaces/$metadata
- Entity: http://www.example.com/OData.svc/workspaces('project')/$metadata

The main difference between service and instance metadata is the list of **expando fields**: instance metadata contains these but service metadata does not. The main difference between instance metadata types can be the contained entity types. Entity metadata contains only one entity definition and collection metadata contains entity types originated from the available content types that are defined on the parent content.

## Examples

##### Information retrieval

HTTP method: **GET**

The examples for the GET method can be directly executed in the address bar of the browser.

Getting names of the first three document workspaces (collection):

`http://www.example.com/OData.svc/workspaces/Document?$top=3&$select=Name`

Getting a workspace (single entity):

`http://www.example.com/OData.svc/workspaces/Document('londondocumentworkspace')`

Filter by Content Type (IsOf):

`http://www.example.com/OData.svc/Root/IMS/BuiltIn/Portal?$filter=isof('User')`

Filter by Content Type (complementer set):

`http://www.example.com/OData.svc/Root/IMS/BuiltIn/Portal?$filter=not isof('User')`

Filter by part of name (such as SQL "LIKE"):

```
http://www.example.com/OData.svc/Root/IMS/BuiltIn/Portal?$filter=startswith(Name, 'Admin')
http://www.example.com/OData.svc/Root/IMS/BuiltIn/Portal?$filter=endswith(Name, ')')
http://www.example.com/OData.svc/Root/IMS/BuiltIn/Portal?$filter=substringof('-', Name)
```

##### Creating an entity

HTTP method: **POST**

Create an `EventList` under a workspace with the name Calendar and fill it’s Index field with 2.

```js
$.ajax({
    url: "/OData.svc/workspaces/Project('budapestprojectworkspace')",
    dataType: "json",
    type: 'POST',
    data: "models=[" + JSON.stringify({ '__ContentType':'EventList' , 'DisplayName': 'Calendar', 'Index': 2 }) + "]",
    success: function () {
      console.log('Content is successfully created');
    }
});
```

##### Content types

The content type of the new entity will be the first allowed content type of the parent entity. The default content type is overridable in the posted JSON object with the "__ContentType" property, as you can see in the example above.

##### Creating an entity from template

HTTP method: **POST**

This verb enables you to cretae a content by a Content Template creating an `EventList` under a workspace with the name Calendar and fill its Index field with 2. Other field values will be filled by the default values in 'CalendarTemplate3' ContentTemplate. See the `__ContentTemplate` parameter:

```js
$.ajax({
    url: "/OData.svc/workspaces/Project('budapestprojectworkspace')",
    dataType: "json",
    type: 'POST',
    data: "models=[" + JSON.stringify({ '__ContentType':'EventList', '__ContentTemplate':'Calendar3',
                                        'DisplayName': 'Calendar', 'Index': 2 }) + "]",
    success: function () {
      console.log('Content is successfully created');
    }
});
```

If the determined content template is not found or you leave this property out, the content will be created without a template. The '__ContentTemplate' parameter can be used only in content creation. HTTP methods other than POST and member requests (e.g. actions, functions) ignore this parameter.

##### Modifying one or more fields of an entity

HTTP method: **PATCH**

This verb enables you to modify a single or multiple fields of an entity. Let's change the index of a workspace to 142.

```js
$.ajax({
     url: "/OData.svc/workspaces/Project('budapestprojectworkspace')",
     dataType: "json",
     type: 'PATCH',
     data: "models=[" + JSON.stringify({'Index': 142}) + "]",
     success: function () {
		  console.log('Field is successfully modified');
     }
});
```

The following example demonstrates how can you modify a reference field of an entity. Let's change the Manager of a workspace:

```js
$.ajax({
     url: "/OData.svc/workspaces/Project('budapestprojectworkspace')",
     dataType: "json",
     type: 'PATCH',
     data: "models=[" + JSON.stringify({'Manager': 12345 }) + "]",
     success: function () {
		  console.log('Field is successfully modified');
     }
});
```

You may use either content id or path in case of reference fields. In case of a multiple reference field you should provide an array of ids or paths, as you can see below.

```js
$.ajax({
     url: "/OData.svc/workspaces/Project('budapestprojectworkspace')",
     dataType: "json",
     type: 'PATCH',
     data: "models=[" + JSON.stringify({'Customers': [ '/Root/Customer1', '/Root/Customer2'] }) + "]",
     success: function () {
		  console.log('Field is successfully modified');
     }
});
```

##### Setting all fields of an entity

HTTP method: **PUT**

This verb enables you to set multiple fields of an entity and clear the rest. Let's set the manager of a workspace to í_Alba Monday_ and clear all other fields.

```js
$.ajax({
       url:"/OData.svc/workspaces/Project('budapestprojectworkspace')",
       dataType: "json",
       type: 'PUT',
       data: "models=[" + JSON.stringify({ 'Manager': '/Root/IMS/BuiltIn/Demo/ProjectManagers/alba' }) + "]",
        success: function () {
          console.log('Fields are successfully modified');
        }
});
```

##### Delete an entity

HTTP method: **DELETE**

Delete the _Calendar list_ under one of the project workspaces:

```js
$.ajax({
       url: "/OData.svc/workspaces/Project/budapestprojectworkspace/('Calendar')",
       dataType: "json",
       type: 'DELETE',
       success: function () {
        console.log('Content is successfully deleted');
       }
});
```