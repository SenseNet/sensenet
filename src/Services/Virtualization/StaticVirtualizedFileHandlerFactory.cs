using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SenseNet.Portal.Virtualization
{
    internal class StaticVirtualizedFileHandlerFactory : IHttpHandlerFactory
    {
        #region IHttpHandlerFactory Members

        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {

            bool isVisitor = !context.User.Identity.IsAuthenticated;
            bool isHttpGet = requestType.Equals("GET", StringComparison.CurrentCultureIgnoreCase);
            bool isDefaultContentPropertyRequested = context.Request.QueryString["NodeProperty"] == null;

            IHttpHandler handlerInstance;

            if (isVisitor && isHttpGet && isDefaultContentPropertyRequested)
            {
                handlerInstance = new StaticVirtualizedFileHandler();
            }
            else
            {
                Type staticFileHandlerType = Type.GetType("System.Web.StaticFileHandler, System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                handlerInstance = Activator.CreateInstance(staticFileHandlerType, true) as IHttpHandler;
            }

            return handlerInstance;
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
        }

        #endregion
    }
}
