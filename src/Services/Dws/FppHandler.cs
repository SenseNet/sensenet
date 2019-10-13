using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using SenseNet.Configuration;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.WebDav;

namespace SenseNet.Portal.Dws
{
    public class FppHandler : IHttpHandler
    {
        // !!! NOTE !!!
        // FPP requires all line endings to be 0A, which is \n. Environment.NewLine, or breaking the lines DO NOT WORK!
        // Using different line endings than \n will result in messages not related to the sent packages will not be requested!
        // Use the GetFormattedString() function for valid formatting!

        // =========================================================================================================== Const strings 

        private const string DATEFORMAT = "dd MMM yyyy hh:mm:ss zzz";

        private const string ENTRYPOINTSTR = @"<!-- FrontPage Configuration Information
FPVersion=""14.00.0.000""
FPShtmlScriptUrl=""_vti_bin/shtml.dll/_vti_rpc""
FPAuthorScriptUrl=""_vti_bin/_vti_aut/author.dll""
FPAdminScriptUrl=""_vti_bin/_vti_adm/admin.dll""
TPScriptUrl=""_vti_bin/owssvr.dll""
-->";

        private const string VERSIONSTR = @"<html><head><title>vermeer RPC packet</title></head>
<body>
<p>method=server version:14.0.0.6009
<p>server version=
<ul>
<li>major ver=14
<li>minor ver=0
<li>phase ver=0
<li>ver incr=6029
</ul>
<p>source control=1
</body>
</html>";

        private const string URLTOWEBURLSTR = @"<html><head><title>vermeer RPC packet</title></head>
<body>
<p>method=url to web url:14.0.0.6009
<p>webUrl={0}
<p>fileUrl={1}
</body>
</html>";

        private const string OPENSERVICESTR = @"<html><head><title>vermeer RPC packet</title></head>
<body>
<p>method=open service:14.0.0.6009
<p>service=
<ul>
<li>service_name={0}
<li>meta_info=
<ul>
<li>vti_casesensitiveurls
<li>IX|0
<li>vti_longfilenames
<li>IX|1
<li>vti_welcomenames
<li>VX|
<li>vti_username
<li>SX|{1}
<li>vti_servertz
<li>SX|{2}
<li>vti_sourcecontrolsystem
<li>SR|lw
<li>vti_doclibwebviewenabled
<li>IX|1
<li>vti_sourcecontrolcookie
<li>SX|fp_internal
<li>vti_sourcecontrolproject
<li>SX|&#60;STS-based Locking&#62;
</ul>
</ul>
</body>
</html>";

        private const string GETDOCSMETAINFOSTR = @"<html><head><title>vermeer RPC packet</title></head>
<body>
<p>method=getDocsMetaInfo:14.0.0.6009
<p>document_list=
<ul>
{0}
</ul>
<p>urldirs=
<ul>
{1}
</ul>
</body>
</html>";

        private const string METAINFOSTR = @"<ul>
<li>vti_timecreated
<li>TR|{0}
<li>vti_timelastmodified
<li>TR|{1}
<li>vti_timelastwritten
<li>TR|{2}
<li>vti_filesize
<li>IR|{3}
<li>vti_sourcecontrolversion
<li>SR|{4}
<li>vti_modifiedby
<li>SR|{5}
<li>vti_author
<li>SR|{6}{7}
</ul>";

        private const string GETDOCSMETAINFODOCUMENTSTR = @"<ul>
<li>document_name={0}
<li>meta_info=
{1}
</ul>";

        private const string GETDOCSMETAINFODOCUMENTCHECKEDOUTSTR = @"
<li>vti_sourcecontrolmultiuserchkoutby
<li>VR|{0}
<li>vti_sourcecontrolcheckedoutby
<li>SR|{1}
<li>vti_sourcecontroltimecheckedout
<li>TR|{2}";

