using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Steps
{
    public class AddResource : Step
    {
        private static readonly string EMPTY_RESOURCE = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Resources>
</Resources>";

        public string ContentName { get; set; }
        public string ClassName { get; set; }
        public IEnumerable<XmlElement> Resources { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if (string.IsNullOrEmpty(ContentName))
                throw new PackagingException(string.Format(SR.Errors.Content.InvalidContentName_1, ContentName));
            if (string.IsNullOrEmpty(ClassName))
                throw new PackagingException(string.Format(SR.Errors.Resource.AttributeIsMissing_1, "ClassName"));

            if (Resources == null || !Resources.Any())
            {
                context.Console.WriteLine("Resources list is empty, no content was modified.");
                return;
            }

            var resource = Node.Load<Resource>(RepositoryPath.Combine(RepositoryStructure.ResourceFolderPath, ContentName));

            // if the resource content does not exist, create it
            if (resource == null)
            {
                resource = new Resource(Node.LoadNode(RepositoryStructure.ResourceFolderPath)) { Name = ContentName };

                var binData = new BinaryData { FileName = new BinaryFileName(ContentName) };
                binData.SetStream(RepositoryTools.GetStreamFromString(EMPTY_RESOURCE));

                resource.Binary = binData;
                resource.Save();

                context.Console.WriteLine("NEW resource content: {0}", resource.Path);
            }
            else 
            {
                context.Console.WriteLine("Adding resources to {0}.", resource.Path);
            }

            // load original resource xml
            var xDoc = new XmlDocument();
            using (var resStream = resource.Binary.GetStream())
            {
                xDoc.Load(resStream);
            }

            // load or create the class element
            var resClassElement = LoadOrAddElement(xDoc.DocumentElement, string.Format("ResourceClass[@name='{0}']", ClassName), "ResourceClass",
                new Dictionary<string, string>
                {
                    { "name", ClassName }
                },
                @"<Languages></Languages>");

            // iterate through the resource elements in the step definition
            foreach (var resourceElement in Resources)
            {
                // check step metadata
                var keyAttr = resourceElement.Attributes["key"];
                var langAttr = resourceElement.Attributes["lang"];
                if (keyAttr == null || string.IsNullOrEmpty(keyAttr.Value))
                    throw new PackagingException(string.Format(SR.Errors.Resource.AttributeIsMissing_1, "key"));
                if (langAttr == null || string.IsNullOrEmpty(langAttr.Value))
                    throw new PackagingException(string.Format(SR.Errors.Resource.AttributeIsMissing_1, "lang"));

                // main operation: add or modify xml elements for one resource
                AddOrEditResource(resClassElement, ClassName, langAttr.Value, keyAttr.Value, resourceElement.InnerXml, context);
            }

            // save the resource content
            using (var resStream = RepositoryTools.GetStreamFromString(xDoc.OuterXml))
            {
                resource.Binary.SetStream(resStream);
                resource.Save(SavingMode.KeepVersion);
            }            
        }

        private static void AddOrEditResource(XmlElement resClassElement, string className, string languageCode, string resourceKey, string resourceValue, ExecutionContext context)
        {
            var languagesElement = LoadOrAddElement(resClassElement, "Languages", "Languages");
            var languageElement = LoadOrAddElement(languagesElement, string.Format("Language[@cultureName='{0}']", languageCode), "Language",
                new Dictionary<string, string>
                    {
                        {"cultureName", languageCode}
                    });

            // add or modify string resource
            var dataElement = LoadOrAddElement(languageElement, string.Format("data[@name='{0}']", resourceKey), "data",
                new Dictionary<string, string>
                    {
                        { "name", resourceKey },
                        { "xml:space", "preserve"}
                    },
                string.Format("<value>{0}</value>", resourceValue));

            context.Console.WriteLine("Resource added or modifified: {0}, {1}", className, resourceKey);
        }
        
        private static XmlElement LoadOrAddElement(XmlNode parentNode, string xPath, string name, IDictionary<string, string> attributes = null, string innerXml = null)
        {
            var xmlElement = parentNode.SelectSingleNode(xPath) as XmlElement;
            if (xmlElement != null) 
                return xmlElement;

            xmlElement = parentNode.OwnerDocument.CreateElement(name);

            if (attributes != null)
            {
                foreach (var key in attributes.Keys)
                {
                    xmlElement.SetAttribute(key, attributes[key]);
                }
            }

            if (!string.IsNullOrEmpty(innerXml))
                xmlElement.InnerXml = innerXml;

            parentNode.AppendChild(xmlElement);

            return xmlElement;
        }
    }
}
