using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ContentRepository
{
    public class TemplateManager
    {
        private static class RegexGroupNames
        {
            public static readonly string PropertiesAndExpressions = "PropertyName";
        }

        // Main regex for matching the full template (e.g. '@@CurrentUser.Owner.CreationDate+12hours@@') 
        // and separating the template name (first part) from the rest.
        public static readonly string TEMPLATE_PATTERN_FORMAT = @"@@{0}(?<PropertyName>[\.\+\-][^@]+)?@@";
        // Regex for finding the first property in the expression.
        public static readonly string TEMPLATE_PROPERTY_REGEX = @"^(?<Property>[\w]+)([\.\+\-]|$)";
        // Regex for identifying parts of an expression (e.g. operator and unit).
        public static readonly string TEMPLATE_EXPRESSION_REGEX = @"(?<Modifier>((?<Operator>[\+\-])(?<Number>[\d]+)(?<Unit>[a-z]+)?)|((?<MethodOperator>Add|Plus|Minus|Subtract|Substract)(?<MethodUnit>[a-z]+)?\((?<Parameter>[\+\-]?[\d]+)\)))?";

        // ========================================================================= Replacers

        private static Dictionary<string, Dictionary<string, Regex>> TemplateRegexes { get; set; }

        private static readonly object LOCK_OBJECT = new object();
        private static Dictionary<string, Dictionary<string, TemplateReplacerBase>> _templateReplacers;
        private static Dictionary<string, Dictionary<string, TemplateReplacerBase>> TemplateReplacers
        {
            get
            {
                if (_templateReplacers == null)
                {
                    lock (LOCK_OBJECT)
                    {
                        if (_templateReplacers == null)
                            _templateReplacers = DiscoverTemplateReplacers();
                    }

                    SnLog.WriteInformation("TemplateReplacers created, see supported templates below.", 
                        properties: _templateReplacers.Keys.ToDictionary(name => name, name => (object)string.Join(", ", _templateReplacers[name].Keys)));
                }

                return _templateReplacers;
            }
        }

        private static Dictionary<string, Dictionary<string, TemplateReplacerBase>> DiscoverTemplateReplacers()
        {
            var trType = typeof(TemplateReplacerBase);
            var replacerTypes = TypeResolver.GetTypesByBaseType(trType);
            var replacers = new Dictionary<string, Dictionary<string, TemplateReplacerBase>>();

            TemplateRegexes = new Dictionary<string, Dictionary<string, Regex>>();

            // cache a template name --> replacer instance dictionary for every discovered base type
            foreach (var replacerType in replacerTypes)
            {
                var itr = replacerType;

                // Find the base type for this replacer type 'subtree' that
                // is the direct children of the abstract replacer base type
                while (itr.BaseType != null && itr.BaseType.FullName != trType.FullName)
                {
                    itr = itr.BaseType;
                }

                if (replacers.ContainsKey(itr.FullName))
                    continue;

                // Store the replace patterns for this replacer base type. Key is the base type name,
                // value is a _dictionary_ containing replacer instances for template names handled by
                // this replacer family.
                var replacerBaseInstance = Activator.CreateInstance(itr) as TemplateReplacerBase;
                if (replacerBaseInstance == null)
                    continue;

                // Collect all replacers in this family and build a templatename --> instance dictionary.
                var replacerFamily = CollectTemplateReplacers(itr);
                replacers.Add(itr.FullName, replacerFamily);

                // Cache regexes for all template names in the replacer family.
                TemplateRegexes.Add(itr.FullName, CollectTemplateRegexes(replacerBaseInstance, replacerFamily));
            }

            return replacers;
        }

        private static Dictionary<string, TemplateReplacerBase> CollectTemplateReplacers(Type replacerBaseType)
        {
            // Collect all replacers in a replacer family: that are inherited
            // from the same base class. Build a dictionary from all the template
            // names handled by these types and cache an instance of the type
            // that will handle that template. This is how it is possible to change
            // the behavior of a template: inherited types will be used instead of 
            // base classes if they handle the same template name.
            var replacers = new Dictionary<string, TemplateReplacerBase>();
            var replacerTypes = new List<Type> { replacerBaseType };

            replacerTypes.AddRange(TypeResolver.GetTypesByBaseType(replacerBaseType));

            foreach (var replacerType in replacerTypes)
            {
                var replacerInstance = Activator.CreateInstance(replacerType) as TemplateReplacerBase;
                if (replacerInstance == null)
                    continue;

                foreach (var templateName in replacerInstance.TemplateNames)
                {
                    if (replacers.ContainsKey(templateName))
                    {
                        if (replacers[templateName].GetType().IsAssignableFrom(replacerType))
                            replacers[templateName] = replacerInstance;
                    }
                    else
                    {
                        replacers.Add(templateName, replacerInstance);
                    }
                }
            }

            return replacers;
        }

        private static Dictionary<string, Regex> CollectTemplateRegexes(TemplateReplacerBase replacerBase, Dictionary<string, TemplateReplacerBase> replacers)
        {
            // use the common format string provided by the base class of the family, not the inherited types
            var templatePatternFormat = replacerBase.TemplatePatternFormat ?? TEMPLATE_PATTERN_FORMAT;
            var regexes = new Dictionary<string, Regex>();

            // pin a precompiled Regex object for every template name handled by this replacer family
            foreach (var replacer in replacers)
            {
                // insert the replacer name (e.g. CurrentDate) into the regex
                var templatePattern = string.Format(templatePatternFormat, replacer.Key);
                
                // Create a _compiled_ regex. This forces .Net to cache a compiled (MSIL) version
                // of the regex so that it can be re-used whenever we need the same regex.
                // We have to work with Regex _instances_ instead of relying on the static API of
                // the Regex class (which is the recommended way) because the static API does not 
                // contain the necessary overloads we need (a possibility to provide a start index
                // in the source text).
                // For details see the 'Compilation and Reuse in Regular Expressions' article.
                // https://msdn.microsoft.com/en-us/library/8zbs0h2f.aspx
                regexes.Add(replacer.Key, new Regex(templatePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled));
            }

            return regexes;
        }

        // ========================================================================= Replace methods

        public static string Replace(Type replacerBaseType, string text, object templatingContext = null)
        {
            if (replacerBaseType == null)
                throw new ArgumentNullException("replacerBaseType");

            return Replace(replacerBaseType.FullName, text, templatingContext);
        }

        public static string Replace(string replacerBaseType, string text, object templatingContext = null)
        {
            if (string.IsNullOrEmpty(replacerBaseType))
                throw new ArgumentNullException("replacerBaseType");
            if (!TemplateReplacers.ContainsKey(replacerBaseType))
                throw new InvalidOperationException("No template replacer found with the name " + replacerBaseType);

            if (string.IsNullOrEmpty(text))
                return text;

            return ReplaceTemplates(TemplateReplacers[replacerBaseType], TemplateRegexes[replacerBaseType], text, templatingContext);
        }

        // ========================================================================= Replace methods - private

        private static string ReplaceTemplates(Dictionary<string, TemplateReplacerBase> replacers, Dictionary<string, Regex> regexes, string text, object templatingContext)
        {
            // Iterate through all template names handled by this particular 
            // template replacer family (e.g. CurrentThis, CurrentThat).
            foreach (var templateName in replacers.Keys)
            {
                var index = 0;
                var regex = regexes[templateName]; // load the cached regex that contains this template name

                while (true)
                {
                    var match = regex.Match(text, index);
                    if (!match.Success)
                        break;

                    var propsAndExp = match.Groups[RegexGroupNames.PropertiesAndExpressions];
                    var templateValue = replacers[templateName].EvaluateTemplate(
                        templateName, 
                        propsAndExp == null ? null : propsAndExp.Value.TrimStart('.'), 
                        templatingContext) ?? string.Empty;

                    // remove template and insert the evaluated value
                    text = text
                        .Remove(match.Index, match.Length)
                        .Insert(match.Index, templateValue);

                    // move the pointer to the position after the replaced text
                    index = match.Index + templateValue.Length;

                    if (index >= text.Length)
                        break;
                }
            }

            return text;
        }

        // ========================================================================= Helper methods

        public static void Init()
        {
            // init replacers
            var reps = TemplateReplacers;
        }

        public static string GetProperty(GenericContent content, string propertyName)
        {
            if (content == null)
                return string.Empty;
            if (string.IsNullOrEmpty(propertyName))
                return content.Id.ToString();

            var value = content.GetProperty(propertyName);
            return value == null ? string.Empty : value.ToString();
        }

        public static string GetProperty(Node node, string propertyName)
        {
            if (node == null)
                return string.Empty;
            if (string.IsNullOrEmpty(propertyName))
                return node.Id.ToString();

            var value = node[propertyName];
            return value == null ? string.Empty : value.ToString();
        }
    }
}
