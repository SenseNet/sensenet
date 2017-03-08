************************************************************************************
								Sense/Net ECM platform
						  Content Repository and Services
************************************************************************************

To get started with Sense/Net ECM platform, please follow these steps:

1. Please make sure that the asssembly bindings are correct in the runtime section in Web.config: 
	- all Newtonsoft.Json versions should be redirected to the latest version (currently 9.0.1) instead of 6.
	- please copy the whole runtime section from your Web.config to the Tools\SnAdminRuntime.exe.config file added by this package.

2. Change the Global.asax markup file:
	- Inherits="SenseNet.Portal.Global"

3. Change the Global.asax.cs codebehind:
	- the application class should inherit from SenseNet.Services.SenseNetGlobal
	- change the Application_Start method header and call the base method:

	protected override void Application_Start(object sender, EventArgs e, HttpApplication application)
    {
        base.Application_Start(sender, e, application);
		
		// all generated and custom method calls should go here: GlobalConfiguration, RegisterRoutes, etc.
		// ....
        AreaRegistration.RegisterAllAreas();
        GlobalConfiguration.Configure(WebApiConfig.Register);
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
    }

4. Change Statup.cs: remove the following attribute

	[assembly: OwinStartupAttribute(typeof(WebApplication1.Startup))]

5. Build your solution, make sure that there are no build errors.

6. Install Sense/Net ECM Content Repository database. Please make sure that you have access to a SQL Server.
	- open a command line and go to the \Admin\bin folder (added by this package)
	- execute the installnuget command with the SnAdmin tool (you can specify optional parameters for SQL server and database name)

	snadmin installnuget [dataSource:SERVERNAME\SQL2016] [initialCatalog:sensenet]


You are good to go! Hit F5 and start experimenting with the Sense/Net REST api!
For more information and support, please visit http://sensenet.com