        // candeleteversion: when checkin dialog appears, it displays an option to delete current version and return to the previous version
        private const string GETDOCSMETAINFOURLDIRSSTR = @"<ul>
<li>url={0}
<li>meta_info=
<ul>
<li>vti_timecreated
<li>TR|{1}
<li>vti_timelastmodified
<li>TR|{2}
<li>vti_timelastwritten
<li>TR|{3}
<li>vti_hassubdirs
<li>BR|{4}
<li>vti_isbrowsable
<li>BR|true
<li>vti_isexecutable
<li>BR|false
<li>vti_isscriptable
<li>BR|false
<li>vti_listenableversioning
<li>BR|{5}
<li>vti_candeleteversion
<li>BR|false
<li>vti_listenableminorversions
<li>BR|{6}
<li>vti_listenablemoderation
<li>BR|{7}
<li>vti_listrequirecheckout
<li>BR|true
</ul>
</ul>";


        private const string PUTDOCUMENTSTR = @"<html><head><title>vermeer RPC packet</title></head>
<body>
<p>method=put document:14.0.0.6009
<p>message=successfully put document
<p>document=
{0}
</body>
</html>";

        private const string CHECKOUTDOCUMENTSTR = @"<html><head><title>vermeer RPC packet</title></head>
<body>
<p>method=checkout document:14.0.0.6009
<p>meta_info=
{0}
</body>
</html>";


        // =========================================================================================================== Helper methods 
        private static string GetFormattedString(string s)
        {
            return s.Replace("\r", "").Replace("\n\n","\n");
        }
        private static string Encode(string s)
        {
            var sb = new StringBuilder();

            foreach (var ch in s)
            {
                int ascii = ch;
                // 3 digit and 2 digit
                var digit3 = (ascii == 123 || ascii == 125 || (ascii >= 128 && ascii <= 255));
                var digit2 = (ascii < 7 || ascii == 11 || (ascii >= 14 && ascii <= 31) || ascii == 34 || (ascii >= 59 && ascii <= 62) || ascii == 92);
                if (digit3 || digit2)
                {
                    sb.Append("&#" + ascii.ToString() + ";");
                }
                else
                    sb.Append(ch);
            }
            return sb.ToString();
        }
        private static string GetDocMetaInfo(Node doc)
        {
            var timecreated = doc.CreationDate.ToString(DATEFORMAT); // 23 Mar 2011 11:31:58 +0000
            var timelastmodified = doc.ModificationDate.ToString(DATEFORMAT);
            var timelastwritten = timelastmodified;
            var filesize = (doc as File).Size;
            var sourcecontrolversion = string.Format("V{0}.{1}", doc.Version.Major, doc.Version.Minor); // "V1.1";
            var modifiedby = Encode((doc.ModifiedBy as IUser).Name);
            var author = Encode((doc.CreatedBy as IUser).Name);
            var checkedoutStr = string.Empty;
            // hack: if autocheckoutfiles is true we should not return checkout info, because then office would ask us to check in when closing word (unnecessary dialog, we will checkin word anyway)
            // this does not cause problems, office will know from webdav lock requests that the current user can edit the file, and for others the document will be opened in read-only mode
            if (doc.LockedBy != null && !Webdav.AutoCheckoutFiles)
            {
                var checkedoutby = Encode(doc.LockedBy.Name); // sn\user -> sn&#92;user
                var checkedoutby2 = Encode(doc.LockedBy.Name.Replace("\\", "\\\\")); // sn&#92;&#92;user
                var checkedoutdate = doc.LockDate.ToString(DATEFORMAT);
                checkedoutStr = string.Format(GETDOCSMETAINFODOCUMENTCHECKEDOUTSTR, checkedoutby2, checkedoutby, checkedoutdate);
                // checkoutToLocalStr = GETDOCSMETAINFODOCUMENTCHECKOUTTOLOCALSTR;
            }
            // The vti_canmaybeedit metakey contains a flag indicating whether the client user has sufficient permissions to edit items in the List folder. Individual documents MAY have different permission levels applied.
            // <li>vti_canmaybeedit
            // <li>BX|true

            var metainfo = string.Format(METAINFOSTR, timecreated, timelastmodified, timelastwritten, filesize, sourcecontrolversion, modifiedby, author, checkedoutStr);
            return metainfo;
        }
        private static string GetDocInfo(Node doc)
        {
            var doclib = DwsHelper.GetDocumentLibraryForNode(doc);
            var name = doc.Name;

            // name should be documentLibrary/x.docx
            if (doclib != null)
                name = doc.Path.Substring(doclib.ParentPath.Length + 1);

            var metainfo = GetDocMetaInfo(doc);
            var dinfo = string.Format(GETDOCSMETAINFODOCUMENTSTR, name, metainfo);
            return dinfo;
        }


