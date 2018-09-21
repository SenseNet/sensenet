using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Tests;

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

        private GenericContent CreateTestRoot()
        {
            var node = new SystemFolder(Repository.Root) { Name = "_IndexingTests" };
            node.Save();
            return node;
        }

    }
}
