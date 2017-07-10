using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Portal.Virtualization;

namespace SenseNet.ApplicationModel
{
    public class WebdavOpenAction : UrlAction
    {
        private static readonly string OFFICESCHEMEWORD = "ms-word:";
        private static readonly string OFFICESCHEMEEXCEL = "ms-excel:";
        private static readonly string OFFICESCHEMEPOWERPOINT = "ms-powerpoint:";
        private static readonly string OFFICESCHEMEPROJECT = "ms-project:";
        private static readonly string OFFICESCHEMEACCESS = "ms-access:";
        private static readonly string OFFICESCHEMEPUBLISHER = "ms-publisher:";

        private static readonly string OFFICESCHEMESTRING = "ofe|u|";

        private static readonly Dictionary<string, string> EXTENSION_SCHEMES = new Dictionary<string, string>
        {
            { ".doc", OFFICESCHEMEWORD },
            { ".dot", OFFICESCHEMEWORD },
            { ".docb", OFFICESCHEMEWORD },
            { ".docm", OFFICESCHEMEWORD },
            { ".docx", OFFICESCHEMEWORD },
            { ".dotm", OFFICESCHEMEWORD },
            { ".dotx", OFFICESCHEMEWORD },
            { ".xll", OFFICESCHEMEEXCEL },
            { ".xlm", OFFICESCHEMEEXCEL },
            { ".xls", OFFICESCHEMEEXCEL },
            { ".xlt", OFFICESCHEMEEXCEL },
            { ".xlw", OFFICESCHEMEEXCEL },
            { ".xlam", OFFICESCHEMEEXCEL },
            { ".xlsb", OFFICESCHEMEEXCEL },
            { ".xlsm", OFFICESCHEMEEXCEL },
            { ".xlsx", OFFICESCHEMEEXCEL },
            { ".xltm", OFFICESCHEMEEXCEL },
            { ".xltx", OFFICESCHEMEEXCEL },
            { ".pot", OFFICESCHEMEPOWERPOINT },
            { ".pps", OFFICESCHEMEPOWERPOINT },
            { ".ppt", OFFICESCHEMEPOWERPOINT },
            { ".potm", OFFICESCHEMEPOWERPOINT },
            { ".potx", OFFICESCHEMEPOWERPOINT },
            { ".ppam", OFFICESCHEMEPOWERPOINT },
            { ".ppsm", OFFICESCHEMEPOWERPOINT },
            { ".ppsx", OFFICESCHEMEPOWERPOINT },
            { ".pptm", OFFICESCHEMEPOWERPOINT },
            { ".pptx", OFFICESCHEMEPOWERPOINT },
            { ".sldm", OFFICESCHEMEPOWERPOINT },
            { ".sldx", OFFICESCHEMEPOWERPOINT },
            { ".mpp", OFFICESCHEMEPROJECT },
            { ".accdb", OFFICESCHEMEACCESS },
            { ".accde", OFFICESCHEMEACCESS },
            { ".accdr", OFFICESCHEMEACCESS },
            { ".accdt", OFFICESCHEMEACCESS },
            { ".pub", OFFICESCHEMEPUBLISHER }
        }; 

        public override string Uri
        {
            get
            {
                if (Content == null || this.Forbidden || !this.Visible)
                    return string.Empty;

                var extension = ContentNamingProvider.GetFileExtension(Content.Name).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension))
                    return string.Empty;

                string officeScheme;
                if (!EXTENSION_SCHEMES.TryGetValue(extension, out officeScheme))
                    return string.Empty;

                return officeScheme + OFFICESCHEMESTRING + PortalContext.Current.CurrentSiteAbsoluteUrl + SiteRelativePath;
            }
        }

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            // this action should be accessible only if we are on NTLM (Windows) authentication, or using HTTPS
            if (!string.Equals(PortalContext.Current.AuthenticationMode, "Windows", StringComparison.OrdinalIgnoreCase))
            {
                if (!PortalContext.Current.IsSecureConnection)
                    this.Forbidden = true;
            }

            if (!Webdav.WebdavEditExtensions.Any(extension => context.Name.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase)))
                this.Visible = false;
        }
    }
}