        // =========================================================================================================== IHttpHandlers 
        public bool IsReusable
        {
            get { return false; }
        }
        public void ProcessRequest(HttpContext context)
        {
            context.Response.TrySkipIisCustomErrors = true;
            context.Response.Headers.Add("MicrosoftSharePointTeamServices", "14.0.0.5128");
            if (DwsHelper.CheckVisitor())
                return;

            var requestPath = PortalContext.Current.RequestedUri.AbsolutePath.ToLower();

            // mock workflow.asmx -> we don't implement, simply return HTTP 200
            if (requestPath.EndsWith("_vti_bin/workflow.asmx"))
            {
                HandleWorkflow(context);
                return;
            }

            // initial request to _vti_inf.html
            if (requestPath.EndsWith("_vti_inf.html"))
            {
                HandleVtiInf(context);
                return;
            }

            var method = context.Request.Form["method"];

            if (method == null)
            {
                // check inputstream's first line
                using (var reader = new System.IO.StreamReader(context.Request.InputStream))
                {
                    var firstLine = reader.ReadLine();
                    if (!string.IsNullOrEmpty(firstLine))
                    {
                        var decodedFirstLine = HttpUtility.UrlDecode(firstLine);

                        if (decodedFirstLine.StartsWith("method=put document"))
                        {
                            // seek to end of first line (also skip end-of-line char)
                            using (var resultStream = new OffsetStream(reader.BaseStream, firstLine.Length + 1))
                            {
                                resultStream.Position = 0;
                                HandleAuthorPutDocument(context, decodedFirstLine, resultStream);
                            }
                            return;
                        }
                    }
                }
                HandleError(context);
                return;
            }


            if (requestPath.EndsWith("_vti_rpc"))
            {
                if (method.StartsWith("server version"))
                {
                    HandleServerVersion(context);
                    return;
                }
                if (method.StartsWith("url to web url"))
                {
                    HandleUrlToWebUrl(context);
                    return;
                }
            }

            if (requestPath.EndsWith("_vti_aut/author.dll"))
            {
                // method=open+service%3a12%2e0%2e0%2e6415&service%5fname=%2f
                if (method.StartsWith("open service"))
                {
                    HandleAuthorOpenService(context);
                    return;
                }
                if (method.StartsWith("getDocsMetaInfo"))
                {
                    HandleAuthorGetDocsMetaInfo(context);
                    return;
                }
                if (method.StartsWith("get document"))
                {
                    HandleError(context);
                    return;
                }
                if (method.StartsWith("put document"))
                {
                    // this is handled with inputstream's first line above
                    HandleError(context);
                    return;
                }
                if (method.StartsWith("checkout document"))
                {
                    HandleAuthorCheckoutDocument(context);
                    return;
                }
            }

            context.Response.Flush();
            context.Response.End();
        }


