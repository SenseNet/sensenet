using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.ContentRepository;

namespace SenseNet.Packaging.Steps
{
    public abstract class XmlEditorStep : Step
    {
        [XmlFragment]
        [Annotation("Content can be any xml elements")]
        public string Source { get; set; }
        public virtual string Xpath { get; set; }

        public string File { get; set; }
        public string Content { get; set; }
        public string Field { get; set; }
        public PathRelativeTo PathIsRelativeTo { get; set; } = PathRelativeTo.TargetDirectory;

        public override void Execute(ExecutionContext context)
        {
            if (!string.IsNullOrEmpty(File) && string.IsNullOrEmpty(Content) && string.IsNullOrEmpty(Field))
                ExecuteOnFile(context);
            else if (string.IsNullOrEmpty(File) && !string.IsNullOrEmpty(Content))
                ExecuteOnContent(context);
            else
                throw new PackagingException(SR.Errors.InvalidParameters);
        }
        private void ExecuteOnContent(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var path = (string)context.ResolveVariable(Content);
            var content = SenseNet.ContentRepository.Content.Load(path);
            var data = content[Field ?? "Binary"];
            SenseNet.ContentRepository.Storage.BinaryData binaryData = null;
            var xmlSrc = data as string;
            if (xmlSrc == null)
            {
                binaryData = data as SenseNet.ContentRepository.Storage.BinaryData;
                if (binaryData != null)
                {
                    using (var r = new System.IO.StreamReader(binaryData.GetStream()))
                        xmlSrc = r.ReadToEnd();
                }
                else
                {
                    //TODO: empty stream: handle by step config (default: throw)
                }
            }

            var doc = new XmlDocument();
            doc.LoadXml(xmlSrc);

            // check if the original xml contains a declaration header
            var omitXmlDeclaration = doc.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault() == null;

            EditXml(doc, content.Path);

            // we format the xml first (indented)
            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = omitXmlDeclaration }))
                doc.Save(writer);

            if (binaryData != null)
                binaryData.SetStream(RepositoryTools.GetStreamFromString(sb.ToString()));
            else
                content[Field] = sb.ToString();

            content.Save();
        }
        private void ExecuteOnFile(ExecutionContext context)
        {
            foreach (var path in ResolvePaths(File, context))
            {
                string xmlSrc = null;
                using (var reader = new System.IO.StreamReader(path))
                    xmlSrc = reader.ReadToEnd();

                var doc = new XmlDocument();
                doc.LoadXml(xmlSrc);

                // check if the original xml contains a declaration header
                var omitXmlDeclaration = doc.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault() == null;

                if (!EditXml(doc, path))
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

        protected abstract bool EditXml(XmlDocument doc, string path);

        protected IEnumerable<string> ResolvePaths(string path, ExecutionContext context)
        {
            var resolvedPath = (string)context.ResolveVariable(path);

            return PathIsRelativeTo == PathRelativeTo.Package
                ? new[] { ResolvePackagePath(resolvedPath, context) }
                : ResolveAllTargets(resolvedPath, context);
        }
    }

    public class AppendXmlFragment : XmlEditorStep
    {
        protected override bool EditXml(XmlDocument doc, string path)
        {
            var edited = 0;
            var skipped = 0;

            foreach (var node in SelectXmlNodes(doc, this.Xpath))
            {
                var element = node as XmlElement;
                if (element != null)
                {
                    element.InnerXml += this.Source;
                    edited++;
                }
                else
                {
                    skipped++;
                }
            }

            string msg;
            switch (edited)
            {
                case 0: msg = "No element"; break;
                case 1: msg = "One element"; break;
                default: msg = edited.ToString() + " elements are"; break;
            }
            Logger.LogMessage("{0} changed. XPath: {1}. Path: {2}", msg, this.Xpath, path);

            if (skipped != 0)
                Logger.LogMessage(
                    "{0} cannot be changed because {1} not element. XPath: {2}. Path: {3}",
                    skipped == 1 ? "One node" : (skipped.ToString() + " nodes"),
                    skipped == 1 ? "it is" : "they are",
                    this.Xpath, path);

            return edited > 0;
        }
    }

    public class AppendXmlAttributes : XmlEditorStep
    {
        [DefaultProperty]
        [Annotation("Content can be any xml elements")]
        public new string Source { get; set; }

        protected override bool EditXml(XmlDocument doc, string path)
        {
            var edited = 0;
            var skipped = 0;

            var attrs = ParseAttributes();

            foreach (var node in SelectXmlNodes(doc, this.Xpath))
            {
                var element = node as XmlElement;
                if (element != null)
                {
                    foreach (var item in attrs)
                        element.SetAttribute(item.Key, item.Value);
                    edited++;
                }
                else
                {
                    skipped++;
                }
            }

            string msg;
            switch (edited)
            {
                case 0: msg = "No element"; break;
                case 1: msg = "One element"; break;
                default: msg = edited.ToString() + " elements are"; break;
            }
            Logger.LogMessage("{0} changed. XPath: {1}. Path: {2}", msg, this.Xpath, path);

            if (skipped != 0)
                Logger.LogMessage(
                    "{0} cannot be changed because {1} not element. XPath: {2}. Path: {3}",
                    skipped == 1 ? "One node" : (skipped.ToString() + " nodes"),
                    skipped == 1 ? "it is" : "they are",
                    this.Xpath, path);

            return edited > 0;
        }

        private Dictionary<string, string> ParseAttributes()
        {
            var result = new Dictionary<string, string>();
            var srcObject = Newtonsoft.Json.Linq.JObject.Parse(this.Source);
            foreach (var item in srcObject.Properties())
                result.Add(item.Name, item.Value.ToString());

            return result;
        }
    }

    public class EditXmlNodes : XmlEditorStep
    {
        protected override bool EditXml(XmlDocument doc, string path)
        {
            var edited = 0;
            var skipped = 0;

            foreach (XmlNode node in SelectXmlNodes(doc, this.Xpath))
            {
                var attr = node as XmlAttribute;
                if (attr != null)
                {
                    attr.Value = this.Source;
                    edited++;
                    continue;
                }

                var element = node as XmlElement;
                if (element != null)
                {
                    element.InnerXml = this.Source;
                    edited++;
                    continue;
                }

                skipped++;
            }

            string msg;
            switch (edited)
            {
                case 0: msg = "No node"; break;
                case 1: msg = "One node"; break;
                default: msg = edited.ToString() + " nodes are"; break;
            }
            Logger.LogMessage("{0} changed. XPath: {1}. Path: {2}", msg, this.Xpath, path);

            if(skipped != 0)
                Logger.LogMessage(
                    "{0} cannot be changed because {1} not attribute or element. XPath: {2}. Path: {3}",
                    skipped == 1 ? "One node" : (skipped.ToString() + " nodes"),
                    skipped == 1 ? "it is" : "they are",
                    this.Xpath, path);

            return edited > 0;
        }
    }

    public class DeleteXmlNodes : XmlEditorStep
    {
        protected override bool EditXml(XmlDocument doc, string path)
        {
            var deleted = 0;
            var skipped = 0;

            foreach (XmlNode node in SelectXmlNodes(doc, this.Xpath))
            {
                if (node.ParentNode != null)
                {
                    node.ParentNode.RemoveChild(node);
                    deleted++;
                }
                else
                {
                    skipped++;
                }
            }

            string msg;
            switch (deleted)
            {
                case 0: msg = "No node"; break;
                case 1: msg = "One node"; break;
                default: msg = deleted.ToString() + " nodes are"; break;
            }
            Logger.LogMessage("{0} deleted. XPath: {1}. Path: {2}", msg, this.Xpath, path);

            if (skipped != 0)
                Logger.LogMessage(
                    "{0} cannot be deleted. XPath: {1}. Path: {2}",
                    skipped == 1 ? "One node" : (skipped.ToString() + " nodes"),
                    this.Xpath, path);

            return deleted > 0;
        }
    }

}
