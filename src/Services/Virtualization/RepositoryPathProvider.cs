using System;
using System.IO;
using System.Reflection;
using System.Web.Hosting;
using System.Web;
using System.Collections;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using System.Configuration;
using SenseNet.Diagnostics;
using System.Linq;
using SenseNet.Configuration;
using File = SenseNet.ContentRepository.File;


namespace SenseNet.Portal.Virtualization
{

    public class RepositoryPathProvider : VirtualPathProvider
    {
        public static void Register()
        {
            if (Providers.RepositoryPathProviderEnabled)
                HostingEnvironment.RegisterVirtualPathProvider(new RepositoryPathProvider());
        }

        private RepositoryPathProvider()
        {
        }

        private static bool IsFileExistsInRepository(string path)
        {
            return NodeHead.Get(path) != null;
        }

        private bool IsFileExistsInAssembly(string virtualPath)
        {
            if(!virtualPath.Contains("!Assembly"))
                return false;
            var pathElements = virtualPath.Split('/');
            int idx = 0;
            int asmIdx = -1;
            while(idx<pathElements.Length && asmIdx<0)
            {
                if(pathElements[idx].ToLower().Equals("!assembly"))
                    asmIdx = idx;
                idx++;
            }
            if(asmIdx+2>pathElements.Length)
                return false;


            var asmFullName = pathElements[asmIdx + 1];
            var resourceFullName = pathElements[asmIdx + 2];

            var asm = Assembly.Load(asmFullName);
            if(asm==null)
                return false;

            return asm.GetManifestResourceNames().Contains(resourceFullName);
        }

        [Obsolete("Use SenseNet.Configuration.WebApplication.DiskFSSupportMode instead.")]
        public static DiskFSSupportMode DiskFSSupportMode => WebApplication.DiskFSSupportMode;

        public override bool FileExists(string virtualPath)
        {
            virtualPath = VirtualPathUtility.ToAbsolute(virtualPath);

            if (WebApplication.DiskFSSupportMode == DiskFSSupportMode.Prefer &&
                base.FileExists(virtualPath))
                return true;

            var currentPortalContext = PortalContext.Current;

            // Indicates that the VirtualFile is requested by a HttpRequest 
            // (a Page.LoadControl also can be a caller, or an aspx for its codebehind file...)
            bool isRequestedByHttpRequest;

            try
            {
                isRequestedByHttpRequest = (HttpContext.Current != null) &&
                                           (string.Compare(virtualPath, HttpContext.Current.Request.Url.LocalPath,
                                               StringComparison.InvariantCultureIgnoreCase) == 0);
            }
            catch (Exception)
            {
                isRequestedByHttpRequest = false;
            }

            if (isRequestedByHttpRequest && currentPortalContext.IsRequestedResourceExistInRepository)
                return true;
            if (IsFileExistsInRepository(virtualPath))
                return true;
            if (IsFileExistsInAssembly(virtualPath))
                return true;
            
            // Otherwise it may exist in the filesystem - call the base
            return base.FileExists(virtualPath);
        }

        

        public override VirtualFile GetFile(string virtualPath)
        {
            var currentPortalContext = PortalContext.Current;

            // office protocol: instruct microsoft office to open the document without further webdav requests when simply downloading the file
            // webdav requests would cause an authentication window to pop up when downloading a docx
            if (HttpContext.Current != null && HttpContext.Current.Response != null)
            {
                if (WebApplication.DownloadExtensions.Any(extension => virtualPath.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase)))
                {
                    // we need to do it this way to support a 'download' query parameter with or without a value
                    var queryParams = HttpContext.Current.Request.QueryString.GetValues(null);
                    var download = HttpContext.Current.Request.QueryString.AllKeys.Contains("download") || (queryParams != null && queryParams.Contains("download"));

                    if (download)
                    {
                        var fName = string.Empty;

                        if (currentPortalContext != null && currentPortalContext.IsRequestedResourceExistInRepository)
                        {
                            // look for a content
                            var node = Node.LoadNode(virtualPath);
                            if (node != null)
                                fName = DocumentBinaryProvider.Current.GetFileName(node);
                        }
                        else
                        {
                            // look for a file in the file system
                            fName = Path.GetFileName(virtualPath);
                        }

                        HttpHeaderTools.SetContentDispositionHeader(fName);
                    }
                }
            }

            if (WebApplication.DiskFSSupportMode == DiskFSSupportMode.Prefer && base.FileExists(virtualPath))
            {
                var result = base.GetFile(virtualPath);

                // let the client code log file downloads
                if (PortalContext.Current != null && PortalContext.Current.ContextNodePath != null && 
                    string.Compare(virtualPath, PortalContext.Current.ContextNodePath, StringComparison.Ordinal) == 0)
                    File.Downloaded(virtualPath);

                return result;
            }

            // Indicates that the VirtualFile is requested by a HttpRequest (a Page.LoadControl also can be a caller, or an aspx for its codebehind file...)
            var isRequestedByHttpRequest = 
                (HttpContext.Current != null) && (string.Compare(virtualPath, HttpContext.Current.Request.Url.LocalPath, StringComparison.InvariantCultureIgnoreCase) == 0);

            if (isRequestedByHttpRequest && currentPortalContext.IsRequestedResourceExistInRepository)
            {
                return new RepositoryFile(virtualPath, currentPortalContext.RepositoryPath);
            }
            else if (IsFileExistsInRepository(virtualPath))
            {
                return new RepositoryFile(virtualPath, virtualPath);
            }
            else if(IsFileExistsInAssembly(virtualPath))
            {
                return new EmbeddedFile(virtualPath);
            }
            else
            {
                // Otherwise it may exist in the filesystem - call the base
                return base.GetFile(virtualPath);
            }
        }

        public override System.Web.Caching.CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            return null;
        }

        
        // Return a hash value indicating a key to test this file and dependencies have not been modified
        public override string GetFileHash(string virtualPath, IEnumerable virtualPathDependencies)
        {
            if (WebApplication.DiskFSSupportMode == DiskFSSupportMode.Prefer &&
                base.FileExists(virtualPath))
            {
                var result = base.GetFileHash(virtualPath, virtualPathDependencies);
                return result;
            }

            string vp;
            if (virtualPath.EndsWith(PortalContext.InRepositoryPageSuffix))
                vp = virtualPath.Substring(0, virtualPath.Length - PortalContext.InRepositoryPageSuffix.Length);
            else
                vp = virtualPath;

            if (IsFileExistsInRepository(vp))
            {
                HashCodeCombiner hashCodeCombiner = new HashCodeCombiner();
                foreach (string virtualDependency in virtualPathDependencies)
                {
                    string vd;
                    if (virtualDependency.EndsWith(PortalContext.InRepositoryPageSuffix))
                        vd = virtualDependency.Substring(0, virtualDependency.Length - PortalContext.InRepositoryPageSuffix.Length);
                    else
                        vd = virtualDependency;

                    var nodeDesc = NodeHead.Get(vd);
                    if (nodeDesc != null)
                    {
                        hashCodeCombiner.AddLong(Convert.ToInt64(nodeDesc.ModificationDate.GetHashCode()));
                    }

                }
                return hashCodeCombiner.CombinedHashString;
            }
            else
            {
                return base.GetFileHash(vp, virtualPathDependencies);
            }
        }
    }
}