        // =========================================================================================================== Method handlers 
        private void HandleWorkflow(HttpContext context)
        {
            context.Response.Flush();
            context.Response.End();
        }
        private void HandleVtiInf(HttpContext context)
        {
            var responseStr = GetFormattedString(ENTRYPOINTSTR);
            context.Response.AddHeader("Accept-Ranges", "bytes");
            context.Response.AddHeader("ETag", "\"2f7ad4cbe6fc61:33a\"");
            context.Response.Write(responseStr);
            context.Response.AddHeader("Content-Length", responseStr.Length.ToString());
            context.Response.Flush();
            context.Response.End();
        }
        private void HandleServerVersion(HttpContext context)
        {
            var responseStr = GetFormattedString(VERSIONSTR);
            context.Response.Charset = "";
            context.Response.ContentType = "application/x-vermeer-rpc";
            context.Response.AddHeader("Content-Length", responseStr.Length.ToString());
            context.Response.Write(responseStr);
            context.Response.Flush();
            context.Response.End();
        }
        private void HandleUrlToWebUrl(HttpContext context)
        {
            var path = context.Request.Form["url"];

            var weburl = path;
            var fileurl = string.Empty;

            // ie.: /Sites or /Sites/Default_Site/workspace/Document/myws/Document_Library/mydoc.docx
            if (path != "/")
            {
                path = DwsHelper.GetFullPath(path);
                weburl = path;

                var node = Node.LoadNode(path);
                if (node == null)
                    node = Node.LoadNode(RepositoryPath.GetParentPath(path));

                // searching starts from parentpath
                if (node != null)
                {
                    using (new SystemAccount())
                    {
                        var doclib = DwsHelper.GetDocumentLibraryForNode(node);
                        if (doclib != null)
                        {
                            // weburl should be doclibs parent (most of the time currentworkspace)
                            // fileurl should be doclib name and doclib relative path
                            // this will work for /Sites/MySite/Doclib/document.docx, for /Sites/Mysite/myworkspace/Doclib/document.docx and for /Root/Doclib/document.docx
                            weburl = doclib.ParentPath;
                            fileurl = (path.Length > doclib.ParentPath.Length) ? path.Substring(doclib.ParentPath.Length + 1) : string.Empty;
                        }
                        else
                        {
                            // weburl should be parent's parentpath
                            // fileurl should be parentname + name  -> parent will act as a mocked document library
                            // this will work for /Root/YourDocuments/document.docx
                            if (node.Parent != null)
                            {
                                weburl = node.Parent.ParentPath;
                                fileurl = RepositoryPath.Combine(node.Parent.Name, node.Name);
                            }
                        }
                    }
                }
            }

            var responseStr = GetFormattedString(string.Format(URLTOWEBURLSTR, weburl, fileurl));
            context.Response.Charset = "";
            context.Response.ContentType = "application/x-vermeer-rpc";
            context.Response.AddHeader("Content-Length", responseStr.Length.ToString());
            context.Response.Write(responseStr);
            context.Response.Flush();
            context.Response.End();
        }
        private void HandleAuthorOpenService(HttpContext context)
        {
            var servicename = context.Request.Form["service_name"];
            var username = Encode(User.Current.Name);
            var servertz = DateTime.UtcNow.ToString("zz") + TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.UtcNow).ToString("mm"); // "+0100";
            var responseStr = GetFormattedString(string.Format(OPENSERVICESTR, servicename, username, servertz));

