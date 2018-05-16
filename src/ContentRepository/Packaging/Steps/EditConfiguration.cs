﻿using System.Collections.Generic;
using System.Linq;
using System.Xml;
// ReSharper disable PossibleNullReferenceException

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps
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

        private static readonly string[] BuiltinSections = {"connectionStrings", "appSettings", "applicationSettings"};

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
                string xmlSrc;
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
            if(Move == null)
                return new MoveOperation[0];

            return Move.Select((e) =>
            {
                var op = new MoveOperation
                {
                    SourceSection = GetAttributeValue(e, "sourceSection", true).Trim().Trim('/'),
                    TargetSection = GetAttributeValue(e, "targetSection", true).Trim().Trim('/'),
                    SourceKey = GetAttributeValue(e, "sourceKey", false),
                    TargetKey = GetAttributeValue(e, "targetKey", false),
                    DeleteIfValueIs = GetAttributeValue(e, "deleteIfValueIs", false),
                };

                if (string.IsNullOrEmpty(op.SourceKey))
                    throw new InvalidStepParameterException("Invalid Move. The 'sourceKey' is required.");

                if (op.SourceKey == "*")
                {
                    if (!string.IsNullOrEmpty(op.TargetKey))
                        throw new InvalidStepParameterException("Invalid Move. The 'sourceKey' cannot be '*' if the 'targetKey' is given.");
                    if (!string.IsNullOrEmpty(op.DeleteIfValueIs))
                        throw new InvalidStepParameterException("Invalid Move. The 'sourceKey' cannot be '*' if the 'deleteIfValueIs' is given.");
                }

                return op;
            }).ToArray();
        }
        private DeleteOperation[] ParseDeleteElements()
        {
            if (Delete == null)
                return new DeleteOperation[0];

            return Delete.Select(e => new DeleteOperation
            {
                Section = GetAttributeValue(e, "section", true).Trim().Trim('/'),
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
            Logger.LogMessage("Edit " + path);

            if (deletes != null && deletes.Length > 0)
            {
                Logger.LogMessage("  DELETE");
                foreach (var delete in deletes)
                    if (!ExecuteDelete(xml, delete))
                        return false;
            }
            if (moves != null && moves.Length > 0)
            {
                Logger.LogMessage("  MOVE");
                foreach (var move in moves)
                    if (!ExecuteMove(xml, move))
                        return false;
            }
            return true;
        }

        private bool ExecuteDelete(XmlDocument xml, DeleteOperation delete)
        {
            var sourceSectionElement = (XmlElement)xml.DocumentElement.SelectSingleNode(delete.Section);
            if (sourceSectionElement == null)
                return true;

            if (delete.Key != null)
            {
                Logger.LogMessage($"    KEY: {delete.Section}/{delete.Key}");
                var sourceElement = (XmlElement)sourceSectionElement.SelectSingleNode($"add[@key='{delete.Key}']");
                sourceElement?.ParentNode.RemoveChild(sourceElement);
                return true;
            }

            // empty the section
            Logger.LogMessage($"    SECTION: {delete.Section}");
            var children = sourceSectionElement.SelectNodes("*");
            foreach(XmlElement child in children)
                child.ParentNode.RemoveChild(child);

            DeleteSection(sourceSectionElement, delete.Section);

            return true;
        }

        private bool ExecuteMove(XmlDocument xml, MoveOperation move)
        {
            var sourceSectionElement = (XmlElement) xml.DocumentElement.SelectSingleNode(move.SourceSection);
            if (sourceSectionElement == null)
                return true;

            XmlElement targetSectionElement;

            if (move.SourceKey != "*")
            {
                // move element
                var sourceElement = (XmlElement) sourceSectionElement.SelectSingleNode($"add[@key='{move.SourceKey}']");
                if (sourceElement == null)
                    return true;

                if (move.DeleteIfValueIs != null && sourceElement.Attributes["value"].Value == move.DeleteIfValueIs)
                {
                    Logger.LogMessage($"    KEY DELETED: {move.SourceSection}/{move.SourceKey} VALUE: {move.DeleteIfValueIs}");
                    sourceElement.ParentNode.RemoveChild(sourceElement);
                    return true;
                }

                var msg = $"    KEY: {move.SourceSection}/{move.SourceKey} TO {move.TargetSection}";
                if (move.TargetKey != null)
                {
                    // rename
                    msg += $" NAME: {move.TargetKey}";
                    sourceElement.SetAttribute("key", move.TargetKey);
                }

                targetSectionElement = (XmlElement)xml.DocumentElement.SelectSingleNode(move.TargetSection)
                                           ?? CreateSection(xml, move.TargetSection);

                Logger.LogMessage(msg);

                MoveElement(sourceElement, targetSectionElement);
                return true;
            }


            // move the whole section
            var sourceElements = sourceSectionElement.SelectNodes("*");
            targetSectionElement = (XmlElement)xml.DocumentElement.SelectSingleNode(move.TargetSection)
                                    ?? CreateSection(xml, move.TargetSection);

            foreach (XmlElement sourceElement in sourceElements)
                MoveElement(sourceElement, targetSectionElement);

            DeleteSection(sourceSectionElement, move.SourceSection);

            return true;
        }

        private void DeleteSection(XmlElement sectionElement, string sectionPath)
        {
            DeleteElementIfEmpty(sectionElement);

            var steps = sectionPath.Split('/');
            var lastName = steps[steps.Length - 1];

            var xpath = "configSections/"
                + string.Join("/", steps
                    .Take(steps.Length - 1)
                    .Select(n => $"sectionGroup[@name='{n}']")
                    .ToArray())
                + (steps.Length > 1 ? "/" : "")
                + $"section[@name='{lastName}']";

            var xml = sectionElement.OwnerDocument;
            DeleteElementIfEmpty((XmlElement) xml.DocumentElement.SelectSingleNode(xpath));
        }
        private void DeleteElementIfEmpty(XmlElement deletable)
        {
            while (deletable.LocalName != "configuration" && deletable.SelectNodes("*").Count == 0)
            {
                Logger.LogMessage($"    ELEMENT: {deletable.OuterXml}");
                var parent = (XmlElement)deletable.ParentNode;
                parent.RemoveChild(deletable);
                deletable = parent;
            }
        }

        private void MoveElement(XmlElement sourceElement, XmlElement targetSectionElement)
        {
            // remove old element if exists
            var sourceKey = sourceElement.Attributes["key"].Value;
            var oldElement = targetSectionElement.SelectSingleNode($"add[@key='{sourceKey}']");
            if (oldElement != null && oldElement != sourceElement)
            {
                Logger.LogMessage("  Rewritten element in {0}", GetPath(targetSectionElement));
                Logger.LogMessage("    {0}", oldElement.OuterXml);
                oldElement.ParentNode.RemoveChild(oldElement);
            }

            targetSectionElement.AppendChild(sourceElement.ParentNode.RemoveChild(sourceElement));
        }

        private string GetPath(XmlElement sectionElement)
        {
            if (sectionElement == null)
                return "/";
            return $"{GetPath(sectionElement.ParentNode as XmlElement)}/{sectionElement.LocalName}";
        }

        internal static XmlElement CreateSection(XmlDocument xml, string sectionPath)
        {
            var steps = sectionPath.Split('/');
            var lastName = steps[steps.Length - 1];

            if (!BuiltinSections.Contains(sectionPath))
            {
                var configSections = (XmlElement) xml.DocumentElement.SelectSingleNode("configSections");
                if (configSections == null)
                {
                    Logger.LogMessage("    CREATE configSections");
                    configSections = xml.CreateElement("configSections");
                    xml.DocumentElement.InsertBefore(configSections, xml.DocumentElement.FirstChild);
                }

                // ensure configSections
                var sectionRoot = configSections;
                if (steps.Length > 1)
                {
                    for (var i = 0; i < steps.Length - 1; i++)
                    {
                        var sectionGroup =
                            (XmlElement) sectionRoot.SelectSingleNode($"sectionGroup[@name='{steps[i]}']");
                        if (sectionGroup == null)
                        {
                            Logger.LogMessage($"    CREATE sectionGroup: {steps[i]}");
                            sectionGroup = xml.CreateElement("sectionGroup");
                            sectionGroup.SetAttribute("name", steps[i]);
                            sectionRoot.AppendChild(sectionGroup);
                        }
                        sectionRoot = sectionGroup;
                    }
                }

                // ensure section definition
                var sectionDef = (XmlElement) sectionRoot.SelectSingleNode($"section[@name='{lastName}']");
                if (sectionDef == null)
                {
                    Logger.LogMessage($"    CREATE section definition: {lastName}");
                    sectionDef = xml.CreateElement("section");
                    sectionDef.SetAttribute("name", lastName);
                    sectionDef.SetAttribute("type", "System.Configuration.NameValueFileSectionHandler");
                    sectionRoot.AppendChild(sectionDef);
                }
            }

            // create sections
            var section = xml.DocumentElement;
            foreach (string name in steps)
            {
                var childSection = (XmlElement)section.SelectSingleNode(name);
                if (childSection == null)
                {
                    Logger.LogMessage($"    CREATE section: {name}");
                    childSection = xml.CreateElement(name);
                    section.AppendChild(childSection);
                }
                section = childSection;
            }

            return section;
        }
    }
}
