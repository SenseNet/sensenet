using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SNCS = SenseNet.ContentRepository.Schema;

namespace SenseNet.Packaging.Steps
{
    [Annotation("Checks the index integrity by comparation the index and database.")]
    public class AllowChildTypes : EditContentType
    {
        [DefaultProperty]
        [Annotation("Comma separated content type names to be allowed")]
        public string ChildTypes { get; set; }

        [Annotation("Repository path of the content to be modified")]
        public string Path { get; set; }

        public override void Execute(ExecutionContext context)
        {
            Path = GetNormalizedStringValue(Path, context);
            ContentType = GetNormalizedStringValue(ContentType, context);
            ChildTypes = GetNormalizedStringValue(ChildTypes, context);

            if (Path != null && ContentType != null)
                throw new SnNotSupportedException("Path and ContentType is not allowed together.");
            if (Path == null && ContentType == null)
                throw new SnNotSupportedException("Path and ContentType is not allowed together.");

            if (ChildTypes == null)
            {
                Logger.LogMessage(@"ChildTypes is not provided.");
                return;
            }

            context.AssertRepositoryStarted();

            var newChildTypes = ChildTypes.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToArray();

            if (Path != null)
                ExecuteOnContent(Path.Trim(), newChildTypes);
            else
                ExecuteOnContentType(ContentType.Trim(), newChildTypes);
        }

        private string GetNormalizedStringValue(string value, ExecutionContext context)
        {
            value = context.ResolveVariable(value) as string;
            return value?.Length == 0 ? null : value;
        }

        private void ExecuteOnContent(string path, string[] newContentTypes)
        {
            var content = Content.Load(path);
            if (content == null)
            {
                Logger.LogMessage("Content does not exist: {0}", path);
                return;
            }

            var gc = content.ContentHandler as GenericContent;
            if (gc == null)
            {
                Logger.LogMessage("Content does not support AllowedContentTypes", path);
                return;
            }

            var newChiltTypeNames = gc.GetAllowedChildTypeNames()
                    .Union(newContentTypes)
                    .Distinct()
                    .ToArray();

            gc.AllowChildTypes(newChiltTypeNames);
            gc.Save();
        }

        private void ExecuteOnContentType(string contentTypeName, string[] newContentTypes)
        {
            var ct = SNCS.ContentType.GetByName(contentTypeName);
            var newChiltTypeNames = string.Join(",",
                ct.AllowedChildTypeNames
                    .Union(newContentTypes)
                    .Distinct()
                    .ToArray());

            var xDoc = LoadContentTypeXmlDocument();

            var propertyElement = LoadOrAddChild(xDoc.DocumentElement, "AllowedChildTypes");
            propertyElement.InnerXml = newChiltTypeNames;

            SNCS.ContentTypeInstaller.InstallContentType(xDoc.OuterXml);
        }
    }
}
