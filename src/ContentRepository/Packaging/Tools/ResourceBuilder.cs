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
        IResourceValueBuilder AddResource(string key, string value);
    }

    public interface IResourceValueBuilder
    {
        IResourceClassBuilder Class(string className);
        IResourceCultureBuilder Culture(string cultureName);
        IResourceValueBuilder AddResource(string key, string value);
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

            rcb = new ResourceClassBuilder(this, className);
            Classes.Add(rcb);

            return rcb;
        }
    }
    internal class ResourceClassBuilder : IResourceClassBuilder
    {
        internal string ClassName { get; }
        internal IList<ResourceCultureBuilder> Cultures { get; } = new List<ResourceCultureBuilder>();
        internal ResourceContentBuilder ContentBuilder { get; }

        internal ResourceClassBuilder(ResourceContentBuilder contentBuilder, string className)
        {
            ContentBuilder = contentBuilder;
            ClassName = className;
        }
        
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

        internal IDictionary<string, string> Resources => ResourceValueBuilder.Resources;
        internal ResourceClassBuilder ClassBuilder { get; }

        private ResourceValueBuilder _resourceValueBuilder;
        private ResourceValueBuilder ResourceValueBuilder => _resourceValueBuilder ??= new ResourceValueBuilder(this);

        internal ResourceCultureBuilder(ResourceClassBuilder classBuilder, string cultureName)
        {
            ClassBuilder = classBuilder;
            CultureName = cultureName;
        }
        
        internal IResourceCultureBuilder Culture(string cultureName)
        {
            return ClassBuilder.Culture(cultureName);
        }

        public IResourceValueBuilder AddResource(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            return ResourceValueBuilder.AddResource(key, value);
        }
    }

    internal class ResourceValueBuilder : IResourceValueBuilder
    {
        private readonly ResourceCultureBuilder _cultureBuilder;
        internal IDictionary<string, string> Resources { get; } = new Dictionary<string, string>();

        internal ResourceValueBuilder(ResourceCultureBuilder cultureBuilder)
        {
            _cultureBuilder = cultureBuilder;
        }

        public IResourceClassBuilder Class(string className)
        {
            return _cultureBuilder.ClassBuilder.ContentBuilder.Class(className);
        }
        public IResourceCultureBuilder Culture(string cultureName)
        {
            return _cultureBuilder.Culture(cultureName);
        }
        public IResourceValueBuilder AddResource(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            Resources[key] = value;

            return this;
        }
    }
    #endregion

    /// <summary>
    /// Resource editor API for adding and editing string resources.
    /// </summary>
    public class ResourceBuilder
    {
        private const string EmptyResource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Resources>
</Resources>";

        internal IList<ResourceContentBuilder> ResourceContentBuilders { get; } = new List<ResourceContentBuilder>();

        public IResourceContentBuilder Content(string contentName)
        {
            if (string.IsNullOrEmpty(contentName))
                throw new ArgumentNullException(nameof(contentName));

            var rcb = ResourceContentBuilders.FirstOrDefault(res => res.ContentName == contentName);
            if (rcb != null) 
                return rcb;

            rcb = new ResourceContentBuilder(contentName);
            ResourceContentBuilders.Add(rcb);

            return rcb;
        }
        public void Apply()
        {
            foreach (var resourceBuilder in ResourceContentBuilders)
            {
                var resourcePath = RepositoryPath.Combine(RepositoryStructure.ResourceFolderPath, resourceBuilder.ContentName);
                var resource = Node.Load<Resource>(resourcePath) ??
                               Node.Load<Resource>(resourcePath + ".xml");

                // create resource content if necessary
                if (resource == null)
                {
                    resource = new Resource(Node.LoadNode(RepositoryStructure.ResourceFolderPath))
                    {
                        Name = resourceBuilder.ContentName
                    };

                    var binData = new BinaryData { FileName = new BinaryFileName(resourceBuilder.ContentName) };
                    binData.SetStream(RepositoryTools.GetStreamFromString(EmptyResource));

                    resource.Binary = binData;
                    resource.Save();

                    //TODO: log!
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
            var valueNode = dataElement.SelectSingleNode("value");
            if (valueNode != null)
                valueNode.InnerXml = resourceValue;
        }
        #endregion
    }
}
