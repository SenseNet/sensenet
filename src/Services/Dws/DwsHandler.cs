using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Web.Services.Description;
using System.Xml;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Fields;
using SenseNet.Portal.WebDAV;
using SenseNet.Search;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Configuration;

namespace SenseNet.Portal.Dws
{
    /// <summary>
    /// Summary description for dws
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class DwsHandler : System.Web.Services.WebService
    {
        private const string SUFFIXPATH = "/_vti_bin/dws.asmx";
        public static readonly string[] DWSLOGCATEGORY = new[] { "Dws" };
        public static readonly string[] ExcludedUserNames = new[] { SenseNet.ContentRepository.User.Visitor.Name, SenseNet.ContentRepository.User.Administrator.Name };


        // =========================================================================================== Properties
        private string _requestPath;
        public string RequestPath
        {
            get { return _requestPath ?? (_requestPath = GetRequestPath(SUFFIXPATH)); }
        }
        private Node _requestedNode;
        public Node RequestedNode
        {
            get
            {
                if (_requestedNode == null)
                    _requestedNode = Node.LoadNode(this.RequestPath);

                return _requestedNode;
            }
        }
        private string _requestUrl;
        public string RequestUrl
        {
            get { return _requestUrl ?? (_requestUrl = GetRequestUrl(SUFFIXPATH)); }
        }
        private Workspace _currentWorkspace;
        public Workspace CurrentWorkspace
        {
            get
            {
                if (_currentWorkspace == null)
                {
                    if (this.RequestedNode != null)
                        _currentWorkspace = Workspace.GetWorkspaceForNode(this.RequestedNode);
                }
                return _currentWorkspace;
            }
        }


        // =========================================================================================== Helper methods
        private static string CreateError(DwsErrorCodes errorCode, string message, string accessUrl = "")
        {
            XmlDocument xml = new XmlDocument();
            XmlElement error = xml.CreateElement("Error");
            error.SetAttribute("ID", ((int)errorCode).ToString());
            if (errorCode == DwsErrorCodes.NoAccess) error.SetAttribute("AccessUrl", accessUrl);
            error.InnerText = message;
            return error.OuterXml;
        }
        private static void WriteDebug(string functionName)
        {
            WriteDebug(functionName, string.Empty, string.Empty);
        }
        private static void WriteDebug(string functionName, string requestPath, string requestUrl)
        {
        }

        public static string GetRequestPath(string suffixPath)
        {
            var path = HttpUtility.UrlDecode(PortalContext.Current.RequestedUri.AbsolutePath);
            path = path.Substring(0, path.Length - suffixPath.Length);

            // site relative?
            if (!path.ToLower().StartsWith("/root") && PortalContext.Current.Site != null)
            {
                path = string.IsNullOrEmpty(path)
                           ? PortalContext.Current.Site.Path
                           : RepositoryPath.Combine(PortalContext.Current.Site.Path, path);
            }

            return path;
        }

        public static string GetRequestUrl(string suffixPath)
        {
            var requestUrl = PortalContext.Current.RequestedUri.AbsoluteUri;
            
            // https hack
            if (HttpContext.Current != null && HttpContext.Current.Request.ServerVariables["HTTP_HOST"].EndsWith(":443") && requestUrl.StartsWith("http://"))
                requestUrl = string.Concat("https://", requestUrl.Substring(7));

            requestUrl = requestUrl.Substring(0, requestUrl.Length - suffixPath.Length);

            return requestUrl;
        }

        // =========================================================================================== Public webservice methods
        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/CanCreateDwsUrl", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string CanCreateDwsUrl(string url)
        {
            // This operation determines whether an authenticated user has permission to create a Document Workspace at the specified URL.

            // The protocol server MUST respond with an HTTP 401 error if the user is not authorized to create the specified Document Workspace.
            // The server MUST respond with an Error element, with the identifier set to 2 and a string of "Failed" when the length of the site URL is greater than an implementation-dependent length.

            WriteDebug("CanCreateDwsUrl");
            if (DwsHelper.CheckVisitor())
                return null;

            return String.Concat("<Result>", url, "</Result>"); // Can create Document Workspace on the specified URL
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/CreateDws", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string CreateDws(string name, string users, string title, string documents)
        {
            // This operation creates a new Document Workspace.

            // The protocol server MUST reply with an HTTP 401 error if the authenticated user is not authorized to create the Document Workspace.
            // The protocol server MUST reply with an Error element in the response message if it fails to create the specified Document Workspace.
            // The protocol server MUST return a CreateDwsResponse response message with either a Result element or an Error element. The CreateDwsResponse response message MUST NOT be empty.

            WriteDebug("CreateDws");
            if (DwsHelper.CheckVisitor())
                return null;

            // parent exists?
            if (this.RequestedNode == null)
                return CreateError(DwsErrorCodes.FolderNotFound, string.Format("Parent container '{0}' does not exist!", this.RequestPath));

            // already exists?
            var nameBase = String.IsNullOrEmpty(name) ? title : name;
            var nodePath = RepositoryPath.Combine(this.RequestPath, nameBase);
            var nodeHead = NodeHead.Get(nodePath);
            if (nodeHead != null)
                return CreateError(DwsErrorCodes.AlreadyExists, string.Format("The requested Document Workspace '{0}' already exists!", nodePath));

            try
            {
                var newDocWS = ContentTemplate.CreateTemplated(this.RequestedNode, ContentTemplate.GetTemplate("DocumentWorkspace"), nameBase);
                newDocWS.Save();
            }
            catch (SenseNetSecurityException ex)
            {
                SnLog.WriteException(ex, categories: DWSLOGCATEGORY);

                HttpContext.Current.Response.StatusCode = 401;
                HttpContext.Current.Response.Flush();
                return null;
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, categories: DWSLOGCATEGORY);
                return CreateError(DwsErrorCodes.ServerFailure, ex.Message);
            }

            XmlDocument xml = new XmlDocument();
            XmlElement results = xml.CreateElement("Results");

            XmlElement url = xml.CreateElement("Url");
            XmlElement doclibUrl = xml.CreateElement("DoclibUrl");
            XmlElement parentWeb = xml.CreateElement("ParentWeb");
            XmlElement failedUsers = xml.CreateElement("FailedUsers");
            XmlElement addUsersUrl = xml.CreateElement("AddUsersUrl");
            XmlElement addUsersRole = xml.CreateElement("AddUsersRole");

            url.InnerText = string.Concat(DwsHelper.GetHostStr(), RepositoryPath.Combine(this.RequestPath, nameBase));
            doclibUrl.InnerText = "Document_Library";
            parentWeb.InnerText = "";

            results.AppendChild(url);
            results.AppendChild(doclibUrl);
            results.AppendChild(parentWeb);
            results.AppendChild(failedUsers);
            results.AppendChild(addUsersUrl);
            results.AppendChild(addUsersRole);

            xml.AppendChild(results);

            return xml.OuterXml; // The newly created Document Workspace
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/CreateFolder", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string CreateFolder(string url)
        {
            // This operation creates a folder in the document library of the current Document Workspace site.

            // If the parent folder for the specified URL does not exist, the protocol server MUST return an Error element with a FolderNotFound error code.
            // If the specified URL already exists, the protocol server MUST return an Error element with an AlreadyExists error code.
            // If the user does not have sufficient access permissions to create the folder, the protocol server MUST return an Error element with a NoAccess error code.
            // The protocol server MUST return an Error element with a Failed or a ServerFailure error code if an unspecified error prevents creating the specified folder.
            // If none of the prior conditions apply, the protocol server MUST create the folder specified in the CreateFolder element.

            WriteDebug("CreateFolder");

            if (DwsHelper.CheckVisitor())
                return null;

            if (string.IsNullOrEmpty(url))
                return "<Error>FolderNotFound</Error>";

            var sepIndex = url.IndexOf(RepositoryPath.PathSeparator);
            var folderName = sepIndex < 0 ? url : url.Substring(sepIndex + 1);

            if (string.IsNullOrEmpty(folderName))
                return "<Error>FolderNotFound</Error>";

            var parentPath = sepIndex < 1
                                 ? this.RequestPath
                                 : RepositoryPath.Combine(this.RequestPath, url.Substring(0, sepIndex));

            var folderPath = RepositoryPath.Combine(parentPath, folderName);
            if (Node.Exists(folderPath))
                return "<Error>AlreadyExists</Error>";

            var parent = Node.Load<Node>(parentPath);
            if (parent == null)
                return "<Error>FolderNotFound</Error>";

            var folder = new Folder(parent) { Name = folderName };

            try
            {
                folder.Save();
            }
            catch (SenseNetSecurityException ex)
            {
                SnLog.WriteException(ex);
                return "<Error>NoAccess</Error>";
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
                return "<Error>Failed</Error>";
            }

            return "<Result />"; // Folder has been successfully created.
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/DeleteDws", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string DeleteDws()
        {
            // This operation deletes a Document Workspace from the protocol server.

            // The protocol server MUST return an Error element with a NoAccess code if the authenticated user is not authorized to delete the Document Workspace. 
            // If the specified Document Workspace has subsites, the protocol server MUST return an Error element with the WebContainsSubwebs error code.
            // If the specified Document Workspace does not exist, the protocol server MUST return a Result element.
            // If the specified Document Workspace is the root site of the site collection, the protocol server MUST return an Error element with the ServerFailure error code.
            // If none of the prior conditions apply, the protocol server MUST delete the specified Document Workspace and return a Result element.

            WriteDebug("DeleteDws");
            if (DwsHelper.CheckVisitor())
                return null;

            return "<Result />"; // Either Document Workspace successfully deleted or it didn't exist.
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/DeleteFolder", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string DeleteFolder(string url)
        {
            // This operation deletes a folder from a document library on the site.

            // If the parent of the specified URL does not exist, the protocol server MUST return an Error element with a FolderNotFound error code.
            // If the specified URL does not exist, the protocol server MUST return a Result element as specified in DeleteFolderResponse.
            // If an unspecified error prevents the deletion of the specified folder, the protocol server MUST return an Error element with a Failed or a ServerFailure error code.
            // If none of the prior conditions apply, the protocol server MUST delete the folder specified in the CreateFolder element and return a Result element.

            WriteDebug("DeleteFolder");
            if (DwsHelper.CheckVisitor())
                return null;

            if (string.IsNullOrEmpty(url))
                return "<Error>FolderNotFound</Error>";

            var folderPath = RepositoryPath.Combine(this.RequestPath, url);
            var parentPath = RepositoryPath.GetParentPath(folderPath);

            if (!Node.Exists(parentPath))
                return "<Error>FolderNotFound</Error>";

            try
            {
                Node.ForceDelete(folderPath);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
                return "<Error>Failed</Error>";
            }

            return "<Result />"; // Either folder successfully deleted or it didn't even exist.
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/FindDwsDoc", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string FindDwsDoc(string id)
        {
            // This operation obtains a URL for a named document in a Document Workspace.

            // If the protocol server cannot locate a document with the specified identifier, it MUST return an Error element with a code of ItemNotFound.
            // If the protocol server encounters another error that prevents it from providing a response with the correct URL, it MUST return an Error element with a code of ServerFailure.
            // If none of the prior conditions apply, the protocol server SHOULD reply with a Result element containing an absolute URL for the specified document.

            WriteDebug("FindDwsDoc");
            if (DwsHelper.CheckVisitor())
                return null;

            return "<Result />"; // the absolute URL of the specified document
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/RemoveDwsUser", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string RemoveDwsUser(string id)
        {
            // This operation deletes a user from a Document Workspace.

            // If the protocol server successfully deletes the specified user from the workspace members list, the protocol server MUST return a string containing an empty Result element.
            // If an error of any type occurs during the processing, the protocol server MUST return an Error element with an error code of ServerFailure.

            WriteDebug("RemoveDwsUser");
            if (DwsHelper.CheckVisitor())
                return null;

            return "<Result />"; // DWS User successfully removed.
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/RenameDws", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string RenameDws(string title)
        {
            // This operation changes the title of a Document Workspace.

            // If a processing failure prevents the protocol server from recording the new title, the protocol server MUST return an Error element with a Failed code.
            // If the user submitting the request is not authorized to change the title, the protocol server MUST return an Error element with a NoAccess code. The Error element MUST NOT contain an AccessUrl attribute.
            // If another error occurs during processing, the protocol server MUST return an Error element with a code of ServerFailure.
            // If the protocol server successfully changes the title of the workspace, it MUST return an empty Result element.

            WriteDebug("RenameDws");
            if (DwsHelper.CheckVisitor())
                return null;

            return "<Result />"; // Document Workspace title successfully renamed.
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/GetDwsData", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string GetDwsData(bool minimal = true, string lastUpdate = "", string document = "", string id = "")
        {
            // This operation returns general information about the Document Workspace site, as well as its members, documents, links, and tasks.

            // If the Document Workspace at the specified URL does not have a valid parent site, the protocol server MUST return an Error element with the DocumentNotFound code.
            // If the protocol client provides a non-empty document parameter and the protocol server cannot locate the document, the protocol server SHOULD return an Error element with the ListNotFound code.
            // If the protocol server detects an access restriction during processing, it MUST return an Error with the NoAccess code and a URL for an authentication page.
            // If the protocol server detects some other problem during processing, it MUST return an Error with the ServerFailure code.
            // If no Error elements are returned as previously described, the protocol server MUST return a Result element with the appropriate information for the Document Workspace and document.

            WriteDebug("GetDwsData");
            if (DwsHelper.CheckVisitor())
                return null;

            XmlDocument doc = new XmlDocument();

            XmlElement Results = doc.CreateElement("Results");

            XmlElement Title = doc.CreateElement("Title");
            Title.InnerText = this.CurrentWorkspace.GetProperty<string>("DisplayName");
            Results.AppendChild(Title);

            XmlElement LastUpdate = doc.CreateElement("LastUpdate");
            Results.AppendChild(LastUpdate);

            // ******************************************************************************************************
            //                                             USER INFO
            // ******************************************************************************************************

            XmlElement User = doc.CreateElement("User");
            // GET USER INFO
            XmlElement UserID = doc.CreateElement("ID");
            UserID.InnerText = ContentRepository.User.Current.Id.ToString();
            User.AppendChild(UserID);

            XmlElement UserName = doc.CreateElement("Name");
            UserName.InnerText = ContentRepository.User.Current.FullName;
            User.AppendChild(UserName);

            XmlElement UserLoginName = doc.CreateElement("LoginName");
            UserLoginName.InnerText = ContentRepository.User.Current.Username;
            User.AppendChild(UserLoginName);

            XmlElement UserEmail = doc.CreateElement("Email");
            UserEmail.InnerText = ContentRepository.User.Current.Email;
            User.AppendChild(UserEmail);

            XmlElement UserIsDomainGroup = doc.CreateElement("IsDomainGroup");
            UserIsDomainGroup.InnerText = false.ToString(); // specifies whether this record belongs to a group or user
            User.AppendChild(UserIsDomainGroup);

            XmlElement IsSiteAdmin = doc.CreateElement("IsSiteAdmin");
            IsSiteAdmin.InnerText = ContentRepository.User.Current.IsInGroup(Group.Administrators).ToString();
            User.AppendChild(IsSiteAdmin);

            Results.AppendChild(User);

            var members = GetWorkspaceMembers(doc);
            // GET MEMBERS INFO
            Results.AppendChild(members);

            if (!minimal)
            {
                var docLibNode = DwsHelper.GetDocumentLibraryForNode(Node.LoadNode(DwsHelper.GetPathFromUrl(document)));

                // This element specifies the users assigned to the workspace.
                var assignees = GetWorkspaceTasksAssignees(doc);
                Results.AppendChild(assignees);

                // This element contains information about the Tasks list. The Name attribute MUST be set to "Tasks".
                XmlElement ListTasks = GetWorkspaceTasks(doc, RepositoryPath.Combine(this.CurrentWorkspace.Path, "Tasks"));
                Results.AppendChild(ListTasks);

                // This element contains information about the Documents list. The Name attribute MUST be set to "Documents".
                XmlElement ListDocuments = GetWorkspaceDocuments(doc, docLibNode.Path, docLibNode.Name);
                Results.AppendChild(ListDocuments);

                // This element contains information about the Links list. The Name attribute MUST be set to "Links".
                XmlElement ListLinks = GetWorkspaceLinks(doc, RepositoryPath.Combine(this.CurrentWorkspace.Path, "Links"));
                Results.AppendChild(ListLinks);
            }

            doc.AppendChild(Results);

            return doc.OuterXml;
        }

        /// <summary>
        /// This operation returns information about a Document Workspace site and the lists that it contains.
        /// </summary>
        /// <param name="document">This is a site-relative URL that specifies the list or document to describe in the response.</param>
        /// <param name="id">An uniquq ID that a protocol client can use instead of the document URL.</param>
        /// <param name="minimal">A Boolean value that specifies whether to return information.</param>
        /// <returns>This operation returns information about a Document Workspace site and the lists that it contains.</returns>
        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/GetDwsMetaData", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string GetDwsMetaData(bool minimal, string document = "", string id = "")
        {
            // ******************************************************************************************************
            //                                          ERROR HANDLING
            // ******************************************************************************************************

            WriteDebug("GetDwsMetaData", this.RequestPath, this.RequestUrl);
            if (DwsHelper.CheckVisitor())
                return null;

            // check whether the requested document exists or not
            if (this.RequestedNode == null) return CreateError(DwsErrorCodes.DocumentNotFound, "Requested document does not exist.");

            // check if the user has enough permissions
            if (!RequestedNode.Security.HasPermission(PermissionType.See, PermissionType.Open)) return CreateError(DwsErrorCodes.NoAccess, "You're not permitted to access the requested document.");

            // ******************************************************************************************************

            try
            {
                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("Results");
                if (minimal == false)
                {
                    // SubscribeUrl: This is the URI of a page that enables users to subscribe to changes in the document specified in the parameters (see MS-OSALER and MS-ALERTSS for more information).
                    XmlElement SubscribeUrl = doc.CreateElement("SubscribeUrl");
                    root.AppendChild(SubscribeUrl);
                }

                // MtgInstance: If document element specifies a meeting item, this element MUST represent a string that contains the meeting information. Otherwise, the element SHOULD be empty.
                XmlElement MtgInstance = doc.CreateElement("MtgInstance");
                root.AppendChild(MtgInstance);

                // SettingUrl: URI of a page that enables workspace settings to be modified.
                XmlElement SettingUrl = doc.CreateElement("SettingUrl");
                // var actionEdit = ActionFramework.GetAction("Edit", Content.Create(this.CurrentWorkspace), null);
                // actionEdit.IncludeBackUrl = false;
                // SettingUrl.InnerText = actionEdit.Forbidden ? String.Empty : String.Format("http://{0}{1}", PortalContext.Current.SiteUrl, actionEdit.Uri);
                root.AppendChild(SettingUrl);

                // PermsUrl: URI of a page that enables the workspace permissions settings to be modified.
                XmlElement PermsUrl = doc.CreateElement("PermsUrl");
                root.AppendChild(PermsUrl);

                // UserInfoUrl: URI of a page that enables the list of users to be modified.
                XmlElement UserInfoUrl = doc.CreateElement("UserInfoUrl");
                root.AppendChild(UserInfoUrl);

                // Roles: Specifies the roles that apply to the workspace.
                XmlElement Roles = doc.CreateElement("Roles");
                // GET ROLES
                root.AppendChild(Roles);

                if (minimal == false)
                {
                    // ******************************************************************************************************
                    //                                               SCHEMA
                    // ******************************************************************************************************

                    XmlElement SchemaTasks = doc.CreateElement("Schema");
                    SchemaTasks.SetAttribute("Name", "Tasks");
                    SchemaTasks.AppendChild(GetFieldInfo(doc, "ContentType", "Computed", false, null));
                    SchemaTasks.AppendChild(GetFieldInfo(doc, "Title", "Text", false, null));
                    SchemaTasks.AppendChild(GetFieldInfo(doc, "Predecessors", "Lookup", false, null));
                    SchemaTasks.AppendChild(GetFieldInfo(doc, "Priority", "Choice", false, GetChoiceFieldOptions(ContentType.GetByName("Task"), "Priority")));
                    SchemaTasks.AppendChild(GetFieldInfo(doc, "Status", "Choice", false, GetChoiceFieldOptions(ContentType.GetByName("Task"), "Status")));
                    SchemaTasks.AppendChild(GetFieldInfo(doc, "PercentComplete", "Number", false, null));
                    SchemaTasks.AppendChild(GetFieldInfo(doc, "AssignedTo", "User", false, null));
                    SchemaTasks.AppendChild(GetFieldInfo(doc, "Body", "Note", false, null));
                    SchemaTasks.AppendChild(GetFieldInfo(doc, "StartDate", "DateTime", false, null));
                    SchemaTasks.AppendChild(GetFieldInfo(doc, "DueDate", "DateTime", false, null));
                    root.AppendChild(SchemaTasks);

                    XmlElement SchemaDocuments = doc.CreateElement("Schema");
                    SchemaDocuments.SetAttribute("Name", "Documents");
                    SchemaDocuments.SetAttribute("Url", DwsHelper.GetDocumentLibraryForNode(Node.LoadNode(DwsHelper.GetPathFromUrl(document))).Name); // TODO loading the whole node is not required!
                    SchemaDocuments.AppendChild(GetFieldInfo(doc, "ContentType", "Computed", false, null));
                    SchemaDocuments.AppendChild(GetFieldInfo(doc, "FileLeafRef", "File", true, null));
                    SchemaDocuments.AppendChild(GetFieldInfo(doc, "Title", "Text", false, null));
                    root.AppendChild(SchemaDocuments);

                    XmlElement SchemaLinks = doc.CreateElement("Schema");
                    SchemaLinks.SetAttribute("Name", "Links");
                    SchemaLinks.AppendChild(GetFieldInfo(doc, "ContentType", "Computed", false, null));
                    SchemaLinks.AppendChild(GetFieldInfo(doc, "URL", "URL", true, null));
                    SchemaLinks.AppendChild(GetFieldInfo(doc, "Comments", "Note", false, null));
                    root.AppendChild(SchemaLinks);

                    // ******************************************************************************************************
                    //                                             LISTINFO
                    // ******************************************************************************************************                    

                    root.AppendChild(GetListInfo(doc, "Tasks", false));
                    root.AppendChild(GetListInfo(doc, "Documents", false));
                    root.AppendChild(GetListInfo(doc, "Links", false));
                }

                // ******************************************************************************************************

                // Permissions: This element contains the permissions of the authenticated user for this Document Workspace. This element contains elements that indicate permissible operations.
                XmlElement Permissions = doc.CreateElement("Permissions");

                XmlElement ManageSubwebs = doc.CreateElement("ManageSubwebs");
                Permissions.AppendChild(ManageSubwebs);
                XmlElement ManageWeb = doc.CreateElement("ManageWeb");
                Permissions.AppendChild(ManageWeb);
                XmlElement ManageRoles = doc.CreateElement("ManageRoles");
                Permissions.AppendChild(ManageRoles);
                XmlElement ManageLists = doc.CreateElement("ManageLists");
                Permissions.AppendChild(ManageLists);
                XmlElement InsertListItems = doc.CreateElement("InsertListItems");
                Permissions.AppendChild(InsertListItems);
                XmlElement EditListItems = doc.CreateElement("EditListItems");
                Permissions.AppendChild(EditListItems);
                XmlElement DeleteListItems = doc.CreateElement("DeleteListItems");
                Permissions.AppendChild(DeleteListItems);

                root.AppendChild(Permissions);

                // HasUniquePerm: Set to TRUE if, and only if, the workspace has custom role assignments; otherwise, 
                // if role assignments are inherited from the site in which the workspace is created, set to FALSE.
                XmlElement HasUniquePerm = doc.CreateElement("HasUniquePerm");
                HasUniquePerm.InnerText = true.ToString();
                root.AppendChild(HasUniquePerm);

                // WorkspaceType: This value MUST be "DWS", "MWS", or an empty string. "DWS" identifies the workspace as a Document Workspace; "MWS" identifies it as a Meeting Workspace. 
                // If the workspace is not one of those types, an empty string MUST be returned.
                XmlElement WorkspaceType = doc.CreateElement("WorkspaceType");
                WorkspaceType.InnerText = "DWS";
                root.AppendChild(WorkspaceType);

                // IsADMode: Set to TRUE if, and only if, the workspace is set to Active Directory mode, otherwise set to FALSE.
                XmlElement IsADMode = doc.CreateElement("IsADMode");
                IsADMode.InnerText = false.ToString();
                root.AppendChild(IsADMode);

                // DocUrl: This MUST be set to the value of document from the GetDwsMetaData request or be empty if the value of document is not specified in the GetDwsMetaData request.
                XmlElement DocUrl = doc.CreateElement("DocUrl");
                DocUrl.InnerText = document;
                root.AppendChild(DocUrl);

                // Minimal: This element contains the minimal flag from the GetDwsMetaData request. This value MUST match the value in the request.
                XmlElement Minimal = doc.CreateElement("Minimal");
                Minimal.InnerText = minimal.ToString();
                root.AppendChild(Minimal);

                // GetDwsData result
                XmlElement Results = doc.CreateElement("Results");
                XmlDocument dwsDataDoc = new XmlDocument();
                dwsDataDoc.LoadXml(GetDwsData(minimal, null, document));
                Results.InnerXml = dwsDataDoc.DocumentElement.InnerXml;

                root.AppendChild(Results);

                doc.AppendChild(root);

                return doc.InnerXml;
            }
            catch (Exception ex)
            {
                return CreateError(DwsErrorCodes.ServerFailure, ex.Message);
            }
        }

        private List<string> GetChoiceFieldOptions(ContentType contentType, string fieldName)
        {
            var cfs = contentType.GetFieldSettingByName(fieldName) as ChoiceFieldSetting;
            if (cfs != null)
            {
                return cfs.Options.Select(o => o.Text).ToList<string>();
            }
            return new List<string>();
        }

        private XmlElement GetListInfo(XmlDocument doc, string name, bool moderated)
        {
            XmlElement info = doc.CreateElement("ListInfo");
            info.SetAttribute("Name", name);

            XmlElement moderatedElement = doc.CreateElement("Moderated");
            moderatedElement.InnerText = moderated.ToString();
            info.AppendChild(moderatedElement);

            XmlElement listPermissions = doc.CreateElement("ListPermissions");
            XmlElement InsertListItems = doc.CreateElement("InsertListItems");
            XmlElement EditListItems = doc.CreateElement("EditListItems");
            XmlElement DeleteListItems = doc.CreateElement("DeleteListItems");
            XmlElement ManageLists = doc.CreateElement("ManageLists");

            listPermissions.AppendChild(InsertListItems);
            listPermissions.AppendChild(EditListItems);
            listPermissions.AppendChild(DeleteListItems);
            listPermissions.AppendChild(ManageLists);

            info.AppendChild(listPermissions);
            return info;
        }

        private XmlElement GetFieldInfo(XmlDocument doc, string name, string type, bool required, List<string> choicesList)
        {
            XmlElement field = doc.CreateElement("Field");
            field.SetAttribute("Name", name);
            field.SetAttribute("Type", type);
            field.SetAttribute("Requires", required.ToString());

            XmlElement choices = doc.CreateElement("Choices");
            if (choicesList != null && choicesList.Count > 0)
            {
                foreach (string ch in choicesList)
                {
                    XmlElement choiceElement = doc.CreateElement("Choice");
                    choiceElement.InnerText = ch;
                    choices.AppendChild(choiceElement);
                }
            }
            field.AppendChild(choices);
            return field;
        }

        private XmlElement GetWorkspaceTasksAssignees(XmlDocument doc)
        {
            var assignees = doc.CreateElement("Assignees");
            var assignedUsers = new List<User>();

            using (new SystemAccount())
            {
                try
                {
                    foreach (var member in this.RequestedNode.Security.GetEffectiveEntries().Select(entry => entry.IdentityId).Distinct()
                            .Select(Node.LoadNode).Where(node => node != null))
                    {
                        var user = member as User;
                        if (user != null)
                        {
                            if (ExcludedUserNames.Contains(user.Name))
                                continue;

                            if (!assignedUsers.Any(a => a.Id == user.Id))
                                assignedUsers.Add(user);

                            continue;
                        }

                        var group = member as Group;
                        if (group == null)
                            continue;

                        if (Identifiers.SpecialGroupPaths.Contains(group.Path))
                            continue;

                        var memberUsers = group.GetAllMemberUsers();
                        foreach (var memberUser in memberUsers)
                        {
                            var mu = memberUser as User;

                            if (mu != null && !ExcludedUserNames.Contains(mu.Name) && !assignedUsers.Any(a => a.Id == mu.Id))
                                assignedUsers.Add(mu);
                        }
                    }

                    foreach (var m in assignedUsers.Select(assignedUser => GetMemberElement(doc, assignedUser, false)).Where(m => m != null))
                    {
                        assignees.AppendChild(m);
                    }
                }
                catch (SenseNetSecurityException) // suppressed
                {
                    //TODO: return <Error ... DwsErrorCodes.NoAccess />                    
                }
                catch (Exception) // suppressed
                {
                    //TODO: return <Error DwsErrorCodes.ServerFailure />
                }
            }

            return assignees;
        }
        private XmlElement GetWorkspaceMembers(XmlDocument doc)
        {
            var members = doc.CreateElement("Members");
            
            try
            {
                foreach (var member in WebDavProvider.Current.GetWorkspaceMembers(CurrentWorkspace)
                                                       .Select(member => GetMemberElement(doc, (Node)member, true))
                                                       .Where(member => member != null))
                {
                    members.AppendChild(member);
                }
            }
            catch (SenseNetSecurityException secEx)
            {
                members.InnerXml = CreateError(DwsErrorCodes.NoAccess, secEx.Message);
            }
            catch (Exception ex)
            {
                members.InnerXml = CreateError(DwsErrorCodes.ServerFailure, ex.Message);
            }

            return members;
        }
        private XmlElement GetMemberElement(XmlDocument doc, Node memberNode, bool extended)
        {
            if (memberNode == null)
                return null;

            var member = doc.CreateElement("Member");

            var idElement = doc.CreateElement("ID");
            idElement.InnerText = memberNode.Path;
            member.AppendChild(idElement);

            var user = memberNode as User;

            var nameElement = doc.CreateElement("Name");
            var name = user != null && !string.IsNullOrEmpty(user.FullName) ? user.FullName : memberNode.DisplayName;
            nameElement.InnerText = name;
            member.AppendChild(nameElement);

            var loginNameElement = doc.CreateElement("LoginName");
            loginNameElement.InnerText = user == null ? string.Empty : user.Name;
            member.AppendChild(loginNameElement);

            if (extended)
            {
                var emailElement = doc.CreateElement("Email");
                emailElement.InnerText = user == null ? string.Empty : (user.Email ?? string.Empty);
                member.AppendChild(emailElement);

                var groupElement = doc.CreateElement("IsDomainGroup");
                groupElement.InnerText = user == null ? "True" : "False";
                member.AppendChild(groupElement);
            }

            return member;
        }

        /// <summary>
        /// Returns a list with all the available links in the current workspace.
        /// </summary>
        /// <param name="doc">Parent XmlDocument to insert the result into.</param>
        /// <param name="linksPath">Path to the Links folder.</param>
        /// <returns>Returns an XmlElement including all the Links found in the Workspace.</returns>
        private XmlElement GetWorkspaceLinks(XmlDocument doc, string linksPath)
        {
            var list = doc.CreateElement("List");
            list.SetAttribute("Name", "Links");

            try
            {
                NodeHead linksNodeHead = NodeHead.Get(linksPath);

                // If there is no Links folder we return a ListNotFound error
                if (linksNodeHead == null)
                {
                    //TODO: string resource!
                    list.InnerXml = CreateError(DwsErrorCodes.ListNotFound, "List not found: Links");
                }
                else
                {
                    XmlElement ID = doc.CreateElement("ID");
                    ID.InnerText = linksNodeHead.Id.ToString();
                    list.AppendChild(ID);

                    // Get all the Links
                    var result = ContentQuery.Query(ContentRepository.SafeQueries.InTreeAndTypeIs, null, linksPath, "Link");

                    foreach (Content cnt in result.Nodes.Select(n => Content.Create(n)))
                    {
                        XmlElement zrow = doc.CreateElement("z", "row", "#RowsetSchema");

                        zrow.SetAttribute("ows_Created", ((DateTime)cnt.Fields["CreationDate"].GetData()).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
                        zrow.SetAttribute("ows_FileRef", String.Concat(cnt.Id, ";#", cnt.Path.Substring(cnt.Path.IndexOf("Links/"))));

                        var author = cnt.ContentHandler.GetProperty<User>("CreatedBy");
                        if (author != null) zrow.SetAttribute("ows_Author", String.Concat(author.Id, ";#", author.FullName));
                        else zrow.SetAttribute("ows_Author", "");

                        var editor = cnt.ContentHandler.GetProperty<User>("ModifiedBy");
                        if (editor != null) zrow.SetAttribute("ows_Editor", String.Concat(editor.Id, ";#", editor.FullName));
                        else zrow.SetAttribute("ows_Editor", "");

                        zrow.SetAttribute("ows_Modified", ((DateTime)cnt.Fields["ModificationDate"].GetData()).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
                        zrow.SetAttribute("ows_ID", cnt.Id.ToString());

                        zrow.SetAttribute("ows_URL", String.Concat(cnt.Fields["Url"].GetData().ToString(), ", ", cnt.DisplayName));
                        zrow.SetAttribute("ows_Comments", cnt.Description);

                        zrow.SetAttribute("ows_owshiddenversion", "0");

                        list.AppendChild(zrow);
                    }
                }
            }
            catch (SenseNetSecurityException secEx)
            {
                list.InnerXml = CreateError(DwsErrorCodes.NoAccess, secEx.Message);
            }
            catch (Exception ex)
            {
                list.InnerXml = CreateError(DwsErrorCodes.ServerFailure, ex.Message);
            }

            return list;
        }

        /// <summary>
        /// Returns a list with all the available documents and folders in the current workspace.
        /// </summary>
        /// <param name="doc">Parent XmlDocument to insert the result into.</param>
        /// <param name="documentsPath">Path to the Documents_Library folder.</param>
        /// <returns>Returns an XmlElement including all the Documents found in the Workspace.</returns>
        private XmlElement GetWorkspaceDocuments(XmlDocument doc, string documentsPath, string docLibName)
        {
            var list = doc.CreateElement("List");
            list.SetAttribute("Name", "Documents");

            try
            {
                NodeHead documentsNodeHead = NodeHead.Get(documentsPath);

                // If there is no Documents folder we return a ListNotFound error
                if (documentsNodeHead == null)
                {
                    //TODO: string resource!
                    list.InnerXml = CreateError(DwsErrorCodes.ListNotFound, "List not found: Documents");
                }
                else
                {
                    XmlElement ID = doc.CreateElement("ID");
                    ID.InnerText = documentsNodeHead.Id.ToString();
                    list.AppendChild(ID);

                    // Get all the files and folders in the given path except parent folder
                    var docs = WebDavProvider.Current.GetDocumentsAndFolders(documentsPath);

                    foreach (Content cnt in docs.Select(n => Content.Create(n)))
                    {
                        XmlElement zrow = doc.CreateElement("z", "row", "#RowsetSchema");
                        zrow.SetAttribute("ows_FileRef", cnt.Path.Substring(cnt.Path.IndexOf(docLibName + "/")));

                        // The content is a file
                        if (cnt.ContentHandler is IFile)
                        {
                            zrow.SetAttribute("ows_Created", ((DateTime)cnt.Fields["CreationDate"].GetData()).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));

                            var author = cnt.ContentHandler.GetProperty<User>("CreatedBy");
                            if (author != null) zrow.SetAttribute("ows_Author", String.Concat(author.Id, ";#", author.FullName));
                            else zrow.SetAttribute("ows_Author", "");

                            var editor = cnt.ContentHandler.GetProperty<User>("ModifiedBy");
                            if (editor != null) zrow.SetAttribute("ows_Editor", String.Concat(editor.Id, ";#", editor.FullName));
                            else zrow.SetAttribute("ows_Editor", "");

                            zrow.SetAttribute("ows_Modified", ((DateTime)cnt.Fields["ModificationDate"].GetData()).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
                            zrow.SetAttribute("ows_ID", cnt.Id.ToString());

                            zrow.SetAttribute("ows_ProgID", "");
                            zrow.SetAttribute("ows_FSObjType", "0"); // 0 means it's a file
                        }
                        // The content is a folder
                        else if (cnt.ContentHandler is IFolder)
                        {
                            zrow.SetAttribute("ows_FSObjType", "1"); // 1 means it's a folder
                        }

                        list.AppendChild(zrow);
                    }
                }
            }
            catch (SenseNetSecurityException secEx)
            {
                list.InnerXml = CreateError(DwsErrorCodes.NoAccess, secEx.Message);
            }
            catch (Exception ex)
            {
                list.InnerXml = CreateError(DwsErrorCodes.ServerFailure, ex.Message);
            }

            return list;
        }

        /// <summary>
        /// Returns a list with all the available Tasks in the current workspace.
        /// </summary>
        /// <param name="doc">Parent XmlDocument to insert the result into.</param>
        /// <param name="tasksPath">Path to the Tasks folder.</param>
        /// <returns>Returns an XmlElement including all the Tasks found in the Workspace.</returns>
        private XmlElement GetWorkspaceTasks(XmlDocument doc, string tasksPath)
        {
            var list = doc.CreateElement("List");
            list.SetAttribute("Name", "Tasks");
            try
            {
                NodeHead tasksNodeHead = NodeHead.Get(tasksPath);

                // If there is no Tasks folder we return a ListNotFound error
                if (tasksNodeHead == null)
                {
                    //TODO: string resource!
                    list.InnerXml = CreateError(DwsErrorCodes.ListNotFound, "List not found: Tasks");
                }
                else
                {
                    XmlElement ID = doc.CreateElement("ID");
                    ID.InnerText = tasksNodeHead.Id.ToString();
                    list.AppendChild(ID);

                    var result = ContentQuery.Query(ContentRepository.SafeQueries.InTreeAndTypeIs, null, tasksPath, "Task");

                    foreach (Content cnt in result.Nodes.Select(n => Content.Create(n)))
                    {
                        XmlElement zrow = doc.CreateElement("z", "row", "#RowsetSchema");

                        //TODO: should convert it to the right format that Word accepts
                        zrow.SetAttribute("ows_Created", ((DateTime)cnt.Fields["CreationDate"].GetData()).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));

                        var author = cnt.ContentHandler.GetProperty<User>("CreatedBy");
                        if (author != null) zrow.SetAttribute("ows_Author", String.Concat(author.Id, ";#", author.FullName));
                        else zrow.SetAttribute("ows_Author", "");

                        var editor = cnt.ContentHandler.GetProperty<User>("ModifiedBy");
                        if (editor != null) zrow.SetAttribute("ows_Editor", String.Concat(editor.Id, ";#", editor.FullName));
                        else zrow.SetAttribute("ows_Editor", "");

                        zrow.SetAttribute("ows_Modified", ((DateTime)cnt.Fields["ModificationDate"].GetData()).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
                        zrow.SetAttribute("ows_ID", cnt.Id.ToString());
                        zrow.SetAttribute("ows_FileRef", String.Concat(cnt.Id, ";#", cnt.Path.Substring(cnt.Path.IndexOf("Tasks/"))));

                        var optStatus = (cnt.Fields["Status"].FieldSetting as ChoiceFieldSetting).Options.Where(opt => opt.Value == cnt.ContentHandler.GetPropertySafely("Status").ToString()).FirstOrDefault();
                        zrow.SetAttribute("ows_Status", optStatus != null ? optStatus.Text : String.Empty);

                        var optPriority = (cnt.Fields["Priority"].FieldSetting as ChoiceFieldSetting).Options.Where(opt => opt.Value == cnt.ContentHandler.GetPropertySafely("Priority").ToString()).FirstOrDefault();
                        zrow.SetAttribute("ows_Priority", optPriority != null ? optPriority.Text : String.Empty);

                        zrow.SetAttribute("ows_DueDate", ((DateTime)cnt.Fields["DueDate"].GetData()).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
                        zrow.SetAttribute("ows_Body", cnt.Description);
                        zrow.SetAttribute("ows_Title", cnt.DisplayName);

                        // there can be more users assigned to a task, we use only the first one here
                        var assignedTo = (cnt["AssignedTo"] as IEnumerable<Node>).FirstOrDefault() as User;
                        zrow.SetAttribute("ows_AssignedTo", assignedTo == null ? string.Empty : assignedTo.Path);

                        zrow.SetAttribute("ows_owshiddenversion", "0");

                        list.AppendChild(zrow);
                    }
                }
            }
            catch (SenseNetSecurityException secEx)
            {
                list.InnerXml = CreateError(DwsErrorCodes.NoAccess, secEx.Message);
            }
            catch (Exception ex)
            {
                list.InnerXml = CreateError(DwsErrorCodes.ServerFailure, ex.Message);
            }

            return list;
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/dws/UpdateDwsData", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/dws/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public string UpdateDwsData(string updates, string meetingInstance)
        {
            // ==============================================================
            //                    D E P R E C A T E D 
            // ==============================================================

            // This operation modifies the metadata of a Document Workspace. This method is deprecated and SHOULD NOT be called by the protocol client.

            // If there is a failure during processing of this operation, the protocol server MUST return a ServerFailure error.
            // If the protocol server does not return an Error element, it MUST return a Result element as specified in [MS-WSSCAML].

            WriteDebug("UpdateDwsData");
            if (DwsHelper.CheckVisitor())
                return null;

            var xdoc = new XmlDocument();
            xdoc.LoadXml(updates);

            // get method (Update, Delete, New)
            var methodNode = xdoc.GetElementsByTagName("Method")[0];
            var method = methodNode.Attributes["ID"].Value;

            // get list id
            var setListNode = methodNode["SetList"];
            var listId = Convert.ToInt32(setListNode.InnerText);
            var list = Node.LoadNode(listId);
            if (list == null)
                return CreateError(DwsErrorCodes.ItemNotFound, "ItemNotFound");
            var listType = list.NodeType.Name == "LinkList" ? ListType.Link : ListType.Task;

            // get node id
            var nodeId = 0;
            if (method == "Update" || method == "Delete")
            {
                var idNode = methodNode.SelectSingleNode("SetVar[@Name='ID']");
                nodeId = Convert.ToInt32(idNode.InnerText);
            }

            try
            {
                Node node = null;

                if (method == "Delete")
                {
                    // delete
                    node = Node.LoadNode(nodeId);
                    if (node == null)
                        return CreateError(DwsErrorCodes.ItemNotFound, "ItemNotFound");
                    node.Delete();
                }
                else
                {
                    // update or new
                    if (method == "Update")
                        node = Node.LoadNode(nodeId);

                    if (method == "New")
                        node = SenseNet.ContentRepository.Storage.Schema.NodeType.CreateInstance(listType.ToString(), list);

                    if (node == null)
                        return CreateError(DwsErrorCodes.ItemNotFound, "ItemNotFound");

                    var content = Content.Create(node);

                    // set fields
                    var fields = methodNode.SelectNodes("SetVar[contains(@Name, 'urn:schemas-microsoft-com:office:office')]");
                    foreach (XmlNode field in fields)
                    {
                        var fullFieldName = field.Attributes["Name"].Value;
                        var fieldName = fullFieldName.Substring(fullFieldName.IndexOf('#') + 1);
                        var value = field.InnerText;
                        switch (listType)
                        {
                            case ListType.Link:
                                if (fieldName == "URL")
                                {
                                    var commaIdx = value.IndexOf(',');
                                    var url = value.Substring(0, commaIdx);
                                    var name = value.Substring(commaIdx + 2);
                                    node["Url"] = url;
                                    node.DisplayName = name;
                                    if (node.Id == 0)
                                        node.Name = ContentNamingProvider.GetNameFromDisplayName(node.Name, name);
                                }
                                if (fieldName == "Comments")
                                {
                                    node["Description"] = value;
                                }
                                break;
                            case ListType.Task:
                                if (fieldName == "Title")
                                {
                                    node.DisplayName = value;
                                    if (node.Id == 0)
                                        node.Name = ContentNamingProvider.GetNameFromDisplayName(node.Name, value);
                                }
                                if (fieldName == "AssignedTo")
                                {
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        var user = Node.LoadNode(value);
                                        if (user != null)
                                            node.SetReference("AssignedTo", user);
                                    }
                                }
                                if (fieldName == "Status")
                                {
                                    var optStatus = (content.Fields["Status"].FieldSetting as ChoiceFieldSetting).Options.Where(opt => opt.Text == value).FirstOrDefault();
                                    node["Status"] = optStatus.Value;
                                }
                                if (fieldName == "Priority")
                                {
                                    var optPriority = (content.Fields["Priority"].FieldSetting as ChoiceFieldSetting).Options.Where(opt => opt.Text == value).FirstOrDefault();
                                    node["Priority"] = optPriority.Value;
                                }
                                if (fieldName == "DueDate")
                                {
                                    DateTime date;
                                    if (DateTime.TryParse(value, out date))
                                        node["DueDate"] = date;
                                }
                                if (fieldName == "Body")
                                {
                                    node["Description"] = value;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    node.Save();
                }
            }
            catch (SenseNetSecurityException secex)
            {
                return CreateError(DwsErrorCodes.NoAccess, secex.Message);
            }
            catch (InvalidContentActionException icex)
            {
                return CreateError(DwsErrorCodes.NoAccess, icex.Message);
            }
            catch (Exception ex)
            {
                return CreateError(DwsErrorCodes.ServerFailure, ex.Message);
            }

            return
                string.Format("<Results><Result ID=\"{0}\" Code=\"{1}\" List=\"{2}\" Version=\"{3}\"></Result></Results>", method, "0", listId.ToString(), "0");
        }

        public enum ListType
        {
            None = 0,
            Task = 1,
            Link = 2
        }

        public enum DwsErrorCodes
        {
            ServerFailure = 1,
            Failed,
            NoAccess,
            Conflict,
            ItemNotFound,
            MemberNotFound,
            ListNotFound,
            TooManyItems,
            DocumentNotFound,
            FolderNotFound,
            WebContainsSubwebs,
            ADMode,
            AlreadyExists,
            QuotaExceeded

        }
    }
}
