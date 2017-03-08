using System.Web;

namespace SenseNet.ApplicationModel.AspectActions
{
    public abstract class AspectActionBase : ActionBase, IHttpHandler
    {
        public override string Uri { get; } = null;
        public bool IsReusable { get; } = true;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.StatusCode = 204;
            context.Response.Clear();
        }

        public override bool IsHtmlOperation { get; } = false;
        public override bool IsODataOperation { get; } = true;
        public override bool CausesStateChange { get; } = true;
    }
}
