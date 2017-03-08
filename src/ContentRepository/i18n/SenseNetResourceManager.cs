using System;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Compilation;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage;
using System.Xml;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.Search;

namespace SenseNet.ContentRepository.i18n
{
    public class ResourceManagerBridge : IResourceManager
    {
        public bool Running
        {
            get { return SenseNetResourceManager.Running; }
        }

        public string GetString(string fullResourceKey)
        {
            return SenseNetResourceManager.Current.GetString(fullResourceKey);
        }
        public string GetString(string fullResourceKey, params object[] args)
        {
            return String.Format(SenseNetResourceManager.Current.GetString(fullResourceKey), args);
        }

        public bool ParseResourceKey(string source, out string className, out string name)
        {
            return SenseNetResourceManager.ParseResourceKey(source, out className, out name);
        }

        public IEnumerable<string> GetResourceFilesForClass(string className)
        {
            return SenseNetResourceManager.Current.GetResourceFilesForClass(className);
        }
    }

    public sealed class SenseNetResourceManager
    {
        // ================================================================ Cross appdomain part
        [Serializable]
        internal sealed class ResourceManagerResetDistributedAction : DistributedAction
        {
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                // Local echo of my action: Return without doing anything
                if (onRemote && isFromMe)
                    return;
                SenseNetResourceManager.ResetPrivate();
            }
        }

        internal static void Reset()
        {
            SnLog.WriteInformation("ResourceManager.Reset called.", EventId.RepositoryRuntime,
                properties: new Dictionary<string, object> { { "AppDomain", AppDomain.CurrentDomain.FriendlyName } });

            new ResourceManagerResetDistributedAction().Execute();
        }
        private static void ResetPrivate()
        {
            SnLog.WriteInformation("ResourceManager.Reset executed.", EventId.RepositoryRuntime,
                properties: new Dictionary<string, object> { { "AppDomain", AppDomain.CurrentDomain.FriendlyName } });

            lock (_syncRoot)
            {
                _current = null;
            }
        }

        // ================================================================ Static part

        public const string ResourceStartKey = "<%$";
        public const string ResourceEndKey = "%>";
        public static readonly char ResourceKeyPrefix = '$';
        public static readonly string ResourceEditorCookieName = "AllowResourceEditorCookie";

        private static object _syncRoot = new Object();

