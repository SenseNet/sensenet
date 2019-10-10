using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Indexing;
using SenseNet.Tests;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class IndexSerializationTests : TestBase
    {
        [TestMethod]
        public void IndxSer_Deserialization()
        {
            // ARRANGE
            var doc = new IndexDocument
            {
                new IndexField("String1", "value", IndexingMode.Default, IndexStoringMode.Default, IndexTermVector.Default),
                new IndexField("StringArray1", new[] {"value1", "value2"}, IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.No),
                new IndexField("Boolean1", true, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.WithOffsets),
                new IndexField("Integer1", 42, IndexingMode.No, IndexStoringMode.Default, IndexTermVector.WithPositions),
                new IndexField("Long1", 42L, IndexingMode.Analyzed, IndexStoringMode.Default, IndexTermVector.WithPositionsOffsets),
                new IndexField("Float1", (float) 123.45, IndexingMode.NotAnalyzed, IndexStoringMode.Default, IndexTermVector.Yes),
                new IndexField("Double1", 123.45d, IndexingMode.NotAnalyzedNoNorms, IndexStoringMode.Default, IndexTermVector.Default),
                new IndexField("DateTime1", new DateTime(2019, 04, 19, 9, 38, 15), IndexingMode.Default, IndexStoringMode.Default, IndexTermVector.Default)
            };
            var serialized = doc.Serialize();

            // ACTION
            var deserialized = IndexDocument.Deserialize(serialized);

            // ASSERT
            Assert.AreNotSame(doc, deserialized);
            Assert.AreEqual(serialized, deserialized.Serialize());
        }
    }
}
