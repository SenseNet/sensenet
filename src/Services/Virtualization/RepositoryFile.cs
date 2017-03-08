using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Hosting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using System.IO;
using System.Linq;
using SenseNet.ContentRepository.Storage.Security;
using System.Web;
using SenseNet.Diagnostics;

namespace SenseNet.Portal.Virtualization
{
    internal class RepositoryFile : VirtualFile
    {
        public static readonly string RESPONSECONTENTTYPEKEY = "SenseNetResponseContentType";
        private static readonly string[] COMPILE_ALLOWED_EXTENSIONS = new[] { ".ascx", ".aspx", ".cshtml", ".vbhtml", ".xslt", ".ashx" };

        private string _repositoryPath;
        private Node _node;

        public RepositoryFile(string virtualPath, string repositoryPath)
            : base(virtualPath)
        {
            if (virtualPath.EndsWith(PortalContext.InRepositoryPageSuffix))
            {
                // It's an aspx page, a Page or a Site
                PortalContext currentPortalContext = PortalContext.Current;
                if (currentPortalContext == null)
                {
                    throw new ApplicationException(string.Format("RepositoryFile cannot be instantiated. PortalContext.Current is null. virtualPath='{0}', repositoryPath='{1}'.", virtualPath, repositoryPath));
                }
                if (currentPortalContext.Page == null)
                {
                    throw new ApplicationException(string.Format("RepositoryFile cannot be instantiated. PortalContext.Current is available, but the PortalContext.Current.Page is null. virtualPath='{0}', repositoryPath='{1}'.", virtualPath, repositoryPath));
                }
                _repositoryPath = currentPortalContext.Page.Path;
            }
            else
                _repositoryPath = virtualPath;
        }

