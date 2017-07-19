using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Search
{
    [Obsolete("", true)]//UNDONE:! Delete unused class
    public class NodeQueryTemplateReplacer
    {
        private static string[] objectNames = new string[0];
        public virtual IEnumerable<string> ObjectNames { get { return objectNames; } }
        public virtual void OnStartReplace(XmlDocument queryXml) { }
        public virtual string EvaluateObjectProperty(string objectName, string propertyName) { return string.Empty; }
        public virtual void OnEndReplace(XmlDocument queryXml) { }

        internal static Dictionary<string, NodeQueryTemplateReplacer> DiscoverTemplateReplacers()
        {
            var replacerTypes = TypeResolver.GetTypesByBaseType(typeof(NodeQueryTemplateReplacer));
            var replacers = new Dictionary<string, NodeQueryTemplateReplacer>();

            foreach (var replacerType in replacerTypes)
            {
                var replacerInstance = (NodeQueryTemplateReplacer)Activator.CreateInstance(replacerType);
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
        internal static void ReplaceTemplates(XmlDocument queryXml, Dictionary<string, NodeQueryTemplateReplacer> templateReplacers)
        {
            var allReplacers = templateReplacers.Values.Distinct();

            foreach (var replacer in allReplacers)
                replacer.OnStartReplace(queryXml);

            Replace(queryXml, templateReplacers);

            foreach (var replacer in allReplacers)
                replacer.OnEndReplace(queryXml);
        }

        private static void Replace(XmlDocument queryXml, Dictionary<string, NodeQueryTemplateReplacer> templateReplacers)
        {
            foreach (var objectName in templateReplacers.Keys)
            {
                XmlNodeList list = queryXml.GetElementsByTagName(objectName);
                while (list.Count > 0)
                {
                    string propertyName = null;
                    var propertyAttr = list[0].Attributes["property"];
                    if (propertyAttr != null)
                        propertyName = propertyAttr.Value;
                    string replacedValue = templateReplacers[objectName].EvaluateObjectProperty(objectName, propertyName);
                    list[0].ParentNode.InnerXml = list[0].ParentNode.InnerXml.Replace(list[0].OuterXml, replacedValue ?? string.Empty);

                    list = queryXml.GetElementsByTagName(objectName);
                }
            }
        }
    }
}
