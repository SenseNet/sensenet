using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.Search;
using SenseNet.Search.Querying;
// ReSharper disable PossibleNullReferenceException

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps
{
    public class DeleteContentType : Step
    {
        public enum Mode { No, IfNotUsed, Force }

        internal class ContentTypeDependencies
        {
            public string ContentTypeName { get; set; }
            public string[] InheritedTypeNames { get; set; }
            public int InstanceCount { get; set; }
            public ContentType[] RelatedContentTypes { get; set; }
            public ReferenceFieldSetting[] RelatedFieldSettings { get; set; }
            public Dictionary<string, string> RelatedContentCollection { get; set; }
            public bool HasDependency => InheritedTypeNames.Length > 0 ||
                                         InstanceCount > 0 ||
                                         RelatedContentTypes.Length > 0 ||
                                         RelatedFieldSettings.Length > 0 ||
                                         RelatedContentCollection.Count > 0;
        }

        [DefaultProperty]
        [Annotation("Name of the content type that will be deleted.")]
        public string Name { get; set; }
        [Annotation("Execution mode.")]
        public Mode Delete { get; set; }

        public override void Execute(ExecutionContext context)
        {
            var name = ResolveVariable(Name, context);

            context.AssertRepositoryStarted();

            var currentContentType = ContentType.GetByName(name);
            if (null == currentContentType)
            {
                Logger.LogMessage("Content type is already deleted: " + name);
                return;
            }

            var dependencies = GetDependencies(currentContentType);

            Logger.LogMessage("DELETING CONTENT TYPE: " + name);

            PrintDependencies(dependencies);


            if (Delete == Mode.No)
            {
                Logger.LogMessage($"The {name} content type is not removed, this step provides only information.");
                return;
            }

            if (Delete == Mode.IfNotUsed && dependencies.HasDependency)
            {
                ContentTypeInstaller.RemoveContentType(name);
                Logger.LogMessage($"The {name} content type has no any depencency.");
                Logger.LogMessage($"The {name} content type removed successfully.");
                return;
            }

            if (dependencies.InstanceCount > 0)
                DeleteInstances(name);
            RemoveAllowedTypes(dependencies);

            ContentTypeInstaller.RemoveContentType(name);
            Logger.LogMessage($"The {name} content type removed successfully.");
        }

        internal ContentTypeDependencies GetDependencies(ContentType currentContentType)
        {
            var name = currentContentType.Name;

            var typeSubtreeQuery = ContentQuery.CreateQuery(ContentRepository.SafeQueries.InTree,
                QuerySettings.AdminSettings, currentContentType.Path);
            var typeSubtreeResult = typeSubtreeQuery.Execute();
            var typeNames = typeSubtreeResult.Nodes.Select(n => n.Name).ToArray();
            var inheritedTypeNames = typeNames.Where(s => s != name).ToArray();

            var queryContext = new SnQueryContext(QuerySettings.AdminSettings, User.Current.Id);
            var contentInstancesCount = SnQuery.Parse($"+TypeIs:{name} .COUNTONLY", queryContext)
                .Execute(queryContext)
                .TotalCount;

            var result = new ContentTypeDependencies
            {
                ContentTypeName = name,
                // +InTree:[currentContentType.Path]
                InheritedTypeNames = inheritedTypeNames,
                // +TypeIs:[name]
                InstanceCount = contentInstancesCount,
                // ContentType/AllowedChildTypes: "Folder,File"
                RelatedContentTypes = GetContentTypesWhereTheyAreAllowed(typeNames),
                // ContentType/Fields/Field/Configuration/AllowedTypes/Type: "Folder"
                RelatedFieldSettings = GetContentTypesWhereTheyAreAllowedInReferenceField(typeNames),
                // ContentMetaData/Fields/AllowedChildTypes: "Folder File"
                RelatedContentCollection = GetContentPathsWhereTheyAreAllowedChildren(typeNames)
            };
            return result;
        }

        private ContentType[] GetContentTypesWhereTheyAreAllowed(string[] names)
        {
            return ContentType.GetContentTypes()
                    .Where(c => !names.Contains(c.Name))
                    .Where(c => c.AllowedChildTypeNames != null && c.AllowedChildTypeNames.Intersect(names).Any())
                    .ToArray();
        }
        private ReferenceFieldSetting[] GetContentTypesWhereTheyAreAllowedInReferenceField(string[] names)
        {
            return ContentType.GetContentTypes().SelectMany(t => t.FieldSettings)
                .Where(f => f is ReferenceFieldSetting).Cast<ReferenceFieldSetting>()
                .Where(r => r.AllowedTypes != null && r.AllowedTypes.Intersect(names).Any())
                .Distinct()
                .ToArray();
        }
        private Dictionary<string, string> GetContentPathsWhereTheyAreAllowedChildren(string[] names)
        {
            var result = new Dictionary<string, string>();

            var whereClausePart = string.Join(Environment.NewLine + "    OR" + Environment.NewLine,
                names.Select(n =>
                    $"    (t.Value like '{n}' OR t.Value like '% {n} %' OR t.Value like '{n} %' OR t.Value like '% {n}')"));

            // testability: the first line is recognizable for the tests.
            var sql = $"-- GetContentPathsWhereTheyAreAllowedChildren: [{string.Join(", ", names)}]" +
                      Environment.NewLine;
            sql += @"SELECT n.Path, t.Value FROM TextPropertiesNVarchar t
	JOIN SchemaPropertyTypes p ON p.PropertyTypeId = t.PropertyTypeId
	JOIN Versions v ON t.VersionId = v.VersionId
	JOIN Nodes n ON n.NodeId = v.NodeId
WHERE p.Name = 'AllowedChildTypes' AND (
" + whereClausePart + @"
)
UNION ALL
SELECT n.Path, t.Value FROM TextPropertiesNText t
	JOIN SchemaPropertyTypes p ON p.PropertyTypeId = t.PropertyTypeId
	JOIN Versions v ON t.VersionId = v.VersionId
	JOIN Nodes n ON n.NodeId = v.NodeId
WHERE p.Name = 'AllowedChildTypes' AND (
" + whereClausePart + @"
)
";

            using (var cmd = DataProvider.CreateDataProcedure(sql))
            {
                cmd.CommandType = CommandType.Text;
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        result.Add(reader.GetString(0), reader.GetString(1));
            }

            return result;
        }

        private void PrintDependencies(ContentTypeDependencies dependencies)
        {
            var names = dependencies.InheritedTypeNames.ToList();
            names.Add(dependencies.ContentTypeName);

            var inheritedTypeNames = dependencies.InheritedTypeNames;
            Logger.LogMessage("  Dependencies: ");
            Logger.LogMessage("    Inherited types: ");
            Logger.LogMessage("      " + (inheritedTypeNames.Length == 0 ? "There is no related item." : string.Join(", ", inheritedTypeNames)));
            Logger.LogMessage(string.Empty);
            Logger.LogMessage("    Content instances: " + dependencies.InstanceCount);
            Logger.LogMessage(string.Empty);

            Logger.LogMessage("    Remaining allowed child types in other content types after deletion:");
            var relevantContentTypes = dependencies.RelatedContentTypes;
            if (0 == relevantContentTypes.Length)
            {
                Logger.LogMessage("      There is no related item.");
            }
            else
            {
                foreach (var contentType in relevantContentTypes)
                {
                    var remaining = string.Join(", ", contentType.AllowedChildTypeNames.Except(names).ToArray());
                    remaining = remaining.Length == 0 ? "[empty]" : remaining;
                    Logger.LogMessage($"      {contentType.Name}: {remaining}");
                }
            }
            Logger.LogMessage(string.Empty);

            Logger.LogMessage("    Remaining allowed child types in reference fields after deletion:");
            var relevantFieldSettings = dependencies.RelatedFieldSettings;
            if (0 == relevantFieldSettings.Length)
            {
                Logger.LogMessage("      There is no related item.");
            }
            else
            {
                foreach (var fieldSetting in relevantFieldSettings)
                {
                    var remaining = string.Join(", ", fieldSetting.AllowedTypes.Except(names).ToArray());
                    remaining = remaining.Length == 0 ? "[empty]" : remaining;
                    Logger.LogMessage($"      {fieldSetting.Owner.Name}.{fieldSetting.Name}: {remaining}");
                }
            }
            Logger.LogMessage(string.Empty);

            Logger.LogMessage("    Remaining allowed child types in content after deletion:");
            var relatedContentCollection = dependencies.RelatedContentCollection;
            if (0 == relatedContentCollection.Count)
            {
                Logger.LogMessage("      There is no related item.");
            }
            else
            {
                foreach (var item in relatedContentCollection)
                {
                    var remaining = string.Join(", ", item.Value.Split(' ').Except(names).ToArray());
                    Logger.LogMessage($"      {item.Key}: {(remaining.Length == 0 ? "[empty]" : remaining)}");
                }
            }
            Logger.LogMessage(string.Empty);
        }

        private void DeleteInstances(string contentTypeName)
        {
            Logger.LogMessage($"Deleting content by content-type: {contentTypeName}");

            var nodes = ContentQuery.CreateQuery(
                    ContentRepository.SafeQueries.TypeIs, QuerySettings.AdminSettings, contentTypeName)
                .Execute().Nodes;

            foreach (var node in nodes)
            {
                Logger.LogMessage($"    {node.Path}");
                node.ForceDelete();
            }
        }
        private void RemoveAllowedTypes(ContentTypeDependencies dependencies)
        {
            Logger.LogMessage("Remove from allowed types:");

            var names = dependencies.InheritedTypeNames.ToList();
            names.Add(dependencies.ContentTypeName);
            var ns = EditContentType.NamespacePrefix;

            if (dependencies.RelatedContentTypes.Length + dependencies.RelatedFieldSettings.Length > 0)
            {
                foreach (var contentType in ContentType.GetContentTypes())
                {
                    var changed = false;
                    var xml = new XmlDocument();
                    xml.Load(contentType.Binary.GetStream());
                    var nsmgr = EditContentType.GetNamespaceManager(xml);

                    var allowedChildTypesElement =
                        (XmlElement) xml.SelectSingleNode($"/{ns}:ContentType/{ns}:AllowedChildTypes", nsmgr);
                    if (allowedChildTypesElement != null)
                    {
                        var oldList = allowedChildTypesElement.InnerText.Split(new[] {','},
                            StringSplitOptions.RemoveEmptyEntries);
                        var newList = oldList.Except(names).ToArray();
                        if (oldList.Length != newList.Length)
                        {
                            changed = true;
                            Logger.LogMessage($"    {contentType.Name}");
                            allowedChildTypesElement.InnerText = string.Join(",", newList);
                        }
                    }

                    foreach (XmlElement refFieldElement in xml.SelectNodes($"//{ns}:Field[@type='Reference']", nsmgr))
                    {
                        var elementsToDelete = new List<XmlElement>();
                        var oldAllowedTypes =
                            refFieldElement.SelectNodes($"{ns}:Configuration/{ns}:AllowedTypes/{ns}:Type", nsmgr);
                        foreach (XmlElement typeElement in oldAllowedTypes)
                            if (names.Contains(typeElement.InnerText))
                                elementsToDelete.Add(typeElement);
                        if (elementsToDelete.Any())
                        {
                            var fieldName = refFieldElement.Attributes["name"].Value;
                            Logger.LogMessage($"    {contentType.Name}.{fieldName}");
                            changed = true;
                        }
                        foreach (var element in elementsToDelete)
                            element.ParentNode.RemoveChild(element);
                    }

                    if (changed)
                    {
                        ContentTypeInstaller.InstallContentType(xml.OuterXml);
                    }
                }
            }

            if (dependencies.RelatedContentCollection.Count > 0)
            {
                foreach (var item in dependencies.RelatedContentCollection)
                {
                    Logger.LogMessage($"    {item.Key}");

                    var content = Content.Load(item.Key);
                    if (content.ContentHandler is GenericContent gc)
                    {
                        var newList = new List<ContentType>();
                        var oldList = gc.AllowedChildTypes.ToList();
                        foreach(var ct in oldList)
                            if (!names.Contains(ct.Name))
                                newList.Add(ct);

                        gc.AllowedChildTypes = newList;
                        gc.Save();
                    }
                }
            }
        }
    }
}