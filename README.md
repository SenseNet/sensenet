# Welcome to SenseNet
The first Open Source Enterprise Content Management platform for .NET!

[![Join the chat at https://gitter.im/SenseNet/sn-taskmanagement](https://badges.gitter.im/SenseNet/sensenet.svg)](https://gitter.im/SenseNet/sensenet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

If you need...
- a **Content Repository** with a powerful query engine (built on [Lucene.Net](https://lucenenet.apache.org)) for storing *millions* of documents,
- an extendable .Net **development platform** with many features developers will like (*OData REST API* with a .Net client SDK, *LINQ to SenseNet*, a unified *Content layer* - and many more),
- a flexible **security** layer with customizable content permissions, honored by the query engine,
- a scalable enterprise architecture with NLB and background task management,
- *workspaces*, *lists* and *libraries* to make collaboration easier

...you're covered!

![Workspaces](http://wiki.sensenet.com/images/5/5e/Ws-main.png "Workspaces")

## SenseNet can be a lots of things

- a development platform
- an internet and intranet portal
- a central Content Repository for all kinds of custom content
- an integration point between your (or your clients') existing applications

Let us know which part you're intrested in most!

## License
SenseNet is available in two editions:

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

Take into account that currently there is _no UI layer available out of the box_ for Sense/Net ECM 7.0. We are working on moving features available in Sense/Net 6.5 to the new platform. They will be published as NuGet and SnAdmin install packages so that you will be able to build your solution easily using only the components that you truly need.

### Sense/Net ECM 6.5
A feature-rich Enterprise CMS with predefined UI and building blocks: pages, portlets, action controls and more. Build your solution with almost no development effort.

If you are new to SenseNet, it is worth checking out these introductory articles on our [wiki](http://wiki.sensenet.com):
- [Getting started - using SenseNet](http://wiki.sensenet.com/Getting_started_-_using_Sense/Net)
- [Getting started - installation and maintenance](http://wiki.sensenet.com/Getting_started_-_installation_and_maintenance)
- [Getting started - building portals](http://wiki.sensenet.com/Getting_started_-_building_portals)
- [Getting started - developing applications](http://wiki.sensenet.com/Getting_started_-_developing_applications)

## Contributing
All kinds of contributions are welcome! We are happy if you have an idea, bugfix or feature request to share with others. Please check out our [Contribution guide](CONTRIBUTING.md) for details.
