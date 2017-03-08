using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SenseNet.Services.Instrumentation;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Portal;
using SenseNet.Portal.Dws;
using SenseNet.Diagnostics;

namespace SenseNet.Services.WebDav
{
    [TraceSourceName("WebDAV")]
    public class WebDavHandler : System.Web.SessionState.IRequiresSessionState, IHttpHandler
    {
        private WebdavType _wdType = WebdavType.File;

        public string Protocol
        {
            get
            {
                return Context.Request.Url.Scheme.ToLower() == "http" && Context.Request.Url.Port != 443 && !Host.EndsWith(":443")
                    ? "http://"
                    : "https://";
            }
        }

        public string Host
        {
            get
            {
                return Context.Request.ServerVariables["HTTP_HOST"];
            }
        }

        public string Path { get; private set; }

        public WebdavType WebdavType
        {
            get { return _wdType; }
            private set { _wdType = value; }
        }

        public string GetGlobalPath(string path)
        {
            if (path.StartsWith("/Root"))
                return path;

            if (path.StartsWith("Root"))
                return string.Concat(RepositoryPath.PathSeparator, path);

            // bring the path to the correct format
            path = path.TrimEnd('/');

            // check if the path is site-relative
            if (PortalContext.Current != null)
            {
                var absolutePath = RepositoryPath.Combine(PortalContext.Current.Site.Path, path);
                if (Node.Exists(absolutePath))
                    return absolutePath;

                // Check parent only if the path is not a single folder - because in that case
                // the parent would be the site itself which obviously exists. That would result
                // in a false positive value down here.
                if (path.LastIndexOf(RepositoryPath.PathSeparator, StringComparison.Ordinal) > 0)
                {
                    // Check parent path. If it exists, this path is correct, only the node does not exist yet.
                    if (Node.Exists(RepositoryPath.GetParentPath(absolutePath)))
                        return absolutePath;
                }
            }

            // check if path is almost full, only without the '/Root' prefix (because of network drive mapping)
            var pathProbe = RepositoryPath.Combine("/Root", path);
            if (Node.Exists(pathProbe))
                return pathProbe;

            // Check parent path. If it exists, this path is correct, only the node does not exist yet.
            if (Node.Exists(RepositoryPath.GetParentPath(pathProbe)))
                return pathProbe;

            return path.StartsWith(RepositoryPath.PathSeparator) ? path : string.Concat(RepositoryPath.PathSeparator, path); // this will not happen
        }

        public string GlobalPath
        {
            get
            {
                return GetGlobalPath(Path);
            }
        }

        public HttpContext Context
        {
            get
            {
                return HttpContext.Current;
            }
        }

        public string RepositoryPathToUrl(string repositoryPath)
        {
            var repoPath = repositoryPath;
            var domain = Protocol + Host;

            if (string.IsNullOrEmpty(repoPath))
                return domain;

            var parentPath = RepositoryPath.GetParentPath(repositoryPath);
            var name = RepositoryPath.GetFileNameSafe(repositoryPath);

            // encode the filename
            return RepositoryPath.Combine(domain, parentPath, Uri.EscapeUriString(name));
        }

        #region IHttpHandler Members

