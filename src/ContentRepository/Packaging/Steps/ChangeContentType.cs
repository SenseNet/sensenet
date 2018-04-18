using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps
{
    public class ChangeContentType : Step
    {
        public enum Mode { No, Simulation, Force }

        /// <summary>Content query for collecting content to be changed.</summary>
        [Annotation("Content query for collecting content to be changed.")]
        public string ContentQuery { get; set; }

        /// <summary>Name of the target content type.</summary>
        [Annotation("Name of the target content type.")]
        public string TargetType { get; set; }

        [Annotation("Comma (or space) separated list of source content types. Ignores ContentQuery.")]
        public string SourceType { get; set; }

        [Annotation("Execution mode.")]
        public Mode Change { get; set; }

        public IEnumerable<XmlElement> FieldMapping { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if ((string.IsNullOrEmpty(ContentQuery) && string.IsNullOrEmpty(SourceType)) || string.IsNullOrEmpty(TargetType))
                throw new PackagingException(SR.Errors.InvalidParameters);

            var targetContentType = ContentType.GetByName(TargetType);
            if (targetContentType == null)
                throw new PackagingException("Unknown content type: " + TargetType);

            var fieldMapping = ParseMapping(FieldMapping, targetContentType);

            var count = 0;

            ContentQuery query;
            if (string.IsNullOrEmpty(SourceType))
            {
                query = Search.ContentQuery.CreateQuery(ContentQuery);
            }
            else
            {
                var sourceTypes = SourceType.Split(",; ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                query = Search.ContentQuery.CreateQuery(ContentRepository.SafeQueries.TypeIs, QuerySettings.AdminSettings, sourceTypes);
            }

            if (Change == Mode.No)
                Logger.LogMessage("INFORMATION (database will not changed)");
            else if (Change == Mode.Simulation)
                Logger.LogMessage("SIMULATION (database will not changed)");
            Logger.LogMessage("ContentQuery: " + ContentQuery);
            Logger.LogMessage("SourceType:   " + SourceType);
            Logger.LogMessage("TargetType:   " + TargetType);
            Logger.LogMessage("");

            var result = query.Execute();
            var allCount = result.Count;

            if (Change == Mode.No)
            {
                switch (allCount)
                {
                    case 0:
                        Logger.LogMessage("No item can be changed.", allCount);
                        break;
                    case 1:
                        Logger.LogMessage("1 item's ContentType can be changed.", allCount);
                        break;
                    default:
                        Logger.LogMessage("{0} items' ContentType can be changed.", allCount);
                        break;
                }
                return;
            }

            if (Change == Mode.Simulation)
            {
                var handledItems = new List<int>();
                foreach (var sourceContent in result.Nodes.Select(Content.Create))
                {
                    try
                    {
                        if (!(sourceContent.ContentHandler is GenericContent))
                            throw new PackagingException("Cannot change the type of a content that is ContentType. Path: " + sourceContent.Path);
                        if (sourceContent.Children.Any())
                            throw new PackagingException("Cannot change the type of a content that has children. Path: " + sourceContent.Path);

                        var targetName = Guid.NewGuid().ToString();
                        var parent = (GenericContent)sourceContent.ContentHandler.Parent;

                        Logger.LogMessage($"{count+1}/{allCount}: Change {sourceContent.ContentType.Name}: {sourceContent.Path}");

                        if (parent != null && !parent.GetAllowedChildTypeNames().Contains(targetContentType.Name))
                        {
                            parent.AllowChildType(targetContentType, setOnAncestorIfInherits: true, throwOnError: false, save: true);
                            if (!handledItems.Contains(parent.Id))
                            {
                                handledItems.Add(parent.Id);
                                Logger.LogMessage($"   Allow child type {targetContentType.Name} of the {parent.Path}");
                            }
                        }

                        var targetContent = Content.CreateNew(TargetType, parent, targetName);

                        // copy fields (skip Name and all the missing fields)
                        CopyFields(sourceContent, targetContent, fieldMapping);

                        count++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex);
                    }
                }
                switch (count)
                {
                    case 0:
                        Logger.LogMessage("No item can be changed.", count);
                        break;
                    case 1:
                        Logger.LogMessage("1 item's ContentType can be changed.", count);
                        break;
                    default:
                        Logger.LogMessage("{0} items' ContentType can be changed.", count);
                        break;
                }
                return;
            }

            // force
            foreach (var sourceContent in result.Nodes.Select(Content.Create))
            {
                try
                {
                    if (!(sourceContent.ContentHandler is GenericContent))
                        throw new PackagingException("Cannot change the type of a content that is ContentType. Path: " + sourceContent.Path);
                    if (sourceContent.Children.Any())
                        throw new PackagingException("Cannot change the type of a content that has children. Path: " + sourceContent.Path);

                    var targetName = Guid.NewGuid().ToString();
                    var parent = (GenericContent) sourceContent.ContentHandler.Parent;

                    Logger.LogMessage($"{count + 1}/{allCount}: Change {sourceContent.ContentType.Name}: {sourceContent.Path}");

                    if (parent != null && !parent.GetAllowedChildTypeNames().Contains(targetContentType.Name))
                    {
                        parent.AllowChildTypes(
                            contentTypes: parent.GetAllowedChildTypes().Union(new[] { targetContentType }),
                            setOnAncestorIfInherits: true, throwOnError: false, save: true);
                        Logger.LogMessage($"    Allow child type {targetContentType.Name} of the {parent.Path}");
                    }

                    var targetContent = Content.CreateNew(TargetType, parent, targetName);

                    // copy fields (skip Name and all the missing fields)
                    CopyFields(sourceContent, targetContent, fieldMapping);

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
            switch (count)
            {
                case 0:
                    Logger.LogMessage("No item changed.", count);
                    break;
                case 1:
                    Logger.LogMessage("1 item's ContentType changed.", count);
                    break;
                default:
                    Logger.LogMessage("{0} items' ContentType changed.", count);
                    break;
            }
        }

        private void CopyFields(Content source, Content target, Dictionary<string, Dictionary<string, string>> fieldMapping)
        {
            var availableFieldNames = target.Fields.Keys.ToArray();

            foreach (var field in source.Fields.Values)
            {
                if (field.Name == "Name")
                    continue;
                var targetFieldName = TranslateFieldName(field.Content.ContentType.Name, field.Name, availableFieldNames, fieldMapping);
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

            var types = new Dictionary<string, Dictionary<string, string>>();
            if (fieldMapping == null)
                return types;
            var targetFieldNames = targetType.FieldSettings.Select(f => f.Name).ToArray();


            // parses field element and add to mappings
            // ReSharper disable once SuggestBaseTypeForParameter
            void AddMapping(XmlElement fieldElement, string sourceTypeName, string targetTypeName)
            {
                var source = fieldElement.Attributes["source"].Value;
                var target = fieldElement.Attributes["target"].Value;
                if(!targetFieldNames.Contains(target))
                    throw new InvalidStepParameterException($"The {target} is not a field of the {targetTypeName} content type.");

                if (!types.TryGetValue(sourceTypeName, out Dictionary<string, string> fields))
                {
                    fields = new Dictionary<string, string>();
                    types.Add(sourceTypeName, fields);
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
                            AddMapping(element, typeName, targetType.Name);
                        }
                        break;
                    case fieldElementName:
                        AddMapping(typeElement, defaultTypeName, targetType.Name);
                        break;
                    default:
                        throw new InvalidStepParameterException($"Unknown element in the FieldMapping: {typeElement.LocalName}. Expected:<{typeElementName} name=''> or <{fieldElementName} source='' target=''>.");
                }
            }
            return types;
        }
    }
}
