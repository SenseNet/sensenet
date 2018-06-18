using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;

// ReSharper disable CheckNamespace
namespace SenseNet.Packaging.Steps.Internal
{
    public class UpgradeProviderConfiguration : Step
    {
        private string _file;

        public string File
        {
            get
            {
                if (string.IsNullOrEmpty(_file))
                    throw new InvalidStepParameterException("Missing 'file' attribute");
                return _file;
            }
            set => _file = value;
        }

        public PathRelativeTo PathIsRelativeTo { get; set; } = PathRelativeTo.TargetDirectory;

        public override void Execute(ExecutionContext context)
        {
            foreach (var path in ResolvePaths(File, context))
            {
                string xmlSrc;
                using (var reader = new System.IO.StreamReader(path))
                    xmlSrc = reader.ReadToEnd();

                var doc = new XmlDocument();
                doc.LoadXml(xmlSrc);

                // check if the original xml contains a declaration header
                var omitXmlDeclaration = doc.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault() == null;

                Execute(doc);

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = omitXmlDeclaration,
                    CloseOutput = true
                };

                using (var writer = XmlWriter.Create(path, settings))
                    doc.Save(writer);
            }
        }

        private void Execute(XmlDocument xml)
        {
            DeleteUnnecessareUnityElements(xml);
            TransformProviderElements(xml);
            CleanupUnitySection(xml);
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private void DeleteUnnecessareUnityElements(XmlDocument xml)
        {
            var deletableElements = new List<XmlElement>();

            foreach (XmlElement element in xml.SelectNodes("/configuration/unity/containers/container[@name='Providers']/types/type[@type='ActionBase']"))
                deletableElements.Add(element);

            foreach (XmlElement element in xml.SelectNodes("/configuration/unity/containers/container[@name='Providers']/types/type[@type='GenericScenario']"))
                deletableElements.Add(element);

            var deletableAliases = deletableElements.Select(e => e.Attributes["name"]?.Value).Union(
                deletableElements.Select(e => e.Attributes["mapto"]?.Value)).ToList();
            deletableAliases.Add("ActionBase");
            deletableAliases.Add("GenericScenario");

            deletableElements.AddRange(
                deletableAliases
                    .Select(a => (XmlElement)xml.SelectSingleNode($"/configuration/unity/typeAliases/typeAlias[@alias='{a}']"))
                    .Where(e => e != null)
                    .ToArray()
            );

            foreach (var element in deletableElements)
                element.ParentNode?.RemoveChild(element);
        }

        private void TransformProviderElements(XmlDocument xml)
        {
            DeleteProvider(xml, "ISearchEngine");
            MoveProvider(xml, "IViewManager", "ViewManager");
            MoveProvider(xml, "PurgeUrlCollector", "PurgeUrlCollector");
            MoveProvider(xml, "IApplicationCache", "ApplicationCache");
        }

        private void DeleteProvider(XmlDocument xml, string providerName)
        {
            var typeElement = GetTypeElement(xml, providerName);
            if (typeElement == null)
                return;

            var mapToAlias = typeElement.Attributes["mapTo"]?.Value;

            DeleteElement(GetTypeAliasElement(xml, mapToAlias));
            DeleteElement(GetTypeAliasElement(xml, providerName));
            DeleteElement(typeElement);
        }

        private void MoveProvider(XmlDocument xml, string searchName, string providerName)
        {
            var typeElement = GetTypeElement(xml, searchName);
            if (typeElement == null)
                return;

            var mapToAlias = typeElement.Attributes["mapTo"]?.Value;
            var typeAliasElement = GetTypeAliasElement(xml, mapToAlias);
            var providerTypeName = typeAliasElement.Attributes["type"].Value;

            CreateProviderElement(xml, providerName, providerTypeName);

            DeleteElement(GetTypeAliasElement(xml, typeElement.Attributes["type"]?.Value));
            DeleteElement(typeAliasElement);
            DeleteElement(typeElement);
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private void CleanupUnitySection(XmlDocument xml)
        {
            var relevantElements = new List<XmlElement>();

            foreach (XmlElement element in xml.SelectNodes("/configuration/unity/containers/container[@name='Providers']/types/*"))
                relevantElements.Add(element);
            foreach (XmlElement element in xml.SelectNodes("/configuration/unity/typeAliases/*"))
                relevantElements.Add(element);

            if (relevantElements.Count > 1)
            {
                Logger.LogMessage("The configuration contains one or more unkown elements.");
                Logger.LogMessage("Unity section is not removed.");
                return;
            }

            DeleteElement((XmlElement)xml.SelectSingleNode("/configuration/configSections/section[@name='unity']"));
            DeleteElement((XmlElement)xml.SelectSingleNode("/configuration/unity"));

            Logger.LogMessage("Unity section is totally removed.");
        }

        private XmlElement GetTypeElement(XmlDocument xml, string type)
        {
            return (XmlElement)xml.SelectSingleNode($"/configuration/unity/containers/container[@name='Providers']/types/type[@type='{type}']");
        }
        private XmlElement GetTypeAliasElement(XmlDocument xml, string alias)
        {
            return (XmlElement)xml.SelectSingleNode($"/configuration/unity/typeAliases/typeAlias[@alias='{alias}']");
        }
        private void CreateProviderElement(XmlDocument xml, string providerName, string providerTypeName)
        {
            if (providerName == null || providerTypeName == null)
                return;

            var providersElement = EditConfiguration.CreateSection(xml, "sensenet/providers");

            var providerElement = xml.CreateElement("add");
            providersElement.AppendChild(providerElement);

            providerElement.SetAttribute("key", providerName);
            providerElement.SetAttribute("value", providerTypeName.Split(',')[0].Trim());
        }
        private void DeleteElement(XmlElement element)
        {
            element?.ParentNode?.RemoveChild(element);
        }


        private IEnumerable<string> ResolvePaths(string path, ExecutionContext context)
        {
            var resolvedPath = (string)context.ResolveVariable(path);

            return PathIsRelativeTo == PathRelativeTo.Package
                ? new[] { ResolvePackagePath(resolvedPath, context) }
                : ResolveAllTargets(resolvedPath, context);
        }
    }
}
