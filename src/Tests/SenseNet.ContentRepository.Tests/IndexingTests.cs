using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using STT=System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.InMemory;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class IndexingTests : TestBase
    {
        #region Text extracion

        private class CustomTextExtractor : TextExtractor
        {
            //TODO: not thread safe. In multithread execution these properties must thread-dependent. 
            public static bool Called { get; private set; }
            public static string Extraction { get; private set; }
            public static void Reset()
            {
                Called = false;
                Extraction = null;
            }

            public override bool IsSlow => true;

            public override string Extract(Stream stream, TextExtractorContext context)
            {
                Called = true;
                using (var reader = new StreamReader(stream))
                    Extraction = reader.ReadToEnd();
                return Extraction;
            }
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_TextExtraction()
        {
            Test(() =>
            {
                // Define a custom filename extension.
                var ext = "testext";

                // Create custom textextractor with log and IsSlow = true.
                var extractor = new CustomTextExtractor();

                // Hack textextractor for filename custom extension.
                var extractorSettings = @"{TextExtractors: {""" + ext + @""": """ + typeof(CustomTextExtractor).FullName + @"""}}";
                var settingsFile = Settings.GetSettingsByName<IndexingSettings>(IndexingSettings.SettingsName, Repository.RootPath);
                settingsFile.Binary.SetStream(RepositoryTools.GetStreamFromString(extractorSettings));
                settingsFile.Save();

                try
                {
                    // Create file with the custom filename extension.
                    var testRoot = new SystemFolder(Repository.Root) { Name = "TestRoot" };
                    testRoot.Save();
                    var file = new File(testRoot) { Name = "TestFile." + ext };

                    // Write some well known words into the file's binary.
                    var text = "tema tis rolod muspi meroL";
                    var binaryData = new BinaryData() { FileName = file.Name };
                    binaryData.SetStream(RepositoryTools.GetStreamFromString(text));
                    file.Binary = binaryData;

                    // Save the file.
                    file.Save();
                    var fileId = file.Id;

                    // Check and reset the custom extractor's log.
                    var called = CustomTextExtractor.Called;
                    var extraction = CustomTextExtractor.Extraction;
                    CustomTextExtractor.Reset();
                    Assert.IsTrue(called);
                    Assert.AreEqual(text, extraction);

                    // Check the index with queries by well known words in the default (_Text) and "Binary" field.
                    var words = text.Split(' ');
                    var results = new []
                    {
                        CreateSafeContentQuery($"+{words[4]} +Name:{file.Name} .AUTOFILTERS:OFF").Execute().Nodes.ToArray(),
                        CreateSafeContentQuery($"+{words[0]} +{words[2]} +Name:{file.Name} .AUTOFILTERS:OFF").Execute().Nodes.ToArray(),
                        CreateSafeContentQuery($"+{words[1]} +{words[3]} +Name:{file.Name} .AUTOFILTERS:OFF").Execute().Nodes.ToArray(),
                    };

                    Assert.AreEqual(1, results[0].Length);
                    Assert.AreEqual(1, results[1].Length);
                    Assert.AreEqual(1, results[2].Length);

                    var expectedIds = $"{fileId}, {fileId}, {fileId}";
                    var actualIds = string.Join(", ", results.Select(r => r.First().Id.ToString()).ToArray());
                    Assert.AreEqual(expectedIds, actualIds);
                }
                finally
                {
                    // Remove the hack.
                    settingsFile.Binary.SetStream(RepositoryTools.GetStreamFromString(null));
                    settingsFile.Save();
                }
            });
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_XmlFile_Wellformed()
        {
            Test(() =>
            {
                var testRoot = new SystemFolder(Repository.Root) { Name = "TestRoot" };
                testRoot.Save();

                // Create an xml file
                var file = new File(testRoot) { Name = "TestFile.xml" };

                // Write a well-formed xml into the file's binary.
                var text = "<rootelement42><xmlelement42 attr42='attrvalue'>elementtext1 elementtext2 elementtext3</xmlelement42></rootelement42>";
                var binaryData = new BinaryData() { FileName = file.Name };
                binaryData.SetStream(RepositoryTools.GetStreamFromString(text));
                file.Binary = binaryData;

                // Save the file.
                file.Save();
                var fileId = file.Id;

                // Check the index with queries by well known words in the default (_Text) field.
                var results = new[]
                {
                    CreateSafeContentQuery("rootelement42").Execute().Nodes.ToArray(),
                    CreateSafeContentQuery("xmlelement42").Execute().Nodes.ToArray(),
                    CreateSafeContentQuery("attr42").Execute().Nodes.ToArray(),

                    CreateSafeContentQuery("elementtext1").Execute().Nodes.ToArray(),
                    CreateSafeContentQuery("elementtext2").Execute().Nodes.ToArray(),
                    CreateSafeContentQuery("elementtext3").Execute().Nodes.ToArray(),

                    CreateSafeContentQuery("attrvalue").Execute().Nodes.ToArray(),
                };

                var expectedCounts = "0, 0, 0, 1, 1, 1, 0";
                var actualCounts = string.Join(", ", results.Select(r => r.Length.ToString()).ToArray());
                Assert.AreEqual(expectedCounts, actualCounts);
            });
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_XmlFile_NotWellformed()
        {
            Test(() =>
            {
                var testRoot = new SystemFolder(Repository.Root) { Name = "TestRoot" };
                testRoot.Save();

                // Create an xml file
                var file = new File(testRoot) { Name = "TestFile.xml" };

                // Write a not well-formed xml into the file's binary.
                var text = "<rootelement42><xmlelement42 attr42='attrvalue'>elementtext1 elementtext2 elementtext3</xmlelement42><rootelement42>";
                var binaryData = new BinaryData() { FileName = file.Name };
                binaryData.SetStream(RepositoryTools.GetStreamFromString(text));
                file.Binary = binaryData;

                // Save the file.
                file.Save();

                // Check the index with queries by well known words in the default (_Text) field.
                var results = new[]
                {
                    CreateSafeContentQuery("rootelement42").Execute().Nodes.ToArray(),
                    CreateSafeContentQuery("xmlelement42").Execute().Nodes.ToArray(),
                    CreateSafeContentQuery("attr42").Execute().Nodes.ToArray(),

                    CreateSafeContentQuery("elementtext1").Execute().Nodes.ToArray(),
                    CreateSafeContentQuery("elementtext2").Execute().Nodes.ToArray(),
                    CreateSafeContentQuery("elementtext3").Execute().Nodes.ToArray(),

                    CreateSafeContentQuery("attrvalue").Execute().Nodes.ToArray(),
                };

                var expectedCounts = "1, 1, 1, 1, 1, 1, 1";
                var actualCounts = string.Join(", ", results.Select(r => r.Length.ToString()).ToArray());
                Assert.AreEqual(expectedCounts, actualCounts);
            });
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_OpenXmlFile() => Test(() =>
        {
            var testRoot = new SystemFolder(Repository.Root) {Name = "TestRoot"};
            testRoot.Save();

            // Office file contents: sensenet1234test
            const string fileName = "sensenettest.docx";
            var file = new File(testRoot) {Name = fileName};

            var assembly = Assembly.GetExecutingAssembly();
            using (var fs = assembly.GetManifestResourceStream($"SenseNet.ContentRepository.Tests.TestFiles.{fileName}"))
            {
                var binaryData = new BinaryData {FileName = file.Name};
                binaryData.SetStream(fs);
                file.Binary = binaryData;

                file.Save();
            }

            // Check the index with queries by well known words in the binary
            var results = new[]
            {
                CreateSafeContentQuery("sensenet123test").Execute().Nodes.ToArray(),
                CreateSafeContentQuery("sensenet1234test").Execute().Nodes.ToArray(),
                CreateSafeContentQuery("+TypeIs:File +Name:sensenet1234test").Execute().Nodes.ToArray(),
                CreateSafeContentQuery("+TypeIs:File +sensenet1234test").Execute().Nodes.ToArray()
            };

            var actualCounts = string.Join(", ", results.Select(r => r.Length.ToString()).ToArray());
            Assert.AreEqual("0, 1, 0, 1", actualCounts);
        });
        #endregion

        #region Choice field

        private enum TestEnum1ForChoiceField { Enum1Value1, Enum1Value2, Enum1Value3 };
        private enum TestEnum2ForChoiceField { Enum2Value1, Enum2Value2, Enum2Value3 };

        private const string ExplicitKey0 = "key0";
        private const string ExplicitKey1 = "key1";
        private const string ExplicitKey2 = "key2";
        private const string ExplicitValue0 = "VALUE0";
        private const string ExplicitValue1 = "VALUE1";
        private const string ExplicitValue2 = "VALUE2";

        #region <ContentType name='ChoiceFieldIndexingTestContentType' ...
        readonly string _choiceFieldIndexingTestContentType = $@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='ChoiceFieldIndexingTestContentType' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <AllowIncrementalNaming>true</AllowIncrementalNaming>
  <Fields>
    <Field name='Choice_ExplicitValues' type='Choice'>
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <AllowExtraValue>false</AllowExtraValue>
        <Options>
          <Option value='{ExplicitKey0}'>{ExplicitValue0}</Option>
          <Option value='{ExplicitKey1}'>{ExplicitValue1}</Option>
          <Option value='{ExplicitKey2}'>{ExplicitValue2}</Option>
        </Options>
      </Configuration>
    </Field>
    <Field name='Choice_Enum' type='Choice'>
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <AllowExtraValue>false</AllowExtraValue>
        <Options>
          <Enum type='{typeof(TestEnum1ForChoiceField).FullName}' />
        </Options>
      </Configuration>
    </Field>
    <Field name='Choice_Enum_Localized' type='Choice'>
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <AllowExtraValue>false</AllowExtraValue>
        <Options>
          <Enum type='{typeof(TestEnum2ForChoiceField).FullName}' />
        </Options>
      </Configuration>
    </Field>
  </Fields>
</ContentType>
";
        #endregion

        [TestMethod]
        public void Indexing_Choice_ExplicitValues()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(_choiceFieldIndexingTestContentType);
                var root = CreateTestRoot();

                var contents = (new[]{ ExplicitKey0, ExplicitKey1, ExplicitKey2})
                    .Select(x =>
                    {
                        var content = Content.CreateNew("ChoiceFieldIndexingTestContentType", root, $"Content_{x}");
                        content["Choice_ExplicitValues"] = x;
                        content.Save();
                        return content;
                    }).ToArray();

                var result1 =
                    CreateSafeContentQuery($"Choice_ExplicitValues:${ExplicitKey1}")
                        .Execute();
                var result2 =
                    CreateSafeContentQuery($"Choice_ExplicitValues:{ExplicitValue1}")
                        .Execute();

                Assert.AreEqual(1, result1.Count);
                Assert.AreEqual(1, result2.Count);
            });
        }

        //[TestMethod]/
        public void Indexing_Choice_Enum_NotLocalized()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(_choiceFieldIndexingTestContentType);
                var root = CreateTestRoot();

                var contents = Enum.GetValues(typeof(TestEnum1ForChoiceField))
                    .OfType<TestEnum1ForChoiceField>()
                    .Select(x =>
                    {
                        var content = Content.CreateNew("ChoiceFieldIndexingTestContentType", root, $"Content_{x}");
                        content["Choice_Enum"] = (int)x;
                        content.Save();
                        return content;
                    }).ToArray();

                var result1 =
                    CreateSafeContentQuery($"Choice_Enum:${(int)TestEnum1ForChoiceField.Enum1Value2}")
                    .Execute();
                var result2 =
                    CreateSafeContentQuery($"Choice_Enum:{TestEnum1ForChoiceField.Enum1Value2.ToString()}")
                    .Execute();

                Assert.AreEqual(1, result1.Count);
                Assert.AreEqual(1, result2.Count);
            });
        }
        //[TestMethod]
        public void Indexing_Choice_Enum_Localized()
        {
            var typeName = typeof(TestEnum2ForChoiceField).Name;
            var ctName = "ChoiceFieldIndexingTestContentType";
            var localizationData = new Dictionary<string, List<string>>
            {
                { "en", new List<string>
                    {
                        $"{TestEnum2ForChoiceField.Enum2Value1}en",
                        $"{TestEnum2ForChoiceField.Enum2Value2}en",
                        $"{TestEnum2ForChoiceField.Enum2Value3}en",
                    }
                },
                { "xx", new List<string>
                    {
                        $"{TestEnum2ForChoiceField.Enum2Value1}xx",
                        $"{TestEnum2ForChoiceField.Enum2Value2}xx",
                        $"{TestEnum2ForChoiceField.Enum2Value3}xx",
                    }
                },
            };
            var xml = GetResourceXml(ctName, "Choice_Enum_Localized", localizationData);

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(_choiceFieldIndexingTestContentType);
                InstallResourceFile("CtdResourcesXX.xml", xml);

                var root = CreateTestRoot();

                var contents = Enum.GetValues(typeof(TestEnum2ForChoiceField))
                    .OfType<TestEnum2ForChoiceField>()
                    .Select(x =>
                    {
                        var content = Content.CreateNew("ChoiceFieldIndexingTestContentType", root, $"Content_{x}");
                        content["Choice_Enum_Localized"] = (int)x;
                        content.Save();
                        return content;
                    }).ToArray();

                var result1 = CreateSafeContentQuery(
                        $"Choice_Enum_Localized:${(int)TestEnum2ForChoiceField.Enum2Value2}").Execute();
                var result2 = CreateSafeContentQuery(
                        $"Choice_Enum_Localized:{TestEnum2ForChoiceField.Enum2Value2.ToString()}").Execute();
                var result3 = CreateSafeContentQuery(
                        $"Choice_Enum_Localized:{TestEnum2ForChoiceField.Enum2Value2}en").Execute();
                var result4 = CreateSafeContentQuery(
                        $"Choice_Enum_Localized:{TestEnum2ForChoiceField.Enum2Value2}xx").Execute();

                Assert.AreEqual(1, result1.Count);
                Assert.AreEqual(1, result2.Count);
                Assert.AreEqual(1, result3.Count);
                Assert.AreEqual(1, result4.Count);
            });
        }

        private void InstallResourceFile(string fileName, string xml)
        {
            var localizationFolder = Node.LoadNode("/Root/Localization");
            if (localizationFolder == null)
            {
                localizationFolder = new SystemFolder(Repository.Root, "Resources") {Name = "Localization"};
                localizationFolder.Save();
            }
            var resourceNode = new Resource(localizationFolder) {Name = fileName};
            resourceNode.Binary.SetStream(RepositoryTools.GetStreamFromString(xml));
            resourceNode.Save();

            SenseNetResourceManager.Reset();
        }

        private string GetResourceXml(string ctName, string fieldName, Dictionary<string, List<string>> localizationData)
        {
            const string en = "en";
            const string xx = "xx";
            var key = $"Enum-{ctName}-{fieldName}";

            return $@"<?xml version='1.0' encoding='utf-8'?>
<?xml-stylesheet type='text/xsl' href='view.xslt'?>
<Resources>
  <ResourceClass name='Ctd'>
    <Languages>
      <Language cultureName='{en}'>
        <data name='{key}-{TestEnum2ForChoiceField.Enum2Value1}' xml:space='preserve'>
          <value>{localizationData[en][0]}</value>
        </data>
        <data name='{key}-{TestEnum2ForChoiceField.Enum2Value2}' xml:space='preserve'>
          <value>{localizationData[en][1]}</value>
        </data>
        <data name='{key}-{TestEnum2ForChoiceField.Enum2Value3}' xml:space='preserve'>
          <value>{localizationData[en][2]}</value>
        </data>
      </Language>
      <Language cultureName='{xx}'>
        <data name='{key}-{TestEnum2ForChoiceField.Enum2Value1}' xml:space='preserve'>
          <value>{localizationData[xx][0]}</value>
        </data>
        <data name='{key}-{TestEnum2ForChoiceField.Enum2Value2}' xml:space='preserve'>
          <value>{localizationData[xx][1]}</value>
        </data>
        <data name='{key}-{TestEnum2ForChoiceField.Enum2Value3}' xml:space='preserve'>
          <value>{localizationData[xx][2]}</value>
        </data>
      </Language>
    </Languages>
  </ResourceClass>
</Resources>
";
        }

        #endregion

        #region IndexHandler

        #region <ContentType name='JsonFieldIndexingTestContentType' ...
        readonly string _jsonFieldIndexingTestContentType = $@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='JsonFieldIndexingTestContentType' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <Fields>
    <Field name='JsonExtrafield1' type='LongText'>
        <Indexing>
         <IndexHandler>SenseNet.Search.Indexing.GeneralJsonIndexHandler</IndexHandler>
      </Indexing>
    </Field>
  </Fields>
</ContentType>
";
        #endregion

        #region JSON samples
        private const string Json1 = @"
{
    'String': 'abc',
    'Int': 123,
    'Array': [ 'a', 'b' ],
    'Object': {
        'p1': 'def',
        'p2': 456
    },
    'o2': null
}
";
        private const string Json2 = @"
{
    'String': 'ghi',
    'Int': 789,
    'Object': {
        'p1': 'jkl',
        'p2': 123
    }
}
";
        #endregion

        [TestMethod]
        public void Indexing_Json_IndexHandler()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(_jsonFieldIndexingTestContentType);                           
                
                var root = CreateTestRoot();
                var tc1 = Content.CreateNew("JsonFieldIndexingTestContentType", root, "JC1");
                tc1["JsonExtrafield1"] = Json1;
                tc1.Save();

                var indexFields = tc1.Fields["JsonExtrafield1"].GetIndexFields(out var textExtract).ToArray();
                var indexValues = indexFields.Single().StringArrayValue;

                Assert.AreEqual("string#abc", indexValues[0]);
                Assert.AreEqual("int#123", indexValues[1]);
                Assert.AreEqual("object#p1#def", indexValues[2]);
                Assert.AreEqual("object#p2#456", indexValues[3]);
                Assert.AreEqual("o2#null", indexValues[4]);                

                // create another content with a different json to check querying
                var tc2 = Content.CreateNew("JsonFieldIndexingTestContentType", root, "JC2");
                tc2["JsonExtrafield1"] = Json2;
                tc2.Save();

                var query = CreateSafeContentQuery("JsonExtrafield1:String#abc", QuerySettings.AdminSettings).Execute();

                Assert.AreEqual(tc1.Id, query.Identifiers.Single());

                query = CreateSafeContentQuery("JsonExtrafield1:object#p2#123", QuerySettings.AdminSettings).Execute();

                Assert.AreEqual(tc2.Id, query.Identifiers.Single());
            });
        }

        #endregion

        // ReSharper disable once InconsistentNaming
        private class TestEventLoggerForIndexing_ExecuteUnprocessed_FaultTolerance : IEventLogger
        {
            public List<string> Events { get; } = new List<string>();
            public void Write(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title,
                IDictionary<string, object> properties)
            {
                Events.Add($"{severity}: {message}");
            }
        }

        [TestMethod, TestCategory("IR")]
        public async STT.Task Indexing_ExecuteUnprocessed_FaultToleranceAsync()
        {
            // Temporary storages for manage repository's restart.
            InMemoryDataProvider dataProvider = null;
            InMemorySearchEngine searchProvider = null;

            // Storage for new contents' ids and version ids
            var ids = new Tuple<int, int>[4];

            // Regular start.
            Test(() =>
            {
                // Memorize instances.
                dataProvider = (InMemoryDataProvider)DataStore.DataProvider;
                searchProvider = (InMemorySearchEngine)SearchManager.SearchEngine;

                // Create 8 activities.
                for (int i = 0; i < 4; i++)
                {
                    // "Add" activity (1, 3, 5, 7).
                    var content = Content.CreateNew("SystemFolder", Repository.Root, $"Folder{i}");
                    content.Save();
                    ids[i] = new Tuple<int, int>(content.Id, content.ContentHandler.VersionId);
                    // "Update" activity (2, 4, 6, 8).
                    content.Index++;
                    content.Save();
                }
            });

            // Error simulation: remove the index document of the "Folder2", "Folder3".
            var versions = dataProvider.DB.Versions
                .Where(v => v.VersionId == ids[1].Item2 || v.VersionId == ids[2].Item2);
            foreach (var version in versions)
                version.IndexDocument = string.Empty;

            // Roll back the time. Expected unprocessed sequence when next restart:
            //   Update "Folder2" (error), Add "Folder3", Update "Folder3", ...
            await ((InMemoryIndexingEngine)searchProvider.IndexingEngine)
                .WriteActivityStatusToIndexAsync(new IndexingActivityStatus { LastActivityId = 3 }, CancellationToken.None).ConfigureAwait(false);

            // ACTION
            // Restart the repository with the known provider instances.
            var originalLogger = Configuration.Providers.Instance.EventLogger;
            var logger = new TestEventLoggerForIndexing_ExecuteUnprocessed_FaultTolerance();
            try
            {
                Test(builder =>
                {
                    builder
                        .UseLogger(logger)
                        .UseDataProvider(dataProvider)
                        .UseInitialData(null)
                        .UseSearchEngine(searchProvider)
                        // rewrite these instances with the original base dataProvider.
                        .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dataProvider))
                        .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider));

                }, () =>
                {
                    // Do nothing but started successfully
                });
            }
            catch (Exception e)
            {
                Assert.Fail("Restart failed: " + e.Message);
            }
            finally
            {
                Configuration.Providers.Instance.EventLogger = originalLogger;
            }

            // ASSERT
            // 1 - Check the indexing status
            // Before fix the last activity id was ok but the status had 3 gaps
            // After fix all activities need to be executed.
            var status = await ((InMemoryIndexingEngine)searchProvider.IndexingEngine)
                .ReadActivityStatusFromIndexAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(8, status.LastActivityId);
            Assert.AreEqual(0, status.Gaps.Length);

            // 2 - Check the existing warning in the log.
            // The original version (before fix) contained three unwanted lines:
            // Error: Indexing activity execution error. Activity: #5 (AddDocument)\r\nAttempting to deserialize an empty stream.
            // Error: Indexing activity execution error. Activity: #4 (UpdateDocument)\r\nAttempting to deserialize an empty stream.
            // Error: Indexing activity execution error. Activity: #6 (UpdateDocument)\r\nAttempting to deserialize an empty stream.
            // After fix only one warning is expected with the list of the problematic versionIds
            var relevatEvent = logger.Events
                .FirstOrDefault(e => e.StartsWith("Warning: Cannot index"));
            Assert.IsNotNull(relevatEvent);

            var expectedIds = $"{ids[1].Item1},{ids[1].Item2}; {ids[2].Item1},{ids[2].Item2}";
            Assert.IsTrue(relevatEvent.Contains(expectedIds), $"Expected Ids: {expectedIds}, Event src: {relevatEvent}");
        }

        private GenericContent CreateTestRoot()
        {
            var node = new SystemFolder(Repository.Root) { Name = "_IndexingTests" };
            node.Save();
            return node;
        }

    }
}
