using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Steps
{
    public class AddReference : SetField
    {
        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            Content content;
            Dictionary<string, string> fieldValues;
            bool overwrite;

            ParseParameters(context, out content, out fieldValues, out overwrite);

            Logger.LogMessage($"Updating: {content.Path}");

            var xDoc = GetFieldXmlDocument(fieldValues);
            var node = content.ContentHandler;

            foreach (var fieldName in fieldValues.Keys)
            {
                var fieldNode = xDoc.DocumentElement.SelectSingleNode($"//{fieldName}");

                if (!(content.Fields[fieldName] is ReferenceField))
                    throw new InvalidStepParameterException($"{fieldName} is not a reference field.");

                var references = fieldNode.ChildNodes.Cast<XmlNode>()
                    .Select(x => Node.LoadNodeByIdOrPath(x.InnerText.Trim())).Where(n => n != null);

                node.AddReferences(fieldName, references);
            }

            // wrap the node into a content just to make saving the same version easier
            var editedContent = ContentRepository.Content.Create(node);
            editedContent.SaveSameVersion();
        }
    }
}
