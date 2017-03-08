using System;
using System.Text.RegularExpressions;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Portal.UI
{
    [ContentHandler]
    public class HtmlTemplate : File
    {
        public static class Names
        {
            public const string ActionButton = "button.html";
            public const string ActionImageButton = "imagebutton.html";
            public const string ActionLink = "link.html";
        }

        private static readonly string ACTIONTEMPLATEFOLDERPATH = "$skin/Templates/action";
        private static readonly string TEMPLATEREGEX = @"(\/\/ ){0,1}template (?<category>.+)";

        // ================================================================================= Constructors

        public HtmlTemplate(Node parent) : this(parent, null) { }
        public HtmlTemplate(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected HtmlTemplate(NodeToken nt) : base(nt) { }

        // ================================================================================= Properties

        private const string TemplateTextProperty = "TemplateText";
        public string TemplateText
        {
            get
            {
                var text = this.GetCachedData(TemplateTextProperty) as string;
                if (text == null)
                {
                    using (var stream = this.Binary.GetStream())
                    {
                        text = stream == null ? string.Empty : (RepositoryTools.GetStreamString(stream) ?? string.Empty);
                    }

                    this.SetCachedData(TemplateTextProperty, text);
                }

                return text;
            }
        }

        // ================================================================================= Overrides

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case TemplateTextProperty:
                    return this.TemplateText;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case TemplateTextProperty:
                    // readonly property, do nothing
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        // ================================================================================= Static API
        
        public static string GetActionLinkTemplate(string templateName)
        {
            using (new SystemAccount())
            {
                // As this is an SN7 feature, we use TryResolve instead of Resolve because we
                // do not want to look for the path in the Global folder, only under skins.
                string actionTemplatePath;
                if (SkinManagerBase.TryResolve(RepositoryPath.Combine(ACTIONTEMPLATEFOLDERPATH, templateName), out actionTemplatePath))
                {
                    var template = Node.Load<HtmlTemplate>(actionTemplatePath);
                    if (template != null)
                        return template.TemplateText;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Compiles a template script that contains a Javascript variable definition and a JSON object 
        /// with local and global templates that belong to the provided skin and category.
        /// </summary>
        [ODataFunction]
        public static string GetTemplateScript(Content content, string skin, string category)
        {
            var response = HttpContext.Current.Response;
            var cacheSettings = HttpHeaderTools.GetCacheHeaderSetting(content.Path, "File", "js");
            var maxAge = TimeSpan.FromSeconds(cacheSettings.HasValue ? cacheSettings.Value : 60);

            response.ContentType = "application/javascript";

            var script = HtmlTemplateCache.GetScript(content.Path, skin, category);
            if (script.FromCache)
            {
                HttpHeaderTools.EndResponseForClientCache(script.CacheDate, false);

                // If the response should end because there was no change, return an empty result 
                // instead of flushing and ending the request to avoid a thread abort exception.
                if (response.StatusCode == 304)
                {
                    HttpHeaderTools.SetCacheControlHeaders(HttpCacheability.Public, script.CacheDate, maxAge);
                    return string.Empty;
                }
            }

            HttpHeaderTools.SetCacheControlHeaders(HttpCacheability.Public, script.CacheDate, maxAge);

            return script.Script;
        }
        /// <summary>
        /// Composes a URL for loading template scripts from the provided skin and category for the provided context.
        /// </summary>
        public static string GetTemplateScriptRequest(Content content, string skin, string category)
        {
            //TODO: generalize OData url creation in ODataRequest
            return string.Format("/" + Configuration.Services.ODataServiceToken + "{0}('{1}')/GetTemplateScript?skin={2}&category={3}", content.ContentHandler.ParentPath, content.Name, skin, category);
        }
        
        /// <summary>
        /// Tries to parse a header line used in js files to define a reference for templates (e.g. '// template action').
        /// </summary>
        /// <param name="dependencyHeader">A header line that comes from a js file.</param>
        /// <param name="category">The category (e.g. 'action') if found.</param>
        /// <returns>Whether the parse was successful or not.</returns>
        public static bool TryParseTemplateCategory(string dependencyHeader, out string category)
        {
            category = string.Empty;
            if (string.IsNullOrEmpty(dependencyHeader))
                return false;

            var match = Regex.Match(dependencyHeader, TEMPLATEREGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (!match.Success)
                return false;

            category = match.Groups["category"].Value;
            return true;
        }
    }
}
