using SenseNet.Portal.OData;
using System.Web;
using System.Web.Routing;

namespace SenseNet.Portal.Routing
{
    public class ODataRouteHandler : IRouteHandler
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new ODataHandler();
        }
    }
}
