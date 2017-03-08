using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Web.Services.Description;
using System.Xml;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Versioning;
using System.Xml.Serialization;
using System.Web;
using SenseNet.ContentRepository.Storage.Security;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.Portal.Dws
{
    public class VersionErrorCodes
    {
        public static readonly XmlQualifiedName CLIENT_ERROR = SoapException.ClientFaultCode;
        public static readonly XmlQualifiedName SERVER_ERROR = SoapException.ServerFaultCode;

        // Following error codes are defined in document MS-VERSS
        public static readonly string FILE_NOT_FOUND = "0x81070906";
        public static readonly string INVALID_FILE_NAME = "0x81020030";
        public static readonly string VERSION_NOT_FOUND = "0x80131600";
        public static readonly string CANNOT_RESTORE_CURRENT_VERSION = "0x80131600";
        public static readonly string GETVERSIONS_FILE_NOT_FOUND = "0x80070002";
        public static readonly string CHECK_OUT_BEFORE = "0x8007009e";
        public static readonly string SERVER_REQUIRES_CHECK_OUT_FIRST = "0x81070975";
    }

    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class VersionsHandler : WebService
    {
        private const string SUFFIXPATH = "/_vti_bin/versions.asmx";

        private string _requestPath;
        public string RequestPath
        {
            get { return _requestPath ?? (_requestPath = DwsHandler.GetRequestPath(SUFFIXPATH)); }
        }

        private string _requestUrl;
        public string RequestUrl
        {
            get { return _requestUrl ?? (_requestUrl = DwsHandler.GetRequestUrl(SUFFIXPATH)); }
        }


        /// <summary>
        /// Restores the desired document to the given version.
        /// </summary>
        /// <param name="fileName">The document to be modified.</param>
        /// <param name="fileVersion">The version to be restored.</param>
        /// <returns></returns>
        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/RestoreVersion", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        [XmlInclude(typeof(VersionOperationResult))]
        public object RestoreVersion(string fileName, string fileVersion)
        {

            string[] vrsSpl = fileVersion.Split('.');
            int major = Convert.ToInt32(vrsSpl[0]);
            int minor = Convert.ToInt32(vrsSpl[1]);

            var path = RepositoryPath.Combine(this.RequestPath, fileName);
            var docNodeHead = NodeHead.Get(path);
            if (docNodeHead == null)
            {
                // maybe it is a full path or url (Office 2013) instead of a name
                path = DwsHelper.GetPathFromUrl(fileName);
                docNodeHead = NodeHead.Get(path);
            }

            if (docNodeHead != null)
            {
                var content = Content.Load(docNodeHead.Id, new VersionNumber(major, minor));

                if (content != null)
                {
                    try
                    {
                        content.Save();
                        if (content.Approvable) content.Approve();
                        return GetVersions(fileName);
                    }
                    catch (SenseNetSecurityException)
                    {
                        MakeError(VersionErrorCodes.SERVER_ERROR, "You do not have enough permissions to restore previous versions of the current document.");
                    }
                    catch (InvalidContentActionException icaex)
                    {
                        MakeError(VersionErrorCodes.SERVER_ERROR, icaex.Message);
                    }
                }
            }

            MakeError(VersionErrorCodes.SERVER_ERROR, "The system cannot find the file specified. (Exception from HRESULT: 0x80070002)");
            return null;
        }

        private void MakeError(XmlQualifiedName errorType, string errorString, string errorCode = null)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode node = doc.CreateNode(XmlNodeType.Element, SoapException.DetailElementName.Name, SoapException.DetailElementName.Namespace);

            XmlNode errorStringNode = doc.CreateNode(XmlNodeType.Element, "errorstring", "http://schemas.microsoft.com/sharepoint/soap/");
            errorStringNode.InnerText = errorString;
            node.AppendChild(errorStringNode);

            if (!String.IsNullOrEmpty(errorCode))
            {
                XmlNode errorCodeNode = doc.CreateNode(XmlNodeType.Element, "errorcode", "http://schemas.microsoft.com/sharepoint/soap/");
                errorCodeNode.InnerText = errorCode;
                node.AppendChild(errorCodeNode);
            }

            throw new SoapException(errorString, errorType, Context.Request.Url.AbsoluteUri, node);
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/DeleteVersion", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public object DeleteVersion(string fileName, string fileVersion)
        {
            MakeError(VersionErrorCodes.SERVER_ERROR, "The requested operation is currently not supported.");
            return null;
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/DeleteAllVersions", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        public object DeleteAllVersions(string fileName)
        {
            MakeError(VersionErrorCodes.SERVER_ERROR, "The requested operation is currently not supported.");
            return null;
        }

        [SoapDocumentMethodAttribute("http://schemas.microsoft.com/sharepoint/soap/GetVersions", RequestNamespace = "http://schemas.microsoft.com/sharepoint/soap/", ResponseNamespace = "http://schemas.microsoft.com/sharepoint/soap/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        [WebMethod]
        [XmlInclude(typeof(VersionOperationResult))]
        public VersionOperationResult GetVersions(string fileName)
        {
            if (DwsHelper.CheckVisitor() || string.IsNullOrEmpty(fileName))
                return null;

            var url = this.RequestUrl + "/" + fileName;
            var path = RepositoryPath.Combine(this.RequestPath, fileName);
            var file = Node.Load<GenericContent>(path);
            if (file == null)
            {
                // maybe it is a full path or url (Office 2013) instead of a name
                path = DwsHelper.GetPathFromUrl(fileName);
                file = Node.Load<GenericContent>(path);

                // re-set the url
                if (file != null)
                {
                    var bUrl = file.BrowseUrl;
                    var domain = new Uri(this.RequestUrl).GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped);

                    // remove query string parameters
                    url = new Uri(domain + bUrl).GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped);
                }
            }

            if (file == null)
                return null;
            
            var list = file.ContentListId;

            var vr = new VersionOperationResult();
            vr.results = new VersionResults();
            vr.results.list = new VersionResultsList();
            vr.results.list.id = list == 0 ? string.Empty : list.ToString();
            vr.results.settings = new VersionResultsSettings();
            vr.results.settings.url = url;
            vr.results.versioning = new VersionResultsVersioning();
            vr.results.versioning.enabled = file.VersioningMode > VersioningType.None ? (byte)1 : (byte)0;

            var versionData = new List<VersionData>();

            foreach (var version in file.Versions)
            {
                var createdBy = version.VersionModifiedBy as User;
                var fileVersion = version as File;

                var vd = new VersionData();
                vd.version = String.Format("{0}{1}.{2}", file.Version == version.Version ? "@" : String.Empty, version.Version.Major, version.Version.Minor);
                vd.url = url + "?version=" + version.Version;
                vd.created = version.VersionCreationDate.ToString();
                vd.createdRaw = version.VersionCreationDate.ToString("yyyy-MM-ddThh:mm:ssZ");
                vd.createdBy = createdBy == null ? string.Empty : createdBy.Username;
                vd.createdByName = createdBy == null ? string.Empty : createdBy.DisplayName;
                vd.size = fileVersion == null ? (ulong)0 : (ulong)fileVersion.Size;
                vd.comments = version.GetProperty<string>("CheckInComments") ?? string.Empty;

                versionData.Add(vd);
            }

            vr.results.result = versionData.ToArray();

            return vr;
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/sharepoint/soap/")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://schemas.microsoft.com/sharepoint/soap/", IsNullable = false)]
    public partial class VersionOperationResult
    {

        private VersionResults resultsField;

        /// <remarks/>
        public VersionResults results
        {
            get
            {
                return this.resultsField;
            }
            set
            {
                this.resultsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/sharepoint/soap/")]
    public partial class VersionResults
    {

        private VersionResultsList listField;

        private VersionResultsVersioning versioningField;

        private VersionResultsSettings settingsField;

        private VersionData[] resultField;

        /// <remarks/>
        public VersionResultsList list
        {
            get
            {
                return this.listField;
            }
            set
            {
                this.listField = value;
            }
        }

        /// <remarks/>
        public VersionResultsVersioning versioning
        {
            get
            {
                return this.versioningField;
            }
            set
            {
                this.versioningField = value;
            }
        }

        /// <remarks/>
        public VersionResultsSettings settings
        {
            get
            {
                return this.settingsField;
            }
            set
            {
                this.settingsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("result")]
        public VersionData[] result
        {
            get
            {
                return this.resultField;
            }
            set
            {
                this.resultField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/sharepoint/soap/")]
    public partial class VersionResultsList
    {

        private string idField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/sharepoint/soap/")]
    public partial class VersionData
    {

        private string versionField;

        private string urlField;

        private string createdField;

        private string createdRawField;

        private string createdByField;

        private string createdByNameField;

        private ulong sizeField;

        private string commentsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string url
        {
            get
            {
                return this.urlField;
            }
            set
            {
                this.urlField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string created
        {
            get
            {
                return this.createdField;
            }
            set
            {
                this.createdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string createdRaw
        {
            get
            {
                return this.createdRawField;
            }
            set
            {
                this.createdRawField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string createdBy
        {
            get
            {
                return this.createdByField;
            }
            set
            {
                this.createdByField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string createdByName
        {
            get
            {
                return this.createdByNameField;
            }
            set
            {
                this.createdByNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ulong size
        {
            get
            {
                return this.sizeField;
            }
            set
            {
                this.sizeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string comments
        {
            get
            {
                return this.commentsField;
            }
            set
            {
                this.commentsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/sharepoint/soap/")]
    public partial class VersionResultsVersioning
    {

        private byte enabledField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte enabled
        {
            get
            {
                return this.enabledField;
            }
            set
            {
                this.enabledField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/sharepoint/soap/")]
    public partial class VersionResultsSettings
    {

        private string urlField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string url
        {
            get
            {
                return this.urlField;
            }
            set
            {
                this.urlField = value;
            }
        }
    }
}