        public void ProcessRequest(HttpContext context)
        {

            #region Debug

            string traceMessage = string.Concat("METHOD: ", context.Request.HttpMethod, " Path: '", context.Request.Path, "'", Environment.NewLine);
            traceMessage = string.Concat(traceMessage, "   Authenticated: ", HttpContext.Current.User.Identity.IsAuthenticated.ToString(), ", ", "UserName: ", HttpContext.Current.User.Identity.Name, Environment.NewLine);
            traceMessage = string.Concat(traceMessage, "   HEADERS: ", Environment.NewLine);

            foreach (var x in context.Request.Headers.AllKeys)
            {
                traceMessage = string.Concat(traceMessage, string.Format("      {0}={1}", x, context.Request.Headers[x]));
                traceMessage = string.Concat(traceMessage, Environment.NewLine);
            }

            System.Diagnostics.Debug.Write(traceMessage);

            #endregion

            context.Response.TrySkipIisCustomErrors = true;
            context.Response.Headers.Add("MicrosoftSharePointTeamServices", "14.0.0.5128");

            // check authentication
            if (DwsHelper.CheckVisitor())
                return;

            Path = context.Request.Path;

            if (Path.Contains(PortalContext.InRepositoryPageSuffix))
            {
                Path = "";
            }

            if (Path.ToLower().EndsWith(".content"))
            {
                Path = Path.Remove(Path.LastIndexOf('.'));
                WebdavType = WebdavType.Content;
            }
            else if (Path.ToLower().EndsWith(".aspx"))
            {
                WebdavType = WebdavType.Page;
            }
            else if (Path.ToLower().EndsWith("ctd.xml"))
            {
                WebdavType = WebdavType.ContentType;
            }

            // LATER: do not handle specific types in current version
            switch (WebdavType)
            {
                case WebdavType.File:
                case WebdavType.Folder:
                    break;
                default:
                    context.Response.StatusCode = 200;
                    context.Response.Flush();
                    context.Response.End();
                    return;
            }

            Path = Path.Replace("//", "/");
            Path = Path.TrimStart('/');

            // switch by method type - see RFC 2518
            try
            {
                switch (context.Request.HttpMethod)
                {
                    case "OPTIONS":
                        {
                            var o = new Options(this);
                            o.HandleMethod();
                            break;
                        }

                    case "PROPFIND":
                        {
                            var pf = new Propfind(this);
                            pf.HandleMethod();
                            break;
                        }
                    case "GET":
                        {
                            var g = new Get(this);
                            g.HandleMethod();
                            break;
                        }
                    case "HEAD":
                        {
                            var h = new Head(this);
                            h.HandleMethod();
                            break;
                        }
                    case "PUT":
                        {
                            var p = new Put(this);
                            p.HandleMethod();
                            break;
                        }
                    case "PROPPATCH":
                        {
                            var pp = new Proppatch(this);
                            pp.HandleMethod();
                            break;
                        }
                    case "MKCOL":
                        {
                            var md = new MkCol(this);
                            md.HandleMethod();
                            break;
                        }
                    case "MOVE":
                        {
                            var m = new Move(this);
                            m.HandleMethod();
                            break;
                        }
                    case "DELETE":
                        {
                            var d = new Delete(this);
                            d.HandleMethod();
                            break;
                        }
                    case "TRACE":
                        {
                            var t = new Trace(this);
                            t.HandleMethod();
                            break;
                        }
                    case "LOCK":
                        {
                            var l = new Lock(this);
                            l.HandleMethod();
                            break;
                        }
                    case "UNLOCK":
                        {
                            var ul = new UnLock(this);
                            ul.HandleMethod();
                            break;
                        }
                    case "POST":
                        {
                            Context.Response.StatusCode = 404;
                            context.Response.Flush();
                            break;
                        }
                    default:
                        {
                            context.Response.StatusCode = 501; // not implemented
                            break;
                        }
                }

                context.Response.End();
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, null, EventId.Services,
                    properties: new Dictionary<string, object> { { "Path", Path }, { "Global path", GlobalPath } });

                try
                {
                    Context.Response.StatusCode = 404;
                    context.Response.Flush();
                }
                catch
                {
                    // last catch, can be suppressed
                }
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        #endregion

        internal static string GetAttachmentName(Node node, string propertyName)
        {
            var binaryName = RepositoryPath.GetFileName(System.IO.Path.GetFileName(((BinaryData)node[propertyName]).FileName));

            if (string.IsNullOrEmpty(binaryName) || binaryName.StartsWith("."))
                binaryName = node.Name + binaryName;

            return binaryName;
        }

        internal static Node GetNodeByBinaryName(Node parentNode, string fileName, out string binaryPropertyName)
        {
            Node foundNode = null;
            binaryPropertyName = string.Empty;

            if (parentNode == null || !(parentNode is IFolder))
                return null;

            // binary check: if the requested file is the binary of a content
            // with a different name (e.g PersonalizationSettings)
            foreach (var child in ((IFolder)parentNode).Children.Where(c => c.SavingState == ContentSavingState.Finalized))
            {
                foreach (var propType in child.PropertyTypes.Where(pt => pt.DataType == DataType.Binary))
                {
                    //TODO: BETTER FILENAME CHECK!
                    var binaryName = GetAttachmentName(child, propType.Name);
                    if (string.Compare(binaryName, fileName, StringComparison.Ordinal) != 0)
                        continue;

                    binaryPropertyName = propType.Name;
                    foundNode = child;
                    break;
                }

                if (foundNode != null)
                    break;
            }

            return foundNode;
        }
    }
}
