# Welcome to Sense/Net
The first Open Source Enterprise Content Management platform for .NET!

[![Join the chat at https://gitter.im/SenseNet/sn-taskmanagement](https://badges.gitter.im/SenseNet/sensenet.svg)](https://gitter.im/SenseNet/sensenet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

> **Sense/Net Services 7.0 beta** is out! Jump to the [Getting started](#GettingStarted) section below to start experimenting right away!

If you need...
- a **Content Repository** with a powerful query engine (built on [Lucene.Net](https://lucenenet.apache.org)) for storing *millions* of documents,
- an extendable .Net **development platform** with many features developers will like (*OData REST API* with a .Net client SDK, *LINQ to Sense/Net*, a unified *Content layer* - and many more),
- a flexible **security** layer with customizable content permissions, honored by the query engine,
- a scalable enterprise architecture with NLB and background task management,
- *workspaces*, *lists* and *libraries* to make collaboration easier

...you're covered!

![Workspaces](http://wiki.sensenet.com/images/5/5e/Ws-main.png "Workspaces")

## Sense/Net can be a lots of things

- a development platform
- an internet and intranet portal
- a central Content Repository for all kinds of custom content
- an integration point between your (or your clients') existing applications

Let us know which part you're intrested in most!

## License
Sense/Net is available in two editions:

1. **Community Edition**: a community-supported [GPL v2](LICENSE) edition with almost all the features.
   The source code is available currently on [CodePlex](http://sensenet.codeplex.com), but we'll move it here soon :smiley:.
2. **Enterprise Edition**: with additional enterprise-grade features and support! For details, visit the [licencing page](http://www.sensenet.com/sensenet-ecm/licencing) on our site.

## Contact and support
Whether you're a community member or enterprise customer, feel free to visit our communication channels for demo, examples and support:
- Website: http://www.sensenet.com
- Main chat channel: https://gitter.im/SenseNet/sensenet
- All chat channels: https://gitter.im/SenseNet
- Community support: http://stackoverflow.com/questions/tagged/sensenet
- Enterprise support: http://support.sensenet.com

<a name="GettingStarted"></a>
## Getting started
Currently we offer two different versions of Sense/Net ECM. We recommend version 7.0 for new projects as it is more lightweight and flexible.
### Sense/Net ECM 7.0 (beta)
A modern ECM platform that can be integrated into existing or new web applications.
- **Sense/Net Services** (the current GitHub repository): a robust Content Repository with enterprise features (security, querying, lists and libraries, dynamic content types), accessible through a REST API, often referred to as a _headless ECMS_.
- a selection of [components and plugins](https://github.com/SenseNet/awesome-sensenet) that are built on this platform.

Developers may start integrating the Sense/Net platform by installing the **Sense/Net Services NuGet package** into an ASP.NET MVC web application. 

[![NuGet](https://img.shields.io/nuget/v/SenseNet.Services.svg)](https://www.nuget.org/packages/SenseNet.Services)

1. Create a new ASP.NET web application (using the MVC template, optionally adding Web api or Web Forms), or use an existing one.
2. Install the following NuGet package (either in the Package Manager console or the Manage NuGet Packages window)

`> Install-Package SenseNet.Services`

3. To finalize the install process, please follow the steps described in the [readme file](/src/nuget/readme.txt) of the NuGet package (it opens automatically when you install the package). 
    - You will have to make a few modifications to your config files and Global.asax markup and codebehind file.
    - You will have to compile your solution and **execute a command line tool** that will **create the database** and import the necessary initial content.
    - If you are using the built-in _ActionLink_ helper method in your MVC views, you'll have to replace them with a new extension method added by this package: _MvcActionLink_.

Take into account that currently there is _no UI layer available out of the box_ for Sense/Net ECM 7.0. We are working on moving features available in Sense/Net 6.5 to the new platform. They will be published as NuGet and SnAdmin install packages so that you will be able to build your solution easily using only the components that you truly need.

#### After installing Sense/Net Services
After you added the Sense/Net Services [NuGet package](https://www.nuget.org/packages/SenseNet.Services), you can start sending requests to the site. 

Consider using the following client projects to manipulate data in the Content Repository through its REST API:

- [Sense/Net JavaScript Client](https://github.com/SenseNet/sn-client-js)
- [Sense/Net .Net Client](https://github.com/SenseNet/sn-client-dotnet)

Here are a couple of examples for accessing the REST API from native JavaScript, if you prefer that. For detailed examples, please visit the [REST API article](http://wiki.sensenet.com/OData_REST_API).

##### Log in
This is how you can log in from JavaScript using one of the existing users:
````
$.ajax({
     url: "/Odata.svc/('Root')/Login?metadata=no",
     dataType: "json",
     type: 'POST',
     data: JSON.stringify({
          'username': "admin",
          'password': "admin"
     }),
     success: function (d) {
          console.log('You are logged in!')
     }
});
````
The response will contain a standard ASP.NET authentication cookie that your browser will send with subsequent requests automatically.
##### Create content
You can create a workspace under the Root content:
````
$.ajax({
    url: "/OData.svc/('Root')",
    dataType: "json",
    type: 'POST',
    data: "models=[" + JSON.stringify({ '__ContentType':'Workspace' , 'DisplayName': 'Workspace' }) + "]",
    success: function () {
        console.log('Success');
    }
});
````
Create a document library in the workspace:
````
$.ajax({
    url: "/OData.svc/Root/('Workspace')",
    dataType: "json",
    type: 'POST',
    data: "models=[" + JSON.stringify({ '__ContentType':'DocumentLibrary' , 'DisplayName': 'DocLib' }) + "]",
    success: function () {
        console.log('Success');
    }
});
````

### Sense/Net ECM 6.5
A feature-rich Enterprise CMS with predefined UI and building blocks: pages, portlets, action controls and more. Build your solution with almost no development effort.

If you are new to Sense/Net, it is worth checking out these introductory articles on our [wiki](http://wiki.sensenet.com):
- [Getting started - using Sense/Net](http://wiki.sensenet.com/Getting_started_-_using_Sense/Net)
- [Getting started - installation and maintenance](http://wiki.sensenet.com/Getting_started_-_installation_and_maintenance)
- [Getting started - building portals](http://wiki.sensenet.com/Getting_started_-_building_portals)
- [Getting started - developing applications](http://wiki.sensenet.com/Getting_started_-_developing_applications)

## Contributing
All kinds of contributions are welcome! We are happy if you have an idea, bugfix or feature request to share with others. Please check out our [Contribution guide](CONTRIBUTING.md) for details.