        private static SenseNetResourceManager _current;
        public static SenseNetResourceManager Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_syncRoot)
                    {
                        if (_current == null)
                        {
                            var current = new SenseNetResourceManager();
                            current.Load();
                            _current = current;
                            SnLog.WriteInformation("ResourceManager created: " + _current.GetType().FullName);
                        }
                    }
                }

                return _current;
            }
        }

        [Obsolete("After V6.5 PATCH 9: Use RepositoryEnvironment.FallbackCulture instead.")]
        public static string FallbackCulture => RepositoryEnvironment.FallbackCulture;

        public static bool Running
        {
            get { return _current != null; }
        }

        private SenseNetResourceManager() { }

        // ================================================================ Instance part

        /// <summary>
        /// The date when the last resource modification happened in the system.
        /// </summary>
        public DateTime LastResourceModificationDate { get; private set; }

        private Dictionary<CultureInfo, Dictionary<string, Dictionary<string, object>>> _items = new Dictionary<CultureInfo, Dictionary<string, Dictionary<string, object>>>();
        private Dictionary<string, List<string>> _classFiles = new Dictionary<string, List<string>>();

        private void Load()
        {
            var resNodeType = ActiveSchema.NodeTypes[typeof(Resource).Name];
            if (resNodeType != null)
            {
                // search for all Resource content
                NodeQuery query = new NodeQuery();
                query.Add(new StringExpression(StringAttribute.Path, StringOperator.StartsWith,
                        String.Concat(RepositoryStructure.ResourceFolderPath, RepositoryPath.PathSeparator)));
                query.Add(new TypeExpression(resNodeType));

                // Elevation: caching all string resource files 
                // is independent from the current user.
                using (new SystemAccount())
                {
                    IEnumerable<Node> nodes;

                    if (RepositoryInstance.ContentQueryIsAllowed)
                    {
                        nodes = query.Execute().Nodes.OrderBy(i => i.Index);
                    }
                    else
                    {
                        var r = NodeQuery.QueryNodesByTypeAndPath(ActiveSchema.NodeTypes["Resource"]
                            , false
                            , String.Concat(RepositoryStructure.ResourceFolderPath, RepositoryPath.PathSeparator)
                            , true);
                        nodes = r.Nodes.OrderBy(i => i.Index);
                    }

                    LastResourceModificationDate = nodes.Any()
                        ? nodes.Max(x => x.ModificationDate)
                        : DateTime.MinValue;

                    // Workaround: truncate milliseconds, because the 'If-Modified-Since' header sent 
                    // by clients will not contain milliseconds, so comparisons would fail.
                    LastResourceModificationDate = LastResourceModificationDate.AddTicks(-(LastResourceModificationDate.Ticks % TimeSpan.TicksPerSecond));

                    ParseAll(nodes);
                }
            }
        }
        private void ParseAll(IEnumerable<Node> nodes)
        {
            // <Resources>
            //  <ResourceClass name="Portal">
            //    <Languages>
            //      <Language cultureName="hu">
            //        <data name="CheckedOutBy" xml:space="preserve">
            //          <value>Kivette</value>
            try
            {
                foreach (Resource res in nodes)
                {
                    try
                    {
                        var xml = new XmlDocument();
                        xml.Load(res.Binary.GetStream());
                        foreach (XmlElement classElement in xml.SelectNodes("/Resources/ResourceClass"))
                        {
                            var className = classElement.Attributes["name"].Value;
                            foreach (XmlElement languageElement in classElement.SelectNodes("Languages/Language"))
                            {
                                var cultureName = languageElement.Attributes["cultureName"].Value;
                                var cultureInfo = CultureInfo.GetCultureInfo(cultureName);
                                foreach (XmlElement dataElement in languageElement.SelectNodes("data"))
                                {
                                    var key = dataElement.Attributes["name"].Value;
                                    var value = dataElement.SelectSingleNode("value").InnerXml;

                                    AddItem(cultureInfo, className, key, value, res);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex, "Invalid resource: " + res.Path);
                    }
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }
        }
        private void AddItem(CultureInfo cultureInfo, string className, string key, string value, Resource res)
        {
            Dictionary<string, Dictionary<string, object>> culture;
            Dictionary<string, object> category;
            object item;

            if (!_items.TryGetValue(cultureInfo, out culture))
            {
                culture = new Dictionary<string, Dictionary<string, object>>();
                _items.Add(cultureInfo, culture);
            }
            if (!culture.TryGetValue(className, out category))
            {
                category = new Dictionary<string, object>();
                culture.Add(className, category);
            }

            if (!category.TryGetValue(key, out item))
                category.Add(key, value);
            else
                category[key] = value;

            // add resource file path to the className -> pathlist dictionary 
            List<string> resFiles;
            if (!_classFiles.TryGetValue(className, out resFiles))
            {
                resFiles = new List<string> { res.Path };
                _classFiles.Add(className, resFiles);
            }

            if (!resFiles.Contains(res.Path))
            {
                resFiles.Add(res.Path);
            }
        }

        // ---------------------------------------------------------------- Resource editor

        /// <summary>
        /// Creates markup to be consumed by the SN.ResourceEditor client script.
        /// </summary>
        /// <param name="className">The resource class</param>
        /// <param name="name">The resource key</param>
        /// <param name="s">The resource value</param>
        /// <returns>Markup for SN.ResourceEditor</returns>
        public static string GetEditorMarkup(string className, string name, string s)
        {
            // If you change this markup, please change the regular expression for the IsEditorMarkup method too.
            var linkstart = "<a href='javascript:' onclick=\"SN.ResourceEditor.editResource('" + className + "','" + name + "');\">";
            var text = "<span class='sn-redit-resource'>" + HttpUtility.HtmlEncode(s) + "</span>";
            return linkstart + text + "</a>";
        }

        /// <summary>
        /// Gets whether resource editing mode is allowed or not.
        /// </summary>
        public static bool IsResourceEditorAllowed
        {
            get
            {
                // circumvent "Request is not available in this context" errors on startup
                if (!Repository.Started())
                    return false;

                if (HttpContext.Current == null || User.Current == null)
                    return false;

                return HttpContext.Current.Request.Cookies.AllKeys.Contains(ResourceEditorCookieName) && User.Current.IsInGroup(Group.Administrators);
            }
        }

        private static readonly string EDITORMARKUP_REGEX = "<a\\shref='javascript:'\\sonclick=\"SN\\.ResourceEditor\\.editResource\\('[^<']+','[^<']+'\\);\"><span\\sclass='sn-redit-resource'>[^<]*</span></a>";

        /// <summary>
        /// Determines if the provided text is a resource editor markup. It must be an 'a' tag with the appropriate resource editor javascript method call and css class.
        /// </summary>
        /// <param name="text">The text to check.</param>
        public static bool IsEditorMarkup(string text)
        {
            return !string.IsNullOrEmpty(text) && text.StartsWith("<") && Regex.IsMatch(text, EDITORMARKUP_REGEX);
        }

        // ---------------------------------------------------------------- Accessors

        /// <summary>
        /// Gets the resource string for the given full resource key if it is in a correct format. 
        /// If the given parameter is not a resource key, the original string will be returned without changes.
        /// </summary>
        /// <param name="fullResourceKey">Any string or a full resource key in the following format: $[ClassName],[Key]</param>
        /// <returns></returns>
        public string GetString(string fullResourceKey)
        {
            if (string.IsNullOrEmpty(fullResourceKey))
                return fullResourceKey;

            string className, name;

            return ParseResourceKey(fullResourceKey, out className, out name)
                ? GetString(className, name)
                : fullResourceKey;
        }

        public string GetString(string className, string name)
        {
            return GetString(className, name, CultureInfo.CurrentUICulture);
        }
        /// <summary>
        /// Gets the specified string resource of the given classname for the CurrentUICulture property of the current thread. 
        /// </summary>
        /// <param name="className">Name of the class. (Represents a categoryname)</param>
        /// <param name="name">Name of the resource.</param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public string GetString(string className, string name, CultureInfo cultureInfo)
        {
            return GetObject(className, name, cultureInfo) as string;
        }
        /// <summary>
        /// Gets the value of the resource for the specified culture and class.
        /// </summary>
        /// <param name="className">Name of the category.</param>
        /// <param name="name">Name of the resource.</param>
        /// <param name="cultureInfo"></param>
        /// <returns>The value of the resource, If a match is not possible, a generated resourcekey is returned.</returns>
        public object GetObject(string className, string name, CultureInfo cultureInfo)
        {
            var s = GetObjectInternal(cultureInfo, className, name);
            if (s == null)
            {
                if (!string.IsNullOrEmpty(RepositoryEnvironment.FallbackCulture))
                {
                    try
                    {
                        // look for resource value using the fallback culture
                        var enCultureInfo = CultureInfo.GetCultureInfo(RepositoryEnvironment.FallbackCulture);
                        s = GetObjectInternal(enCultureInfo, className, name);
                    }
                    catch (CultureNotFoundException ex)
                    {
                        SnLog.WriteException(ex, string.Format("Invalid fallback culture: {0} ({1}, {2})", RepositoryEnvironment.FallbackCulture, className, name));
                    }
                }

                // no fallback resource, display the class and key instead
                if (s == null)
                    s = String.Concat(className, cultureInfo.Name, name);
            }

            if (!IsResourceEditorAllowed)
                return s;

            return GetEditorMarkup(className, name, s as string);
        }
        public string GetStringOrNull(string className, string name)
        {
            return GetStringOrNull(className, name, CultureInfo.CurrentUICulture);
        }
        public string GetStringOrNull(string className, string name, CultureInfo cultureInfo)
        {
            return GetObjectOrNull(className, name, cultureInfo) as string;
        }
        public object GetObjectOrNull(string className, string name, CultureInfo cultureInfo)
        {
            return GetObjectOrNull(className, name, cultureInfo, true);
        }
        public object GetObjectOrNull(string className, string name, CultureInfo cultureInfo, bool allowMarkup, bool allowFallbackToParentCulture = true)
        {
            var s = GetObjectInternal(cultureInfo, className, name, allowFallbackToParentCulture);

            if (!allowMarkup || !IsResourceEditorAllowed)
                return s;

            return GetEditorMarkup(className, name, s as string);
        }

        public string GetStringByExpression(string expression)
        {
            return IsExpression(expression) ? GetStringByExpressionInternal(expression) : null;
        }
        private static bool IsExpression(string expression)
        {
            return expression.StartsWith(ResourceStartKey) && expression.EndsWith(ResourceEndKey);
        }
        private string GetStringByExpressionInternal(string expression)
        {
            if (String.IsNullOrEmpty(expression))
                throw new ArgumentNullException("expression");

            expression = expression.Replace(" ", "");
            expression = expression.Replace(ResourceStartKey, "");
            expression = expression.Replace(ResourceEndKey, "");

            if (expression.Contains("Resources:"))
                expression = expression.Remove(expression.IndexOf("Resources:"), 10);

            var expressionFields = ResourceExpressionBuilder.ParseExpression(expression);
            if (expressionFields == null)
            {
                var context = HttpContext.Current;
                var msg = String.Format("{0} is not a valid string resource format.", expression);
                if (context == null)
                    throw new ApplicationException(msg);
                return String.Format(msg);
            }


            return GetString(expressionFields.ClassKey, expressionFields.ResourceKey);
        }

        private object GetObjectInternal(CultureInfo cultureInfo, string className, string name, bool allowFallbackToParentCulture = true)
        {
            var item = this.Get(cultureInfo, className, name);
            if (item != null)
                return item;

            if (!allowFallbackToParentCulture)
                return null;

            var test = cultureInfo.IsNeutralCulture;
            if (cultureInfo == CultureInfo.InvariantCulture)
                return null;

            item = this.Get(cultureInfo.Parent, className, name);
            return item;
        }
        private object Get(CultureInfo cultureInfo, string className, string name)
        {
            Dictionary<string, Dictionary<string, object>> culture;
            Dictionary<string, object> category;
            object item;
            if (!_items.TryGetValue(cultureInfo, out culture))
                return null;
            if (!culture.TryGetValue(className, out category))
                return null;
            if (!category.TryGetValue(name, out item))
                return null;
            return item;
        }
        public Dictionary<string, object> GetClassItems(string className, CultureInfo cultureInfo)
        {
            Dictionary<string, Dictionary<string, object>> culture;
            Dictionary<string, object> category;
            if (!_items.TryGetValue(cultureInfo, out culture))
                return null;
            if (!culture.TryGetValue(className, out category))
                return null;
            return category;
        }

        public IEnumerable<string> GetClassKeys(string className)
        {
            var result = _items
                .SelectMany(x => x.Value)
                .Where(x => x.Key == className)
                .SelectMany(x => x.Value)
                .Select(x => x.Key)
                .Distinct();

            return result;
        }

        public IEnumerable<string> GetClasses()
        {
            var result = _items
                .SelectMany(x => x.Value)
                .Select(x => x.Key)
                .Distinct();

            return result;
        }

        public IEnumerable<string> GetResourceFilesForClass(string className)
        {
            List<string> files;

            if (string.IsNullOrEmpty(className) || !_classFiles.TryGetValue(className, out files))
                return new List<string>();
            
            return files;
        }

        // ---------------------------------------------------------------- Tools

        public CultureInfo[] GetCultures()
        {
            return _items.Keys.ToArray();
        }
        public string[] GetStrings(string className, string name)
        {
            var strings = new List<string>();
            foreach (var culture in _items.Values)
            {
                Dictionary<string, object> category;
                object item;
                if (!culture.TryGetValue(className, out category))
                    continue;
                if (!category.TryGetValue(name, out item))
                    continue;
                strings.Add(item.ToString());
            }
            return strings.ToArray();
        }
        public string[] GetStrings(string className, string name, out CultureInfo[] cultures)
        {
            var strings = new List<string>();
            var cults = new List<CultureInfo>();
            foreach (var cultItem in _items)
            {
                Dictionary<string, object> category;
                object item;
                if (!cultItem.Value.TryGetValue(className, out category))
                    continue;
                if (!category.TryGetValue(name, out item))
                    continue;
                strings.Add(item.ToString());
                cults.Add(cultItem.Key);
            }
            cultures = cults.ToArray();
            return strings.ToArray();
        }

        public static bool ParseResourceKey(string source, out string className, out string name)
        {
            // "$" *[Whitespace] ["Resources:"] *[Whitespace] ClassName *[Whitespace] "," *[Whitespace] Key *[Whitespace]
            // "$  Resources:   ClassName  ,  Key   "
            
            className = name = null;

            if (string.IsNullOrEmpty(source))
                return false;

            if (source.Length < 3)
                return false;

            if (source[0] != ResourceKeyPrefix)
                return false;

            source = source.Substring(1).Trim();
            var s = source.Split(new[] { ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (s.Length == 2)
            {
                className = s[0].Trim();
                name = s[1].Trim();
                return true;
            }
            if (s.Length == 3)
            {
                if (s[0].ToLower() != "resources")
                    return false;
                className = s[1].Trim();
                name = s[2].Trim();
                return true;
            }
            return false;
        }

        public static string GetResourceKey(string className, string key)
        {
            return string.Format("${0},{1}", className, key);
        }
    }
}