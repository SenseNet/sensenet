using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.Portal.Virtualization;
using System.Xml;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.ContentRepository.Fields;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Diagnostics;
using SenseNet.Portal.OData;

namespace SenseNet.Portal
{
    /// <summary>
    /// Represents a web site in the Sense/Net Portal.
    /// </summary>
    /// <remarks>
    /// In the ECMS (Enterprise Content Management) systems all the data and all the objects are handled as contents. 
    /// Everything (like the web contents, web pages, but the portal users, your business data as well) is an 
    /// enterprise content, and can be stored in the Sense/Net Content Repository. The Site class represents a web 
    /// site that is stored in the content repository.
    /// </remarks>
	[ContentHandler]
    public class Site : Workspace
    {
        private IDictionary<string, string> _urlList;

        /// <summary>
        /// Initializes a new instance of the <see cref="Site"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public Site(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Site"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public Site(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Site"/> class during the loading process.
        /// Do not use this constructor directly in your code.
        /// </summary>
        protected Site(NodeToken nt) : base(nt) { }

        //UNDONE: XMLDOC: Site.PendingUserLang
        [RepositoryProperty("PendingUserLang")]
        public string PendingUserLang
        {
            get { return this.GetProperty<string>("PendingUserLang"); }
            set { this["PendingUserLang"] = value; }
        }
        /// <summary>
        /// Gets or stes the language name of this <see cref="Site"/> (e.g. "en-us").
        /// </summary>
        [RepositoryProperty("Language")]
        public string Language
        {
            get { return this.GetProperty<string>("Language"); }
            set { this["Language"] = value; }
        }

        /// <summary>
        /// Gets or sets a boolean value that represents whether the client's culture info is acceptable or not.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("EnableClientBasedCulture", RepositoryDataType.Int)]
        public virtual bool EnableClientBasedCulture
        {
            get { return base.GetProperty<int>("EnableClientBasedCulture") != 0; }
            set { this["EnableClientBasedCulture"] = value ? 1 : 0; }
        }

        private const string ENABLEUSERBASEDCULTURE = "EnableUserBasedCulture";
        /// <summary>
        /// Gets or sets a boolean value that represents whether the user's culture info is acceptable or not.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty(ENABLEUSERBASEDCULTURE, RepositoryDataType.Int)]
        public virtual bool EnableUserBasedCulture
        {
            get { return base.GetProperty<int>(ENABLEUSERBASEDCULTURE) != 0; }
            set { this[ENABLEUSERBASEDCULTURE] = value ? 1 : 0; }
        }

        /// <summary>
        /// Gets or sets the list of URLs handled by this <see cref="Site"/> instance.
        /// Represented by a IDictionary&lt;string, string&gt; when the key is the URL without protocol and
        /// value is the authentication mode (can be "None", "Forms" or "Windows").
        /// Persisted as an XML fragment in a <see cref="RepositoryDataType.Text"/> field.
        /// </summary>
        [RepositoryProperty("UrlList", RepositoryDataType.Text)]
        public IDictionary<string, string> UrlList
        {
            get { return _urlList ?? (_urlList = ParseUrlList(this.GetProperty<string>("UrlList"))); }
            set
            {
                this["UrlList"] = UrlListToString(value);
                _urlList = null;
            }
        }

        /// <summary>
        /// Gets or sets the reference of the site's start page.
        /// Persisted as <see cref="RepositoryDataType.Reference"/>.
        /// </summary>
        [RepositoryProperty("StartPage", RepositoryDataType.Reference)]
        public Node StartPage
        {
            get { return this.GetReference<Node>("StartPage"); }
            set { this.SetReference("StartPage", value); }
        }
        /// <summary>
        /// Gets or sets the reference of the site's login page.
        /// Persisted as <see cref="RepositoryDataType.Reference"/>.
        /// </summary>
        [RepositoryProperty("LoginPage", RepositoryDataType.Reference)]
        public Node LoginPage
        {
            get { return this.GetReference<Node>("LoginPage"); }
            set { this.SetReference("LoginPage", value); }
        }

