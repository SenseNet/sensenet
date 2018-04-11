using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.Search;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps
{
    public class DeleteContentType : Step
    {
        [DefaultProperty]
        [Annotation("Name of the content type that will be deleted.")]
        public string Name { get; set; }

        public override void Execute(ExecutionContext context)
        {
            var names = ResolveVariable(Name, context)
                .Split(" ,;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .ToArray();

            context.AssertRepositoryStarted();

            foreach (var name in names)
            {
                var currentContentType = ContentType.GetByName(name);
                if (null == currentContentType)
                {
                    Logger.LogMessage("Content type is already deleted: " + name);
                    continue;
                }

                Logger.LogMessage("DELETING CONTENT TYPE: " + name);
                var typeSubtreeQuery = ContentQuery.CreateQuery(ContentRepository.SafeQueries.InTree, QuerySettings.AdminSettings, currentContentType.Path);
                var typeSubtreeResult = typeSubtreeQuery.Execute();
                var inheritedTypeNames = typeSubtreeResult.Nodes.Select(n => n.Name).ToArray();
                Logger.LogMessage("  Inherited types to delete: " + string.Join(", ", inheritedTypeNames));
                Logger.LogMessage(string.Empty);

                var contentInstancesQuery = ContentQuery.CreateQuery(ContentRepository.SafeQueries.TypeIs, QuerySettings.AdminSettings, name);
                var contentInstancesResult = contentInstancesQuery.Execute();
                Logger.LogMessage("  Content to delete: " + contentInstancesResult.Count);
                Logger.LogMessage(string.Empty);

                // ContentType/AllowedChildTypes: "Folder,File"
                GetContentTypesWhereTheyAreAllowed(inheritedTypeNames);

                // ContentType/Fields/Field/Configuration/AllowedTypes/Type: "Folder"
                GetContentTypesWhereTheyAreAllowedInReferenceField(inheritedTypeNames);

                // ContentMetaData/Fields/AllowedChildTypes: "Folder File"
                GetContentPathsWhereTheyAreAllowedChildren(inheritedTypeNames);

            }

            for (int i = 0; i < 30; i++)
            {
                Console.Write($"\r{30 - i}  ");
                Thread.Sleep(50);
            }
            Console.Write("\r");


            Logger.LogMessage("Ok. ");
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

        private List<string> GetContentPathsWhereTheyAreAllowedChildren(string[] names)
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

            return result;
        }
    }
}