using SenseNet.Portal.Virtualization;

namespace SenseNet.ApplicationModel
{
    public abstract class PortalAction : ActionBase
    {
        public virtual string IconTag { get; set; }

        public virtual string SiteRelativePath => PortalContext.GetSiteRelativePath(Content.Path);

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
