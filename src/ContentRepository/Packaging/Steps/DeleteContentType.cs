using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.Search;
using SenseNet.Search.Querying;

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
            public string[] RelatedContentPaths { get; set; }
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

            var inheritedTypeNames = dependencies.InheritedTypeNames;
            Logger.LogMessage("  Inherited types to delete: ");
            Logger.LogMessage("    " + (inheritedTypeNames.Length == 0
                                  ? "There is no related item."
                                  : string.Join(", ", inheritedTypeNames)));
            Logger.LogMessage(string.Empty);

            Logger.LogMessage("  Content to delete: " + dependencies.InstanceCount);
            Logger.LogMessage(string.Empty);

            if (dependencies.RelatedContentTypes.Length > 0 ||
                dependencies.RelatedFieldSettings.Length > 0 || dependencies.RelatedContentPaths.Length > 0)
                throw new NotImplementedException();

            if (Delete == Mode.No)
            {
                Logger.LogMessage($"The {name} content type is not removed, this step provides only information.");
                return;
            }

            if (dependencies.InstanceCount > 0)
                DeleteInstances(name);

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
                RelatedContentPaths = GetContentPathsWhereTheyAreAllowedChildren(typeNames)
            };
            return result;
        }

        private ContentType[] GetContentTypesWhereTheyAreAllowed(string[] names)
        {
            Logger.LogMessage("  Remaining allowed child types in other content types after deletion:");

            var relevantContentTypes =
                ContentType.GetContentTypes()
                    .Where(c => !names.Contains(c.Name))
                    .Where(c => c.AllowedChildTypeNames != null && c.AllowedChildTypeNames.Intersect(names).Any())
                    .ToArray();
            if (0 == relevantContentTypes.Length)
            {
                Logger.LogMessage("    There is no related item.");
            }
            else
            {
                foreach (var contentType in relevantContentTypes)
                {
                    var remaining = string.Join(", ", contentType.AllowedChildTypeNames.Except(names).ToArray());
                    remaining = remaining.Length == 0 ? "[empty]" : remaining;
                    Logger.LogMessage($"    {contentType.Name}: {remaining}");
                }
            }
            Logger.LogMessage(string.Empty);

            return relevantContentTypes;
        }

        private ReferenceFieldSetting[] GetContentTypesWhereTheyAreAllowedInReferenceField(string[] names)
        {
            Logger.LogMessage("  Remaining allowed child types in reference fields after deletion:");

            var relevantFieldSettings = ContentType.GetContentTypes().SelectMany(t => t.FieldSettings)
                .Where(f => f is ReferenceFieldSetting).Cast<ReferenceFieldSetting>()
                .Where(r => r.AllowedTypes != null && r.AllowedTypes.Intersect(names).Any())
                .Distinct()
                .ToArray();

            if (0 == relevantFieldSettings.Length)
            {
                Logger.LogMessage("    There is no related item.");
            }
            else
            {
                foreach (var fieldSetting in relevantFieldSettings)
                {
                    var remaining = string.Join(", ", fieldSetting.AllowedTypes.Except(names).ToArray());
                    remaining = remaining.Length == 0 ? "[empty]" : remaining;
                    Logger.LogMessage($"    {fieldSetting.Owner.Name}.{fieldSetting.Name}: {remaining}");
                }
            }
            Logger.LogMessage(string.Empty);

            return relevantFieldSettings;
        }

        private string[] GetContentPathsWhereTheyAreAllowedChildren(string[] names)
        {
            Logger.LogMessage("  Remaining allowed child types in content after deletion:");

            var result = new List<string>();

            var whereClausePart = string.Join(Environment.NewLine + "    OR" + Environment.NewLine,
                names.Select(n =>$"    (t.Value like '{n}' OR t.Value like '% {n} %' OR t.Value like '{n} %' OR t.Value like '% {n}')"));

            // testability: the first line is recognizable for the tests.
            var sql = $"-- GetContentPathsWhereTheyAreAllowedChildren: [{string.Join(", ", names)}]" + Environment.NewLine; 
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
                {
                    if (!reader.HasRows)
                    {
                        Logger.LogMessage("    There is no related item.");
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            var path = reader.GetString(0);
                            result.Add(path);

                            var value = reader.GetString(1);
                            var storedNames = value.Split(' ');

                            var remaining = string.Join(", ", storedNames.Except(names).ToArray());
                            remaining = remaining.Length == 0 ? "[empty]" : remaining;

                            Logger.LogMessage($"    {path}: {remaining}");
                        }
                    }
                }
            }
            Logger.LogMessage(string.Empty);

            return result.ToArray();
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

    }
}