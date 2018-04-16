using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps
{
    public class ChangeContentType : Step
    {
        /// <summary>Content query for collecting content to be changed.</summary>
        [Annotation("Content query for collecting content to be changed.")]
        public string ContentQuery { get; set; }

        /// <summary>Name of the target content type.</summary>
        [Annotation("Name of the target content type.")]
        public string ContentTypeName { get; set; }

        public IEnumerable<XmlElement> FieldMapping { get; set; }
        private Dictionary<string, Dictionary<string, string>> _fieldMapping;

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if (string.IsNullOrEmpty(ContentQuery) || string.IsNullOrEmpty(ContentTypeName))
                throw new PackagingException(SR.Errors.InvalidParameters);

            var ct = ContentType.GetByName(ContentTypeName);
            if (ct == null)
                throw new PackagingException("Unknown content type: " + ContentTypeName);

            _fieldMapping = ParseMapping(FieldMapping, ct);

            var count = 0;

            foreach (var sourceContent in Search.ContentQuery.Query(ContentQuery).Nodes.Select(Content.Create))
            {
                try
                {
                    if (sourceContent.Children.Any())
                        throw new PackagingException("Cannot change the type of a content that has children. Path: " + sourceContent.Path);

                    var targetName = Guid.NewGuid().ToString();
                    var parent = sourceContent.ContentHandler.Parent;
                    var targetContent = Content.CreateNew(ContentTypeName, parent, targetName);

                    // copy fields (skip Name and all the missing fields)
                    CopyFields(sourceContent, targetContent);

                    // save the new content
                    targetContent.Save();

                    // delete the original
                    sourceContent.ForceDelete();

                    // rename the target
                    targetContent["Name"] = sourceContent.Name;
                    targetContent.Save(SavingMode.KeepVersion);

                    count++;

                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }

            Logger.LogMessage("{0} content were changed to be {1}.", count, ContentTypeName);
        }

        private void CopyFields(Content source, Content target)
        {
            var availableFieldNames = target.Fields.Keys.ToArray();

            foreach (var field in source.Fields.Values)
            {
                if (field.Name == "Name")
                    continue;
                var targetFieldName = TranslateFieldName(field.Content.ContentType.Name, field.Name, availableFieldNames, _fieldMapping);
                if(targetFieldName != null)
                    target[targetFieldName] = field.GetData(false);
            }
        }

        private string TranslateFieldName(string sourceContentTypeName, string fieldName, string[] availableTargetNames, Dictionary<string, Dictionary<string, string>> mapping)
        {
            if (!mapping.TryGetValue(sourceContentTypeName, out var fields))
                mapping.TryGetValue("", out fields);

            if(fields != null)
                if (fields.TryGetValue(fieldName, out var mappedFieldName))
                    return mappedFieldName;

            return availableTargetNames.Contains(fieldName) ? fieldName : null;
        }

        private Dictionary<string, Dictionary<string, string>> ParseMapping(IEnumerable<XmlElement> fieldMapping, ContentType targetType)
        {
            const string typeElementName = "ContentType";
            const string fieldElementName = "Field";
            const string defaultTypeName = "";

            var targetFieldNames = targetType.FieldSettings.Select(f => f.Name).ToArray();

            var types = new Dictionary<string, Dictionary<string, string>>();

            // parses field element and add to mappings
            // ReSharper disable once SuggestBaseTypeForParameter
            void AddMapping(XmlElement fieldElement, string typeName)
            {
                var source = fieldElement.Attributes["source"].Value;
                var target = fieldElement.Attributes["target"].Value;
                if(!targetFieldNames.Contains(target))
                    throw new InvalidStepParameterException($"The {target} is not a field of the {targetType.Name} content type.");

                if (!types.TryGetValue(typeName, out Dictionary<string, string> fields))
                {
                    fields = new Dictionary<string, string>();
                    types.Add(typeName, fields);
                }

                fields[source] = target;
            }

            // Parse root elements. These can be: ContentType or Field
            foreach (var typeElement in fieldMapping)
            {
                switch (typeElement.LocalName)
                {
                    case typeElementName:
                        var typeName = typeElement.Attributes["name"].Value;
                        foreach (XmlElement element in typeElement.ChildNodes)
                        {
                            if(element.LocalName != fieldElementName)
                                throw new InvalidStepParameterException($"Invalid child element in the FieldMapping/ContentType. Expected: <{fieldElementName} source='' target=''>.");
                            AddMapping(element, typeName);
                        }
                        break;
                    case fieldElementName:
                        AddMapping(typeElement, defaultTypeName);
                        break;
                    default:
                        throw new InvalidStepParameterException($"Unknown element in the FieldMapping: {typeElement.LocalName}. Expected:<{typeElementName} name=''> or <{fieldElementName} source='' target=''>.");
                }
            }
            return types;
        }
    }
}
