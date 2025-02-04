using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;
using BinaryData = SenseNet.ContentRepository.Storage.BinaryData;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class FieldIndexHandlerTests : TestBase
    {
        private class IndexableField : IIndexableField
        {
            public string Name => "TestField";
            public bool IsInIndex => true;
            public bool IsBinaryField => false;
            private object _value;

            public IndexableField(object value)
            {
                _value = value;
            }

            public object GetData()
            {
                return _value;
            }
            public IEnumerable<IndexField> GetIndexFields(out string textExtract)
            {
                throw new NotImplementedException();
            }

            public bool LocalizationEnabled { get; } = false;
            public bool IsLocalized { get; } = false;
            public string GetStoredValue()
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_BooleanIndexHandler()
        {
            var fieldValue = true;
            var fieldValueObject = (object)fieldValue;
            var snField = new IndexableField(fieldValueObject);

            var fieldIndexHandler = new BooleanIndexHandler();
            fieldIndexHandler.OwnerIndexingInfo = new TestPerfieldIndexingInfoBool();

            var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
            var parsed = fieldIndexHandler.Parse("True");
            var termValue = fieldIndexHandler.ConvertToTermValue(fieldValue);
            Assert.AreEqual(indexed.First().Type, parsed.Type);
            Assert.AreEqual(parsed.Type, termValue.Type);

            var retrieved = fieldIndexHandler.GetBack("yes");
            Assert.AreEqual(fieldValue, retrieved);
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_IntegerIndexHandler()
        {
            var fieldValue = 42;
            var fieldValueObject = (object)fieldValue;
            var snField = new IndexableField(fieldValueObject);

            var fieldIndexHandler = new IntegerIndexHandler();
            fieldIndexHandler.OwnerIndexingInfo = new TestPerfieldIndexingInfoInt();

            var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
            var parsed = fieldIndexHandler.Parse("42");
            var termValue = fieldIndexHandler.ConvertToTermValue(fieldValue);
            Assert.AreEqual(indexed.First().Type, parsed.Type);
            Assert.AreEqual(parsed.Type, termValue.Type);

            var retrieved = fieldIndexHandler.GetBack("42");
            Assert.AreEqual(fieldValue, retrieved);
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_NumberIndexHandler()
        {
            var fieldValue = Convert.ToDecimal(42L + int.MaxValue);
            var fieldValueObject = (object)fieldValue;
            var snField = new IndexableField(fieldValueObject);

            var fieldIndexHandler = new NumberIndexHandler();
            fieldIndexHandler.OwnerIndexingInfo = new TestPerfieldIndexingInfoLong();

            var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
            var parsed = fieldIndexHandler.Parse(fieldValue.ToString());
            var termValue = fieldIndexHandler.ConvertToTermValue(fieldValue);
            Assert.AreEqual(indexed.First().Type, parsed.Type);
            Assert.AreEqual(parsed.Type, termValue.Type);

            var retrieved = fieldIndexHandler.GetBack(fieldValue.ToString());
            Assert.AreEqual(fieldValue, retrieved);
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_LongTextIndexHandler()
        {
            var fieldValue = "Long text.";
            var fieldValueObject = (object)fieldValue;
            var snField = new IndexableField(fieldValueObject);

            var fieldIndexHandler = new LongTextIndexHandler();
            fieldIndexHandler.OwnerIndexingInfo = new TestPerfieldIndexingInfoString();

            var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
            var parsed = fieldIndexHandler.Parse(fieldValue.ToString());
            var termValue = fieldIndexHandler.ConvertToTermValue(fieldValue);
            Assert.AreEqual(indexed.First().Type, parsed.Type);
            Assert.AreEqual(parsed.Type, termValue.Type);

            // get back is not supported
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_RichTextIndexHandler()
        {
            var fieldValue = new RichTextFieldValue {Text = "Lorem ipsum", Editor = "{dolor: 'sit amet'}"};
            var fieldValueInIndex = "lorem ipsum";
            var fieldValueObject = (object)fieldValue;
            var snField = new IndexableField(fieldValueObject);

            var fieldIndexHandler = new RichTextIndexHandler();
            fieldIndexHandler.OwnerIndexingInfo = new TestPerfieldIndexingInfoString();

            var indexed = fieldIndexHandler.GetIndexFields(snField, out _).ToArray();
            var parsed = fieldIndexHandler.Parse(fieldValueInIndex.ToString());
            var termValue = fieldIndexHandler.ConvertToTermValue(fieldValueInIndex);
            Assert.AreEqual(indexed[0].Type, parsed.Type);
            Assert.AreEqual(fieldValueInIndex, indexed[0].StringValue);
            Assert.AreEqual(parsed.Type, termValue.Type);

            // get back is not supported
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_InFolderIndexHandler()
        {
            var fieldValue = "/Root/A/B";
            var fieldValueObject = (object)fieldValue;
            var snField = new IndexableField(fieldValueObject);

            var fieldIndexHandler = new InFolderIndexHandler();
            fieldIndexHandler.OwnerIndexingInfo = new TestPerfieldIndexingInfoString();

            var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
            var parsed = fieldIndexHandler.Parse(fieldValue.ToString());
            var termValue = fieldIndexHandler.ConvertToTermValue(fieldValue);
            Assert.AreEqual(indexed.First().Type, parsed.Type);
            Assert.AreEqual(parsed.Type, termValue.Type);

            var retrieved = fieldIndexHandler.GetBack("/Root/A/B");
            Assert.AreEqual(fieldValue, retrieved);
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_InTreeIndexHandler()
        {
            var fieldValue = "/Root/A/B";
            var fieldValueObject = (object)fieldValue;
            var snField = new IndexableField(fieldValueObject);

            var fieldIndexHandler = new InTreeIndexHandler();
            fieldIndexHandler.OwnerIndexingInfo = new TestPerfieldIndexingInfoString();

            var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
            var parsed = fieldIndexHandler.Parse(fieldValue.ToString());
            var termValue = fieldIndexHandler.ConvertToTermValue(fieldValue);
            Assert.AreEqual(indexed.First().Type, parsed.Type);
            Assert.AreEqual(parsed.Type, termValue.Type);

            // get back is not supported
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_HyperLinkIndexHandler()
        {
            var fieldValue = new HyperLinkField.HyperlinkData("href", "text", "title", "target");
            var fieldValueObject = (object)fieldValue;
            var snField = new IndexableField(fieldValueObject);

            var fieldIndexHandler = new HyperLinkIndexHandler();
            fieldIndexHandler.OwnerIndexingInfo = new TestPerfieldIndexingInfoString();

            var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
            Assert.AreEqual(IndexValueType.StringArray, indexed.First().Type);
            var parsed = fieldIndexHandler.Parse(fieldValue.ToString());
            Assert.AreEqual(IndexValueType.String, parsed.Type);
            var termValue = fieldIndexHandler.ConvertToTermValue(fieldValue.ToString());
            Assert.AreEqual(parsed.Type, termValue.Type);

            // get back is not supported
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_PermissionChoiceIndexHandler()
        {
            var fieldValue = new[] { "href", "text", "title", "target" };
            var fieldValueObject = (object)fieldValue;
            var snField = new IndexableField(fieldValueObject);

            var fieldIndexHandler = new PermissionChoiceIndexHandler();
            fieldIndexHandler.OwnerIndexingInfo = new TestPerfieldIndexingInfoString();

            var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
            Assert.AreEqual(IndexValueType.StringArray, indexed.First().Type);
            var parsed = fieldIndexHandler.Parse(fieldValue.ToString());
            Assert.AreEqual(IndexValueType.String, parsed.Type);
            var termValue = fieldIndexHandler.ConvertToTermValue(fieldValue.ToString());
            Assert.AreEqual(parsed.Type, termValue.Type);

            // get back is not supported
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_TagIndexHandler()
        {
            var fieldValue = "Tag1,Tag2,Tag3";
            var fieldValueObject = (object)fieldValue;
            var snField = new IndexableField(fieldValueObject);

            var fieldIndexHandler = new TagIndexHandler();
            fieldIndexHandler.OwnerIndexingInfo = new TestPerfieldIndexingInfoString();

            var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
            Assert.AreEqual(IndexValueType.StringArray, indexed.First().Type);
            var parsed = fieldIndexHandler.Parse(fieldValue.ToString());
            Assert.AreEqual(IndexValueType.String, parsed.Type);
            var termValue = fieldIndexHandler.ConvertToTermValue(fieldValue.ToString());
            Assert.AreEqual(parsed.Type, termValue.Type);

            var retrieved = fieldIndexHandler.GetBack("tag1");
            Assert.AreEqual("tag1", retrieved);
        }

        /* ===================================================================== tests with repo */

        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_LowerStringIndexHandler()
        {
            Test(() =>
            {
                var content = Content.Load(Identifiers.PortalRootId);
                var contentName = content.Name;
                var snField = content.Fields["Name"];

                var fieldIndexHandler = new LowerStringIndexHandler();
                fieldIndexHandler.OwnerIndexingInfo = snField.FieldSetting.IndexingInfo;

                var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
                Assert.AreEqual(IndexValueType.String, indexed.First().Type);
                var parsed = fieldIndexHandler.Parse(contentName);
                Assert.AreEqual(IndexValueType.String, parsed.Type);
                var termValue = fieldIndexHandler.ConvertToTermValue(contentName);
                Assert.AreEqual(parsed.Type, termValue.Type);

                var retrieved = fieldIndexHandler.GetBack(contentName);
                Assert.AreEqual(contentName, retrieved);
            });
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_DateTimeIndexHandler()
        {
            Test(() =>
            {
                var content = Content.Load(Identifiers.PortalRootId);
                var contentName = content.Name;
                var snField = content.Fields["CreationDate"];

                var fieldIndexHandler = new DateTimeIndexHandler();
                fieldIndexHandler.OwnerIndexingInfo = snField.FieldSetting.IndexingInfo;

                var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
                Assert.AreEqual(IndexValueType.DateTime, indexed.First().Type);
                var parsed = fieldIndexHandler.Parse("2018-02-19 08:12:24");
                Assert.AreEqual(IndexValueType.DateTime, parsed.Type);
                var termValue = fieldIndexHandler.ConvertToTermValue(DateTime.Now);
                Assert.AreEqual(parsed.Type, termValue.Type);

                var now = DateTime.Now;
                var retrieved = fieldIndexHandler.GetBack(now.Ticks.ToString());
                Assert.AreEqual(now, retrieved);
            });
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_TypeTreeIndexHandler()
        {
            Test(() =>
            {
                var content = Content.Load(Identifiers.PortalRootId);
                var contentName = content.Name;
                var snField = content.Fields["Id"];

                var fieldIndexHandler = new TypeTreeIndexHandler();
                fieldIndexHandler.OwnerIndexingInfo = snField.FieldSetting.IndexingInfo;

                var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
                Assert.AreEqual(IndexValueType.StringArray, indexed.First().Type);
                var parsed = fieldIndexHandler.Parse("PortalRoot");
                Assert.AreEqual(IndexValueType.String, parsed.Type);
                var termValue = fieldIndexHandler.ConvertToTermValue("PortalRoot");
                Assert.AreEqual(parsed.Type, termValue.Type);

                // get back is not supported
            });
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_ExclusiveTypeIndexHandler()
        {
            Test(() =>
            {
                var content = Content.Load(Identifiers.PortalRootId);
                var contentName = content.Name;
                var snField = content.Fields["Id"];

                var fieldIndexHandler = new ExclusiveTypeIndexHandler();
                fieldIndexHandler.OwnerIndexingInfo = snField.FieldSetting.IndexingInfo;

                var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
                Assert.AreEqual(IndexValueType.String, indexed.First().Type);
                var parsed = fieldIndexHandler.Parse("PortalRoot");
                Assert.AreEqual(IndexValueType.String, parsed.Type);
                var termValue = fieldIndexHandler.ConvertToTermValue("PortalRoot");
                Assert.AreEqual(parsed.Type, termValue.Type);

                // get back is not supported
            });
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_ReferenceIndexHandler()
        {
            Test(() =>
            {
                var content = Content.Load(Identifiers.PortalRootId);
                var contentName = content.Name;
                var snField = content.Fields["CreatedBy"];

                var fieldIndexHandler = new ReferenceIndexHandler();
                fieldIndexHandler.OwnerIndexingInfo = snField.FieldSetting.IndexingInfo;

                var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
                Assert.AreEqual(IndexValueType.IntArray, indexed.First().Type);
                var parsed = fieldIndexHandler.Parse("2");
                Assert.AreEqual(IndexValueType.Int, parsed.Type);
                var termValue = fieldIndexHandler.ConvertToTermValue(User.Administrator);
                Assert.AreEqual(parsed.Type, termValue.Type);

                var retrieved = fieldIndexHandler.GetBack("42");
                Assert.AreEqual(42, retrieved);
            });
        }


        private class TestMultiRefField : IIndexableField
        {
            public bool IsInIndex => true;
            public bool IsBinaryField => false;
            public string Name { get; }
            private readonly INode[] _references;
            public TestMultiRefField(string name, INode[] references)
            {
                Name = name;
                _references = references;
            }
            public IEnumerable<IndexField> GetIndexFields(out string textExtract) { throw new NotImplementedException(); }
            public bool LocalizationEnabled { get; } = false;
            public bool IsLocalized { get; } = false;
            public string GetStoredValue()
            {
                throw new NotImplementedException();
            }

            public object GetData()
            {
                return _references;
            }
        }
        private class TestNode : INode
        {
            public int Id { get; set; }
            public int ParentId { get; set; }
            public string Path { get; set; }
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_ReferenceIndexHandler_Multi()
        {
            var nodeIds = new[] {1, 3, 5, 7};
            var expectation = new[]
            {
                new IndexField("Refs", nodeIds, IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No)
            };
            var references = nodeIds.Select(id => new TestNode {Id = id}).ToArray();
            var snField = new TestMultiRefField("Refs", references);
            var fieldIndexHandler = new ReferenceIndexHandler
            {
                OwnerIndexingInfo = new TestPerfieldIndexingInfoInt {IndexStoringMode = IndexStoringMode.No}
            };

            // ACTION
            var indexed = fieldIndexHandler.GetIndexFields(snField, out _).ToArray();

            // ASSERT
            indexed.Should().BeEquivalentTo(expectation);
        }

        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_BinaryIndexHandler()
        {
            Test(() =>
            {
                var binaryData = new BinaryData();
                binaryData.FileName = "file1.txt";
                binaryData.SetStream(RepositoryTools.GetStreamFromString("Stream data."));

                var folder = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folder.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var file = new File(folder) { Name = "file1.txt", Binary = binaryData };
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var content = Content.Load(file.Id);

                var contentName = content.Name;
                var snField = content.Fields["Binary"];

                var fieldIndexHandler = new BinaryIndexHandler();
                fieldIndexHandler.OwnerIndexingInfo = snField.FieldSetting.IndexingInfo;

                var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
                Assert.AreEqual(IndexValueType.String, indexed.First().Type);
                var parsed = fieldIndexHandler.Parse("Word1");
                Assert.AreEqual(IndexValueType.String, parsed.Type);
                var termValue = fieldIndexHandler.ConvertToTermValue("Word1");
                Assert.AreEqual(parsed.Type, termValue.Type);

                // get back is not supported
            });
        }
        [TestMethod, TestCategory("IR")]
        public void FieldIndexHandler_ChoiceIndexHandler()
        {
            Test(() =>
            {
                var content = Content.Load(User.Administrator.Id);
                var contentName = content.Name;
                var snField = content.Fields["MaritalStatus"];

                var fieldIndexHandler = new ChoiceIndexHandler();
                fieldIndexHandler.OwnerIndexingInfo = snField.FieldSetting.IndexingInfo;

                var indexed = fieldIndexHandler.GetIndexFields(snField, out _);
                Assert.AreEqual(IndexValueType.StringArray, indexed.First().Type);
                var parsed = fieldIndexHandler.Parse("married");
                Assert.AreEqual(IndexValueType.String, parsed.Type);
                var termValue = fieldIndexHandler.ConvertToTermValue("married");
                Assert.AreEqual(parsed.Type, termValue.Type);

                // get back is not supported
            });
        }
    }
}
