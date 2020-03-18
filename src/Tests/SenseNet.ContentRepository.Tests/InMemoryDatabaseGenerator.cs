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
using BlobStorage = SenseNet.Configuration.BlobStorage;
using ExecutionContext = SenseNet.Packaging.ExecutionContext;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class InMemoryDatabaseGenerator : TestBase
    {
        [TestMethod]
        public void __nodesWithBlobs()
        {
            var tempFolderPath = @"D:\_InitialData";

            Cache.Reset();
            ContentTypeManager.Reset();
            Providers.Instance.NodeTypeManeger = null;
            Providers.PropertyCollectorClassName = typeof(EventPropertyCollector).FullName;

            var builder = CreateRepositoryBuilder(GetInitialData());

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

                var db = ((InMemoryDataProvider) DataStore.DataProvider).DB;
                var blobDb = (InMemoryBlobProvider)BlobStorageBase.GetProvider(123);

                //var prop = db.Schema.PropertyTypes.First(x => x.Name == "Binary");
                //var node = db.Nodes.First(x => x.Name == "Logging.settings");
                //var version = db.Versions.First(x => x.NodeId == node.NodeId);
                //var binProp = db.BinaryProperties.First(x => x.VersionId == version.VersionId && x.PropertyTypeId == prop.Id);
                //var file = db.Files.First(x => x.FileId == binProp.FileId);
                //var blobCtx = new BlobStorageContext(blobDb, file.BlobProviderData);
                //var stream = blobDb.GetStreamForRead(blobCtx);

                using (new SystemAccount())
                {
                    Save(tempFolderPath, log.ToString());

                    CreateSourceFiles(tempFolderPath);
                }
            }
        }

        //[TestMethod]
        public void __checkNewIndex() //UNDONE: Delete this method
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

        [TestMethod]
        public void InMemoryDatabaseGenerator_HexDump()
        {
            string BytesToHex(byte[] bytes)
            {
                return string.Join(" ", @bytes.Select(x=>x.ToString("X2")));
            }

            #region var testCases = new[] ....
            var testCases = new[]
            {
                new byte[0],
                new byte[] {0x0},
                new byte[]
                {
                    0xFF, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E
                },
                new byte[]
                {
                    0xFF, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
                },
                new byte[]
                {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                    0X1F
                },
                new byte[]
                {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                    0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E,
                },
                new byte[]
                {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                    0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
                },
                new byte[]
                {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                    0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
                    0x2F
                },
            };
            #endregion

            foreach (var expectedBytes in testCases)
            {
                var stream = new MemoryStream(expectedBytes);

                // ACTION
                var dump = GetHexDump(stream);
                var actualBytes = InitialData.ParseHexDump(dump);

                // ASSERT
                var expected = BytesToHex(expectedBytes);
                var actual = BytesToHex(actualBytes);
                Assert.AreEqual(expected, actual);
            }
        }
        [TestMethod]
        public void InMemoryDatabaseGenerator_HexDump_DotsAndChars()
        {
            var bytes = new byte[0x100];
            //for (byte i = 0x0; i < 0x100; i++)
            //    bytes[i] = i;
            for (byte i = 0x0; i < 0xFF; i++)
                bytes[i] = i;
            bytes[0xFF] = 0xFF;
            var stream = new MemoryStream(bytes);

            // ACTION
            var dump = GetHexDump(stream);

            // ASSERT
            var lines = dump.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            var actual = string.Join("", lines.Select(x => x.Substring(48)));

            var expected = "................................" +
                " !.#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[.]^_`abcdefghijklmnopqrstuvwxyz{|}~." +
                "................................................................................................" +
                "................................";
            
            Assert.AreEqual(expected, actual);
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

                CreateSourceFiles(tempFolderPath);

            });
        }



        [TestMethod] //UNDONE: insert this algorithm to a host software (e.g. console app)
        public void GenerateInMemoryDatabaseFromImport()
        {
            var importPath = @"D:\dev\github\sensenet\src\nuget\snadmin\install-services\import\";
            //var importPath = @"D:\_InitialData\defaultdatabase-import\";
            //var importPath = @"D:\_InitialData\initialtestdata-import\";
            var tempFolderPath = @"D:\_InitialData\1";

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

                    var indexFileName = Path.Combine(tempFolderPath, "index.txt");
                    if (SearchManager.SearchEngine is InMemorySearchEngine searchEngine)
                        searchEngine.Index.Save(indexFileName);

                    CreateSourceFiles(tempFolderPath);
                }
            }
        }
        private RepositoryBuilder CreateRepositoryBuilder(InitialData initialData = null)
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
                .UseTestingDataProviderExtension(new InMemoryTestingDataProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
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
// _template[0] ---- data start
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
// _template[9] ---- data end
@"
        };
    }
}
",
// template[10] ---- index start
@"namespace SenseNet.ContentRepository
{
    public static class ____index____
    {
        #region public static readonly string Index

        public static readonly string Index = @""",
// template[11] ---- index end
@""";
        #endregion
    }
}
"
        };
        private void CreateSourceFiles(string tempFolderPath)
        {
            var path = Path.Combine(tempFolderPath, "__InitialData.cs");
            var version = typeof(Content).Assembly.GetName().Version;
            var now = DateTime.Now.ToString("yyyy-MM-dd");

            // WRITE DATA

            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                writer.WriteLine(_template[0], now, version);

                writer.WriteLine(_template[1]);

                using (var reader = new StreamReader(Path.Combine(tempFolderPath, "propertyTypes.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());

                writer.WriteLine(_template[2]);

                using (var reader = new StreamReader(Path.Combine(tempFolderPath, "nodeTypes.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());

                writer.WriteLine(_template[3]);

                using (var reader = new StreamReader(Path.Combine(tempFolderPath, "nodes.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd().Replace("\"", "\"\""));

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

            // WRITE INDEX

            path = Path.Combine(tempFolderPath, "__InitialIndex.cs");
            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                writer.WriteLine(_template[0], now, version);

                writer.Write(_template[10]);

                using (var reader = new StreamReader(Path.Combine(tempFolderPath, "index.txt"), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd().Replace("\"", "\"\""));
                
                writer.Write(_template[11]);
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
                {{ ""Binary:{2}"", @""{3}
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
                    return "[text]:" + Environment.NewLine + 
                           reader.ReadToEnd().Replace("\"", "\"\"");
            return "[bytes]:" + Environment.NewLine +
                   GetHexDump(file.Binary.GetStream());
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
        private string GetHexDump(Stream stream)
        {
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);

            var sb = new StringBuilder();
            var chars = new char[16];
            var nums = new string[16];

            void AddLine()
            {
                sb.Append(string.Join(" ", nums));
                sb.Append(" ");
                sb.AppendLine(string.Join("", chars));
            }

            // add lines
            var col = 0;
            foreach (var @byte in bytes)
            {
                nums[col] = (@byte.ToString("X2"));
                chars[col] = @byte < 32 || @byte >= 127 || @byte == '"' || @byte == '\\' ? '.' : (char) @byte;
                if (++col == 16)
                {
                    AddLine();
                    col = 0;
                }
            }

            // rest
            if (col > 0)
            {
                for (var i = col; i < 16; i++)
                {
                    nums[i] = "  ";
                    chars[i] = ' ';
                }
                AddLine();
            }

            return sb.ToString();
        }
    }
}
