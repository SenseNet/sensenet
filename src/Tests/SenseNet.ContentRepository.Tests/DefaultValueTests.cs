using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.Tests.Core;
using System.Threading;
using FluentAssertions;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using STT = System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DefaultValueTests : TestBase
    {
        private readonly CancellationToken _cancel = new CancellationTokenSource().Token;

        private static readonly string ContentTypeName = "DefaultValueTests";

        private static readonly string ContentType = $@"<ContentType name=""{ContentTypeName}"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields>
    <Field name=""SingleReferenceOptional"" type=""Reference"">
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
      </Configuration>
    </Field>
    <Field name=""SingleReferenceRequired"" type=""Reference"">
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <Compulsory>true</Compulsory>
      </Configuration>
    </Field>
    <Field name=""SingleReferenceRequiredDefault"" type=""Reference"">
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <Compulsory>true</Compulsory>
        <DefaultValue>/Root/Content</DefaultValue>
      </Configuration>
    </Field>
    <Field name=""SingleReferenceRequiredCurrentUser"" type=""Reference"">
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <Compulsory>true</Compulsory>
        <DefaultValue>@@currentuser@@</DefaultValue>
      </Configuration>
    </Field>
    <Field name=""MultiReferenceOptional"" type=""Reference"">
      <Configuration>
        <AllowMultiple>true</AllowMultiple>
      </Configuration>
    </Field>
    <Field name=""MultiReferenceRequired"" type=""Reference"">
      <Configuration>
        <AllowMultiple>true</AllowMultiple>
        <Compulsory>true</Compulsory>
      </Configuration>
    </Field>
    <Field name=""MultiReferenceRequiredDefault"" type=""Reference"">
      <Configuration>
        <AllowMultiple>true</AllowMultiple>
        <Compulsory>true</Compulsory>
        <DefaultValue>/Root/Content,/Root/IMS</DefaultValue>
      </Configuration>
    </Field>
    <Field name=""MultiReferenceRequiredCurrentUser"" type=""Reference"">
      <Configuration>
        <AllowMultiple>true</AllowMultiple>
        <Compulsory>true</Compulsory>
        <DefaultValue>@@currentuser@@</DefaultValue>
      </Configuration>
    </Field>
  </Fields>
</ContentType>
";

        [TestMethod]
        public async STT.Task FieldDefaultValue_New_SingleReferenceOptional()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""SingleReferenceOptional1"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
  </Configuration>
</Field>
<Field name=""SingleReferenceOptional2"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);

                // ACT
                var content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                content["SingleReferenceOptional1"] = Repository.ImsFolder;
                await content.SaveAsync(_cancel);

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                var referenced1 = (loaded["SingleReferenceOptional1"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.AreEqual(Repository.ImsFolder.Id, referenced1?.Id ?? 0);
                var referenced2 = (loaded["SingleReferenceOptional2"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.IsNull(referenced2);

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_New_SingleReferenceOptional_WithDefault()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""SingleReferenceOptional1"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
    <DefaultValue>/Root/IMS</DefaultValue>
  </Configuration>
</Field>
<Field name=""SingleReferenceOptional2"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
    <DefaultValue>/Root/IMS</DefaultValue>
  </Configuration>
</Field>
<Field name=""SingleReferenceOptional3"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
    <DefaultValue>@@currentuser@@</DefaultValue>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);
                var testUser = await CreateTestUserAsync(_cancel);

                // ACT
                Content content;
                using (new CurrentUserBlock(testUser))
                {
                    content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                    content["SingleReferenceOptional1"] = testRoot;
                    await content.SaveAsync(_cancel);
                }

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                var referenced1 = (loaded["SingleReferenceOptional1"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.AreEqual(testRoot.Id, referenced1?.Id ?? 0);
                var referenced2 = (loaded["SingleReferenceOptional2"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.AreEqual(Repository.ImsFolder.Id, referenced2?.Id ?? 0);
                var referenced3 = (loaded["SingleReferenceOptional3"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.AreEqual(testUser.Id, referenced3?.Id ?? 0);

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_New_SingleReferenceRequired()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""SingleReferenceRequired"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
    <Compulsory>true</Compulsory>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);

                // ACT
                Exception exception = null;
                try
                {
                    var content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                    await content.SaveAsync(_cancel);
                    Assert.Fail("The expected InvalidContentException was not thrown.");
                }
                catch (InvalidContentException e)
                {
                    exception = e;
                }

                // ASSERT
                Assert.IsTrue(exception.Message.Contains("SingleReferenceRequired"));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_New_SingleReferenceRequired_WithDefault()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""SingleReferenceRequired"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
    <Compulsory>true</Compulsory>
    <DefaultValue>/Root/IMS</DefaultValue>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);

                // ACT
                var content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                await content.SaveAsync(_cancel);

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                var referenced = (loaded["SingleReferenceRequired"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.AreEqual(Repository.ImsFolder.Id, referenced?.Id ?? 0);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_New_SingleReferenceRequired_CurrentUser()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""SingleReferenceRequired"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
    <Compulsory>true</Compulsory>
    <DefaultValue>@@currentuser@@</DefaultValue>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);
                var testUser = await CreateTestUserAsync(_cancel);
                Content content;

                // ACT
                using (new CurrentUserBlock(testUser))
                {
                    content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                    await content.SaveAsync(_cancel);
                }

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                var referenced = (loaded["SingleReferenceRequired"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.AreEqual(testUser.Id, referenced?.Id ?? 0);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async STT.Task FieldDefaultValue_Modify_SingleReferenceOptional()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""SingleReferenceOptional1"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);
                var content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                content["SingleReferenceOptional1"] = Repository.ImsFolder;
                await content.SaveAsync(_cancel);

                // ACT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                loaded["SingleReferenceOptional1"] = null;
                await loaded.SaveAsync(_cancel);

                // ASSERT
                var reloaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                var referenced1 = (reloaded["SingleReferenceOptional1"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.IsNull(referenced1);

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_Modify_SingleReferenceOptional_WithDefault()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""SingleReferenceOptional1"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
    <DefaultValue>/Root/IMS</DefaultValue>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);
                var content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                content["SingleReferenceOptional1"] = testRoot;
                await content.SaveAsync(_cancel);

                // ACT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                loaded["SingleReferenceOptional1"] = null;
                await loaded.SaveAsync(_cancel);

                // ASSERT
                var reloaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                var referenced1 = (reloaded["SingleReferenceOptional1"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.IsNull(referenced1);

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_Modify_SingleReferenceRequired()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""SingleReferenceRequired"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
    <Compulsory>true</Compulsory>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);
                var content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                content["SingleReferenceRequired"] = testRoot;
                await content.SaveAsync(_cancel);

                // ACT
                Exception exception = null;
                try
                {
                    var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                    loaded["SingleReferenceRequired"] = null;
                    await loaded.SaveAsync(_cancel);
                    Assert.Fail("The expected InvalidContentException was not thrown.");
                }
                catch (InvalidContentException e)
                {
                    exception = e;
                }

                // ASSERT
                Assert.IsTrue(exception.Message.Contains("SingleReferenceRequired"));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_Modify_SingleReferenceRequired_WithDefault()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""SingleReferenceRequired"" type=""Reference"">
  <Configuration>
    <AllowMultiple>false</AllowMultiple>
    <Compulsory>true</Compulsory>
    <DefaultValue>/Root/IMS</DefaultValue>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);
                var content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                content["SingleReferenceRequired"] = testRoot;
                await content.SaveAsync(_cancel);

                // ACT
                Exception exception = null;
                try
                {
                    var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                    loaded["SingleReferenceRequired"] = null;
                    await loaded.SaveAsync(_cancel);
                    Assert.Fail("The expected InvalidContentException was not thrown.");
                }
                catch (InvalidContentException e)
                {
                    exception = e;
                }

                // ASSERT
                Assert.IsTrue(exception.Message.Contains("SingleReferenceRequired"));
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async STT.Task FieldDefaultValue_New_MultiReferenceOptional()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""MultiReferenceOptional1"" type=""Reference"">
  <Configuration>
    <AllowMultiple>true</AllowMultiple>
  </Configuration>
</Field>
<Field name=""MultiReferenceOptional2"" type=""Reference"">
  <Configuration>
    <AllowMultiple>true</AllowMultiple>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);

                // ACT
                var content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                content["MultiReferenceOptional1"] = new Node[] {Repository.ImsFolder, testRoot};
                await content.SaveAsync(_cancel);

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                var referenced1 = loaded["MultiReferenceOptional1"] as IEnumerable<Node>;
                AssertSequenceEqual(new[] {Repository.ImsFolder.Id, testRoot.Id}, referenced1?.Select(x => x.Id));
                var referenced2 = (loaded["MultiReferenceOptional2"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.IsNull(referenced2);

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_New_MultiReferenceOptional_WithDefault()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""MultiReferenceOptional1"" type=""Reference"">
  <Configuration>
    <AllowMultiple>true</AllowMultiple>
    <DefaultValue>/Root/IMS</DefaultValue>
  </Configuration>
</Field>
<Field name=""MultiReferenceOptional2"" type=""Reference"">
  <Configuration>
    <AllowMultiple>true</AllowMultiple>
    <DefaultValue>/Root/IMS</DefaultValue>
  </Configuration>
</Field>
<Field name=""MultiReferenceOptional3"" type=""Reference"">
  <Configuration>
    <AllowMultiple>true</AllowMultiple>
    <DefaultValue>@@currentuser@@</DefaultValue>
  </Configuration>
</Field>
<Field name=""MultiReferenceOptional4"" type=""Reference"">
  <Configuration>
    <AllowMultiple>true</AllowMultiple>
    <DefaultValue>/Root/IMS,@@currentuser@@</DefaultValue>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);
                var testUser = await CreateTestUserAsync(_cancel);

                // ACT
                Content content;
                using (new CurrentUserBlock(testUser))
                {
                    content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                    content["MultiReferenceOptional1"] = new Node[] { Repository.ImsFolder, testRoot };
                    await content.SaveAsync(_cancel);
                }

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                var referenced1 = loaded["MultiReferenceOptional1"] as IEnumerable<Node>;
                AssertSequenceEqual(new[] { Repository.ImsFolder.Id, testRoot.Id }, referenced1?.Select(x => x.Id));
                var referenced2 = (loaded["MultiReferenceOptional2"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.AreEqual(Repository.ImsFolder.Id, referenced2?.Id ?? 0);
                var referenced3 = (loaded["MultiReferenceOptional3"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.AreEqual(testUser.Id, referenced3?.Id ?? 0);
                var referenced4 = loaded["MultiReferenceOptional4"] as IEnumerable<Node>;
                AssertSequenceEqual(new[] { Repository.ImsFolder.Id, testUser.Id }, referenced4?.Select(x => x.Id));

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_New_MultiReferenceRequired()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""MultiReferenceRequired"" type=""Reference"">
  <Configuration>
    <AllowMultiple>true</AllowMultiple>
    <Compulsory>true</Compulsory>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);

                // ACT
                Exception exception = null;
                try
                {
                    var content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                    await content.SaveAsync(_cancel);
                    Assert.Fail("The expected InvalidContentException was not thrown.");
                }
                catch (InvalidContentException e)
                {
                    exception = e;
                }

                // ASSERT
                Assert.IsTrue(exception.Message.Contains("MultiReferenceRequired"));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_New_MultiReferenceRequired_WithDefault()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""MultiReferenceRequired"" type=""Reference"">
  <Configuration>
    <AllowMultiple>true</AllowMultiple>
    <Compulsory>true</Compulsory>
    <DefaultValue>/Root/IMS</DefaultValue>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);

                // ACT
                var content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                await content.SaveAsync(_cancel);

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                var referenced = (loaded["MultiReferenceRequired"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.AreEqual(Repository.ImsFolder.Id, referenced?.Id ?? 0);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_New_MultiReferenceRequired_CurrentUser()
        {
            await Test(async () =>
            {
                InstallContentType(@"
<Field name=""MultiReferenceRequired"" type=""Reference"">
  <Configuration>
    <AllowMultiple>true</AllowMultiple>
    <Compulsory>true</Compulsory>
    <DefaultValue>@@currentuser@@</DefaultValue>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);
                var testUser = await CreateTestUserAsync(_cancel);
                Content content;

                // ACT
                using (new CurrentUserBlock(testUser))
                {
                    content = Content.CreateNew(ContentTypeName, testRoot, "TestContent");
                    await content.SaveAsync(_cancel);
                }

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                var referenced = (loaded["MultiReferenceRequired"] as IEnumerable<Node>)?.FirstOrDefault();
                Assert.AreEqual(testUser.Id, referenced?.Id ?? 0);
            }).ConfigureAwait(false);
        }

        /* =============================================================================== */

        private class ContentHandler1 : GenericContent
        {
            public ContentHandler1(Node parent) : base(parent) { }
            public ContentHandler1(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
            protected ContentHandler1(NodeToken nt) : base(nt) { }

            [RepositoryProperty(nameof(ShortText1), RepositoryDataType.String)]
            public string ShortText1
            {
                get => base.GetProperty<string>(nameof(ShortText1));
                set => this[nameof(ShortText1)] = value;
            }

            public override object GetProperty(string name)
            {
                switch (name)
                {
                    case nameof(ShortText1):
                        return ShortText1;
                }
                return base.GetProperty(name);
            }
            public override void SetProperty(string name, object value)
            {
                switch (name)
                {
                    case nameof(ShortText1):
                        ShortText1 = (string)value;
                        break;
                }
                base.SetProperty(name, value);
            }
        }

        [TestMethod]
        public async STT.Task FieldDefaultValue_New_ShortText_SetInHandler_Optional()
        {
            await Test(async () =>
            {
                InstallContentType("ContentHandler1" , @"
<Field name=""ShortText1"" type=""ShortText"">
  <Configuration>
    <Compulsory>false</Compulsory>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);
                Content content;

                // ACT
                var node = new ContentHandler1(testRoot) {Name = "TestContent", ShortText1 = "ShortTextValue"};
                content = Content.Create(node);
                await content.SaveAsync(_cancel);

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                Assert.AreEqual("ShortTextValue", loaded["ShortText1"]);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task FieldDefaultValue_New_ShortText_SetInHandler_Required()
        {
            await Test(async () =>
            {
                InstallContentType("ContentHandler1", @"
<Field name=""ShortText1"" type=""ShortText"">
  <Configuration>
    <Compulsory>true</Compulsory>
  </Configuration>
</Field>
");
                var testRoot = await CreateTestRootAsync(_cancel);
                Content content;

                // ACT
                var node = new ContentHandler1(testRoot) { Name = "TestContent", ShortText1 = "ShortTextValue" };
                content = Content.Create(node);
                await content.SaveAsync(_cancel);

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                Assert.AreEqual("ShortTextValue", loaded["ShortText1"]);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async STT.Task FieldDefaultValue_New_User_SetInHandler_Required()
        {
            await Test(async () =>
            {
                // ACT
                var node = new User(await Node.LoadNodeAsync("/Root/IMS/BuiltIn/Portal", _cancel))
                {
                    Name = "User-1",
                    Password = "user-1",
                    Email = "user-1@xmple.com",
                    FullName = "John Smith",
                };
                var content = Content.Create(node);
                content["Password"] = "user-1";
                await content.SaveAsync(_cancel);

                // ASSERT
                var loaded = await Content.LoadAsync(content.Id, _cancel).ConfigureAwait(false);
                Assert.AreEqual("user-1@xmple.com", loaded["Email"]);
                Assert.AreEqual("John Smith", loaded["DisplayName"]);
                Assert.AreEqual("John Smith", loaded["FullName"]);
            }).ConfigureAwait(false);
        }

        /* =============================================================================== TOOLS */

        private void InstallContentType(string fieldDefinition)
        {
            ContentTypeInstaller.InstallContentType($@"<ContentType name=""{ContentTypeName}"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields>
    {fieldDefinition}
  </Fields>
</ContentType>");
        }
        private void InstallContentType(string contentTypeName, string fieldDefinition)
        {
            ContentTypeInstaller.InstallContentType($@"<ContentType name=""{contentTypeName}"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields>
    {fieldDefinition}
  </Fields>
</ContentType>");
        }

        private async STT.Task<SystemFolder> CreateTestRootAsync(CancellationToken cancel)
        {
            var node = new SystemFolder(Repository.Root) { Name = "_AspectTests" };
            await node.SaveAsync(cancel).ConfigureAwait(false);
            return node;
        }

        private async STT.Task<IUser> CreateTestUserAsync(CancellationToken cancel)
        {
            var user = new User(await Node.LoadNodeAsync(Identifiers.PortalOrgUnitId, cancel))
            {
                Name = "testUser",
                LoginName = "testUser",
                Email = "testuser@example.com",
                Enabled = true
            };
            await user.SaveAsync(CancellationToken.None);

            Group.Administrators.AddMember(user);

            return user;
        }
    }
}