        /// <summary>
        /// Gets or sets the reference of the site's skin.
        /// Persisted as <see cref="RepositoryDataType.Reference"/>.
        /// </summary>
        [RepositoryProperty("SiteSkin", RepositoryDataType.Reference)]
        public Node SiteSkin
        {
            get { return this.GetReference<Node>("SiteSkin"); }
            set { this.SetReference("SiteSkin", value); }
        }

        private const string DENYCROSSSITEACCESSPROPERTY = "DenyCrossSiteAccess";
        /// <summary>
        /// Gets or sets "true" if this <see cref="Site"/> instance denies the cross site access.
        /// </summary>
        [RepositoryProperty(DENYCROSSSITEACCESSPROPERTY, RepositoryDataType.Int)]
        public bool DenyCrossSiteAccess
        {
            get { return base.GetProperty<int>(DENYCROSSSITEACCESSPROPERTY) != 0; }
            set { base.SetProperty(DENYCROSSSITEACCESSPROPERTY, value ? 1 : 0); }
        }

        /// <summary>
        /// Returns <see cref="Site"/> instance that belongs to the current web request.
        /// </summary>
        public static Site Current => PortalContext.Current?.Site;

        //////////////////////////////////////// Methods //////////////////////////////////////////////

        /// <inheritdoc />
        public override void Save(SavingMode mode)
        {
            ValidateStartPage();

            RefreshUrlList();

            if (this.CopyInProgress)
            {
                // we need to reset these values to avoid conflict with the source site
                this.UrlList = new Dictionary<string, string>();
                this.StartPage = null;
            }
            else
            {
                ValidateUrlList();
            }

            base.Save(mode);

            var action = new PortalContext.ReloadSiteListDistributedAction();
            action.Execute();
        }

        private void ValidateStartPage()
        {
            var startPage = this.StartPage;
            if (startPage == null)
                return;
            if (!startPage.Path.StartsWith(this.Path))
                throw new ApplicationException(SNSR.GetString(SNSR.Exceptions.Site.StartPageMustBeUnderTheSite));
        }

        /// <inheritdoc />
        public override void Delete()
        {
            base.Delete();

            var action = new PortalContext.ReloadSiteListDistributedAction();
            action.Execute();
        }

        /// <inheritdoc />
        public override void ForceDelete()
        {
            base.ForceDelete();

            var action = new PortalContext.ReloadSiteListDistributedAction();
            action.Execute();
        }

        private void RefreshUrlList()
        {
            var originalUrls = this.GetProperty<string>("UrlList");
            var currentUrls = UrlListToString(this.UrlList);
            if (originalUrls != currentUrls)
                this["UrlList"] = currentUrls;
        }

