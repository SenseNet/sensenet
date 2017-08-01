using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;

namespace SenseNet.Packaging.Steps
{
    public class SetUrl : Step
    {
        public string Site { get; set; } = "Default_Site";
        [DefaultProperty]
        public string Url { get; set; }

        public string AuthenticationType { get; set; } = "Forms";

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var siteName = context.ResolveVariable(Site) as string;
            if (string.IsNullOrEmpty(siteName))
                throw new InvalidParameterException("Site name cannot be empty.");

            var url = context.ResolveVariable(Url) as string;
            if (string.IsNullOrEmpty(url))
                throw new InvalidParameterException("Site url cannot be empty.");

            var site = Content.All.FirstOrDefault(c => c.InTree("/Root/Sites") && c.TypeIs("Site") && c.Name == siteName);
            if (site == null)
                throw new InvalidOperationException($"Site not found: {siteName}");

            var authType = context.ResolveVariable(AuthenticationType) as string;
            if (string.IsNullOrEmpty(authType))
                throw new InvalidOperationException("Authentication type cannot be empty.");

            Logger.LogMessage("Setting url {0} on site {1} with auth type {2}.", url, siteName, authType);

            var urlList = (IDictionary<string, string>) site["UrlList"];
            urlList[url] = authType;

            site["UrlList"] = urlList;
            site.SaveSameVersion();
        }
    }
}
