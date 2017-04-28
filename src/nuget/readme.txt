************************************************************************************
                                sensenet ECM platform
                          Content Repository and Services
************************************************************************************

To finalize the installation and get started with sensenet ECM platform, please follow these steps:

1. Please make sure that the assembly bindings are correct in the 'runtime' section in Web.config: 
    - all Newtonsoft.Json versions should be redirected to the latest version (currently 9) instead of 6 
      It is possible that the install process already did this for you. You should see the following binding:

      <dependentAssembly>
        <assemblyIdentity name="Newtonsoft.Json" culture="neutral" publicKeyToken="30ad4fe6b2a6aeed" />
        <bindingRedirect oldVersion="0.0.0.0-9.0.0.0" newVersion="9.0.0.0" />
      </dependentAssembly>

    - please copy the contents of the runtime section from your Web.config to the Tools\SnAdminRuntime.exe.config file.
      (see comment at the end of SnAdminRuntime.exe.config)

2. The connection strings in the Web.config and Tools\SnAdminRuntime.exe.config files are defined to use 
   Integrated Security by default (meaning the Windows account you are using when executing the tool or the one 
   configured for the web app pool). If you are using SQL Server logins instead of Integrated Security, change 
   Integrated Security to false and then add a 'User Id' and 'Password' that will be used to authenticate to 
   the database.
   The account defined in the connectionString must have permissions to create a database, otherwise the install 
   will fail.

<connectionStrings>
    <add name="SnCrMsSql" connectionString="Data Source=SQLSERVER;Initial Catalog=sensenet;Integrated Security=false; User Id=test; Password=testpassword" providerName="System.Data.SqlClient" />
</connectionStrings

   Note that the db server and database name will be edited by the install tool, you do not have to add them manually.

3. Change the Global.asax markup file:
    - Inherits="SenseNet.Portal.Global"

4. Change the Global.asax.cs codebehind:
    - the application class should inherit from SenseNet.Services.SenseNetGlobal
    - change the Application_Start method header and call the base method before all generated and custom method calls:

    protected override void Application_Start(object sender, EventArgs e, HttpApplication application)
    {
        base.Application_Start(sender, e, application);
        
        // all generated and custom method calls should remain here: GlobalConfiguration, RegisterRoutes, etc.
        // ....
    
    Please do not override the whole method, just the header, and add the base method call.

5. Build your solution, make sure that there are no build errors.

6. Install sensenet ECM Content Repository database. Please make sure that you have access to a SQL Server.
    - open a command line and go to the \Admin\bin folder (added by this package)
    - execute the install-services command with the SnAdmin tool (you can specify optional parameters for SQL server and database name)
        - dataSource: your SQL server instance name (e.g. . or MSSQLSERVER\SQL2016)
        - initialCatalog: database name (this is the new db that will be created by the install command below)

    .\snadmin install-services dataSource:. initialCatalog:sensenet
    
    Please note that if the database already exists, this tool will fail to execute. There is a 'ForcedReinstall:true' switch that you can add if you want to execute this command in a repeatable script.

7. Update your Razor views (you can do this later at any time).
   If you use the built-in @Html.ActionLink method to render actions (as it is the case with the default project templates), you have to replace those calls in your .cshtml files with a new extension method added by this package:

   @Html.MvcActionLink

   (the parameters are the same, only the method name changes)

You are good to go! Hit F5 and start experimenting with the sensenet REST api!
For more information and support, please visit http://sensenet.com
