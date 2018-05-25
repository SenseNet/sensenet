---
title: "Configure the repository"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/configure-repository.md'
category: Development
version: v7.0
tags: [configuration, startup, repository, tests, ioc]
description: This article is about starting the application or the repository itself using custom options or providers.
---

# Configure the repository
The Content Repository is a complex structure of data and code. Some parts of the system can be thought of as building blocks and can be replaced by custom implementations (providers). In this article we describe the possibilities for developers to **configure the repository from code**, during application or repository start.

> A **provider** is an implementation of one of the many extensibility points (interfaces or base classes) available in sensenet. Providers are created as alternative implementations for certain APIs (for example accessing the db, handling server-to-server messages or search operations) and can be injected as building blocks to the system.

In many cases it is sufficient to use the default providers (e.g. for messaging) in sensenet, or setting a custom provider in a _configuration file_ (web.config). There are cases however when you want to provide a custom provider instance that cannot be instantiated automatically using its default constructor or you want to control other options when starting the repository.

> An example for a case when you have to use this approach is a messaging provider that needs to be instantiated using a _service url_ parameter. Of course this depends on the design of the provider - for example this may be unnecessary if it is able to read the service url from a config file.

## Repository life cycle
The repository object's life cycle is simple: you start it when the application starts and shut it down when the application exits. The repository instance encapsulates all providers (e.g. database or search providers). This way you can manage all the implementations on top of the content repository. All repository-related operations (e.g. querying, loading or saving content) should happen while the repository is live.

```csharp
using (Repository.Start(repositoryBuilder))
{
    // work with content items
}
```

> In case of an **Asp.Net** application you **do not have to manually start and stop** the repository using the code above - sensenet does it for you in the base global class.

The `repositoryBuilder` parameter above lets developers customize which features of the repository should start (e.g. workflow and search engines) and set the providers responsible for certain features.

## Repository builder
This API lets you change one or more default options or providers when starting the repository. The object has a _fluent api_ that makes it easy to change one or more features.

```csharp
var repositoryBuilder = new RepositoryBuilder()
   .UseSecurityMessageProvider(new MyMessageProvider("serviceurl"))
   .UseCacheProvider(new MyCustomCacheProvider())
   .StartWorkflowEngine(false);
```

We use this API for starting and stopping the repository in _tests_, where it is necessary to replace virtually all important built-in implementations with their in-memory versions so that tests can run fast and without a database dependency.

### Custom providers
This API lets developers add their own custom providers by type or name:

```csharp
repositoryBuilder.UseProvider(MyProviderName, new MyCustomProvider());
```

...and retrieve them later in their custom code using the `Providers` API.

```csharp
public static ICustomProvider Instance
{
   get 
   {
      return Providers.Instance.GetProvider<ICustomProvider>(MyProviderName);
   }
}
```

> Yes, this is a simple but powerful IoC framework inside sensenet.

## Application start
If you [installed sensenet](install-sn-from-nuget.md) in an Asp.Net application you already have an `Application_Start` method in your application class derived from our `SenseNetGlobal` class. If you want to configure the repository builder instance right before sensenet starts the repository, you only have to override the `ConfigureRepository` method and add your providers and options.

```csharp
public class MvcApplication : SenseNet.Portal.SenseNetGlobal
{
    protected override void Application_Start(object sender, EventArgs e, HttpApplication application)
    {
        ...
    }

    protected override void ConfigureRepository(RepositoryBuilder repositoryBuilder)
    {
        repositoryBuilder
            .UseSecurityMessageProvider(new MyMessageProvider("serviceurl"))
            .UseProvider("myprovider", new MyCustomProvider());
    }
}
```