using System.Xml;
using SNCR = SenseNet.ContentRepository;

namespace SenseNet.Packaging.Steps
{
    public class IfXmlNodeExists : ConditionalStep
    {
        public string Xpath { get; set; }

        public string File { get; set; }
        public string Content { get; set; }
        public string Field { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            if (!string.IsNullOrEmpty(File) && string.IsNullOrEmpty(Content) && string.IsNullOrEmpty(Field))
                return EvaluateOnFile(context);
            if (string.IsNullOrEmpty(File) && !string.IsNullOrEmpty(Content))
                return EvaluateOnContent(context);
            
            throw new PackagingException(SR.Errors.InvalidParameters);
        }

        private bool EvaluateOnContent(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var content = SNCR.Content.Load(Content);
            if (content == null)
                throw new PackagingException("Content not found: " + Content);

            var data = content[Field ?? "Binary"];
            var xmlSrc = data as string;
            if (xmlSrc == null)
            {
                var binaryData = data as SNCR.Storage.BinaryData;
                if (binaryData != null)
                {
                    using (var r = new System.IO.StreamReader(binaryData.GetStream()))
                        xmlSrc = r.ReadToEnd();
                }
            }

            if (xmlSrc == null)
                throw new PackagingException("Xml value is empty: " + Content);

            var doc = new XmlDocument();
            doc.LoadXml(xmlSrc);

            return SelectXmlNodes(doc, Xpath).Count > 0;
        }

        private bool EvaluateOnFile(ExecutionContext context)
        {
            // Check the existence of a local path. We assume that if the file exists
            // locally, it will be present on all other web servers. If not, the 
            // child steps of this conditional step will fail anyway.
            var filePath = ResolveTargetPath(File, context);
            if (!System.IO.File.Exists(filePath))
                throw new PackagingException("File not found: " + filePath);

            var doc = new XmlDocument();
            doc.Load(filePath);

            return SelectXmlNodes(doc, Xpath).Count > 0;
        }
    }
}
