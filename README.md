# Welcome to sensenet ECM
The first Open Source Enterprise Content Management platform for .NET! 

> [Try it online](http://www.sensenet.com/try-it) without installation!

[![Join the chat at https://gitter.im/SenseNet/sensenet](https://badges.gitter.im/SenseNet/sensenet.svg)](https://gitter.im/SenseNet/sensenet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

> **sensenet Services 7.0 beta** is out! Jump to the [Getting started](#GettingStarted) section below to start experimenting right away!

If you need...
- a **Content Repository** with a powerful query engine (built on [Lucene.Net](https://lucenenet.apache.org)) for storing *millions* of documents,
- an extendable .Net **development platform** with many features developers will like (*OData REST API* with a .Net client SDK, *LINQ to sensenet*, a unified *Content layer* - and many more),
- a flexible **security** layer with customizable content permissions, honored by the query engine,
- a scalable enterprise architecture with NLB and background task management,
- *workspaces*, *lists* and *libraries* to make collaboration easier

...you're covered!

![Workspaces](http://wiki.sensenet.com/images/5/5e/Ws-main.png "Workspaces")

## sensenet can be a lot of things

- a development platform
- an internet and intranet portal
- a central Content Repository for all kinds of custom content
- an integration point between your (or your clients') existing applications

Let us know which part you're intrested in most!

## License
sensenet is available in two editions:

1. **Community Edition**: a community-supported [GPL v2](LICENSE) edition with almost all the features.
   The source code is available on [CodePlex](http://sensenet.codeplex.com) (for **version 6.5**) and here on *GitHub* (for the new, componentized **version 7.0** - see details below).
2. **Enterprise Edition**: with additional enterprise-grade features (like AD sync, MongoDB blob provider) and vendor support! For details, visit the [licensing page](http://www.sensenet.com/sensenet-ecm/licencing) on our site.

## Contact and support
Whether you're a community member or enterprise customer, feel free to visit our communication channels for demo, examples and support:
- Website: http://www.sensenet.com
- Main chat channel: https://gitter.im/SenseNet/sensenet
- All chat channels: https://gitter.im/SenseNet
- Community support: http://stackoverflow.com/questions/tagged/sensenet
- Enterprise support: http://support.sensenet.com

<a name="GettingStarted"></a>
## Getting started
Currently we offer two different versions of sensenet ECM. We recommend version 7.0 for new projects as it is more lightweight and flexible.

### sensenet ECM 7.0 (beta)
A modern ECM platform that can be integrated into existing or new web applications. We modularized sensenet ECM so that you can install only the parts you need. Take a look at the currently published [core components](/doc/sensenet-components.md)!

There is also a number of other built-in and 3rd party [components and plugins](https://github.com/SenseNet/awesome-sensenet) that are built on this platform either by us or the community.

![sensenet components](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components.png "sensenet components")

- [Core components of sensenet ECM](/doc/sensenet-components.md)
- [Awesome list of components and plugins](https://github.com/SenseNet/awesome-sensenet)
- [Install sensenet ECM from NuGet](/docs/install-sn-from-nuget.md)

#### After installing sensenet ECM
After you installed [sensenet ECM](/docs/install-sn-from-nuget.md), you can start sending requests to the site. 

Consider using the following client projects to manipulate data in the Content Repository through its REST API:

- [sensenet JavaScript Client](https://github.com/SenseNet/sn-client-js)
- [sensenet .Net Client](https://github.com/SenseNet/sn-client-dotnet)

For detailed client side examples, please visit the [REST API article](http://wiki.sensenet.com/OData_REST_API).

### sensenet ECM 6.5
A feature-rich Enterprise CMS with predefined UI and building blocks: pages, portlets, action controls and more. Build your solution with almost no development effort.

If you are new to sensenet, it is worth checking out these introductory articles on our [wiki](http://wiki.sensenet.com):
- [Getting started - using sensenet](http://wiki.sensenet.com/Getting_started_-_using_Sense/Net)
- [Getting started - installation and maintenance](http://wiki.sensenet.com/Getting_started_-_installation_and_maintenance)
- [Getting started - building portals](http://wiki.sensenet.com/Getting_started_-_building_portals)
- [Getting started - developing applications](http://wiki.sensenet.com/Getting_started_-_developing_applications)

## Contributing
All kinds of contributions are welcome! We are happy if you have an idea, bugfix or feature request to share with others. Please check out our [Contribution guide](CONTRIBUTING.md) for details.
