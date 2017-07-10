# Install sensenet ECM 7.0 from NuGet
This article is **for developers** about installing the core layer, [sensenet Services](https://github.com/SenseNet/sensenet), but before getting into that, please take a look at the concept of our install method.

> **TLDR**: jump to the [Install packages](#InstallPackage) section to get started right away!

## Why NuGet?
In the last couple of years [NuGet](https://nuget.org) became the industry standard for delivering (mostly open source) libraries and projects. In our case, we take this even further by letting you install a full ECMS right from *Visual Studio*. This is not a common scenario, so we have to approach it carefully (the upgrade process is still under development).

## Why so many packages?
Sensenet ECM is a huge product consisting of many smaller components (take a look at this curated collection of built-in and 3rd party [components and plugins](https://github.com/SenseNet/awesome-sensenet)) that are not needed by everybody. It is best if you install only the parts you need, so that you can keep maintenance and upgrade costs at minimum. This is why we publish many small packages containing only the minimal set of libraries and content.

About choosing the components you need, take look at [this article](sensenet-components.md) that describes the main components briefly.

#### Two packages per component
Some of our components need multiple things to work, for example:
- server-side code (libraries)
- content items that should be imported at install time
- configuration changes

If you have a single project in *Visual Studio* (a web application), this is not a problem. But if you have *multiple projects* (e.g. a business or data layer beside your web app) that also need the sensenet libraries (to access our c# api), you would end up installing content items that are needed only for installation and/or webapp-specific multiple times, in projects that do not need these items.

This is why we decided to publish two types of packages for our components:

1. **SenseNet.Whatever**: contains only libraries. Should be added to *library projects* that need only the references.
2. **SenseNet.Whatever.Install**: has a NuGet dependency on the library package above, and contains installation artifacts: an [SnAdmin](https://github.com/SenseNet/sn-admin) package, config file or c# code changes/additions, anything that should go only to your web folder, nowhere else.

> **Long story short**: install the *SenseNet.Whatever.Install* package into your *web application* and the basic library package to your other projects.

<a name="InstallPackage"></a>
## Installing sensenet Services

![Sense/Net Services](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components_services.png "Sense/Net Services")

### Create a web project and pull in the package(s)

1. Create a new **ASP.NET web application** (using the MVC template, optionally adding *Web api* or *Web Forms*), or use an existing one.
2. Install the following NuGet packages (either in the Package Manager console or the Manage NuGet Packages window)

#### In the web app

[![NuGet](https://img.shields.io/nuget/v/SenseNet.Services.Install.svg)](https://www.nuget.org/packages/SenseNet.Services.Install)

> `Install-Package SenseNet.Services.Install -Pre`

(this will install the other one too, no need to pull that in manually)

#### In other projects

[![NuGet](https://img.shields.io/nuget/v/SenseNet.Services.svg)](https://www.nuget.org/packages/SenseNet.Services)

> `Install-Package SenseNet.Services -Pre`

### Web app changes
> The install process described below is the same that you will see in the _readme.txt_ that appears in *Visual Studio* after adding the install package. 

1. Change the *Global.asax* **markup** file's (not the cs file's) first line to contain a new parent type:

`Inherits="SenseNet.Portal.Global"`

2. Change the *Global.asax.cs* **codebehind** (the c# class):

- the application class should inherit from **SenseNet.Services.SenseNetGlobal**
- change the *Application_Start* **method header** and call the **base method** before all generated and custom method calls:

````csharp
    protected override void Application_Start(object sender, EventArgs e, HttpApplication application)
    {
        base.Application_Start(sender, e, application);
        
        // all generated and custom method calls should remain here: GlobalConfiguration, RegisterRoutes, etc.
        // ....
````

Please do not override the whole method (!), just the header, and add the base method call as seen above.

3. Optional: update your **Razor views** (you can do this later at any time).

   If you use the built-in *@Html.ActionLink* method to render actions (as it is the case with the default project templates), you have to replace those calls in your *.cshtml* files with a new extension method added by this package:

   `@Html.MvcActionLink`

   (the parameters are the same, only the method name changes)

4. **Build your solution**, make sure that there are no build errors.

### Create the database
Before installing the sensenet ECM Content Repository database, please make sure that you have access to a *SQL database server*.

The process will modify the **connection string** in _Web.config_ and _Tools\SnAdminRuntime.exe.config_ files **automatically**, ensuring that it is pointing to your SQL Server (DataSource) and Database Name (Initial Catalog). 

The connection string is configured to use _Integrated Security_ by default (this means sensenet will use the Windows account you are logged in with to execute the install tool and later to run the web application). 

> If you are using **SQL Server authentication** instead of Integrated Security, please provide the **username/dbusername** and **password/dbpassword** when you execute the install command described below.

Open a **command line** and go to the *[web]\Admin\bin* folder.

Execute the **install-services** command with the [SnAdmin](https://github.com/SenseNet/sn-admin) tool (you can specify optional parameters for SQL server, database name and user credentials).

- **dataSource**: your SQL server instance name (e.g. . or *MSSQLSERVER\SQL2016*)
- **initialCatalog**: database name (this is the new db that will be created by the install command below)
- **username** (optional): in case of SQL authentication the username to access SQL Server with *during the install process*.
- **password**: password for the user above
- **dbusername**: (optional): in case of SQL authentication the username to put into the *connection string in config files*. This is for the web application to access the db.
- **dbpassword**: password for the user above

> Please note that if you want to use SQL auth during both installation and runtime, you have to define both the username, password and the dbusername, dbpassword pairs of properties, there is no fallback.
>
> The database also can be installed on **Azure SQL**. In this case you have to use *SQL authentication* due to the fact that the installer does not support *Azure Active Directory* authentication yet (but after installation you can change the configuration to connect to Azure SQL using AAD of course).

````text
.\snadmin install-services dataSource:. initialCatalog:sensenet
````
    
> Please note that if the *database already exists*, this tool will fail to execute. There is a **ForcedReinstall:true** switch that you can add if you want to execute this command repeatedly, for example in a build script.

You are good to go! Hit F5 in Visual Studio and start experimenting with the sensenet REST api!

In case of errors during execution, please take a look at the [Troubleshooting](#Troubleshooting) section below, or head over to [StackOverflow](http://stackoverflow.com/tags/sensenet) for answers.

## After installing sensenet Services
After you installed sensenet Services, you can start sending requests to the site. 

Consider using the following client projects to manipulate data in the Content Repository through its REST API:

- [sensenet ECM JavaScript Client](https://github.com/SenseNet/sn-client-js)
- [sensenet ECM .Net Client](https://github.com/SenseNet/sn-client-dotnet)

Here are a couple of examples for accessing the REST API from native JavaScript, if you prefer that. 

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

For detailed examples, please visit the [REST API article](http://wiki.sensenet.com/OData_REST_API).

<a name="Troubleshooting"></a>
## Troubleshooting
Here are a few tips in case you encounter an error during or after installation.
#### Build the project
Please make sure that you have built the solution (and there were no build errors) before executing the install command.

#### Incorrect bindings: type loading error

Make sure that the **assembly bindings** are correct in the **runtime** section in *Web.config* and the *Tools\SnAdminRuntime.exe.config* files: all *Newtonsoft.Json* versions should be redirected to the correct version (at least 9, but can be the latest) *instead of 6*. You should see the following binding:

````xml
 <dependentAssembly>
   <assemblyIdentity name="Newtonsoft.Json" culture="neutral" publicKeyToken="30ad4fe6b2a6aeed" />
   <bindingRedirect oldVersion="0.0.0.0-9.0.0.0" newVersion="9.0.0.0" />
 </dependentAssembly>
````
