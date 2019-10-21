# Application

Applications are the basic building blocks of the [Smart Application Model](smart-application-model.md) that define the way specific Content is presented and processed when addressed. [Smart Pages](smart-pages.md) are the most common applications but there are also special Application types that do not appear in the form of pages and return with a custom response. Applications can be invoked via [Actions](action.md).

### Presenting Content with Applications

> The features described in this section (displaying content with Pages) are available only if you have the sensenet ECM [WebPages](https://github.com/SenseNet/sn-webpages) component installed, but the underlying philosophy of arranging applications, security and url generation applies even if you only have the core [Services layer](https://github.com/SenseNet/sensenet).

The simplest way to present a [Content](content.md) is done by placing portlets on individual pages that utilize Content presentational functionality (eg. [Content viewer Portlet](content-viewer-portlet.md). However, Content can also be presented by addressing the Content itself and choosing an Application to present it. The selected Application is usually a simple page that uses [Context bound Portlets](context-bound-portlets.md) to handle the addressed [Content](content.md). These Applications are also referred to as [Smart Pages](smart-pages.md) and a single Smart Page in itself is capable of presenting many different [Content](content.md) of the same type. To create a Smart Page create a new Portlet Page under an _(apps)/[Content Type name]_ folder in the [Content Repository](content-repository.md) and use [Context bound Portlets](context-bound-portlets.md) for Content handling.

### Application model

Path of the created Application bears special importance as it defines the range of Content it is applicable to. The path of an Application can be given in the form:

```xml
/Root/<custompath>/(apps)/<contenttypename>/<applicationname>
```

where

- **custompath** defines the path under which the Application will handle Content. The application is not accessible outside of this subtree.
- **contenttypename** defines the type of Content that is handled by the Application.
- **applicationname** is the name of the Application.

#### Application inheritance

Applications can be *overridden* in a subtree by placing an Application of the same name under an _(apps)_ folder that is placed at a deeper level in the [Content Repository](content-repository.md) and also by using the `This` keyword as contenttypename. See examples and refer to [Smart Application Model](smart-application-model.md) for details.

### Applications and Actions

To connect Content and Application - besides properly setting up the Application Model in the [Content Repository](content-repository.md) - so called [Actions](action.md) can be used. An [Action](action.md) in this case is a link that addresses the [Content](content.md) and specifies the name of the Application, for example:

- **HTML action link** (Edit page): *http://www.example.com/MyBlog/2010/08/Great_day*?**action=Edit**
- **OData action link** (Delete action REST api): *http://www.example.com/odata.svc/MyBlog/2010/08('Great_day')*/**Delete**

The Application is resolved then using [Smart Application Model](smart-application-model.md) mechanisms.

### Application types

Altough an Application for a [Content Type](content-type.md) is usually defined as a [Smart Page](smart-pages.md) to present the [Content](content.md), there are several other Application types defined in the system. An RssApplication for example will return the Rss feed corresponding to the addressed [Content](content.md) in the response. The base [Content Type](content-type.md) for Application types can be found under _/Root/System/Schema/ContentTypes/GenericContent/Application_. Here is a few examples of pre-defined Application types:

- **Page**: defines a page layout to be rendered - when used with [Context bound Portlets](context-bound-portlets.md) it is referred to as a [Smart Page](smart-pages.md).
- **ApplicationOverride**: it can be used to override Fields of an existing Application (Smart Page), without modifying the page layout
- **HttpStatusApplication**: returns with the specified status code.
- **ImgResizeApplication**: resizes and caches the requested image according to set parameters and returns with the cached image in the response.
- **RssApplication**: returns the Rss feed for the requested [Content](content.md) in the response.
- **XsltApplication**: transforms the xml representation of the addressed [Content](content.md) with the specified XSLT and puts the output to the response.

#### OData applications
There is a special type of application that is a placeholder for defining an [OData REST API](odata-rest-api.md) endpoint. This is the **GenericODataApplication** that points to a method that will be executed when the api endpoint is called. These methods are very similar to ASP.NET web api methods, and this is how you customize and extend the REST api of sensenet ECM.

- [Generic OData action](generic-odata-action.md)

### Application configuration

Since an Application itself is a [Content](content.md) in the [Content Repository](content-repository.md), it has got Fields that describe its behavior. These Fields include the following (only listing the most important Fields):

- **Scenario**: the list of scenario 'keywords' that define the places where action links referring the Application will appear. For example an Application with _ListItem_ scenario can be initiated from the dropdown menu in various lists; an Application with `ExploreActions` scenario appears in the [Actions](action.md) menu of [Explore](content-explorer.md). You can read more about this at the [Action](action.md) reference wiki page.
- **ActionTypeName**: the .Net type of action (a class name) that is created when the Application is referred via an [Action](action.md). A few example for built-in types:
  - _UrlAction_ (default): a simple link is created with `?action` url parameter that points to the Application name.
  - _UploadAction_: a `UrlAction` for containers with a custom evaluation: the upload link becomes disabled if the context folder does not allow File types to be added as child Content.
  - _CopyToAction_: a simple link is created that brings up a Content Picker where destination Folder can be selected. Context [Content](content.md) is then copied using the `CopyToTarget` Application.
- **Disabled**: setting it to *true* makes the Application 'disappear' and cannot be invoked through an action link. Applications on higher levels with the same name (those that were overridden by this specific instance) remain functional and take part in the Application path resolution logic.
- **Clear**: similar to Disabled, except that it hides higher level, overrided Applications of the same name as well. This is useful for *disabling an Application for a subtree* when the Application has been defined on a higher level (eg. in _/Root/(apps)_) and is available for [Content](content.md) in several subtrees - but it should not appear for [Content](content.md) that are under a specific subtree.
- **Icon**: name of the Icon for the Application. This icon appears next to action links if it is enabled by the control that renders the link. Icons reside in the _/Root/Global/images/icons_ folder.
- **StyleHint**: an optional string property that can be interpreted by a custom renderer. Built-in renderers do not use this property.
- **RequiredPermissions**: multiselect list that defines the permissions that are required on the target Content for the Application to be applicable to it. For example for an Edit Application one could specify the Edit permission to be required on the [Content](content.md), so that for users that don't possess sufficient rights the Edit link will be disabled and thus cannot navigate to a Content's Edit Application. Besides the permissions specified here there are some necessary basic permission settings for a [Content](content.md) to be presented with an Application.
- **DeepPermissionCheck**: if set to true the required permissions specified above are checked for the entire subtree under the context [Content](content.md). This can be a useful setting for Applications that operate on whole subtrees, not only specific [Content](content.md) (a subtree move or update Application for example).
- **IncludeBackUrl**: setting it to false will guide the action presenter logic not to include a back url in the action url. Default value for the portal is True except for Browse actions. For more info see the [Action](action.md) page.
- **CacheControl**: the response is generated with the selected Cache-control headers. Its value can be one of the standard .NET values (NoCache, Private, Public, Server, ServerAndNoCache, ServerAndPrivate) or Nondefined (see [Proxy Cache Configuration](proxy-cache-configuration.md)).
- **MaxAge**: it is an integer value in seconds for Cache-control: max-age=x header to be sent out (see [Proxy Cache Configuration](proxy-cache-configuration.md)).
- **CustomUrlParameters**: this is a string value containing custom parameters that will be added to the action url that belongs to this application. E.g. _'type=visible;mode=m1'_
