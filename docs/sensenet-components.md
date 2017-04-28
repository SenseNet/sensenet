# Sensenet ECM 7.0 components
With sensenet ECM we offer a robust but modularized *Enterprise Content Management* system. The whole product consists of multiple packages

This article is meant to help you understand the structure of our components and help you answer the first question that arises when starting to work with sensenet ECM:

> Which components should I install?

This is a list of the main components we published so far. To see an expanded, curated list of community components, check out our [awesome list](https://github.com/SenseNet/awesome-sensenet)!

- [Services](#Services): core layer, mother of all components, all other packages are optional.
- [WebPages](#WebPages): admin UI and built-in building blocks for WebForms enthusiasts.

> Alternatively you can install [sensenet 6.5](https://community.sensenet.com/docs/how-to-install-sn6), which is the previous version of sensenet ECM and contains all the features in a single installation.

<a name="Services"></a>
## Services
The Services component lives in the [main repository](https://github.com/SenseNet/sensenet), as it is the core layer of sensenet ECM. Installing it is mandatory and it is the first step of working with sensenet.

This is what you get if you install Services:

- the main Content Repository and dynamic content type system
- a set of built-in basic content types, like *File*, *Folder*, *Workspace* and *ContentList/DocumentLibrary*
- indexing and search engine
- security
- ...and many more core features
- but *no user interface*

After [installing Services](install-sn-from-nuget.md), you'll be able to access the Content Repository through several service entry points like the [REST api](http://wiki.sensenet.com/OData_REST_API) and [WebDav](http://wiki.sensenet.com/Webdav), or open/edit files directly in *Microsoft Office*.

![Sense/Net Services](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components_services.png "sensenet Services")

<a name="WebPages"></a>
## WebPages
Install the [WebPages component](https://github.com/SenseNet/sn-webpages) if you need a **graphical user interface** for browsing the Content Repository and performing the actions available on content items (e.g. CRUD or versioning operations, etc.). Please keep in mind that this package contains only an **admin UI**, no predefined end-user facing interface.

However you can **build pages** using the powerful [app model](http://wiki.sensenet.com/Smart_Application_Model) and our built-in [portlets](http://wiki.sensenet.com/Portlet) (building blocks that display content), or create your own portlets. The items in this component are built using *ASP.NET WebForms*.

![sensenet WebPages](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components_webforms.png "sensenet WebPages")