using SenseNet.ApplicationModel;

namespace Compatibility.SenseNet.ApplicationModel
{
    public abstract class PortalAction : ActionBase
    {
        public virtual string IconTag { get; set; }

        //UNDONE:ODATA: ? PortalAction.SiteRelativePath is not supported
        //public virtual string SiteRelativePath => PortalContext.GetSiteRelativePath(Content.Path);

        protected static string ContinueUri(string uri)
        {
            if (uri.Contains("?"))
                uri += "&";
            else
                uri += "?";

            return uri;
        }
    }
}
