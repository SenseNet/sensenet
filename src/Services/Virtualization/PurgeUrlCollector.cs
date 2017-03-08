using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Portal.Virtualization
{
    public class PurgeUrlCollector
    {
        public virtual List<string> GetUrls(Node context)
        {
            return context == null ? new List<string>() : GetUrls(context.Path);
        }

        public virtual List<string> GetUrls(string path)
        {
            var urlList = new List<string>();

            if (string.IsNullOrEmpty(path))
                return urlList;

            urlList.AddRange(CollectUrlsByContentPath(new List<string>(new[] { path })));
            urlList.Sort();

            return urlList;
        }

        protected virtual IEnumerable<string> CollectUrlsByContentPath(IEnumerable<string> pathList)
        {
            return CollectUrlsByContentPath(pathList, PortalContext.Sites.Keys);
        }

        protected virtual IEnumerable<string> CollectUrlsByContentPath(IEnumerable<string> pathList, IEnumerable<string> siteUrls)
        {
            return CollectAllUrls(pathList, siteUrls);
        }

        /// <summary>
        /// Collects all purge urls for one or more content paths. The result list will include all the site urls extended 
        /// with the full Root relative content paths and (if the content is under a site) the site relative urls of the paths.
        /// </summary>
        /// <param name="pathList">List of content paths ('/Root/MyFolder/MyContent')</param>
        /// <param name="siteUrls">List of site urls ('www.example.com')</param>
        /// <returns></returns>
        public static IEnumerable<string> CollectAllUrls(IEnumerable<string> pathList, IEnumerable<string> siteUrls)
        {
            var urlList = new List<string>();

            if (pathList == null)
                return urlList;

            foreach (var path in pathList.Where(path => !string.IsNullOrEmpty(path) && path.StartsWith(RepositoryPath.PathSeparator)))
            {
                if (siteUrls != null)
                    urlList.AddRange(siteUrls.Select(siteUrl => string.Concat(siteUrl, path)));

                // add site relative path if the content is under one of the sites
                var contextSite = PortalContext.Sites.Values.FirstOrDefault(s => path.StartsWith(s.Path));
                if (contextSite == null)
                    continue;

                var siteRelativePath = PortalContext.GetSiteRelativePath(path, contextSite);
                urlList.AddRange(contextSite.UrlList.Keys.Where(k => siteUrls == null || siteUrls.Contains(k)).Select(contextSiteUrl => string.Concat(contextSiteUrl, siteRelativePath)));
            }

            return urlList;
        }
    }
}
