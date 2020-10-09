﻿using SenseNet.Portal.Virtualization;

namespace SenseNet.ApplicationModel
{
    public class UrlAction : PortalAction
    {
        public override string Uri
        {
            get
            {
                if (Content == null || this.Forbidden)
                    return string.Empty;

                var s = SerializeParameters(GetParameters());
                var uri = SiteRelativePath;

                if (Name.ToLower() != "browse")
                {
                    uri += string.Format("?{0}={1}", PortalContext.ActionParamName, this.Name);
                }

                if (!string.IsNullOrEmpty(s))
                {
                    uri = ContinueUri(uri);
                    uri += s.Substring(1);
                }

                if (this.IncludeBackUrl && !string.IsNullOrEmpty(this.BackUri))
                {
                    uri = ContinueUri(uri);
                    uri += string.Format("{0}={1}", PortalContext.BackUrlParamName, System.Uri.EscapeDataString(this.BackUri));
                }

                return uri;
            }
        }
    }
}
