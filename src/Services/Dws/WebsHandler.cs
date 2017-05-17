using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Services.Protocols;
using System.Web.Services;
using System.Web.Services.Description;
using SenseNet.ContentRepository.Storage;
using System.Web;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Portal.Dws
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebsHandler : System.Web.Services.WebService
    {
        // =========================================================================================== Public webservice methods
        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/WebUrlFromPageUrl", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string WebUrlFromPageUrl(string pageUrl)
        {
            // ie: pageurl:  http://localhost/Root/Sites/Default_Site/workspaces/Document/mydocumentws/Document_Library/my.doc
            // result: http://localhost/Root/Sites/Default_Site/workspaces/Document/mydocumentws

            // get root-relative url
            var hostIdx = pageUrl.IndexOf(HttpContext.Current.Request.Url.Host, StringComparison.InvariantCultureIgnoreCase);
            var prefixLength = hostIdx + HttpContext.Current.Request.Url.Host.Length;
            var path = HttpUtility.UrlDecode(pageUrl.Substring(prefixLength));

            var rootIdx = pageUrl.IndexOf("/root", StringComparison.InvariantCultureIgnoreCase);
            if (rootIdx == -1)
            {
                // host selected in open dialog of office and navigated to doclibrary folder -> /Root is missing
                // ie: path:  /Sites/Default_Site/workspaces/Document/mydocumentws/Document_Library/my.doc
                var addOnlyRootPrefix = path.StartsWith("/sites", StringComparison.InvariantCultureIgnoreCase) ||
                                        PortalContext.Current.Site == null;

                path = RepositoryPath.Combine(addOnlyRootPrefix ? "/Root" : PortalContext.Current.Site.Path, path);
            }

            // searching starts from parentpath
            path = RepositoryPath.GetParentPath(path);
            var node = Node.LoadNode(path);
            var url = string.Empty;
            if (node != null)
            {
                var ws = Workspace.GetWorkspaceForNode(node);
                if (ws != null)
                {
                    url = string.Concat(pageUrl.Substring(0, prefixLength), ws.Path);
                }
                else
                {
                    // workspace not found, it should be parent doclibrary's parent
                    var doclib = DwsHelper.GetDocumentLibraryForNode(node);
                    if (doclib != null)
                    {
                        url = string.Concat(pageUrl.Substring(0, prefixLength), doclib.ParentPath);
                    }
                    else
                    {
                        // standalone document, return parentpath
                        url = string.Concat(pageUrl.Substring(0, prefixLength), node.ParentPath);
                    }
                }
            }

            return String.Concat(url);
        }

    }
}
