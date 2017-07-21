using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Steps
{
    /// <summary>
    /// Modifies a value of one or more fields on a content.
    /// </summary>
    public class SetField : Step
    {
        [Annotation("Repository path of the content to be edited.")]
        public string Content { get; set; }
        [Annotation("Name of the field to be set.")]
        public string Name { get; set; }

        [Annotation("List of field values to be set.")]
        public string Fields { get; set; }

        [DefaultProperty]
        [Annotation("Field value in the same format as in the import .Content files.")]
        public string Value { get; set; }

        [Annotation("Whether the field should be overwritten if not empty. Default: true")]
        public string Overwrite { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            Content content;
            Dictionary<string, string> fieldValues;
            bool overwrite;

            ParseParameters(context, out content, out fieldValues, out overwrite);
            
            var xDoc = GetFieldXmlDocument(fieldValues);
            
            // ReSharper disable once PossibleNullReferenceException
            var importContext = new ImportContext(xDoc.DocumentElement.ChildNodes, null, false, true, true);
            var changed = false;

            foreach (var fieldName in fieldValues.Keys)
            {
                if (!overwrite && content.Fields[fieldName].HasValue())
                    continue;

                var fieldNode = xDoc.DocumentElement.SelectSingleNode($"//{fieldName}");
                content.Fields[fieldName].Import(fieldNode, importContext);

                changed = true;
            }

            if (changed)
            {
                Logger.LogMessage($"Updating: {content.Path}");
                content.SaveSameVersion();
            }
            else
            {
                Logger.LogMessage($"SKIPPED: {content.Path}");
            }
        }

        protected void ParseParameters(ExecutionContext context, 
            out Content content, 
            out Dictionary<string, string> fieldValues, 
            out bool overwrite)
        {
            if (string.IsNullOrEmpty(Content))
                throw new PackagingException(SR.Errors.InvalidParameters);
            
            var path = context.ResolveVariable(Content) as string;
            if (RepositoryPath.IsValidPath(path) != RepositoryPath.PathResult.Correct)
                throw new PackagingException(SR.Errors.InvalidParameters);

            content = ContentRepository.Content.Load(path);
            if (content == null)
                throw new PackagingException("Content not found: " + path);

            fieldValues = new Dictionary<string, string>();

            // either Field or Fields should be filled, but not both
            if ((string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Fields)) ||
                (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Fields)))
                throw new PackagingException(SR.Errors.InvalidParameters);

            if (!string.IsNullOrEmpty(Name))
            {
                // simple syntax, single field definition
                var fieldName = context.ResolveVariable(Name) as string;
                if (string.IsNullOrEmpty(fieldName) || !content.Fields.ContainsKey(fieldName))
                    throw new PackagingException($"Field '{fieldName}' not found on content {path}");

                fieldValues[fieldName] = context.ResolveVariable(Value) as string;
            }
            else
            {
                // complex syntax, multiple field values are provided
                var xDoc = new XmlDocument();
                xDoc.LoadXml($"<Fields>{context.ResolveVariable(Fields) as string}</Fields>");

                if (xDoc.DocumentElement != null)
                {
                    foreach (XmlNode fieldNode in xDoc.DocumentElement.ChildNodes)
                    {
                        var fieldName = fieldNode?.Attributes?["name"]?.Value;
                        if (string.IsNullOrEmpty(fieldName))
                            throw new InvalidStepParameterException("Field name is missing.");

                        if (!content.Fields.ContainsKey(fieldName))
                            throw new InvalidStepParameterException(
                                $"Content {content.Path} does not have a field with the name {fieldName}.");

                        if (fieldNode.FirstChild == null || fieldNode.ChildNodes.Count > 1)
                            throw new InvalidStepParameterException("Incorrect field xml definition.");

                        fieldValues[fieldName] = fieldNode.FirstChild.InnerXml;
                    }
                }
            }

            overwrite = ParseOverwrite(context);
        }

        protected bool ParseOverwrite(ExecutionContext context)
        {
            var overwrite = true;
            var overwriteValue = context.ResolveVariable(Overwrite);

            if (overwriteValue is bool)
            {
                overwrite = (bool)overwriteValue;
            }
            else
            {
                var overwriteText = overwriteValue as string;

                if (!string.IsNullOrEmpty(overwriteText))
                {
                    bool result;
                    if (bool.TryParse(overwriteText, out result))
                        overwrite = result;
                    else
                        throw new InvalidParameterException("Value could not be converted to bool: " + overwriteText);
                }
            }

            return overwrite;
        }

        /// <summary>
        /// Constructs an xml document from field values in a format that is recognised by the import API.
        /// </summary>
        protected XmlDocument GetFieldXmlDocument(Dictionary<string, string> fieldValues)
        {
            // load the values in a fake xml document
            var xDoc = new XmlDocument();
            xDoc.LoadXml($"<Fields>{string.Join(Environment.NewLine, fieldValues.Select(f => $"<{f.Key}>{f.Value}</{f.Key}>"))}</Fields>");

            if (xDoc.DocumentElement == null)
                throw new PackagingException("Invalid field value xml.");

            return xDoc;
        }
    }
}