        public override Stream Open()
        {
            if (_node == null)
            {
                try
                {
                    var allowCompiledContent = AllowCompiledContent(_repositoryPath);

                    // http://localhost/TestDoc.docx?action=RestoreVersion&version=2.0A
                    // When there are 'action' and 'version' parameters in the requested URL the portal is trying to load the desired version of the node of the requested action. 
                    // This leads to an exception when the action doesn't have that version. 
                    // _repositoryPath will point to the node of action and ContextNode will be the document
                    // if paths are not equal then we will return the last version of the requested action.
                    // We also have to ignore the version request parameter in case of binary handler, because that
                    // ashx must not have multiple versions.
                    if (PortalContext.Current == null || PortalContext.Current.BinaryHandlerRequestedNodeHead != null || string.IsNullOrEmpty(PortalContext.Current.VersionRequest) || _repositoryPath != PortalContext.Current.ContextNodePath)
                    {
                        if (allowCompiledContent)
                        {
                            // elevated mode: pages, ascx files, etc.
                            using (new SystemAccount())
                            {
                                _node = Node.LoadNode(_repositoryPath, VersionNumber.LastFinalized);
                            }
                        }
                        else
                        {
                            _node = Node.LoadNode(_repositoryPath, VersionNumber.LastFinalized);
                        }
                    }
                    else
                    {
                        VersionNumber version;
                        if (VersionNumber.TryParse(PortalContext.Current.VersionRequest, out version))
                        {
                            Node node;

                            if (allowCompiledContent)
                            {
                                // elevated mode: pages, ascx files, etc.
                                using (new SystemAccount())
                                {
                                    node = Node.LoadNode(_repositoryPath, version);
                                }
                            }
                            else
                            {
                                node = Node.LoadNode(_repositoryPath, version);
                            }

                            if (node != null && node.SavingState == ContentSavingState.Finalized)
                                _node = node;
                        }
                    }

                    // we cannot serve the binary if the user has only See or Preview permissions for the content
                    if (_node != null && (_node.IsHeadOnly || _node.IsPreviewOnly) && HttpContext.Current != null)
                    {
                        AuthenticationHelper.ThrowForbidden(_node.Name);
                    }
                }
                catch (SenseNetSecurityException ex) // logged
                {
                    SnLog.WriteException(ex);

                    if (HttpContext.Current == null || (_repositoryPath != null && _repositoryPath.ToLower().EndsWith(".ascx")))
                        throw;

                    AuthenticationHelper.DenyAccess(HttpContext.Current.ApplicationInstance);
                }
            }

            if (_node == null)
                throw new ApplicationException(string.Format("{0} not found. RepositoryFile cannot be served.", _repositoryPath));

            string propertyName = string.Empty;
            if (PortalContext.Current != null)
                propertyName = PortalContext.Current.QueryStringNodePropertyName;

            if (string.IsNullOrEmpty(propertyName))
                propertyName = PortalContext.DefaultNodePropertyName;

            var propType = _node.PropertyTypes[propertyName];
            if (propType == null)
            {
                throw new ApplicationException("Property not found: " + propertyName);
            }

            var propertyDataType = propType.DataType;
            Stream stream;

            switch (propertyDataType)
            {
                case DataType.Binary:
                    string contentType;
                    BinaryFileName fileName;
                    stream = DocumentBinaryProvider.Current.GetStream(_node, propertyName, out contentType, out fileName);

                    if (stream == null)
                        throw new ApplicationException(string.Format("BinaryProperty.Value.GetStream() returned null. RepositoryPath={0}, OriginalUri={1}, AppDomainFriendlyName={2} ", this._repositoryPath, ((PortalContext.Current != null) ? PortalContext.Current.RequestedUri.ToString() : "PortalContext.Current is null"), AppDomain.CurrentDomain.FriendlyName));

                    // Set MIME type only if this is the main content (skip asp controls and pages, 
                    // page templates and other repository files opened during the request).
                    // We need this in case of special images, fonts, etc, and handle the variable
                    // at the end of the request (PortalContextModule.EndRequest method).
                    if (HttpContext.Current != null &&
                        PortalContext.Current != null &&
                        PortalContext.Current.RepositoryPath == _repositoryPath &&
                        (string.IsNullOrEmpty(PortalContext.Current.ActionName) || PortalContext.Current.ActionName == "Browse") &&
                        !string.IsNullOrEmpty(contentType) &&
                        contentType != "text/asp")
                    {
                        if (!HttpContext.Current.Items.Contains(RESPONSECONTENTTYPEKEY))
                            HttpContext.Current.Items.Add(RESPONSECONTENTTYPEKEY, contentType);

                        // set the value anyway as it may be useful in case of our custom file handler (SenseNetStaticFileHandler)
                        HttpContext.Current.Response.ContentType = contentType;

                        // add the necessary header for the css font-face rule
                        if (MimeTable.IsFontType(fileName.Extension))
                            HttpHeaderTools.SetAccessControlHeaders();
                    }

                    // set compressed encoding if necessary
                    if (HttpContext.Current != null && MimeTable.IsCompressedType(fileName.Extension))
                        HttpContext.Current.Response.Headers.Add("Content-Encoding", "gzip");

                    // let the client code log file downloads
                    var file = _node as ContentRepository.File;
                    if (file != null)
                        ContentRepository.File.Downloaded(file.Id);
                    break;
                case DataType.String:
                case DataType.Text:
                case DataType.Int:
                case DataType.DateTime:
                    stream = new MemoryStream(Encoding.UTF8.GetBytes(_node[propertyName].ToString()));
                    break;
                default:
                    throw new NotSupportedException(string.Format("The {0} property cannot be served because that's datatype is {1}.", propertyName, propertyDataType));
            }

            return stream;
        }

        private static bool AllowCompiledContent(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var head = NodeHead.Get(path);
            if (head == null)
                return false;

            // pages are basically aspx files, we need to allow their compilation
            if (head.GetNodeType().IsInstaceOfOrDerivedFrom("Page"))
                return true;

            // other content: ascx controls, razor views, etc.
            var extension = Path.GetExtension(path).ToLower();
            if (!string.IsNullOrEmpty(extension) && COMPILE_ALLOWED_EXTENSIONS.Contains(extension))
                return true;

            return false;
        }
    }
}