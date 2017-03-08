using System;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Portal.AppModel
{
    [ContentHandler]
    public class HttpStatusApplication : Application, IHttpHandler
    {
        public HttpStatusApplication(Node parent) : this(parent, null) { }
        public HttpStatusApplication(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected HttpStatusApplication(NodeToken nt) : base(nt) { }

        [RepositoryProperty("StatusCode")]
        public string StatusCode
        {
            get { return base.GetProperty<string>("StatusCode"); }
            set { base.SetProperty("StatusCode", value); }
        }

        [RepositoryProperty("RedirectUrl")]
        public string RedirectUrl
        {
            get { return base.GetProperty<string>("RedirectUrl"); }
            set { base.SetProperty("RedirectUrl", value); }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "StatusCode":
                    return this.StatusCode;
                case "RedirectUrl":
                    return this.RedirectUrl;
                default:
                    return base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "StatusCode":
                    this.StatusCode = (string)value;
                    break;
                case "RedirectUrl":
                    this.RedirectUrl = (string)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        #region IHttpHandler members

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            int code = Int32.Parse(StatusCode);
            HttpContext.Current.Response.StatusCode = code;

            if (code >= 300 && code < 400)
            {
                if (!string.IsNullOrEmpty(RedirectUrl))
                {
                    BackTargetType backTargetType;
                    HttpContext.Current.Response.RedirectLocation = Enum.TryParse(RedirectUrl, out backTargetType) ? 
                        PortalContext.GetBackTargetUrl(null, backTargetType) : 
                        RedirectUrl;
                }
                else
                    HttpContext.Current.Response.StatusCode = 500;
            }

            HttpContext.Current.Response.End();
        }

        #endregion

    }
}
