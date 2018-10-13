using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Tests;
// ReSharper disable UnusedMember.Local

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class NotifyChangedTests : TestBase
    {
        [ContentHandler]
        private class ContentHandler1 : GenericContent
        {
            public static readonly string CTD = $@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='ContentHandler1' parentType='GenericContent' handler='{typeof(ContentHandler1).FullName}' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>ContentHandler1</DisplayName>
  <Description>ContentHandler1</Description>
  <Fields>
    <Field name='ShortTextField1' type='ShortText'>
      <Bind property='ShortText1' />
    </Field>
    <Field name='CustomInt1' type='Integer' />
  </Fields>
</ContentType>";

            public ContentHandler1(Node parent) : base(parent) { }
            public ContentHandler1(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
            protected ContentHandler1(NodeToken nt) : base(nt) { }

            [RepositoryProperty(nameof(ShortText1), RepositoryDataType.String)]
            public string ShortText1
            {
                get => base.GetProperty<string>(nameof(ShortText1));
                set => this[nameof(ShortText1)] = value;
            }

            private int _customInt1;
            public int CustomInt1
            {
                get => _customInt1;
                set
                {
                    _customInt1 = value;
                    PropertyChanged();
                }
            }

            public override object GetProperty(string name)
            {
                switch (name)
                {
                    case nameof(ShortText1):
                        return this.ShortText1;
                    case nameof(CustomInt1):
                        return this.CustomInt1;
                    default:
                        return base.GetProperty(name);
                }
            }
            public override void SetProperty(string name, object value)
            {
                switch (name)
                {
                    case nameof(ShortText1):
                        this.ShortText1 = (string)value;
                        break;
                    case nameof(CustomInt1):
                        this.CustomInt1 = (int)value;
                        break;
                    default:
                        base.SetProperty(name, value);
                        break;
                }
            }
        }

        [TestMethod]
        public void NotifyChanged_ContentInstances()
        {
            var contentName = MethodBase.GetCurrentMethod().Name;
            Test(() =>
            {
                var content = Content.CreateNew(typeof(SystemFolder).Name, Repository.Root, contentName);
                var node = (GenericContent)content.ContentHandler;
                Assert.AreSame(content, node.Content);

                node.Index = 3;
                content.Save();

                Assert.AreSame(content, node.Content);
            });
        }

        [TestMethod]
        public void NotifyChanged_NodeDataStaticProperty()
        {
            var contentName = MethodBase.GetCurrentMethod().Name;
            SnTrace.Test.Enabled = false;
            Test(() =>
            {
                var node = new SystemFolder(Repository.Root) { Name = contentName, Index = 1 };
                var fieldName = nameof(node.Index);
                var content = node.Content;
                Assert.AreEqual(1, (int)content[fieldName]);

                node.Index = 2;
                Assert.AreEqual(2, (int)content[fieldName]);

                node.Save();
                Assert.AreEqual(2, node.Index);
                Assert.AreEqual(2, (int)content[fieldName]);

                content[fieldName] = 42;
                Assert.AreEqual(2, node.Index);
                Assert.AreEqual(42, (int)content[fieldName]);

                node.Index = 3;
                Assert.AreEqual(3, (int)content[fieldName]);

                node.Save();

                Assert.IsTrue(CreateSafeContentQuery($"+{fieldName}:3 +Name:{contentName} .AUTOFILTERS:OFF").Execute().Identifiers.Any());
            });
        }
        [TestMethod]
        public void NotifyChanged_NodeDataStaticProperty_ChangeAndBack()
        {
            var contentName = MethodBase.GetCurrentMethod().Name;
            SnTrace.Test.Enabled = false;
            Test(() =>
            {
                var node = new SystemFolder(Repository.Root) { Name = contentName, Index = 1 };
                var fieldName = nameof(node.Index);
                var content = node.Content;
                Assert.AreEqual(1, (int)content[fieldName]);
                node.Save();

                // change
                node.Index = 42;
                Assert.AreEqual(42, (int)content[fieldName]);

                // take back
                node.Index = 1;

                // check
                Assert.AreEqual(1, (int)content[fieldName]);

                node.Save();
                Assert.IsTrue(CreateSafeContentQuery($"+{fieldName}:1 +Name:{contentName} .AUTOFILTERS:OFF").Execute().Identifiers.Any());
            });
        }
        [TestMethod]
        public void NotifyChanged_NodeDataDynamicProperty()
        {
            var contentName = MethodBase.GetCurrentMethod().Name;

            Test(() =>
            {
                var node = new SystemFolder(Repository.Root) { Name = contentName, Description = "Desc1" };
                var fieldName = nameof(node.Description);
                var content = node.Content;
                Assert.AreEqual("Desc1", (string)content[fieldName]);

                node.Description = "Desc2";
                Assert.AreEqual("Desc2", (string)content[fieldName]);

                node.Save();
                Assert.AreEqual("Desc2", node.Description);
                Assert.AreEqual("Desc2", (string)content[fieldName]);

                content[fieldName] = "Desc42";
                Assert.AreEqual("Desc2", node.Description);
                Assert.AreEqual("Desc42", (string)content[fieldName]);

                node.Description = "Desc3";
                Assert.AreEqual("Desc3", (string)content[fieldName]);

                node.Save();

                Assert.IsTrue(CreateSafeContentQuery($"+{fieldName}:Desc3 +Name:{contentName} .AUTOFILTERS:OFF").Execute().Identifiers.Any());
            });
        }
        [TestMethod]
        public void NotifyChanged_NodeDataDynamicProperty_ChangeAndBack()
        {
            var contentName = MethodBase.GetCurrentMethod().Name;

            Test(() =>
            {
                var originalValue = "Original";
                var node = new SystemFolder(Repository.Root) { Name = contentName, Description = originalValue };
                var fieldName = nameof(node.Description);
                var content = node.Content;
                node.Save();
                Assert.AreEqual(originalValue, node.Description);
                Assert.AreEqual(originalValue, (string)content[fieldName]);

                node.Description = "Changed";
                Assert.AreEqual("Changed", (string)content[fieldName]);

                node.Description = originalValue;
                Assert.AreEqual(originalValue, (string)content[fieldName]);

                node.Save();

                Assert.IsTrue(CreateSafeContentQuery($"+{fieldName}:{originalValue} +Name:{contentName} .AUTOFILTERS:OFF").Execute().Identifiers.Any());
            });
        }
        [TestMethod]
        public void NotifyChanged_NodeDataDifferentBinding()
        {
            var contentName = MethodBase.GetCurrentMethod().Name;

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(ContentHandler1.CTD);
                var root = CreateTestRoot();
                var node = new ContentHandler1(root) { Name = contentName };
                var content = node.Content;
                var fieldName = "ShortTextField1";
                var testValue = "TestValue1";

                node.ShortText1 = testValue;
                Assert.AreEqual(testValue, (string)content[fieldName]);

                content[fieldName] = "TestValue42";
                Assert.AreEqual("TestValue42", (string)content[fieldName]);
                Assert.AreEqual(testValue, node.ShortText1);

                testValue = "TestValue2";
                node.ShortText1 = testValue;
                Assert.AreEqual(testValue, (string)content[fieldName]);

                content[fieldName] = "TestValue42";
                node.Save();
                Assert.AreSame(content, node.Content);
                Assert.AreEqual(testValue, (string)content[fieldName]);
            });
        }
        [TestMethod]
        public void NotifyChanged_CustomProperty()
        {
            var contentName = MethodBase.GetCurrentMethod().Name;

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(ContentHandler1.CTD);
                var root = CreateTestRoot();
                var node = new ContentHandler1(root) { Name = contentName };
                var content = node.Content;
                const string fieldName = nameof(node.CustomInt1);
                var testValue = 1;

                node.CustomInt1 = testValue;
                Assert.AreEqual(testValue, (int)content[fieldName]);

                content[fieldName] = 42;
                Assert.AreEqual(42, (int)content[fieldName]);
                Assert.AreEqual(testValue, node.CustomInt1);

                testValue = 2;
                node.CustomInt1 = testValue;
                Assert.AreEqual(testValue, (int)content[fieldName]);

                content[fieldName] = 42;
                node.Save();
                Assert.AreSame(content, node.Content);
                Assert.AreEqual(testValue, (int)content[fieldName]);
            });
        }
        [TestMethod]
        public void NotifyChanged_CustomFieldIndexed()
        {
            var contentName = MethodBase.GetCurrentMethod().Name;

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(ContentHandler1.CTD);
                var root = CreateTestRoot();
                var node = new ContentHandler1(root) { Name = contentName };
                const string fieldName = "CustomInt1";
                var testValue = 42;
                var content = node.Content;

                content[fieldName] = testValue;
                node.Save();

                Assert.IsTrue(CreateSafeContentQuery($"+{fieldName}:42 +Name:{contentName} .AUTOFILTERS:OFF").Execute().Identifiers.Any());
            });
        }

        private SystemFolder CreateTestRoot()
        {
            var node = new SystemFolder(Repository.Root) { Name = "TestRoot" };
            node.Save();
            return node;
        }
    }
}
