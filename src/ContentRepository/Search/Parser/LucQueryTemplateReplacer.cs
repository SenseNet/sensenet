using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tools;

namespace SenseNet.Search.Parser
{
    [Obsolete("Use ContentQueryTemplateReplacer instead.", false)]
    public class LucQueryTemplateReplacer
    {
        public static readonly string TEMPLATE_PATTERN_FORMAT = "@@{0}(\\.(?<PropName>[^@]+))?@@";

        private static readonly string[] objectNames = new string[0];
        private static readonly object LockObject = new object();
        private static Dictionary<string, LucQueryTemplateReplacer> _templateReplacers;

        // ========================================================================= Singleton

        private static Dictionary<string, LucQueryTemplateReplacer> TemplateReplacers
        {
            get
            {
                if (_templateReplacers == null)
                {
                    lock (LockObject)
                    {
                        if (_templateReplacers == null)
                            _templateReplacers = DiscoverTemplateReplacers();
                    }
                }

                return _templateReplacers;
            }
        }

        private static Dictionary<string, LucQueryTemplateReplacer> DiscoverTemplateReplacers()
        {
            var replacerTypes = TypeResolver.GetTypesByBaseType(typeof(LucQueryTemplateReplacer));
            var replacers = new Dictionary<string, LucQueryTemplateReplacer>();

            foreach (var replacerType in replacerTypes)
            {
                var replacerInstance = (LucQueryTemplateReplacer)Activator.CreateInstance(replacerType);
                foreach (var objectName in replacerInstance.ObjectNames)
                {
                    if (replacers.ContainsKey(objectName))
                    {
                        if (replacers[objectName].GetType().IsAssignableFrom(replacerType))
                            replacers[objectName] = replacerInstance;
                    }
                    else
                    {
                        replacers.Add(objectName, replacerInstance);
                    }
                }
            }

            return replacers;
        }

        // ========================================================================= Virtual methods

        public virtual IEnumerable<string> ObjectNames { get { return objectNames; } }
        public virtual string EvaluateObjectProperty(string objectName, string propertyName) { return string.Empty; }

        // ========================================================================= Replace mechanism
        
        public static string ReplaceTemplates(string queryText)
        {
            foreach (var objectName in TemplateReplacers.Keys)
            {
                var templatePattern = string.Format(TEMPLATE_PATTERN_FORMAT, objectName);
                var index = 0;
                var regex = new Regex(templatePattern, RegexOptions.IgnoreCase);

                while (true)
                {
                    var match = regex.Match(queryText, index);
                    if (!match.Success)
                        break;

                    var templateValue = TemplateReplacers[objectName]
                        .EvaluateObjectProperty(objectName, match.Groups["PropName"].Value) ?? string.Empty;

                    queryText = queryText.Remove(match.Index, match.Length)
                        .Insert(match.Index, templateValue);

                    index = match.Index + templateValue.Length;

                    if (index >= queryText.Length)
                        break;
                }
            }

            return queryText;
        }
    }
}
