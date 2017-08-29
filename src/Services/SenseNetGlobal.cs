using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using SenseNet.ContentRepository.Storage.Security;
using File = System.IO.File;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Virtualization;
using System.IO;
using System.Reflection;
using SenseNet.Diagnostics;
using SenseNet.Portal.Routing;
using SenseNet.Portal.Resources;
using SenseNet.Portal.Handlers;
using System.Web.Hosting;
using System.Web.SessionState;
using SenseNet.Configuration;
using SenseNet.Portal;
using SenseNet.Tools;

namespace SenseNet.Services
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InternalSenseNetHttpApplicationAttribute : Attribute { }

    public class SenseNetGlobal
    {
        /*====================================================================================================================== Static part */
        private static SenseNetGlobal __instance;
        private static SenseNetGlobal Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = ChooseInstance(TypeResolver.GetTypesByBaseType(typeof(SenseNetGlobal)));
                    SnTrace.System.Write("Global class loaded: " + __instance.GetType().FullName);
                }
                return __instance;
            }
        }

        internal static SenseNetGlobal ChooseInstance(Type[] inheritedTypes)
        {
            var globalType = ChooseType(inheritedTypes);
            return globalType != null ? (SenseNetGlobal)Activator.CreateInstance(globalType) : new SenseNetGlobal();
        }
        internal static Type ChooseType(Type[] inheritedTypes)
        {
            // The internal global type is in the upper layer of Sense/Net that may or may not be present.
            // An external global type is the one created by a 3rd party developer who integrates
            // Sense/Net into an ASP.NET web application. Fallback order:
            // 1. external global class if present (it must inherit the internal global class!)
            // 2. internal global class if present
            // 3. base global class (the one we are in now)

            var internalGlobalType = inheritedTypes.FirstOrDefault(IsInternalSenseNetHttpApplication);
            var externalGlobalType = inheritedTypes.FirstOrDefault(t => !IsInternalSenseNetHttpApplication(t));

            if (internalGlobalType == null)
                return externalGlobalType;

            if (externalGlobalType == null)
                return internalGlobalType;

            if (externalGlobalType.BaseType == internalGlobalType)
                return externalGlobalType;

            return internalGlobalType;
        }

        private static bool IsInternalSenseNetHttpApplication(Type type)
        {
            return type.GetCustomAttribute(typeof(InternalSenseNetHttpApplicationAttribute), false) != null;
        }

        internal static void ApplicationStartHandler(object sender, EventArgs e, HttpApplication application)
        {
            Instance.Application_Start(sender, e, application);
        }
        internal static void ApplicationEndHandler(object sender, EventArgs e, HttpApplication application)
        {
            Instance.Application_End(sender, e, application);
        }
        internal static void ApplicationErrorHandler(object sender, EventArgs e, HttpApplication application)
        {
            Instance.Application_Error(sender, e, application);
        }
        internal static void ApplicationBeginRequestHandler(object sender, EventArgs e, HttpApplication application)
        {
            Instance.Application_BeginRequest(sender, e, application);
        }
        internal static void ApplicationEndRequestHandler(object sender, EventArgs e, HttpApplication application)
        {
            Instance.Application_EndRequest(sender, e, application);
        }

        /*====================================================================================================================== Instance part */
        internal static string RunOnceGuid = "101C50EF-24FD-441A-A15B-BD33DE431665";
        private static readonly int[] dontCareErrorCodes = new int[] { 401, 403, 404 };

        protected virtual void Application_Start(object sender, EventArgs e, HttpApplication application)
        {
            using (var op = SnTrace.Repository.StartOperation("Application_Start. Process: {0}, AppDomain: {1}, ",
                                System.Diagnostics.Process.GetCurrentProcess().Id, AppDomain.CurrentDomain.Id))
            {
                var runOnceMarkerPath = application.Server.MapPath("/" + RunOnceGuid);
                var firstRun = File.Exists(runOnceMarkerPath);
                var startConfig = new RepositoryStartSettings { StartLuceneManager = !firstRun, IsWebContext = true };
                startConfig.ConfigureProvider(typeof(ElevatedModificationVisibilityRule), typeof(SnElevatedModificationVisibilityRule));

                Repository.Start(startConfig);

                StorageContext.L2Cache = new L2CacheImpl();

                RegisterRoutes(RouteTable.Routes, application);
                RepositoryPathProvider.Register();

                op.Successful = true;
            }
        }
        protected virtual void Application_End(object sender, EventArgs e, HttpApplication application)
        {
            using (var op = SnTrace.Repository.StartOperation("Application_End"))
            {
                Repository.Shutdown();
                SnLog.WriteInformation("Application_End", EventId.RepositoryLifecycle, properties: new Dictionary<string, object> {
                    { "ShutdownReason", HostingEnvironment.ShutdownReason  }
                });
                op.Successful = true;
            }
            SnTrace.Flush();
        }
        protected virtual void Application_Error(object sender, EventArgs e, HttpApplication application)
        {
            int? originalHttpCode = null;
            var ex = application.Server.GetLastError();

            var httpException = ex as HttpException;
            if (httpException != null)
                originalHttpCode = httpException.GetHttpCode();

            var unknownActionException = ex as UnknownActionException;
            if (unknownActionException != null)
            {
                SnTrace.Web.Write("UnknownActionException: " + unknownActionException.Message);
                originalHttpCode = 404;
            }

            // if httpcode is contained in the dontcare list (like 404), don't log the exception
            var skipLogException = originalHttpCode.HasValue && dontCareErrorCodes.Contains(originalHttpCode.Value);

            if (!skipLogException)
            {
                try
                {
                    SnLog.WriteException(ex);
                }
                catch
                {
                    // if logging failed, cannot do much at this point
                }
            }

            if (ex.InnerException?.StackTrace != null && 
                (ex.InnerException.StackTrace.IndexOf("System.Web.UI.PageParser.GetCompiledPageInstanceInternal", StringComparison.InvariantCulture) != -1))
                return;

            if (HttpContext.Current == null)
                return;

            HttpResponse response;
            try
            {
                response = HttpContext.Current.Response;
            }
            catch (Exception)
            {
                response = null;
            }

            response?.Headers.Remove("Content-Disposition");

            // HACK: HttpAction.cs (and possibly StaticFileHandler) throws 404 and 403 HttpExceptions. 
            // These are not exceptions to be displayed, but "fake" exceptions to handle 404 and 403 requests.
            // Therefore, here we set the statuscode and return, no further logic is executed.
            if (originalHttpCode.HasValue && (originalHttpCode == 404 || originalHttpCode == 403))
            {
                if (response != null)
                    response.StatusCode = originalHttpCode.Value;

                HttpContext.Current.ClearError();
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return;
            }


            var errorPageHtml = string.Empty;

            var exception = ex;
            if (exception.InnerException != null) exception = exception.InnerException;

            var statusCode = GetStatusCode(exception);

            if (response != null)
            {
                if (!HttpContext.Current.Request.Url.AbsoluteUri.StartsWith("http://localhost"))
                {
                    if (originalHttpCode.HasValue)
                        response.StatusCode = originalHttpCode.Value;

                    // If there is a specified status code in statusCodeString then set Response.StatusCode to it.
                    // Otherwise go on to global error page.
                    if (statusCode != null)
                    {
                        application.Response.StatusCode = statusCode.StatusCode;
                        application.Response.SubStatusCode = statusCode.SubStatusCode;
                        response.Clear();
                        HttpContext.Current.ClearError();
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                        return;
                    }

                    application.Response.TrySkipIisCustomErrors = true; // keeps our custom error page defined below instead of using the page of IIS - works in IIS7 only

                    if (application.Response.StatusCode == 200)
                        application.Response.StatusCode = 500;

                    Node globalErrorNode = null;
                    var site = Site.Current;

                    if (site != null)
                    {
                        var path = string.Concat("/Root/System/ErrorMessages/", site.Name, "/UserGlobal.html");
                        globalErrorNode = SystemAccount.Execute(() => Node.LoadNode(path));
                    }

                    if (globalErrorNode != null)
                    {
                        var globalBinary = globalErrorNode.GetBinary("Binary");
                        var stream = globalBinary.GetStream();
                        if (stream != null)
                        {
                            var str = new StreamReader(stream);
                            errorPageHtml = str.ReadToEnd();
                        }
                    }
                    else
                    {
                        errorPageHtml = GetDefaultUserErrorPageHtml(application.Server.MapPath("/"), true);
                    }
                }
                else
                {
                    // if the page is requested from localhost
                    errorPageHtml = GetDefaultLocalErrorPageHtml(application.Server.MapPath("/"), true);
                }
            }
            else
            {
                // TODO: SQL Error handling
            }

            errorPageHtml = InsertErrorMessagesIntoHtml(exception, errorPageHtml);

            application.Response.TrySkipIisCustomErrors = true;

            // If there is a specified status code in statusCodeString then set Response.StatusCode to it.
            // Otherwise go on to global error page.
            if (statusCode != null)
            {
                application.Response.StatusCode = statusCode.StatusCode;
                application.Response.SubStatusCode = statusCode.SubStatusCode;
                response?.Clear();

                HttpContext.Current.ClearError();
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            else
            {
                if (application.Response.StatusCode == 200)
                    application.Response.StatusCode = 500;
            }

            if (response != null)
            {
                response.Clear();
                response.Write(errorPageHtml);
            }

            HttpContext.Current.ClearError();
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        protected virtual void Application_BeginRequest(object sender, EventArgs e, HttpApplication application)
        {
            HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
        }
        protected virtual void Application_EndRequest(object sender, EventArgs e, HttpApplication application)
        {
            if (PortalContext.Current == null)
                return;

            if (PortalContext.Current.IsOfficeProtocolRequest || PortalContext.Current.IsWebdavRequest)
            {
                var redirectLocation = HttpContext.Current?.Response?.RedirectLocation?.ToLower() ?? string.Empty;

                // force 401, if formsauthentication module converted a 401 response to a 302 redirect to login page. we turn it back to 401
                var redirectingToLogin = HttpContext.Current.Response.StatusCode == 302 && redirectLocation.StartsWith(System.Web.Security.FormsAuthentication.LoginUrl);
                if (redirectingToLogin)
                {
                    HttpContext.Current.Response.RedirectLocation = null; // this is not any more a redirect
                    Portal.Dws.DwsHelper.CheckVisitor();
                }
            }
        }

        protected virtual void RegisterRoutes(RouteCollection routes, HttpApplication application)
        {
            routes.Add("SnODataRoute", new Route("odata.svc/{*path}", new ODataRouteHandler()));

            var resourceHandler = new ResourceHandler();
            routes.Add("SnResourceRoute", new Route(ResourceHandler.UrlPart + "/{*anything}", new ProxyingRouteHandler(ctx => resourceHandler)));
            routes.Add("SnBinaryRoute", new Route(BinaryHandlerBase.UrlPart + "/{contentId}/{propertyName}/{maxAge}/{width}/{height}", new RouteValueDictionary(new Dictionary<string, object>()
            {
                {"propertyName", "Binary"},
                {"maxAge", "0"},
                {"width", "0"},
                {"height", "0"},
            }), new ProxyingRouteHandler(ctx =>
            {
                var contentIdStr = ctx.RouteData.Values["contentId"] as string;
                int contentId = 0;
                if (!string.IsNullOrEmpty(contentIdStr) && !int.TryParse(contentIdStr, out contentId))
                    return null;

                var propertyName = ctx.RouteData.Values["propertyName"] as string ?? "Binary";

                var node = Node.LoadNode(contentId);

                if (node != null && propertyName != null && node.HasProperty(propertyName))
                {
                    var maxAgeStr = ctx.RouteData.Values["maxAge"] as string;
                    int maxAge;
                    int.TryParse(maxAgeStr, out maxAge);

                    var widthStr = ctx.RouteData.Values["width"] as string;
                    int width;
                    int.TryParse(widthStr, out width);

                    var heightStr = ctx.RouteData.Values["height"] as string;
                    int height;
                    int.TryParse(heightStr, out height);

                    var handler = new BinaryHandlerBase(node, propertyName, maxAge == 0 ? null : (TimeSpan?)TimeSpan.FromDays(maxAge), width == 0 ? null : (int?)width, height == 0 ? null : (int?)height);
                    return handler;
                }

                return null;
            })));
        }

        /*====================================================================================================================== Helpers */
        private static ErrorStatusCode GetStatusCode(Exception exception)
        {
            if (exception == null)
                return null;

            var tmpException = exception.GetType();
            var tmpExceptionFullName = tmpException.FullName;

            while (tmpExceptionFullName != null && tmpExceptionFullName != "System.Exception")
            {
                ErrorStatusCode statusCode;
                if (WebApplication.ExceptionStatusCodes.TryGetValue(tmpExceptionFullName, out statusCode))
                    return statusCode;

                // move to parent
                tmpException = tmpException.BaseType;

                if (tmpException != null)
                    tmpExceptionFullName = tmpException.FullName;
                else
                    return null;
            }

            return null;
        }
        private static string InsertErrorMessagesIntoHtml(Exception exception, string errorPageHtml)
        {
            errorPageHtml = errorPageHtml.Replace("{exceptionType}", exception.GetType().ToString());
            errorPageHtml = errorPageHtml.Replace("{exceptionMessage}", HttpUtility.HtmlEncode(exception.Message.Replace("\n", "<br />")));
            errorPageHtml = errorPageHtml.Replace("{exceptionToString}", HttpUtility.HtmlEncode(exception.ToString().Replace("\n", "<br />")));
            errorPageHtml = errorPageHtml.Replace("{exceptionSource}", exception.Source.ToString().Replace("\n", "<br />"));
            errorPageHtml = errorPageHtml.Replace("{exceptionStackTrace}", exception.StackTrace.ToString());

            var unknownActionExc = exception as UnknownActionException;
            if (unknownActionExc != null)
            {
                errorPageHtml = errorPageHtml.Replace("{exceptionActionName}", unknownActionExc.ActionName);
            }

            return errorPageHtml;
        }
        private static string GetDefaultUserErrorPageHtml(string serverPath, bool tryOnline)
        {
            return GetDefaultErrorPageHtml(serverPath, "UserGlobal.html", "UserErrorPage.html", tryOnline);
        }
        private static string GetDefaultLocalErrorPageHtml(string serverPath, bool tryOnline)
        {
            return GetDefaultErrorPageHtml(serverPath, "Global.html", "ErrorPage.html", tryOnline);
        }
        private static string GetDefaultErrorPageHtml(string serverMapPath, string page, string offlinePage, bool tryOnline)
        {
            Node global = null;

            if (tryOnline)
            {
                // Elevation: error message html should be 
                // independent from the current user.
                using (new SystemAccount())
                {
                    var site = Site.Current;

                    global = (site != null ? Node.LoadNode(string.Concat("/Root/System/ErrorMessages/", site.Name + "/", page)) : null) ??
                        Node.LoadNode(string.Concat("/Root/System/ErrorMessages/Default/", page));
                }
            }

            if (global != null)
            {
                var globalBinary = global.GetBinary("Binary");
                var stream = globalBinary.GetStream();
                if (stream != null)
                {
                    var str = new StreamReader(stream);
                    return str.ReadToEnd();
                }
            }
            else
            {
                try
                {
                    var path = string.Concat(serverMapPath, offlinePage);
                    using (var fs = File.Open(path, FileMode.Open, FileAccess.Read))
                    {
                        using (var sr = new StreamReader(fs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
                catch (Exception exc) // logged
                {
                    SnLog.WriteException(exc);
                }
            }

            return "<html><head><title>{exceptionType}</title></head><body style=\"font-family:Consolas, 'Courier New', Courier, monospace; background-color:#0033CC;color:#CCCCCC; font-weight:bold\"><br /><br /><br /><div style=\"text-align:center;background-color:#CCCCCC;color:#0033CC\">{exceptionType}</div><br /><br /><div style=\"font-size:large\">{exceptionMessage}<br /></div><br /><div style=\"font-size:x-small\">The source of the exception: {exceptionSource}</div><br /><div style=\"font-size:x-small\">Output of the Exception.ToString():<br />{exceptionToString}<br /><br /></div></body></html>";
        }
    }
}