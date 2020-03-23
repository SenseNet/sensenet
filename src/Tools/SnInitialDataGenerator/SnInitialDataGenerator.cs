using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Packaging;
using SenseNet.Packaging.Steps;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.Messaging;
using SenseNet.Tests;
using ExecutionContext = SenseNet.Packaging.ExecutionContext;

namespace SenseNet.Tools.SnInitialDataGenerator
{
    internal class SnInitialDataGenerator
    {
        static void Main(string[] args)
        {
            var importPath = @"D:\dev\github\sensenet\src\nuget\snadmin\install-services\import\";
            //var importPath = @"D:\_InitialData\defaultdatabase-import\";
            //var importPath = @"D:\_InitialData\initialtestdata-import\";
            var tempFolderPath = @"D:\_InitialData\1";

            GenerateInMemoryDatabaseFromImport(importPath, tempFolderPath);

            //CreateSourceFiles(tempFolderPath);
        }

        public static void GenerateInMemoryDatabaseFromImport(string importPath, string tempFolderPath)
        {
            //Providers.PropertyCollectorClassName = typeof(EventPropertyCollector).FullName;
            var builder = CreateRepositoryBuilder();
            //Indexing.IsOuterSearchEngineEnabled = false;
            builder.StartIndexingEngine = false;

            using (Repository.Start(builder))
            {
                new Installer(CreateRepositoryBuilder()).Import(importPath);

                string importLog = null;
                using (var reader = new StreamReader(Logger.GetLogFileName()))
                    importLog = reader.ReadToEnd();

                User.Current = User.Administrator;

                Console.WriteLine("Set Root permissions.");

                using (new SystemAccount())
                    SecurityHandler.CreateAclEditor()
                        .Allow(Identifiers.PortalRootId, Identifiers.AdministratorUserId,
                            false, PermissionType.BuiltInPermissionTypes)
                        .Allow(Identifiers.PortalRootId, Identifiers.AdministratorsGroupId,
                            false, PermissionType.BuiltInPermissionTypes)
                        .Apply();

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
                        count++;
                        //if(count % 10 == 0)
                            Console.Write($"{count} / {nodeCount}   \r");
                    };
                    populator.RebuildIndexDirectlyAsync("/Root",
                            CancellationToken.None, IndexRebuildLevel.DatabaseAndIndex).ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                    Console.WriteLine();
                }

                Console.WriteLine("Saving data.");

                Save(tempFolderPath, importLog);

                Console.WriteLine("Saving index.");

                var indexFileName = Path.Combine(tempFolderPath, "index.txt");
                if (SearchManager.SearchEngine is InMemorySearchEngine searchEngine)
                    searchEngine.Index.Save(indexFileName);
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
            index.Load(new StringReader(InitialTestIndex.Index));
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

        private static void Save(string tempFolderPath, string importLog)
        {
            using (var writer = new StreamWriter(Path.Combine(tempFolderPath, "_importlog.log"), false, Encoding.UTF8))
                writer.WriteLine(importLog ?? "");

            using (var ptw = new StreamWriter(Path.Combine(tempFolderPath, "propertyTypes.txt"), false, Encoding.UTF8))
            using (var ntw = new StreamWriter(Path.Combine(tempFolderPath, "nodeTypes.txt"), false, Encoding.UTF8))
            using (var nw = new StreamWriter(Path.Combine(tempFolderPath, "nodes.txt"), false, Encoding.UTF8))
            using (var vw = new StreamWriter(Path.Combine(tempFolderPath, "versions.txt"), false, Encoding.UTF8))
            using (var dw = new StreamWriter(Path.Combine(tempFolderPath, "dynamicData.txt"), false, Encoding.UTF8))
                InitialData.Save(ptw, ntw, nw, vw, dw, null,
                    () => ((InMemoryDataProvider)DataStore.DataProvider).DB.Nodes
                        .OrderBy(x => x.Id)
                        .Select(x => x.NodeId));

            var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
            index.Save(Path.Combine(tempFolderPath, "index.txt"));

        }

    }
}
