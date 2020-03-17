using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
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
using SenseNet.Diagnostics;
using SenseNet.Packaging;
using SenseNet.Packaging.Steps;
using SenseNet.Search.Indexing;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using ExecutionContext = SenseNet.Packaging.ExecutionContext;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class InMemoryDatabaseGenerator : TestBase
    {
        //[TestMethod]
        public void xxx() //UNDONE: Delete this method
        {
            var index = GetInitialIndex();
            index.Save("D:\\_InitialData\\index\\1.txt");
            index.Load("D:\\_InitialData\\index\\1.txt");
            index.Save("D:\\_InitialData\\index\\2.txt");
        }

        [TestMethod]
        public void InMemoryDatabaseGenerator_IndexField_2Flags()
        {
            var field = new IndexField("FieldName", "value", IndexingMode.Default, IndexStoringMode.Yes,
                IndexTermVector.WithPositions);

            Assert.AreEqual("SM2,TV3,FieldName:value:S", field.ToString());

            var parsed = IndexField.Parse(field.ToString(), false);

            Assert.AreEqual(field.Name, parsed.Name);
            Assert.AreEqual(field.Type, parsed.Type);
            Assert.AreEqual(field.ValueAsString, parsed.ValueAsString);
            Assert.AreEqual(field.Mode, parsed.Mode);
            Assert.AreEqual(field.Store, parsed.Store);
            Assert.AreEqual(field.TermVector, parsed.TermVector);
        }
        [TestMethod]
        public void InMemoryDatabaseGenerator_IndexField_AllFlags()
        {
            var field = new IndexField("FieldName", "value", IndexingMode.No, IndexStoringMode.Yes,
                IndexTermVector.WithPositions);

            Assert.AreEqual("IM3,SM2,TV3,FieldName:value:S", field.ToString());

            var parsed = IndexField.Parse(field.ToString(), false);

            Assert.AreEqual(field.Name, parsed.Name);
            Assert.AreEqual(field.Type, parsed.Type);
            Assert.AreEqual(field.ValueAsString, parsed.ValueAsString);
            Assert.AreEqual(field.Mode, parsed.Mode);
            Assert.AreEqual(field.Store, parsed.Store);
            Assert.AreEqual(field.TermVector, parsed.TermVector);
        }
        [TestMethod]
        public void InMemoryDatabaseGenerator_IndexField_AllFlags_Stored()
        {
            var field = new IndexField("FieldName", "value", IndexingMode.No, IndexStoringMode.Yes,
                IndexTermVector.WithPositions);

            Assert.AreEqual("IM3,TV3,FieldName:value:S", field.ToString(true));

            var parsed = IndexField.Parse(field.ToString(), true);

            Assert.AreEqual(field.Name, parsed.Name);
            Assert.AreEqual(field.Type, parsed.Type);
            Assert.AreEqual(field.ValueAsString, parsed.ValueAsString);
            Assert.AreEqual(field.Mode, parsed.Mode);
            Assert.AreEqual(field.Store, parsed.Store);
            Assert.AreEqual(field.TermVector, parsed.TermVector);
        }
        [TestMethod] //UNDONE: ?? (int)flag 1 will be converted to 0 (e.g. IndexingMode.Analyzed -> IndexingMode.Default)
        public void InMemoryDatabaseGenerator_IndexField_DefaultFlags()
        {
            var field = new IndexField("FieldName", "value", IndexingMode.Analyzed, IndexStoringMode.No,
                IndexTermVector.No);

            Assert.AreEqual("FieldName:value:S", field.ToString());

            var parsed = IndexField.Parse(field.ToString(), false);

            Assert.AreEqual(field.Name, parsed.Name);
            Assert.AreEqual(field.Type, parsed.Type);
            Assert.AreEqual(field.ValueAsString, parsed.ValueAsString);
            Assert.AreEqual(IndexingMode.Default, parsed.Mode);
            Assert.AreEqual(IndexStoringMode.Default, parsed.Store);
            Assert.AreEqual(IndexTermVector.Default, parsed.TermVector);
        }

        //[TestMethod]
        public async System.Threading.Tasks.Task PatchBeforeSn775()
        {
            var tempFolderPath = @"D:\_InitialData";

            if (SnTrace.SnTracers.Count != 2)
                SnTrace.SnTracers.Add(new SnDebugViewTracer());

            await Test(async () =>
            {
                var nodeType = ActiveSchema.NodeTypes["Application"];
                var toDelete = NodeQuery.QueryNodesByTypeAndName(nodeType, false, "SetPermissions").Nodes.ToArray();
                foreach (var node in toDelete)
                    node.ForceDelete();

                using (var op = SnTrace.Test.StartOperation("@@ ========= Populate"))
                {
                    try
                    {
                        await SearchManager.GetIndexPopulator().RebuildIndexDirectlyAsync("/Root",
                            CancellationToken.None, IndexRebuildLevel.DatabaseAndIndex).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Assert.Fail();
                    }
                    op.Successful = true;
                }

                Save(tempFolderPath, null);

                CreateSourceFile(tempFolderPath);

            });
        }

        //[TestMethod]
        public void GenerateInMemoryDatabaseFromImport()
        {
            //var importPath = @"D:\dev\github\sensenet\src\nuget\snadmin\install-services\import\";
            //var importPath = @"D:\_InitialData\defaultdatabase-import\";
            var importPath = @"D:\_InitialData\initialtestdata-import\";
            var tempFolderPath = @"D:\_InitialData";

            Cache.Reset();
            ContentTypeManager.Reset();
            Providers.Instance.NodeTypeManeger = null;

            Providers.PropertyCollectorClassName = typeof(EventPropertyCollector).FullName;

            var builder = CreateRepositoryBuilder();

            //Indexing.IsOuterSearchEngineEnabled = false;
            builder.StartIndexingEngine = false;

            Cache.Reset();
            ContentTypeManager.Reset();

            var log = new StringBuilder();
            var loggers = new[] { new TestLogger(log) };
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);
            using (Repository.Start(builder))
            {
                new SnMaintenance().Shutdown();

                using (new SystemAccount())
                {
                    try
                    {
                        new Importer().Import(importPath);
                    }
                    catch (Exception e)
                    {
                        var x = log.ToString();
                        throw;
                    }

                    //Indexing.IsOuterSearchEngineEnabled = true;
                    SearchManager.GetIndexPopulator().RebuildIndexDirectlyAsync("/Root",
                        CancellationToken.None, IndexRebuildLevel.DatabaseAndIndex).ConfigureAwait(false).GetAwaiter().GetResult();

                    Save(tempFolderPath, log.ToString());

                    CreateSourceFile(tempFolderPath);
                }
            }
        }
        private RepositoryBuilder CreateRepositoryBuilder()
        {
            var dataProvider = new InMemoryDataProvider();
            Providers.Instance.DataProvider = dataProvider;
            DataStore.InstallInitialDataAsync(InitialData.Load(new SenseNetServicesInitialData()), CancellationToken.None).GetAwaiter().GetResult();

            return new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dataProvider)
                .UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dataProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseAccessTokenDataProviderExtension(new InMemoryAccessTokenDataProvider())
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider))
                .UseTestingDataProviderExtension(new InMemoryTestingDataProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom") as RepositoryBuilder;
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
        private class TestLogger : IPackagingLogger
        {
            public LogLevel AcceptedLevel => LogLevel.File;
            private readonly StringBuilder _sb;
            public TestLogger(StringBuilder sb)
            {
                _sb = sb;
            }
            public string LogFilePath => "[in memory]";
            public void Initialize(LogLevel level, string logFilePath) { }
            public void WriteTitle(string title)
            {
                _sb.AppendLine("================================");
                _sb.AppendLine(title);
                _sb.AppendLine("================================");
            }
            public void WriteMessage(string message)
            {
                _sb.AppendLine(message);
            }
        }
        private class Importer : ImportBase
        {
            private string _fsPath;
            public void Import(string fsPath)
            {
                _fsPath = fsPath;
                Execute(GetExecutionContext());
            }
            public override void Execute(ExecutionContext context)
            {
                RepositoryEnvironment.WorkingMode.SnAdmin = true;
                AbortOnError = false;
                var schemaPath = Path.Combine(_fsPath, @"System\Schema");
                base.DoImport(schemaPath, null, "/Root");
                base.DoImport(null, _fsPath, "/Root");
            }
            private ExecutionContext GetExecutionContext()
            {
                var manifestXml = new XmlDocument();
                manifestXml.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>MyCompany.MyComponent</Id>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");

                var phase = 0;
                var console = new StringWriter();
                var manifest = Manifest.Parse(manifestXml, phase, true, new PackageParameter[0]);
                var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, null, console);
                executionContext.RepositoryStarted = true;
                return executionContext;
            }
        }
        private void Save(string tempFolderPath, string importLog)
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

        private string[] _template = new[]
        {
// _template[0]
@"// Generated by a tool. {0}
// sensenet version: {1}",
// _template[1]
@"using SenseNet.ContentRepository.Storage.DataModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SenseNet.ContentRepository.InMemory
{
    public class ____data____ : IRepositoryDataFile
    {
        private ____data____()
        {
        }

        public static ____data____ Instance { get; } = new ____data____();

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
// _template[9]
@"
        };
    }
}
"
        };
        private void CreateSourceFile(string tempFolderPath)
        {
            var path = Path.Combine(tempFolderPath, "__InitialData.cs");
            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                var version = typeof(Content).Assembly.GetName().Version;
                writer.WriteLine(_template[0], DateTime.Now.ToString("yyyy-MM-dd"), version);

                writer.WriteLine(_template[1]);

                using (var reader = new StreamReader(Path.Combine(tempFolderPath, "propertyTypes.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());

                writer.WriteLine(_template[2]);

                using (var reader = new StreamReader(Path.Combine(tempFolderPath, "nodeTypes.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());

                writer.WriteLine(_template[3]);

                using (var reader = new StreamReader(Path.Combine(tempFolderPath, "nodes.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());

                writer.WriteLine(_template[4]);

                using (var reader = new StreamReader(Path.Combine(tempFolderPath, "versions.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());

                writer.WriteLine(_template[5]);

                using (var reader = new StreamReader(Path.Combine(tempFolderPath, "dynamicData.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd().Replace("\"", "\"\""));

                writer.WriteLine(_template[6]);

                WriteCTDs(writer);

                writer.WriteLine(_template[7]);

                WriteBlobs(writer);

                writer.WriteLine(_template[8]);

                writer.WriteLine(_template[9]);
            }
        }
        private void WriteCTDs(StreamWriter writer)
        {
            var template = @"                #region  {0}. {1}
			    {{ ""{1}"", @""{2}
""}},
            #endregion";
            var count = 0;
            foreach (var ctd in ContentType.GetContentTypes())
                writer.WriteLine(template, ++count, ctd.Name, ctd.ToXml().Replace("\"", "\"\""));
        }
        private void WriteBlobs(StreamWriter writer)
        {
            var template = @"                #region  {0}.{1}
                {{ ""{2}"", @""{3}
""}},
                #endregion";

            var files = NodeQuery.QueryNodesByType(NodeType.GetByName("File"), false).Nodes;
            var count = 0;
            foreach (var file in files)
                writer.WriteLine(template, ++count, file.Name, file.Path, GetFileContent((File)file));
        }
        private string GetFileContent(File file)
        {
            if (IsTextFile(file.Name))
                using (var reader = new StreamReader(file.Binary.GetStream()))
                    return reader.ReadToEnd().Replace("\"", "\"\"");
            return "[binarycontent]";
        }
        private bool IsTextFile(string name)
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
