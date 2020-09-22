using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Tools
{
    #region Interfaces
    public interface IResourceContentBuilder
    {
        IResourceClassBuilder Class(string className);
    }

    public interface IResourceClassBuilder
    {
        IResourceCultureBuilder Culture(string cultureName);
    }

    public interface IResourceCultureBuilder
    {
        IResourceCultureBuilder Culture(string cultureName);
        IResourceCultureBuilder AddResource(string key, string value);
    }
    #endregion

    #region Internal helper classes
    internal class ResourceContentBuilder : IResourceContentBuilder
    {
        internal string ContentName { get; }
        internal  IList<ResourceClassBuilder> Classes { get; } = new List<ResourceClassBuilder>();
        internal ResourceContentBuilder(string contentName) { ContentName = contentName; }

        public IResourceClassBuilder Class(string className)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));

            var rcb = Classes.FirstOrDefault(res => res.ClassName == className);
            if (rcb != null)
                return rcb;

            rcb = new ResourceClassBuilder(className);
            Classes.Add(rcb);

            return rcb;
        }
    }
    internal class ResourceClassBuilder : IResourceClassBuilder
    {
        internal string ClassName { get; }
        internal IList<ResourceCultureBuilder> Cultures { get; } = new List<ResourceCultureBuilder>();
        internal ResourceClassBuilder(string className) { ClassName = className; }
        
        public IResourceCultureBuilder Culture(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                throw new ArgumentNullException(nameof(cultureName));

            var rcb = Cultures.FirstOrDefault(res => res.CultureName == cultureName);
            if (rcb != null)
                return rcb;

            rcb = new ResourceCultureBuilder(this, cultureName);
            Cultures.Add(rcb);

            return rcb;
        }
    }
    internal class ResourceCultureBuilder : IResourceCultureBuilder
    {
        internal string CultureName { get; }
        internal IDictionary<string, string> Resources { get; } = new Dictionary<string, string>();
        private readonly ResourceClassBuilder _classBuilder;

        internal ResourceCultureBuilder(ResourceClassBuilder classBuilder, string cultureName)
        {
            _classBuilder = classBuilder;
            CultureName = cultureName;
        }
        
        public IResourceCultureBuilder AddResource(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            Resources[key] = value;

            return this;
        }
        public IResourceCultureBuilder Culture(string cultureName)
        {
            return _classBuilder.Culture(cultureName);
        }
    }
    #endregion

    public class ResourceBuilder
    {
        private static readonly string EMPTY_RESOURCE = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Resources>
</Resources>";

        internal IList<ResourceContentBuilder> Resources { get; } = new List<ResourceContentBuilder>();

        public IResourceContentBuilder Content(string contentName)
        {
            if (string.IsNullOrEmpty(contentName))
                throw new ArgumentNullException(nameof(contentName));

            var rcb = Resources.FirstOrDefault(res => res.ContentName == contentName);
            if (rcb != null) 
                return rcb;

            rcb = new ResourceContentBuilder(contentName);
            Resources.Add(rcb);

            return rcb;
        }
        public void Apply()
        {
            foreach (var resourceBuilder in Resources)
            {
                var resourcePath = RepositoryPath.Combine(RepositoryStructure.ResourceFolderPath, resourceBuilder.ContentName);
                var resource = Node.Load<Resource>(resourcePath) ??
                               Node.Load<Resource>(resourcePath + ".xml");

                if (resource == null)
                {
                    resource = new Resource(Node.LoadNode(RepositoryStructure.ResourceFolderPath))
                    {
                        Name = resourceBuilder.ContentName
                    };

                    var binData = new BinaryData { FileName = new BinaryFileName(resourceBuilder.ContentName) };
                    binData.SetStream(RepositoryTools.GetStreamFromString(EMPTY_RESOURCE));

                    resource.Binary = binData;
                    resource.Save();

                    //UNDONE: log!
                    //context.Console.WriteLine("NEW resource content: {0}", resource.Path);
                }

                EditResource(resource, resourceBuilder);
            }
        }

        private void EditResource(Resource resource, ResourceContentBuilder builder)
        {
            // load resource xml from repository
            var xDoc = new XmlDocument();
            using var resStream = resource.Binary.GetStream();
            xDoc.Load(resStream);

            foreach (var classBuilder in builder.Classes)
            {
                // load or create the class element
                var resClassElement = LoadOrAddElement(xDoc.DocumentElement,
                    $"ResourceClass[@name='{classBuilder.ClassName}']", "ResourceClass",
                    new Dictionary<string, string>
                    {
                        {"name", classBuilder.ClassName}
                    },
                    @"<Languages></Languages>");

                foreach (var cultureBuilder in classBuilder.Cultures)
                {
                    foreach (var resourceItem in cultureBuilder.Resources)
                    {
                        // main operation: add or modify xml elements for one resource
                        AddOrEditResource(resClassElement, cultureBuilder.CultureName, resourceItem.Key, resourceItem.Value);
                    }
                }
            }

            // save the resource content
            using var modifiedStream = RepositoryTools.GetStreamFromString(xDoc.OuterXml);
            resource.Binary.SetStream(modifiedStream);
            resource.Save(SavingMode.KeepVersion);
        }

        #region Xml editor methods
        private static XmlElement LoadOrAddElement(XmlNode parentNode, string xPath, string name, IDictionary<string, string> attributes = null, string innerXml = null)
        {
            if (parentNode.SelectSingleNode(xPath) is XmlElement xmlElement)
                return xmlElement;

            xmlElement = parentNode.OwnerDocument?.CreateElement(name);
            if (xmlElement == null)
                return null;

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
        private static void AddOrEditResource(XmlElement resClassElement, string languageCode, string resourceKey, string resourceValue)
        {
            var languagesElement = LoadOrAddElement(resClassElement, "Languages", "Languages");
            var languageElement = LoadOrAddElement(languagesElement, $"Language[@cultureName='{languageCode}']", "Language",
                new Dictionary<string, string>
                {
                    {"cultureName", languageCode}
                });

            // add or modify string resource
            var dataElement = LoadOrAddElement(languageElement, $"data[@name='{resourceKey}']", "data",
                new Dictionary<string, string>
                {
                    { "name", resourceKey },
                    { "xml:space", "preserve"}
                },
                $"<value>{resourceValue}</value>");

            // set the value to be sure it has changed
            dataElement.FirstChild.InnerXml = resourceValue;
        }
        #endregion
    }
}
