using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Search.Indexing;

namespace SnInitialDataGenerator.Tests
{
    [TestClass]
    public class GeneratorTests
    {
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
        [TestMethod]
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
                var dump = InitialData.GetHexDump(stream);
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
            var dump = InitialData.GetHexDump(stream);

            // ASSERT
            var lines = dump.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            var actual = string.Join("", lines.Select(x => x.Substring(48)));

            var expected = "................................" +
                " !.#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[.]^_`abcdefghijklmnopqrstuvwxyz{|}~." +
                "................................................................................................" +
                "................................";
            
            Assert.AreEqual(expected, actual);
        }

    }
}
