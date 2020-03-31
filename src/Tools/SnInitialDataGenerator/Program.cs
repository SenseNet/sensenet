using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Transactions;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Packaging;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Tests;
using SenseNet.Tools.CommandLineArguments;
using File = SenseNet.ContentRepository.File;
// ReSharper disable CoVariantArrayConversion

namespace SenseNet.Tools.SnInitialDataGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var arguments = new Arguments();
            ArgumentParser parser = null;
            try
            {
                parser = ArgumentParser.Parse(args, arguments);

                if (parser.IsHelp)
                {
                    Usage(parser);
                    return;
                }

                arguments.Prepare();
            }
            catch (ParsingException e)
            {
                Usage(e.Result, e.FormattedMessage);
                return;
            }
            catch (Exception e)
            {
                Usage(parser, e.Message);
                return;
            }

            GenerateInMemoryDatabaseFromImport(arguments);

            CreateSourceFiles(arguments);

            Console.WriteLine("Done.");
        }

        /* ============================================================================ USAGE */

        private static void Usage(ArgumentParser parser, string message = null)
        {
            if (message != null)
            {
                Console.WriteLine(message);
                Console.WriteLine();
            }
            Console.WriteLine(parser.GetHelpText());
        }

        /* ============================================================================ DATABASE AND INDEX BUILDER */

        public static void GenerateInMemoryDatabaseFromImport(Arguments arguments)
        {
            //Providers.PropertyCollectorClassName = typeof(EventPropertyCollector).FullName;
            var builder = CreateRepositoryBuilder();
            //Indexing.IsOuterSearchEngineEnabled = false;
            builder.StartIndexingEngine = false;

            using (Repository.Start(builder))
            {
                // IMPORT
                new Installer(CreateRepositoryBuilder()).Import(arguments.ImportPath);

                // Copy importlog
                using (var reader = new StreamReader(Logger.GetLogFileName()))
                using (var writer = new StreamWriter(Path.Combine(arguments.OutputPath, "_importlog.log"), false, Encoding.UTF8))
                    writer.WriteLine(reader.ReadToEnd());

                // Re-set initial object
                User.Current = User.Administrator;

                // Add Admin* permissions on the /Root
                Console.WriteLine("Set Root permissions.");
                using (new SystemAccount())
                    SecurityHandler.CreateAclEditor()
                        .Allow(Identifiers.PortalRootId, Identifiers.AdministratorUserId,
                            false, PermissionType.BuiltInPermissionTypes)
                        .Allow(Identifiers.PortalRootId, Identifiers.AdministratorsGroupId,
                            false, PermissionType.BuiltInPermissionTypes)
                        .Apply();

                // Use the path-blacklist
                Console.WriteLine("Remove unnecessary subtrees ({0}):", arguments.SkippedPathArray.Length);
                RemoveSkippedPaths(arguments);

                // Save database to separated files.
                Console.Write("Saving data...");
                SaveData(arguments.OutputPath);
                SavePermissions(Providers.Instance.SecurityDataProvider, arguments.OutputPath);
                Console.WriteLine("ok.");

                // Build index
                Console.WriteLine("Building index.");

                Indexing.IsOuterSearchEngineEnabled = true;

                var nodeCount = DataStore.GetNodeCountAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var count = 0;
                Console.Write($"{count} / {nodeCount}   \r");
                using (new SystemAccount())
                {
                    var populator = SearchManager.GetIndexPopulator();
                    populator.IndexDocumentRefreshed += (sender, e) =>
                    {
                        Console.Write($"{++count} / {nodeCount}   \r");
                    };
                    populator.RebuildIndexDirectlyAsync("/Root",
                        CancellationToken.None, IndexRebuildLevel.DatabaseAndIndex)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    Console.WriteLine();
                }

                // Save index
                Console.Write("Saving index...");
                var indexFileName = Path.Combine(arguments.OutputPath, "index.txt");
                if (SearchManager.SearchEngine is InMemorySearchEngine searchEngine)
                    searchEngine.Index.Save(indexFileName);
                Console.WriteLine("ok.");
            }
        }
        private static RepositoryBuilder CreateRepositoryBuilder(InitialData initialData = null)
        {
            var dataProvider = new InMemoryDataProvider();
            Providers.Instance.DataProvider = dataProvider;

            var builder = new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dataProvider)
                .UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dataProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseInitialData(initialData ?? InitialData.Load(new SenseNetServicesInitialData()))
                .UseAccessTokenDataProviderExtension(new InMemoryAccessTokenDataProvider())
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider))
                //.UseTestingDataProviderExtension(new InMemoryTestingDataProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                //.EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom") as RepositoryBuilder;

            return builder;
        }
        protected static InMemoryIndex GetInitialIndex()
        {
            var index = new InMemoryIndex();
            //index.Load(new StringReader(InitialTestIndex.Index));
            return index;
        }
        protected static ISecurityDataProvider GetSecurityDataProvider(InMemoryDataProvider repo)
        {
            return new MemoryDataProvider(new DatabaseStorage
            {
                Aces = new List<StoredAce>
                {
                    new StoredAce {EntityId = 2, IdentityId = 1, LocalOnly = false, AllowBits = 0x0EF, DenyBits = 0x000}
                },
                Entities = repo.LoadEntityTreeAsync(CancellationToken.None).GetAwaiter().GetResult()
                    .ToDictionary(x => x.Id, x => new StoredSecurityEntity
                    {
                        Id = x.Id,
                        OwnerId = x.OwnerId,
                        ParentId = x.ParentId,
                        IsInherited = true,
                        HasExplicitEntry = x.Id == 2
                    }),
                Memberships = new List<Membership>
                {
                    new Membership
                    {
                        GroupId = Identifiers.AdministratorsGroupId,
                        MemberId = Identifiers.AdministratorUserId,
                        IsUser = true
                    }
                },
                Messages = new List<Tuple<int, DateTime, byte[]>>()
            });
        }

        private static void SaveData(string tempFolderPath)
        {
            InitialData.Save(tempFolderPath, null,
                () => ((InMemoryDataProvider)DataStore.DataProvider).DB.Nodes
                    .OrderBy(x => x.Id)
                    .Select(x => x.NodeId));

            var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
            index.Save(Path.Combine(tempFolderPath, "index.txt"));
        }
        private static void SavePermissions(ISecurityDataProvider db, string tempFolderPath)
        {
            using (var writer = new StreamWriter(Path.Combine(tempFolderPath, "permissions.txt"), false, Encoding.UTF8))
            {
                var breaks = db.LoadSecurityEntities()
                    .Where(e => !e.IsInherited)
                    .Select(e => e.Id)
                    .ToArray();

                var aces = db.LoadAllAces()
                    .OrderBy(a => a.EntityId)
                    .ThenBy(e => e.IdentityId);

                foreach (var ace in aces)
                    writer.WriteLine("            \"{0}{1}\",",
                        breaks.Contains(ace.EntityId) ? "-" : "+",
                        ace.ToString().Replace("(", "").Replace(")", ""));
            }
        }

        private static readonly string[] ProtectedContents = new[]
        {
            "/Root",
            "/Root/System",
            "/Root/System/Schema",
            "/Root/System/Schema/ContentTypes",
            "/Root/System/Schema/ContentTypes/ContentType",
            "/Root/System/Schema/ContentTypes/GenericContent",
        };
        private static void RemoveSkippedPaths(Arguments arguments)
        {
            foreach (var skippedPath in arguments.SkippedPathArray)
            {
                if(ProtectedContents.Contains(skippedPath, StringComparer.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("not removed: {0}", skippedPath);
                    continue;
                }

                var node = Node.LoadNode(skippedPath);
                if (node == null)
                {
                    Console.WriteLine("not found:   {0}", skippedPath);
                    continue;
                }
                if (node.Id < 1000)
                {
                    Console.WriteLine("not removed: {0}", skippedPath);
                    continue;
                }

                Node.ForceDelete(skippedPath);
                Console.WriteLine("    removed: {0}", skippedPath);
            }
        }

        /* ============================================================================ SOURCE FILE GENERATOR */

        #region C# templates
        private static readonly string[] Template = 
{
// _template[0] ---- data start
@"// Generated by a tool. {0}
// sensenet version: {1}",
// _template[1]
@"using SenseNet.ContentRepository.Storage.DataModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace {0}
{{
    public class {1} : IRepositoryDataFile
    {{
        private {1}()
        {{
        }}

        public static {1} Instance {{ get; }} = new {1}();

        public string PropertyTypes => @""",
// _template[2]
@""";

        public string NodeTypes => @""",
// _template[3]
@""";

        public string Nodes => @""",
// _template[4]
@""";

        public string Versions => @""",
// _template[5]
@""";

        public string DynamicData => @""",
// _template[6]
@""";

        public IDictionary<string, string> ContentTypeDefinitions =>
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {",
// _template[7]
@"            });

        public IDictionary<string, string> Blobs =>
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {",
// _template[8]
@"            });

        public IList<string> Permissions => new List<string>
        {
            // ""+E1|Normal|+G1:______________+,Normal|+G2:_____________+_"";


            // RECORD STRUCTURE:
            // Inherited: +, breaked: -
            // EntityId
            // |
            // EntryType: Normal
            // |
            // Global: +, Local only: -
            // IdentityId
            // :
            // Permission bits (64 chars): Not set: _, Allow: +, Deny: -
            //---------------------------------------------------------------------------
            // See: See
            // Pre: Preview
            // PWa: PreviewWithoutWatermark
            // PRd: PreviewWithoutRedaction
            // Opn: Open
            // OpM: OpenMinor
            // Sav: Save
            // Pub: Publish
            // Chk: ForceCheckin
            // Add: AddNew
            // Apr: Approve
            // Del: Delete
            // ReV: RecallOldVersion
            // DeV: DeleteOldVersion
            // ReP: SeePermissions
            // WrP: SetPermissions
            // Run: RunApplication
            // LST: ManageListsAndWorkspaces
            // Own: TakeOwnership
            //---------------------------------------------------------------------------
            //                                                            WrP Del Pub PRd
            //                                                         Own ReP Apr Sav PWa
            //                                                          LST DeV Add OpM Pre
            //            |            custom            ||   unused  |  Run ReV Chk Opn See
            //            3333333333333333222222222222222211111111111111110000000000000000
            //            FEDCBA9876543210FEDCBA9876543210FEDCBA9876543210FEDCBA9876543210
",
// _template[9] ---- data end
@"
        };
    }
}
",
// template[10] ---- index start
@"namespace {0}
{{
    public static class {1}
    {{
        #region public static readonly string Index

        public static readonly string Index = @""",
// template[11] ---- index end
@""";
        #endregion
    }
}
"
        };
        #endregion

        private static void CreateSourceFiles(Arguments arguments)
        {
            Console.WriteLine("Generate C# source files");

            var path = Path.Combine(arguments.OutputPath, arguments.DataFileName);
            var version = typeof(Content).Assembly.GetName().Version;
            var now = DateTime.Now.ToString("yyyy-MM-dd");

            // WRITE DATA

            Console.Write("writing {0} ...", arguments.DataFileName);
            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                writer.WriteLine(Template[0], now, version);

                writer.WriteLine(Template[1], arguments.DatabaseNamespace, arguments.DatabaseClassName);

                using (var reader = new StreamReader(Path.Combine(arguments.OutputPath, "propertyTypes.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());

                writer.WriteLine(Template[2]);

                using (var reader = new StreamReader(Path.Combine(arguments.OutputPath, "nodeTypes.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());

                writer.WriteLine(Template[3]);

                using (var reader = new StreamReader(Path.Combine(arguments.OutputPath, "nodes.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd().Replace("\"", "\"\""));

                writer.WriteLine(Template[4]);

                using (var reader = new StreamReader(Path.Combine(arguments.OutputPath, "versions.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());

                writer.WriteLine(Template[5]);

                using (var reader = new StreamReader(Path.Combine(arguments.OutputPath, "dynamicData.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd().Replace("\"", "\"\""));

                writer.WriteLine(Template[6]);

                WriteCtds(writer);

                writer.WriteLine(Template[7]);

                WriteBlobs(writer);

                writer.WriteLine(Template[8]);

                using (var reader = new StreamReader(Path.Combine(arguments.OutputPath, "permissions.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());

                writer.WriteLine(Template[9]);
            }
            Console.WriteLine("ok.");

            // WRITE INDEX

            Console.Write("writing {0} ...", arguments.IndexFileName);
            path = Path.Combine(arguments.OutputPath, arguments.IndexFileName);
            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                writer.WriteLine(Template[0], now, version);

                writer.Write(Template[10], arguments.IndexNamespace, arguments.IndexClassName);

                using (var reader = new StreamReader(Path.Combine(arguments.OutputPath, "index.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd().Replace("\"", "\"\""));

                writer.Write(Template[11]);
            }
            Console.WriteLine("ok.");
        }
        private static void WriteCtds(StreamWriter writer)
        {
            var template = @"                #region  {0}. {1}
			    {{ ""{1}"", @""{2}
""}},
            #endregion";
            var count = 0;
            foreach (var ctd in ContentType.GetContentTypes())
                writer.WriteLine(template, ++count, ctd.Name, ctd.ToXml().Replace("\"", "\"\""));
        }
        private static void WriteBlobs(StreamWriter writer)
        {
            var template = @"                #region  {0}.{1}
                {{ ""Binary:{2}"", @""{3}
""}},
                #endregion";

            var files = NodeQuery.QueryNodesByType(NodeType.GetByName("File"), false).Nodes;
            var count = 0;
            foreach (var file in files)
                writer.WriteLine(template, ++count, file.Name, file.Path, GetFileContent((File)file));
        }
        private static string GetFileContent(File file)
        {
            if (IsTextFile(file.Name))
                using (var reader = new StreamReader(file.Binary.GetStream()))
                    return "[text]:" + Environment.NewLine +
                           reader.ReadToEnd().Replace("\"", "\"\"");
            return "[bytes]:" + Environment.NewLine +
                InitialData.GetHexDump(file.Binary.GetStream());
        }
        private static bool IsTextFile(string name)
        {
            if (name.EndsWith(".settings", StringComparison.OrdinalIgnoreCase))
                return true;
            if (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                return true;
            if (name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                return true;
            if (name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                return true;
            if (name.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                return true;
            if (name.EndsWith(".asmx", StringComparison.OrdinalIgnoreCase))
                return true;
            if (name.EndsWith(".ashx", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

    }
}
