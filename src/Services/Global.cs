using System;
using System.Web.Compilation;
using SenseNet.ContentRepository;
using SenseNet.Services;

namespace SenseNet.Portal
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            LoggingSettings.SnTraceConfigurator.UpdateStartupCategories();

            BuildManager.GetReferencedAssemblies();
            SenseNetGlobal.ApplicationStartHandler(sender, e, this);
        }
        protected void Application_End(object sender, EventArgs e)
        {
            SenseNetGlobal.ApplicationEndHandler(sender, e, this);
        }
        protected void Application_Error(object sender, EventArgs e)
        {
            SenseNetGlobal.ApplicationErrorHandler(sender, e, this);
        }
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            SenseNetGlobal.ApplicationBeginRequestHandler(sender, e, this);
        }
        protected void Application_EndRequest(object sender, EventArgs e)
        {
            SenseNetGlobal.ApplicationEndRequestHandler(sender, e, this);
        }

    }
}
