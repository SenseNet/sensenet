************************************************************************************
                                sensenet ECM platform
                          Content Repository and Services
************************************************************************************

To read these instructions on the web in a more readable format, follow this link:
https://github.com/SenseNet/sensenet/blob/master/docs/install-sn-from-nuget.md#InstallPackage

To finalize the installation and get started with sensenet ECM platform, please follow these steps:

1. Change the Global.asax markup file's (not the cs file's) first line to contain a new parent type:
   
   Inherits="SenseNet.Portal.Global"

2. Change the Global.asax.cs codebehind (the c# class):

   - the application class should inherit from SenseNet.Services.SenseNetGlobal
   - change the Application_Start method header and call the base method before all generated and custom method calls:

   protected override void Application_Start(object sender, EventArgs e, HttpApplication application)
   {
      base.Application_Start(sender, e, application);
    
      // all generated and custom method calls should remain here: GlobalConfiguration, RegisterRoutes, etc.
      // ....
    
   Please do not override the whole method, just the header, and add the base method call.

3. Optional: update your Razor views (you can do this later at any time).

   If you use the built-in @Html.ActionLink method to render actions (as it is the case with the default project templates), 
   you have to replace those calls in your .cshtml files with a new extension method added by this package:

   @Html.MvcActionLink 

   (the parameters are the same, only the method name changes)

4. Build your solution, make sure that there are no build errors.

5. Install sensenet ECM Content Repository database. Please make sure that you have access to a SQL Server.

   The process will modify the connection string in Web.config and Tools\SnAdminRuntime.exe.config files automatically, 
   ensuring that it is pointing to your SQL Server (DataSource) and Database Name (Initial Catalog).

   If you are using SQL Server authentication instead of Integrated Security, please provide the username/dbusername and 
   password/dbpassword when you execute the install command described below.

   - open a command line and go to the \Admin\bin folder (added by this package)
   - execute the install-services command with the SnAdmin tool (you can specify optional parameters for SQL server and database name)
      - dataSource: your SQL server instance name (e.g. . or MSSQLSERVER\SQL2016)
      - initialCatalog: database name (this is the new db that will be created by the install command below)
      - username (optional): in case of SQL authentication the username to access SQL Server with during the install process.
	  - password: password for the user above
	  - dbusername: (optional): the username to put into the connection string in config files. This is for the web application to access the db.
      - dbpassword: password for the user above

   Please note that if you want to use SQL auth during both installation and runtime, you have to define both the username 
   and the dbusername properties, there is no fallback.

   .\snadmin install-services dataSource:. initialCatalog:sensenet
    
   Please note that if the database already exists, this tool will fail to execute. There is a 'ForcedReinstall:true' switch 
   that you can add if you want to execute this command multiple times or in a repeatable script.


You are good to go! Hit F5 and start experimenting with the sensenet REST api!
For more information and support, please visit http://sensenet.com


************************************************************************************
Troubleshooting
************************************************************************************

Here are a few tips in case you encounter an error during or after installation.

1. Build the project

   Please make sure that you have built the solution (and there were no build errors) before executing the install command.

2. Incorrect bindings: type loading error

   Make sure that the assembly bindings are correct in the runtime section in Web.config and the Tools\SnAdminRuntime.exe.config files. 
   All Newtonsoft.Json versions should be redirected to the correct version (at least 9, but can be the latest) instead of 6. 
   You should see the following binding:

   <dependentAssembly>
      <assemblyIdentity name="Newtonsoft.Json" culture="neutral" publicKeyToken="30ad4fe6b2a6aeed" />
      <bindingRedirect oldVersion="0.0.0.0-9.0.0.0" newVersion="9.0.0.0" />
   </dependentAssembly>

