# sensenet ECM 7.0 components
With sensenet ECM we offer a robust but modularized Enterprise Content Management system. The whole product consists of multiple packages that build on each other. Every package has the **Services** component as a prerequisite, because that is the core layer of sensenet ECM.

This article is meant to help you understand the structure of our components and help you answer the first question that arises when starting to work with sensenet ECM:

> Which components should I install?

This is a list of the main components we published so far. To see an expanded, curated list of community components, check out our [awesome list](https://github.com/SenseNet/awesome-sensenet)!

###### Core layer and admin UI
- [Services](#Services): core layer, mother of all components, all other packages are optional.
- [WebPages](#WebPages): admin UI and built-in building blocks for WebForms enthusiasts.

###### Feature packages
- [Workspaces](#Workspaces): Workspace-related items (content types and templates, workspace dashboards and views) for sensenet ECM.
- [Workflow](#Workflow): Windows Workflow Foundation (WWF 4.5) integration into sensenet ECM.
- [Notification](#Notification): Email notification component for the sensenet ECM platform.
- ...and more!

###### Client SDKs
- [JavaScript/TypeScript client](#ClientJs): a client API that can be used either in the browser or a mobile app.
- [.Net client](#ClientDotNet): C# client API for tools and desktop applications.

> Alternatively you can install [sensenet 6.5](https://community.sensenet.com/docs/how-to-install-sn6), which is the previous version of sensenet ECM and contains all the features in a single installation.

<a name="Services"></a>
## Services
The Services component lives in the [main repository](https://github.com/SenseNet/sensenet), as **it is the core layer of sensenet ECM**. Installing it is mandatory and it is the first step of working with sensenet.

This is the only component you need if you have a web application, and are willing to build **your own custom UI** (views, controllers, etc.) on top of our Content Repository.

This is what you get if you install Services:

- the main Content Repository and dynamic content type system
- a set of built-in basic content types, like *File*, *Folder*, *Workspace* and *ContentList/DocumentLibrary*
- indexing and search engine
- security
- ...and many more core features
- but *no user interface*

After [installing Services](install-sn-from-nuget.md), you'll be able to access the Content Repository through several service entry points like the [REST api](http://wiki.sensenet.com/OData_REST_API) and [WebDav](http://wiki.sensenet.com/Webdav), or open/edit files directly in *Microsoft Office*.

![sensenet Services](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components_services.png "sensenet Services")

<a name="WebPages"></a>
## WebPages
Install the [WebPages component](https://github.com/SenseNet/sn-webpages) if you need a **graphical user interface** for browsing the Content Repository and performing the actions available on content items (e.g. CRUD or versioning operations, etc.). Please keep in mind that this package contains only an **admin UI**, no predefined end-user facing interface.

However you can **build pages** using the powerful [app model](http://wiki.sensenet.com/Smart_Application_Model) and our built-in [portlets](http://wiki.sensenet.com/Portlet) (building blocks that display content), or create your own portlets. The items in this component are built using *ASP.NET WebForms*.

![sensenet WebPages](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components_webforms.png "sensenet WebPages")

<a name="ClientJs"></a>
## JavaScript and TypeScript client
The [JavaScript client component](https://github.com/SenseNet/sn-client-js) lets you work with the sensenet ECM Content Repository (create or manage content, execute queries, etc.) by providing a JavaScript client API for the main content operations.

This library connects to the sensenet ECM REST API, but **hides the underlying HTTP requests**. You can work with simple load or create Content operations in JavaScript or TypeScript, instead of having to construct ajax requests yourself.

Work with the JS client instead of native JavaScript to boost your productivity and make client-server interaction a lot easier.

![sensenet JavaScript client](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components_jsclient.png "sensenet JavaScript client")

<a name="ClientDotNet"></a>
## .Net client
The [.Net client component](https://github.com/SenseNet/sn-client-dotnet) lets you work with the sensenet ECM Content Repository (create or manage content, execute queries, etc.) by providing a C# client API for the main content operations.

This library connects to the sensenet ECM REST API (it is compatible with SN 6.5 and SN 7 too), and **hides the underlying HTTP requests**. You can work with simple load or create Content operations in C#, instead of having to construct web requests yourself.

Speed up your development process and focus on your business logic (either in a custom import or migration tool, or a rich WPF desktop client)!

![sensenet .Net client](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components_netclient.png "sensenet .Net client")

<a name="Workspaces"></a>
## Workspaces
The [Workspaces component](https://github.com/SenseNet/sn-workspaces) is useful for document management or project-oriented scenarios. It gives you predefined workspace structures and dashboards to help organizing different types of content that are related to a project or a client in a unified environment.

<a name="Workflow"></a>
## Workflow
Integrating **Windows Workflow Foundation (WWF 4.5)** into sensenet ECM provides many possibilities for creating content-driven workflows. The [Workflow component](https://github.com/SenseNet/sn-workflow) adds a robust and customizable workflow engine to sensenet ECM. 

<a name="Notification"></a>
## Notification
[Email notification component](https://github.com/SenseNet/sn-notification) for the sensenet ECM platform. Lets users subscribe to content changes and receive emails either almost immediately or in an aggregated way periodically about changes in the repository.