using System;
using System.Web;
using System.Web.Routing;

namespace SenseNet.Portal.Routing
{
    public sealed class ProxyingRouteHandler : IRouteHandler
    {
        private readonly Func<RequestContext, IHttpHandler> _getHttpHandler;

        public ProxyingRouteHandler(Func<RequestContext, IHttpHandler> getHttpHandlerAction)
        {
            _getHttpHandler = getHttpHandlerAction;
        }

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return _getHttpHandler?.Invoke(requestContext);
        }
    }
}