            context.Response.Charset = "";
            context.Response.ContentType = "application/x-vermeer-rpc";
            context.Response.AddHeader("Content-Length", responseStr.Length.ToString());
            context.Response.Write(responseStr);
            context.Response.Flush();
            context.Response.End();
        }
        private void HandleAuthorGetDocsMetaInfo(HttpContext context)
        {
            // method=getDocsMetaInfo:14.0.0.6009&url_list=[http://snbppc070/Root/Sites/Default_Site/workspaces/Document/losangelesdocumentworkspace/Document_Library/Doc4.docx;http://snbppc070/Root/Sites/Default_Site/workspaces/Document/losangelesdocumentworkspace/Document_Library]&listHiddenDocs=false&listLinkInfo=false

            var urllist = context.Request.Form["url_list"];

            IEnumerable<Node> docList = null;
            IEnumerable<Node> dirList = null;
            if (urllist.Contains(';'))
            {
                var pathList = urllist.Substring(1, urllist.Length - 2).Split(';').ToList();
                var nodeList = pathList.Select(path => Node.LoadNode(DwsHelper.GetPathFromUrl(path)));

                docList = nodeList.Where(node => !(node is IFolder));
                dirList = nodeList.Where(node => node is IFolder);
            }
            else
            {
                var path = urllist.Substring(1, urllist.Length - 2);
                var node = Node.LoadNode(DwsHelper.GetPathFromUrl(path));

                if (node is IFolder)
                {
                    docList = Enumerable.Empty<Node>();
                    dirList = new[] { node };
                }
                else
                {
                    docList = new[] { node };

                    using (new SystemAccount())
                    {
                        var doclib = Node.GetAncestorOfNodeType(node, "DocumentLibrary") ?? node.Parent;
                        dirList = new[] { doclib };
                    }
                }
            }

            var docinfo = string.Empty;
            // create docinfo
            foreach (var doc in docList)
            {
                if (doc == null)
                    continue;

                var dinfo = GetDocInfo(doc);
                docinfo = string.Concat(docinfo, dinfo);
            }

            var dirinfo = String.Empty;
            // create dirinfo
            foreach (var dir in dirList)
            {
                if (dir == null)
                    continue;

                var name = dir.Name; // documentLibrary
                var timecreated = dir.CreationDate.ToString(DATEFORMAT); // 23 Mar 2011 11:31:58 +0000
                var timelastmodified = dir.ModificationDate.ToString(DATEFORMAT);
                var timelastwritten = timelastmodified;
                var hassubdirs = (dir as IFolder).ChildCount > 0 ? "true" : "false"; // true / false
                var gc = dir as GenericContent;
                var enableversioning = gc.InheritableVersioningMode != ContentRepository.Versioning.InheritableVersioningType.None ? "true" : "false";
                var enableminorversions = gc.InheritableVersioningMode == ContentRepository.Versioning.InheritableVersioningType.MajorAndMinor ? "true" : "false";
                var enablemoderation = "false";  // word does not seem to handle this - var enablemoderation = gc.HasApproving ? "true" : "false";

                var dinfo = string.Format(GETDOCSMETAINFOURLDIRSSTR, name, timecreated, timelastmodified, timelastwritten, hassubdirs, enableversioning, enableminorversions, enablemoderation);
                dirinfo = string.Concat(dirinfo, dinfo);
            }

            var responseStr = GetFormattedString(string.Format(GETDOCSMETAINFOSTR, docinfo, dirinfo));

            context.Response.Charset = "";
            context.Response.ContentType = "application/x-vermeer-rpc";
            context.Response.AddHeader("Content-Length", responseStr.Length.ToString());
            context.Response.Write(responseStr);
            context.Response.Flush();
            context.Response.End();
        }
        private void HandleAuthorPutDocument(HttpContext context, string firstLine, System.IO.Stream resultStream)
        {
            // firstline: method=put document:14.0.0.6009&service_name=/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/&document=[document_name=Document_Library/Aenean semper.doc;meta_info=[]]&put_option=edit&comment=&keep_checked_out=false
            var serviceNameIdx = firstLine.IndexOf("&service_name=");
            var workspacePathIdx = serviceNameIdx + "&service_name=".Length;
            var documentIdx = firstLine.IndexOf("&document=[document_name=");
            var documentPathIdx = documentIdx + "&document=[document_name=".Length;

            var workspacePath = firstLine.Substring(workspacePathIdx, documentIdx - workspacePathIdx);
            var documentPath = firstLine.Substring(documentPathIdx, firstLine.IndexOf(';') - documentPathIdx);

            var nodePath = RepositoryPath.Combine(workspacePath, documentPath);

            var file = Node.LoadNode(nodePath) as File;
            if (file != null)
            {
                var binaryData = new BinaryData();
                binaryData.SetStream(resultStream);
                file.SetBinary("Binary", binaryData);
                file.Save(SavingMode.KeepVersion);  // file is not checked in as of yet. office will try to check it in via lists.asmx
            }

            var docinfo = GetDocInfo(file);
            var responseStr = GetFormattedString(string.Format(PUTDOCUMENTSTR, docinfo));

            context.Response.Charset = "";
            context.Response.ContentType = "application/x-vermeer-rpc";
            context.Response.AddHeader("Content-Length", responseStr.Length.ToString());
            context.Response.Write(responseStr);
            context.Response.Flush();
            context.Response.End();
        }
        private void HandleAuthorCheckoutDocument(HttpContext context)
        {
            var workspacePath = context.Request.Form["service_name"];
            var documentPath = context.Request.Form["document_name"];
            var nodePath = RepositoryPath.Combine(workspacePath, documentPath);

            // only open it, do not check it out. file is not yet checked in. it will be checked out via lists.asmx
            var file = Node.LoadNode(nodePath) as File;

            var metainfo = GetDocMetaInfo(file);
            var responseStr = GetFormattedString(string.Format(CHECKOUTDOCUMENTSTR, metainfo));

            context.Response.Charset = "";
            context.Response.ContentType = "application/x-vermeer-rpc";
            context.Response.AddHeader("Content-Length", responseStr.Length.ToString());
            context.Response.Write(responseStr);
            context.Response.Flush();
            context.Response.End();
        }
        private void HandleError(HttpContext context)
        {
            context.Response.StatusCode = 501; // not implemented
            context.Response.Flush();
            context.Response.End();
        }
    }
}
