using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Tests;
using SenseNet.Tests.Core;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class IndexSerializationTests : TestBase
    {
        [TestMethod, TestCategory("Services")]
        public void IndexSerialization_IndexDocument_CSrv()
        {
            // ARRANGE
            var doc = new IndexDocument
            {
                new IndexField("String1", "value", IndexingMode.Default, IndexStoringMode.Default, IndexTermVector.Default),
                new IndexField("StringArray1", new[] {"value1", "value2"}, IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.No),
                new IndexField("Boolean1", true, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.WithOffsets),
                new IndexField("Integer1", 42, IndexingMode.No, IndexStoringMode.Default, IndexTermVector.WithPositions),
                new IndexField("IntegerArray1", new[] {42, 43, 44}, IndexingMode.NotAnalyzed, IndexStoringMode.Default, IndexTermVector.WithPositions),
                new IndexField("Long1", 42L, IndexingMode.Analyzed, IndexStoringMode.Default, IndexTermVector.WithPositionsOffsets),
                new IndexField("Float1", (float) 123.45, IndexingMode.NotAnalyzed, IndexStoringMode.Default, IndexTermVector.Yes),
                new IndexField("Double1", 123.45d, IndexingMode.NotAnalyzedNoNorms, IndexStoringMode.Default, IndexTermVector.Default),
                new IndexField("DateTime1", new DateTime(2019, 04, 19, 9, 38, 15), IndexingMode.Default, IndexStoringMode.Default, IndexTermVector.Default)
            };

            // ACT
            var serialized = doc.Serialize();
            var deserialized = IndexDocument.Deserialize(serialized);

            // ASSERT
            var expected = "[\r\n  {\r\n    \"Name\": \"String1\",\r\n    \"Type\": \"String\",\r\n    \"Value\": \"value\"\r\n  },\r\n";
            Assert.IsTrue(serialized.StartsWith(expected));
            Assert.AreNotSame(doc, deserialized);
            Assert.AreEqual(serialized, deserialized.Serialize());
        }

        [TestMethod, TestCategory("Services")]
        public void IndexSerialization_DocumentUpdate()
        {
            var documentUpdate = new DocumentUpdate
            {
                UpdateTerm = new SnTerm("String1", "Value1"),
                Document = new IndexDocument
                {
                    new IndexField("String1", "value", IndexingMode.Analyzed, IndexStoringMode.Default,
                        IndexTermVector.Default),
                    new IndexField("Integer1", 42, IndexingMode.No, IndexStoringMode.Yes,
                        IndexTermVector.Default),
                }
            };

            // ACT
            var serialized = documentUpdate.Serialize();
            var deserialized = DocumentUpdate.Deserialize(serialized);

            // ASSERT
            var expected = @"{
  ""UpdateTerm"": {""Name"": ""String1"", ""Type"": ""String"", ""Value"": ""Value1""},
  ""Document"": [
    {""Name"": ""String1"", ""Type"": ""String"", ""Mode"": ""Analyzed"", ""Value"": ""value""},
    {""Name"": ""Integer1"", ""Type"": ""Int"", ""Mode"": ""No"", ""Store"": ""Yes"", ""Value"": 42}
  ]
}";

            Assert.AreEqual(
                expected.Replace("\r", "").Replace("\n", "").Replace(" ", ""),
                serialized.Replace("\r", "").Replace("\n", "").Replace(" ", ""));

            Assert.AreNotSame(documentUpdate, deserialized);
            Assert.AreEqual(serialized, deserialized.Serialize());
        }


        [TestMethod]
        public void IndexSerialization_IndexField_NameTypeValue()
        {
            var im = IndexingMode.Default;
            var ism = IndexStoringMode.Default;
            var itv = IndexTermVector.Default;
            var doc = new IndexDocument
            {
                new IndexField("String1", "value", im, ism, itv),
                new IndexField("StringArray1", new[] {"value1", "value2"}, im, ism, itv),
                new IndexField("Boolean1", true, im, ism, itv),
                new IndexField("Integer1", 42, im, ism, itv),
                new IndexField("IntegerArray1", new[] {42, 43, 44}, im, ism, itv),
                new IndexField("Long1", 42L, im, ism, itv),
                new IndexField("Float1", (float) 123.45, im, ism, itv),
                new IndexField("Double1", 123.45d, im, ism, itv),
                new IndexField("DateTime1", new DateTime(2019, 04, 19, 9, 38, 15), im, ism, itv),
            };
            foreach (var indexField in doc.Fields.Values)
            {
                // ACT
                var serialized = indexField.Serialize();
                var deserialized = IndexField.Deserialize(serialized);

                // ASSERT
                var name = indexField.Name;
                var type = indexField.Type.ToString();
                string value;
                switch (indexField.Type)
                {
                    case IndexValueType.String:
                        value = $"\"{indexField.StringValue}\"";
                        break;
                    case IndexValueType.StringArray:
                        value = "[\r\n" + string.Join(",\r\n", indexField.StringArrayValue.Select(s => $"    \"{s}\"")) + "\r\n  ]";
                        break;
                    case IndexValueType.Bool:
                        value = indexField.BooleanValue.ToString().ToLowerInvariant();
                        break;
                    case IndexValueType.Int:
                        value = indexField.IntegerValue.ToString();
                        break;
                    case IndexValueType.IntArray:
                        value = "[" + string.Join(",", indexField.IntegerArrayValue) + "]";
                        break;
                    case IndexValueType.Long:
                        value = indexField.LongValue.ToString();
                        break;
                    case IndexValueType.Float:
                        value = indexField.SingleValue.ToString(CultureInfo.InvariantCulture);
                        break;
                    case IndexValueType.Double:
                        value = indexField.DoubleValue.ToString(CultureInfo.InvariantCulture);
                        break;
                    case IndexValueType.DateTime:
                        value = $"\"{indexField.DateTimeValue:yyyy-MM-ddTHH:mm:ssZ}\"";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var expected = $"{{\r\n  \"Name\": \"{name}\",\r\n  \"Type\": \"{type}\",\r\n  \"Value\": {value}\r\n}}";
                Assert.AreEqual(expected, serialized);
                Assert.AreNotSame(indexField, deserialized);
                Assert.AreEqual(serialized, deserialized.Serialize());
            }
        }
        [TestMethod]
        public void IndexSerialization_IndexField_Enums()
        {
            var im = IndexingMode.Default;
            var ism = IndexStoringMode.Default;
            var itv = IndexTermVector.Default;
            var fields = new[]
            {
                new IndexField("Field", "value", im, ism, itv),
                new IndexField("Field", "value", im, ism, IndexTermVector.WithPositionsOffsets),
                new IndexField("Field", "value", im, IndexStoringMode.Yes, itv),
                new IndexField("Field", "value", IndexingMode.AnalyzedNoNorms, ism, itv),
                new IndexField("Field", "value", IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.WithPositionsOffsets),
            };
            var expected = new[]
            {
                "{\r\n  \"Name\": \"Field\",\r\n  \"Type\": \"String\",\r\n  \"Value\": \"value\"\r\n}",
                "{\r\n  \"Name\": \"Field\",\r\n  \"Type\": \"String\",\r\n  \"TermVector\": \"WithPositionsOffsets\",\r\n  \"Value\": \"value\"\r\n}",
                "{\r\n  \"Name\": \"Field\",\r\n  \"Type\": \"String\",\r\n  \"Store\": \"Yes\",\r\n  \"Value\": \"value\"\r\n}",
                "{\r\n  \"Name\": \"Field\",\r\n  \"Type\": \"String\",\r\n  \"Mode\": \"AnalyzedNoNorms\",\r\n  \"Value\": \"value\"\r\n}",
                "{\r\n  \"Name\": \"Field\",\r\n  \"Type\": \"String\",\r\n  \"Mode\": \"AnalyzedNoNorms\",\r\n  \"Store\": \"Yes\",\r\n  \"TermVector\": \"WithPositionsOffsets\",\r\n  \"Value\": \"value\"\r\n}",
            };
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];

                // ACT
                var serialized = field.Serialize();
                var deserialized = IndexField.Deserialize(serialized);

                // ASSERT
                Assert.AreEqual(expected[i], serialized);
                Assert.AreNotSame(field, deserialized);
                Assert.AreEqual(serialized, deserialized.Serialize());
            }
        }

        [TestMethod]
        public void IndexSerialization_SnTerm()
        {
            var terms = new []
            {
                new SnTerm("String1", "value"),
                new SnTerm("StringArray1", new[] {"value1", "value2"}),
                new SnTerm("Boolean1", true),
                new SnTerm("Integer1", 42),
                new SnTerm("IntegerArray1", new[] {42, 43, 44}),
                new SnTerm("Long1", 42L),
                new SnTerm("Float1", (float) 123.45),
                new SnTerm("Double1", 123.45d),
                new SnTerm("DateTime1", new DateTime(2019, 04, 19, 9, 38, 15)),
            };
            foreach (var term in terms)
            {
                // ACT
                var serialized = term.Serialize();
                var deserialized = SnTerm.Deserialize(serialized);

                // ASSERT
                var name = term.Name;
                var type = term.Type.ToString();
                string value;
                switch (term.Type)
                {
                    case IndexValueType.String:
                        value = $"\"{term.StringValue}\"";
                        break;
                    case IndexValueType.StringArray:
                        value = "[" + string.Join(",", term.StringArrayValue.Select(s => $"\"{s}\"")) + "]";
                        break;
                    case IndexValueType.Bool:
                        value = term.BooleanValue.ToString().ToLowerInvariant();
                        break;
                    case IndexValueType.Int:
                        value = term.IntegerValue.ToString();
                        break;
                    case IndexValueType.IntArray:
                        value = "[" + string.Join(",", term.IntegerArrayValue) + "]";
                        break;
                    case IndexValueType.Long:
                        value = term.LongValue.ToString();
                        break;
                    case IndexValueType.Float:
                        value = term.SingleValue.ToString(CultureInfo.InvariantCulture);
                        break;
                    case IndexValueType.Double:
                        value = term.DoubleValue.ToString(CultureInfo.InvariantCulture);
                        break;
                    case IndexValueType.DateTime:
                        value = $"\"{term.DateTimeValue:yyyy-MM-ddTHH:mm:ssZ}\"";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var expected = $"{{\r\n  \"Name\": \"{name}\",\r\n  \"Type\": \"{type}\",\r\n  \"Value\": {value}\r\n}}";
                Assert.AreEqual(expected, serialized);
                Assert.AreNotSame(term, deserialized);
                Assert.AreEqual(serialized, deserialized.Serialize());
            }
        }
    }
}
