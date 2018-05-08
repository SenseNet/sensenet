using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.JScript;
using SenseNet.Packaging;
using SenseNet.Packaging.Steps;

namespace SenseNet.ContentRepository.Packaging.Steps
{
    [Annotation("Organizes (moves and/or deletes) one or more elements in the configuration files.")]
    public class EditConfiguration : Step
    {
        internal class MoveOperation
        {
            public string SourceSection { get; set; }
            public string SourceKey { get; set; }
            public string TargetSection { get; set; }
            public string TargetKey { get; set; }
            public string DeleteIfValueIs { get; set; }
        }
        internal class DeleteOperation
        {
            public string Section { get; set; }
            public string Key { get; set; }
        }

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

        public IEnumerable<XmlElement> Move { get; set; }
        public IEnumerable<XmlElement> Delete { get; set; }

        public override void Execute(ExecutionContext context)
        {
            var moves = ParseMoveElements();
            var deletes = ParseDeleteElements();

            foreach (var path in ResolvePaths(File, context))
            {
                string xmlSrc = null;
                using (var reader = new System.IO.StreamReader(path))
                    xmlSrc = reader.ReadToEnd();

                var doc = new XmlDocument();
                doc.LoadXml(xmlSrc);

                // check if the original xml contains a declaration header
                var omitXmlDeclaration = doc.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault() == null;

                if (!Edit(doc, moves, deletes, path))
                    return;

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

        private IEnumerable<string> ResolvePaths(string path, ExecutionContext context)
        {
            var resolvedPath = (string)context.ResolveVariable(path);

            return PathIsRelativeTo == PathRelativeTo.Package
                ? new[] { ResolvePackagePath(resolvedPath, context) }
                : ResolveAllTargets(resolvedPath, context);
        }
        private MoveOperation[] ParseMoveElements()
        {
            return Move.Select((e) =>
            {
                var op = new MoveOperation
                {
                    SourceSection = GetAttributeValue(e, "sourceSection", true),
                    TargetSection = GetAttributeValue(e, "targetSection", true),
                    SourceKey = GetAttributeValue(e, "sourceKey", false),
                    TargetKey = GetAttributeValue(e, "targetKey", false),
                    DeleteIfValueIs = GetAttributeValue(e, "deleteIfValueIs", false),
                };
                if (string.IsNullOrEmpty(op.SourceKey))
                {
                    if (!string.IsNullOrEmpty(op.TargetKey))
                        throw new InvalidStepParameterException("Invalid Move. The 'sourceKey' is required if the 'targetKey' is given.");
                    if (!string.IsNullOrEmpty(op.DeleteIfValueIs))
                        throw new InvalidStepParameterException("Invalid Move. The 'sourceKey' is required if the 'defaultValue' is given.");
                }
                return op;
            }).ToArray();
        }
        private DeleteOperation[] ParseDeleteElements()
        {
            return Delete.Select(e => new DeleteOperation
            {
                Section = GetAttributeValue(e, "section", true),
                Key = GetAttributeValue(e, "key", false),
            }).ToArray();
        }
        private string GetAttributeValue(XmlElement element, string attrName, bool required)
        {
            var attr = element.Attributes[attrName];
            if (attr != null)
                return attr.Value;
            if (!required)
                return null;
            throw new InvalidStepParameterException($"Invalid {element.ParentNode.LocalName}. Missing '{attrName}'.");
        }

        internal bool Edit(XmlDocument xml, MoveOperation[] moves, DeleteOperation[] deletes, string path)
        {
            if(deletes != null)
                foreach (var delete in deletes) ;
                
            foreach (var move in moves)
            {
                if (!ExecuteMove(xml, move))
                    return false;
            }
            return true;
        }

        private bool ExecuteMove(XmlDocument xml, MoveOperation move)
        {
            var sourceSectionElement = (XmlElement)xml.DocumentElement.SelectSingleNode(move.SourceSection);
            var targetSectionElement = (XmlElement)xml.DocumentElement.SelectSingleNode(move.TargetSection);
            if (move.SourceKey != null)
            {
                // move element
                var sourceElement = (XmlElement) sourceSectionElement.SelectSingleNode($"add[@key='{move.SourceKey}']");
                if (move.TargetKey != null)
                    // rename
                    sourceElement.SetAttribute("key", move.TargetKey);

                // ensure section element
                if (targetSectionElement == null)
                    targetSectionElement = CreateSection(xml, move.TargetSection);

                // move
                var moved = sourceElement.ParentNode.RemoveChild(sourceElement);
                targetSectionElement.AppendChild(moved);
                return true;
            }
            else
            {
                // move the whole section
                //UNDONE: Move section
                throw new NotImplementedException();
            }
        }

        private XmlElement CreateSection(XmlDocument xml, string sectionPath)
        {
            // path = sectionsA/section1
            // <configuration>
            //   <configSections>
            //     <sectionGroup name='sectionsA'>
            //       <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
            //     </sectionGroup>
            //   </configSections>
            //   <sectionsA>
            //     <section1>

            var configSections = (XmlElement) xml.DocumentElement.SelectSingleNode("configSections");
            if (configSections == null)
            {
                configSections = xml.CreateElement("configSections");
                xml.DocumentElement.InsertBefore(configSections, xml.DocumentElement.FirstChild);
            }

            // ensure configSections
            var sectionRoot = configSections;
            var steps = sectionPath.Split('/');
            var lastName = steps[steps.Length - 1];
            if (steps.Length > 1)
            {
                for (var i = 0; i < steps.Length - 1; i++)
                {
                    var sectionGroup = (XmlElement)sectionRoot.SelectSingleNode(steps[i]);
                    if (sectionGroup == null)
                    {
                        sectionGroup = xml.CreateElement("sectionGroup");
                        sectionGroup.SetAttribute("name", steps[i]);
                        sectionRoot.AppendChild(sectionGroup);
                        sectionRoot = sectionGroup;
                    }
                }
            }

            // ensure section definition
            var sectionDef = (XmlElement)sectionRoot.SelectSingleNode(lastName);
            if (sectionDef == null)
            {
                sectionDef = xml.CreateElement("section");
                sectionDef.SetAttribute("name", lastName);
                sectionDef.SetAttribute("type", "System.Configuration.NameValueFileSectionHandler");
                sectionRoot.AppendChild(sectionDef);
            }

            // create sections
            var section = xml.DocumentElement;
            for (int i = 0; i < steps.Length; i++)
            {
                var childSection = (XmlElement)section.SelectSingleNode("steps[i]");
                if (childSection == null)
                {
                    childSection = xml.CreateElement(steps[i]);
                    section.AppendChild(childSection);
                    section = childSection;
                }
            }

            return section;
        }
    }
}
