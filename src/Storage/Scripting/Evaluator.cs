using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Scripting
{
    public static class Evaluator
    {
        static Evaluator()
        {
            // preload all available evaluators
            Load();
        }

        private static void Load()
        {
            foreach (var type in TypeResolver.GetTypesByInterface(typeof(IEvaluator)))
            {
                try
                {
                    if (!(type.GetCustomAttributes(typeof(ScriptTagNameAttribute), false).FirstOrDefault() is ScriptTagNameAttribute tagAttribute))
                    {
                        SnLog.WriteWarning($"Evaluator does not have a ScriptTagNameAttribute: {type.FullName} " +
                                           $"(Assembly: {type.Assembly})");
                        continue;
                    }

                    var fullTagName = GetFullTagName(tagAttribute.TagName);

                    // check if we already have an evaluator for this tag
                    if (Providers.Instance.GetProvider<IEvaluator>(fullTagName) != null)
                        continue;

                    var engine = (IEvaluator)Activator.CreateInstance(type);

                    Providers.Instance.SetProvider(fullTagName, engine);

                    SnLog.WriteInformation("Evaluator loaded: " + tagAttribute.TagName + ": " + engine);
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, $"Error loading script evaluator class. {type.AssemblyQualifiedName}");
                }
            }
        }

        /// <summary>
        /// Evaluates a script using the evaluator appointed by the provided tag in the script.
        /// For example: [Script:jScript]DateTime.UtcNow;[/Script]
        /// </summary>
        /// <returns>Evaluated value or the original script if the provider was not found.</returns>
        public static string Evaluate(string sourceCode)
        {
            if (sourceCode == null)
                throw new ArgumentNullException(nameof(sourceCode));

            var regex = new Regex(@"\[Script:[\w]*\](.+?)\[\/Script\]");
            return regex.Replace(sourceCode, ReplaceTags);
        }

        private static string ReplaceTags(Match match)
        {
            // "[Script:tagName]...[/Script]"
            var src = match.Value;

            var start = "[Script:".Length;
            var end = src.IndexOf("]", StringComparison.Ordinal);

            var tagName = src.Substring(start, end - start);
            var startTag = string.Concat("[Script:", tagName, "]");
            src = src.Replace(startTag, "").Replace("[/Script]", "");

            var evaluator = Providers.Instance.GetProvider<IEvaluator>(GetFullTagName(tagName));
            if (evaluator == null)
                return src;

            string result;

            try
            {
                result = evaluator.Evaluate(src);
                SnTrace.Repository.Write("Script evaluated. Evaluator:{0}, source:{1}", evaluator.GetType().FullName, src);
            }
            catch (Exception e) // logged
            {
                SnLog.WriteException(e);
                var ee = e;
                var msgBuilder = new StringBuilder();
                while (ee != null)
                {
                    msgBuilder.Append(ee.Message).Append("; ");
                    ee = ee.InnerException;
                }
                return msgBuilder.ToString();
            }

            return result;
        }
        internal static string GetFullTagName(string tagName)
        {
            return $"evaluator-{tagName}";
        }
        internal static string GetFullTagName(Type evaluatorType)
        {
            if (evaluatorType?.GetCustomAttributes(typeof(ScriptTagNameAttribute), false).FirstOrDefault() is ScriptTagNameAttribute tagAttribute)
                return GetFullTagName(tagAttribute.TagName);

            return string.Empty;
        }
    }
}
