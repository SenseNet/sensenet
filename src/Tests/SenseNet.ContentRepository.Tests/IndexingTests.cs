using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    internal class CustomTextExtractor : TextExtractor
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

    [TestClass]
    public class IndexingTests : TestBase
    {
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

    }
}
