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
    [Annotation("Allows or disallows child types of a content.")]
    public class EditAllowedChildTypes : EditContentType
    {
        [DefaultProperty]
        [Annotation("Comma separated content type names to be added to allowed list")]
        public string Add { get; set; }

        [Annotation("Comma separated content type names to be removed to allowed list")]
        public string Remove { get; set; }

        [Annotation("Repository path of the content to be modified")]
        public string Path { get; set; }

        public override void Execute(ExecutionContext context)
        {
            Path = GetNormalizedStringValue(Path, context);
            ContentType = GetNormalizedStringValue(ContentType, context);
            var newTypes = GetNormalizedStringValue(Add, context);
            var oldTypes = GetNormalizedStringValue(Remove, context);

            if (Path != null && ContentType != null)
                throw new SnNotSupportedException("Path and ContentType is not allowed together.");
            if (Path == null && ContentType == null)
                throw new SnNotSupportedException("Path and ContentType cannot be empty together.");

            if (string.IsNullOrEmpty(newTypes) && string.IsNullOrEmpty(oldTypes))
            {
                Logger.LogMessage(@"There is no any modificaton.");
                return;
            }

            context.AssertRepositoryStarted();

            if (Path != null)
                ExecuteOnContent(Path.Trim(), newTypes, oldTypes);
            else
                ExecuteOnContentType(ContentType.Trim(), newTypes, oldTypes);
        }

        private string GetNormalizedStringValue(string value, ExecutionContext context)
        {
            value = context.ResolveVariable(value) as string;
            return value?.Length == 0 ? null : value;
        }

        private void ExecuteOnContent(string path, string newTypes, string oldTypes)
        {
            Logger.LogMessage("Edit Content: {0}", path);

            var content = Content.Load(path);
            if (content == null)
            {
                Logger.LogMessage("Content does not exist: {0}", path);
                return;
            }

            if (!(content.ContentHandler is GenericContent gc))
            {
                Logger.LogMessage("Content does not support AllowedContentTypes", path);
                return;
            }

            var newChiltTypeNames = GetEditedList( gc.GetAllowedChildTypeNames(), newTypes, oldTypes);

            gc.AllowChildTypes(newChiltTypeNames);
            gc.Save();
        }

        private void ExecuteOnContentType(string contentTypeName, string newTypes, string oldTypes)
        {
            Logger.LogMessage("Edit ContentType: {0}", contentTypeName);

            var ct = SNCS.ContentType.GetByName(contentTypeName);
            var newChiltTypeNames = string.Join(",", GetEditedList(ct.AllowedChildTypeNames, newTypes, oldTypes));

            var xDoc = LoadContentTypeXmlDocument();

            var propertyElement = LoadOrAddChild(xDoc.DocumentElement, "AllowedChildTypes");
            propertyElement.InnerXml = newChiltTypeNames;

            SNCS.ContentTypeInstaller.InstallContentType(xDoc.OuterXml);
        }

        internal static string[] GetEditedList(IEnumerable<string> origList, string newItems, string retiredItems)
        {
            if (origList == null)
                origList = new string[0];

            var addArray = newItems?
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray() ?? new string[0];
            var removeArray = retiredItems?
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray() ?? new string[0];
            var result = origList
                .Union(addArray)
                .Distinct()
                .Except(removeArray)
                .ToArray();

            Logger.LogMessage("Old items: {0}", string.Join(",", origList));
            Logger.LogMessage("New items: {0}", string.Join(",", result));

            return result;
        }
    }
}