        private void ValidateUrlList()
        {
            foreach (var url in UrlList.Keys)
            {
                if (!IsValidSiteUrl(url))
                    throw new ApplicationException(SNSR.GetString(SNSR.Exceptions.Site.InvalidUri_1, (object)url));
            }

            // if another site already uses one of our urls, throw an exception
            foreach (var url in UrlList.Keys.Where(url => PortalContext.Sites.Keys.Count(k => k == url && PortalContext.Sites[k].Id != this.Id) > 0))
                throw new ApplicationException(SNSR.GetString(SNSR.Exceptions.Site.UrlAlreadyUsed_2, url, PortalContext.Sites[url].DisplayName));
        }
        private bool IsValidSiteUrl(string url)
        {
            var absUrl = "http://" + url;
            if (!Uri.IsWellFormedUriString(absUrl, UriKind.Absolute))
                return false;
            try
            {
                var uri = new Uri(absUrl);
                if (uri.Authority != url)
                    return false;
            }
            catch
            {
                // Do not log this, we only have to decide whether the URL is valid or not.
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns ancestor <see cref="Site"/> instance of the given source <see cref="Node"/>.
        /// </summary>
        /// <param name="source">The <see cref="Node"/> whose ancestor <see cref="Site"/> is looked for.</param>
        public static Site GetSiteByNode(Node source)
        {
            return GetSiteByNodePath(source.Path);
        }

        /// <summary>
        /// Returns ancestor <see cref="Site"/> instance of the given path.
        /// </summary>
        /// <param name="path">The path of the <see cref="Node"/> whose ancestor <see cref="Site"/> is looked for.</param>
        public static Site GetSiteByNodePath(string path)
        {
            return PortalContext.GetSiteByNodePath(path);
        }

        /// <summary>
        /// Returns authentication type of the given URI
        /// </summary>
        public string GetAuthenticationType(Uri uri)
        {
            string url = uri.GetComponents(UriComponents.HostAndPort | UriComponents.Path, UriFormat.Unescaped);
            foreach (string siteUrl in UrlList.Keys)
            {
                if (url.StartsWith(siteUrl))
                    return UrlList[siteUrl];
            }
            return null;
        }
        /// <summary>
        /// Returns parsed data of the given URL list that can be JSON or XML fragment.
        /// For exmple:
        ///   &lt;Url authType="Forms"&gt;localhost:1315/&lt;/Url&gt;
        ///   &lt;Url authType="Windows"&gt;name.server.xy&lt;/Url&gt;
        /// or JSON:
        ///   [ { SiteName: "localhost:1315", AuthenticationType: "Forms" },
        ///     { SiteName: "name.server.xy", AuthenticationType: "Windows" } ]
        /// </summary>
        /// <param name="urlSrc">Source that will be parsed.</param>
        public static IDictionary<string, string> ParseUrlList(string urlSrc)
        {

            var urlList = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(urlSrc))
                return urlList;

            try
            {
                if (urlSrc.TrimStart().StartsWith("<"))
                {
                    // try parsing it as XML
                    var doc = new XmlDocument();
                    doc.LoadXml(string.Concat("<root>", urlSrc, "</root>"));
                    foreach (XmlNode node in doc.SelectNodes("//Url"))
                    {
                        var attr = node.Attributes["authType"];
                        var authType = attr == null ? string.Empty : attr.Value;
                        var url = node.InnerText.Trim();

                        if (!string.IsNullOrEmpty(url) && !urlList.ContainsKey(url))
                            urlList.Add(url, authType);
                    }
                }
                else
                {
                    // try parsing it as JSON
                    var jArray = JsonConvert.DeserializeObject(urlSrc) as JArray;
                    foreach (var authJToken in jArray)
                    {
                        var url = authJToken.Children<JProperty>().First().Name;
                        var authType = authJToken[url].Value<string>();

                        if (!string.IsNullOrEmpty(url) && !urlList.ContainsKey(url))
                            urlList.Add(url, authType);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unknown url list format. Error: " + ex.Message, ex);
            }

            return urlList;
        }
        /// <summary>
        /// Returns XML fragment representation of the given URL list.
        /// For exmple:
        ///   &lt;Url authType="Forms"&gt;localhost:1315/&lt;/Url&gt;
        ///   &lt;Url authType="Windows"&gt;name.server.xy&lt;/Url&gt;
        /// </summary>
        public static string UrlListToString(IDictionary<string, string> urlList)
        {
            if (urlList == null)
                throw new ApplicationException(SNSR.GetString(SNSR.Exceptions.Site.UrlListCannotBeEmpty));

            var sb = new StringBuilder();
            foreach (string key in urlList.Keys)
            {
                string auth = urlList[key];
                sb.Append("<Url");
                if (!String.IsNullOrEmpty(auth))
                    sb.Append(" authType='").Append(auth).Append("'");
                sb.Append(">");
                sb.Append(key);
                sb.Append("</Url>");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns XML fragment representation of the given URL list.
        /// or JSON:
        ///   [ { SiteName: "localhost:1315", AuthenticationType: "Forms" },
        ///     { SiteName: "name.server.xy", AuthenticationType: "Windows" } ]
        /// </summary>
        /// <param name="urlList"></param>
        /// <returns></returns>
        public static string UrlListToJson(IDictionary<string, string> urlList)
        {
            if (urlList == null)
                throw new ArgumentNullException("urlList");

            return JsonConvert.SerializeObject(urlList, Newtonsoft.Json.Formatting.Indented, new UrlListFieldConverter());
        }

        /// <summary>
        /// Returns URL of the specified repository path by the <see cref="Site"/> that belongs to the given URL.
        /// For example: be the input URL: "http://mysite.com/something" and repositoryPath: "/Root/Sites/MySite/MyFolder/MyDoc"
        /// First step is identifying the <see cref="Site"/> instance by the URL. In the example it is "/Root/Sites/MySite"
        /// that has this URL: "mysite.com".
        /// Second step: the given repositoryPath starts with the identified site's URL so after normalization the path is:
        /// "MyFolder/MyDoc".
        /// Last step: The normalized path will be prefixed with the identified site url and the protocol of the 
        /// current webrequest: "http" + "mysite.com" + "/" + "MyFolder/MyDoc".
        /// </summary>
        /// <param name="url">The URL that identifies the <see cref="Site"/> instance.</param>
        /// <param name="repositoryPath">The path that will be normalized by the identified <see cref="Site"/> instance.</param>
        /// <returns></returns>
        public static string GetUrlByRepositoryPath(string url, string repositoryPath)
        {
            return PortalContext.GetUrlByRepositoryPath(url, repositoryPath);
        }

        /// <summary>
        /// Returns list of all available <see cref="ChoiceOption"/> configured in the Language field of the 
        /// Site ContentTypeDefinition.
        /// </summary>
        public static IEnumerable<ChoiceOption> GetAllLanguages()
        {
            var languageFieldSetting = ContentType.GetByName("Site").GetFieldSettingByName("Language") as ChoiceFieldSetting;
            return languageFieldSetting.Options;
        }

        // ================================================================================= Generic Property handling

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "PendingUserLang":
                    return this.PendingUserLang;
                case "Language":
                    return this.Language;
                case "EnableClientBasedCulture":
                    return this.EnableClientBasedCulture;
                case ENABLEUSERBASEDCULTURE:
                    return this.EnableUserBasedCulture;
                case "UrlList":
                    return this.UrlList;
                case "StartPage":
                    return this.StartPage;
                case "LoginPage":
                    return this.LoginPage;
                case "SiteSkin":
                    return this.SiteSkin;
                case DENYCROSSSITEACCESSPROPERTY:
                    return this.DenyCrossSiteAccess;
                default:
                    return base.GetProperty(name);
            }
        }
        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "PendingUserLang":
                    this.PendingUserLang = (string)value;
                    break;
                case "Language":
                    this.Language = (string)value;
                    break;
                case "EnableClientBasedCulture":
                    this.EnableClientBasedCulture = (bool)value;
                    break;
                case ENABLEUSERBASEDCULTURE:
                    this.EnableUserBasedCulture = (bool)value;
                    break;
                case "UrlList":
                    this.UrlList = (Dictionary<string, string>)value;
                    break;
                case "StartPage":
                    this.StartPage = (Node)value;
                    break;
                case "LoginPage":
                    this.LoginPage = (Node)value;
                    break;
                case "SiteSkin":
                    this.SiteSkin = (Node)value;
                    break;
                case DENYCROSSSITEACCESSPROPERTY:
                    this.DenyCrossSiteAccess = (bool)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        //UNDONE: XMLDOC: Site.IsRequested
        public bool IsRequested(Uri uri) //UNDONE: Obsolete, or delete.
        {
            string url = uri.GetComponents(UriComponents.HostAndPort | UriComponents.Path, UriFormat.Unescaped);
            foreach (string key in UrlList.Keys)
                if (url.StartsWith(key))
                    return true;
            return false;
        }
    }
}