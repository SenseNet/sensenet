using System.Web;
using System.Web.Routing;
using SenseNet.Services.Wopi;

namespace SenseNet.Portal.Routing
{
    class WopiRouteHandler : IRouteHandler
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new WopiHandler();
        }
    }
}
