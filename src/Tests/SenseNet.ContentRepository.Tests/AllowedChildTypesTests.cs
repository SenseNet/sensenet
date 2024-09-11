using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class AllowedChildTypesTests : TestBase
    {
        [TestMethod]
        public void AllowedChildTypes_Workspace_NoLocalItems_AddOne()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                var namesBefore = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car" };

                // ACTION
                ts.Workspace1.AllowChildType(additionalNames[0], true, true);

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);
                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Union(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Workspace_LocalItems_AddOne()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                ts.Workspace1.AllowedChildTypes =
                    new[] {"DocumentLibrary", "File", "Folder", "MemoList", "SystemFolder", "TaskList", "Workspace"}
                    .Select(ContentType.GetByName).ToArray();
                ts.Workspace1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var namesBefore = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car" };

                // ACTION
                ts.Workspace1.AllowChildType(additionalNames[0], true, true);

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);
                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Union(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Workspace_NoLocalItems_AddMore()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                var namesBefore = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car", "File", "Memo" };

                // ACTION
                ts.Workspace1.AllowChildTypes(additionalNames, true, true);

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);
                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Union(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Workspace_LocalItems_AddMore()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                ts.Workspace1.AllowedChildTypes =
                    new[] { "DocumentLibrary", "File", "Folder", "MemoList", "SystemFolder", "TaskList", "Workspace" }
                    .Select(ContentType.GetByName).ToArray();
                ts.Workspace1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var namesBefore = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car", "File", "Memo" };

                // ACTION
                ts.Workspace1.AllowChildTypes(additionalNames, true, true);

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);
                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Union(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Workspace_NoLocalItems_ODataActionAdd()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                var namesBefore = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car", "File", "Memo" };

                // ACTION
                var content = Content.Create(ts.Workspace1);
                GenericContent.AddAllowedChildTypes(content, additionalNames);

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);
                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Union(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Workspace_LocalItems_ODataActionAdd()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                ts.Workspace1.AllowedChildTypes =
                    new[] { "DocumentLibrary", "File", "Folder", "MemoList", "SystemFolder", "TaskList", "Workspace" }
                    .Select(ContentType.GetByName).ToArray();
                ts.Workspace1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var namesBefore = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car", "File", "Memo" };

                // ACTION
                var content = Content.Create(ts.Workspace1);
                GenericContent.AddAllowedChildTypes(content, additionalNames);

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);
                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Union(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AllowedChildTypes_Folder_AddOne()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();

                // ACTION
                ts.Folder1.AllowChildType("Car", true, true);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AllowedChildTypes_Folder_AddMore()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                var additionalNames = new[] { "Car", "File", "Memo" };

                // ACTION
                ts.Folder1.AllowChildTypes(additionalNames, true, true);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AllowedChildTypes_Folder_NoLocalItems_ODataActionAdd()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                var additionalNames = new[] { "Car", "File", "Memo" };

                // ACTION
                var content = Content.Create(ts.Folder1);
                GenericContent.AddAllowedChildTypes(content, additionalNames);

                // An InvalidOperationException need to be thrown here
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AllowedChildTypes_Folder_LocalItems_ODataActionAdd()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                ts.Folder1.AllowedChildTypes =
                    new[] { "DocumentLibrary", "File", "Folder", "MemoList", "SystemFolder", "TaskList", "Workspace" }
                    .Select(ContentType.GetByName).ToArray();
                ts.Folder1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var additionalNames = new[] { "Car", "File", "Memo" };

                // ACTION
                var content = Content.Create(ts.Folder1);
                GenericContent.AddAllowedChildTypes(content, additionalNames);

                // An InvalidOperationException need to be thrown here
            });
        }

        /* ---------------------------------------------------------------------------------- transitive */

        [TestMethod]
        public void AllowedChildTypes_Bug1607_Transitive_Folder()
        {
            const string ctd = @"<ContentType name=""{0}"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <AllowedChildTypes>{1}</AllowedChildTypes>
  <Fields><Field name=""TestCount"" type=""Integer""></Field></Fields>
</ContentType>";

            Test(() =>
            {
                // Create 3 CTDs:		MyTypeA, Folder, MyTypeC (Folder is builtin type)
                // AllowChildType:		MyTypeA allows Folder
                // AllowChildType:		Folder allows MyTypeC (irrelevant operation)
                ContentTypeInstaller.InstallContentType(
                    string.Format(ctd, "MyTypeA", "Folder"),
                    string.Format(ctd, "MyTypeC", ""));

                var genericContentType = ContentType.GetByName("GenericContent");
                var myTypeAContentType = ContentType.GetByName("MyTypeA");
                var folderContentType = ContentType.GetByName("Folder");
                var myTypeCContentType = ContentType.GetByName("MyTypeC");
                Assert.AreEqual(genericContentType.Name, myTypeAContentType.ParentTypeName);
                Assert.AreEqual(genericContentType.Name, folderContentType.ParentTypeName);
                Assert.AreEqual(genericContentType.Name, myTypeCContentType.ParentTypeName);

                // AllowChildType:		/Root/Content allows MyTypeA
                var rootContent = Content.Load("/Root/Content");
                var gc = (GenericContent)rootContent.ContentHandler;
                gc.SetAllowedChildTypes(new[] { ContentType.GetByName("MyTypeA") });
                gc.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                // New content:		/Root/Content/MyTypeA-1
                var myTypeA1 = Content.CreateNew("MyTypeA", rootContent.ContentHandler, "MyTypeA-1");
                myTypeA1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                // New content:		/Root/Content/MyTypeA-1/Folder-1
                var folder1 = Content.CreateNew("Folder", myTypeA1.ContentHandler, "Folder-1");
                folder1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                // ACTION: Try new content:	/Root/Content/MyTypeA-1/Folder-1/MyTypeC-1
                var myTypeC1 = Content.CreateNew("MyTypeC", folder1.ContentHandler, "MyTypeC-1");
                Exception expectedException = null;
                try
                {
                    myTypeC1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (InvalidOperationException e)
                {
                    expectedException = e;
                }

                // ASSERT
                Assert.IsNotNull(expectedException);
                Assert.IsTrue(expectedException.Message.Contains("Cannot save the content"));
                Assert.IsTrue(expectedException.Message.Contains("because its ancestor does not allow the type 'MyTypeC'"));
                Assert.IsTrue(expectedException.Message.Contains("Ancestor: /Root/Content/MyTypeA-1 (MyTypeA)."));
                Assert.IsTrue(expectedException.Message.Contains("Allowed types: Folder, SystemFolder"));
            });
        }

        [TestMethod]
        public void AllowedChildTypes_Transitive_IsTransitive()
        {
            Test(() =>
            {
                // ACT-1
                ContentTypeInstaller.InstallContentType(
                    @"<ContentType name=""MyNotTransitiveType1"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                          <Fields/>
                        </ContentType>",
                    @"<ContentType name=""MyNotTransitiveType2"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                          <AllowedChildTypes transitive=""false"" />
                          <Fields/>
                        </ContentType>",
                    @"<ContentType name=""MyTransitiveType1"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                          <AllowedChildTypes transitive=""true"" />
                          <Fields/>
                        </ContentType>"
                );

                // ASSERT-1
                Assert.IsTrue(ContentType.GetByName("Folder").IsTransitiveForAllowedTypes);
                Assert.IsTrue(ContentType.GetByName("MyTransitiveType1").IsTransitiveForAllowedTypes);
                Assert.IsFalse(ContentType.GetByName("MyNotTransitiveType1").IsTransitiveForAllowedTypes);
                Assert.IsFalse(ContentType.GetByName("MyNotTransitiveType2").IsTransitiveForAllowedTypes);

                // ACT-2 error
                string errorMessage = null;
                try
                {
                    ContentTypeInstaller.InstallContentType(
                        @"<ContentType name=""MyTransitiveType2"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                              <AllowedChildTypes transitive=""true"">Folder File</AllowedChildTypes>
                              <Fields/>
                            </ContentType>");
                    Assert.Fail("The expected ContentRegistrationException was not thrown.");
                }
                catch (ContentRegistrationException e)
                {
                    errorMessage = e.Message;
                }

                // ASSERT-2
                Assert.AreEqual("The AllowedChildType element should be empty if the transitive attribute is true.", errorMessage);
            });
        }
        [TestMethod]
        public async System.Threading.Tasks.Task AllowedChildTypes_Transitive_ShowParentStuff()
        {
            await Test(async () =>
            {
                ContentTypeInstaller.InstallContentType(
                    @"<ContentType name=""MyTransitiveType1"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                          <AllowedChildTypes transitive=""true"" />
                          <Fields/>
                        </ContentType>"
                );
                var parent = await Node.LoadAsync<Workspace>("/Root/Content", CancellationToken.None);
                parent.AllowChildType("MyTransitiveType1", save: true);
                var expected = parent.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x);
                var content = Content.CreateNew("MyTransitiveType1", parent, "content1");
                await content.SaveAsync(CancellationToken.None);

                // ACT-1
                var loaded = (GenericContent)(await Content.LoadAsync(content.Path, CancellationToken.None)).ContentHandler;
                var actual = loaded.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();

                // ASSERT-1
                Assert.IsTrue(actual.Contains("MyTransitiveType1"));
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", actual));
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Transitive_Registration_Error()
        {
            Test(() =>
            {
                // ACT-1
                ContentTypeInstaller.InstallContentType(
                    @"<ContentType name=""MyNotTransitiveType1"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                          <Fields/>
                        </ContentType>",
                    @"<ContentType name=""MyNotTransitiveType2"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                          <AllowedChildTypes transitive=""false"" />
                          <Fields/>
                        </ContentType>",
                    @"<ContentType name=""MyTransitiveType1"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                          <AllowedChildTypes transitive=""true"" />
                          <Fields/>
                        </ContentType>"
                );

                // ASSERT-1
                Assert.IsTrue(ContentType.GetByName("Folder").IsTransitiveForAllowedTypes);
                Assert.IsTrue(ContentType.GetByName("MyTransitiveType1").IsTransitiveForAllowedTypes);
                Assert.IsFalse(ContentType.GetByName("MyNotTransitiveType1").IsTransitiveForAllowedTypes);
                Assert.IsFalse(ContentType.GetByName("MyNotTransitiveType2").IsTransitiveForAllowedTypes);

                // ACT-2 error
                string errorMessage = null;
                try
                {
                    ContentTypeInstaller.InstallContentType(
                        @"<ContentType name=""MyTransitiveType2"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                              <AllowedChildTypes transitive=""true"">Folder File</AllowedChildTypes>
                              <Fields/>
                            </ContentType>");
                    Assert.Fail("The expected ContentRegistrationException was not thrown.");
                }
                catch (ContentRegistrationException e)
                {
                    errorMessage = e.Message;
                }

                // ASSERT-2
                Assert.AreEqual("The AllowedChildType element should be empty if the transitive attribute is true.", errorMessage);
            });
        }
        [TestMethod]
        public async System.Threading.Tasks.Task AllowedChildTypes_Transitive_Add_Error()
        {
            var cancel = CancellationToken.None;
            await Test(async () =>
            {
                ContentTypeInstaller.InstallContentType(
                    @"<ContentType name=""CustomType"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                          <Fields/>
                        </ContentType>",
                    @"<ContentType name=""MyNotTransitiveType1"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                          <Fields/>
                        </ContentType>",
                    @"<ContentType name=""MyTransitiveType1"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                          <AllowedChildTypes transitive=""true"" />
                          <Fields/>
                        </ContentType>"
                );

                Assert.IsTrue(ContentType.GetByName("Folder").IsTransitiveForAllowedTypes);
                Assert.IsTrue(ContentType.GetByName("MyTransitiveType1").IsTransitiveForAllowedTypes);
                Assert.IsFalse(ContentType.GetByName("MyNotTransitiveType1").IsTransitiveForAllowedTypes);

                var testRoot = await Node.LoadAsync<GenericContent>("/Root/Content", cancel);
                var customType = ContentType.GetByName("CustomType");
                testRoot.AllowChildTypes(new[] { "MyTransitiveType1", "MyNotTransitiveType1" }, save: true);

                var f1C = Content.CreateNew("Folder", testRoot, "F1");
                await f1C.SaveAsync(cancel);
                var f1 = (GenericContent)f1C.ContentHandler;

                var f11C = Content.CreateNew("MyNotTransitiveType1", f1, "F11");
                await f11C.SaveAsync(cancel);
                var f11 = (GenericContent)f11C.ContentHandler;
                f11.AllowChildTypes(new[] { "MyTransitiveType1" }, save: true);

                var f111C = Content.CreateNew("MyTransitiveType1", f11, "F111");
                await f111C.SaveAsync(cancel);
                var f111 = (GenericContent)f111C.ContentHandler;

                // ACT-1 operation allowed and effective
                f11.AllowChildType(customType, save: true);

                // ASSERT-1
                Assert.IsFalse(f1.AllowedChildTypes.Contains(customType));
                Assert.IsTrue(f11.AllowedChildTypes.Contains(customType));
                f111 = await Node.LoadAsync<GenericContent>(f111.Id, cancel);
                Assert.IsTrue(f111.AllowedChildTypes.Contains(customType));


                // ACT-2 operation not allowed
                string errorMessage1 = null;
                string errorMessage2 = null;
                try
                {
                    f1.AllowChildType(customType);
                    Assert.Fail("The expected InvalidOperationException was not thrown.");
                }
                catch (InvalidOperationException e)
                {
                    errorMessage1 = e.Message;
                }
                try
                {
                    f111.AllowChildType(customType);
                    Assert.Fail("The expected InvalidOperationException was not thrown.");
                }
                catch (InvalidOperationException e)
                {
                    errorMessage2 = e.Message;
                }

                // ASSERT-2
                Assert.AreEqual("Cannot allow any content type on a Folder. Path: /Root/Content/F1", errorMessage1);
                Assert.AreEqual("Cannot allow any content type on a MyTransitiveType1. Path: /Root/Content/F1/F11/F111", errorMessage2);


                // ALIGN-3 Ensure that f11 does not allow the customType as child type
                if (f11.IsAllowedChildType(customType))
                {
                    var list = f11.AllowedChildTypes.ToList();
                    list.Remove(customType);
                    f11.SetAllowedChildTypes(list, save: true);
                    f11 = await Node.LoadAsync<GenericContent>(f11.Id, cancel);
                }
                Assert.IsFalse(f11.IsAllowedChildType(customType));

                // ACT-3 operation skipped when the content is transitive
                var f1Names = await SetAndGetAllowedChildTypeNamesAsync(f1, customType, cancel);
                var f11Names = await SetAndGetAllowedChildTypeNamesAsync(f11, customType, cancel);
                var f111Names = await SetAndGetAllowedChildTypeNamesAsync(f111, customType, cancel);

                // ASSERT-3
                Assert.AreEqual(f1Names.before, f1Names.after);
                Assert.AreNotEqual(f11Names.before, f11Names.after);
                Assert.AreEqual(f111Names.before, f111Names.after);
            });
        }
        private async Task<(string before, string after)> SetAndGetAllowedChildTypeNamesAsync(GenericContent content,
            ContentType additionalType, CancellationToken cancel)
        {
            var namesBefore = content.GetProperty<string>("AllowedChildTypes");
            var list = content.AllowedChildTypes.ToList();
            list.Add(additionalType);
            content.AllowedChildTypes = list;
            await content.SaveAsync(cancel);
            content = await Node.LoadAsync<GenericContent>(content.Id, cancel);
            var namesAfter = content.GetProperty<string>("AllowedChildTypes");
            return (namesBefore, namesAfter);
        }

        /* ---------------------------------------------------------------------------------- */

        [TestMethod]
        public void AllowedChildTypes_Workspace_NoLocalItems_Set()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                var namesBefore = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var namesToSet = new[] { "Memo" };

                // this is to make sure that the test runs in a correct environment
                Assert.IsTrue(namesBefore.Length > 0);
                Assert.AreEqual(0, localNamesBefore.Length);

                // ACTION
                ts.Workspace1.SetAllowedChildTypes(namesToSet.Select(ContentType.GetByName), true, true);

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);

                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();

                AssertSequenceEqual(namesToSet, localNamesAfter);

                // SystemFolder is added on-the-fly for admins
                AssertSequenceEqual(new [] {"Memo", "SystemFolder" }, namesAfter);
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Workspace_LocalItems_Set()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                ts.Workspace1.AllowedChildTypes =
                    new[] { "DocumentLibrary", "File", "Folder", "MemoList", "SystemFolder", "TaskList", "Workspace" }
                        .Select(ContentType.GetByName).ToArray();
                ts.Workspace1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var namesToSet = new[] { "File", "Memo" };

                // ACTION
                ts.Workspace1.SetAllowedChildTypes(namesToSet.Select(ContentType.GetByName), true, true);

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);
                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();

                // SystemFolder is added on-the-fly for admins
                AssertSequenceEqual(new[] { "File", "Memo", "SystemFolder"}, namesAfter);
                AssertSequenceEqual(new[] { "File", "Memo" }, localNamesAfter);
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Workspace_LocalItems_Clear()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                var setNamesOriginal = new[] {"DocumentLibrary", "File", "Folder"};
                var setOnContentType = ts.Workspace1.ContentType.AllowedChildTypes.ToArray();

                // set local type list
                ts.Workspace1.AllowedChildTypes = setNamesOriginal.Select(ContentType.GetByName).ToArray();
                ts.Workspace1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var namesBefore = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();

                // This is to make sure that the test runs in a correct environment.
                // SystemFolder is added on-the-fly for admins.
                AssertSequenceEqual(setNamesOriginal.Union(new []{ "SystemFolder" }), namesBefore);

                // ACTION
                ts.Workspace1.SetAllowedChildTypes(setOnContentType, true, true);

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);

                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var namesOnContentType = setOnContentType.Select(ct => ct.Name).Union(new[] {"SystemFolder"}).Distinct()
                    .OrderBy(x => x).ToArray();

                AssertSequenceEqual(namesOnContentType, namesAfter);
                Assert.AreEqual(0, localNamesAfter.Length);
            });
        }

        /* ---------------------------------------------------------------------------------- */

        [TestMethod]
        public void AllowedChildTypes_Workspace_NoLocalItems_ODataActionRemove()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                var namesBefore = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car", "TaskList", "Workspace" };

                // ACTION
                var content = Content.Create(ts.Workspace1);
                var httpContext = new DefaultHttpContext();
                GenericContent.RemoveAllowedChildTypesAsync(content, httpContext, additionalNames).GetAwaiter().GetResult();

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);
                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Except(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Workspace_LocalItems_ODataActionRemove()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                ts.Workspace1.AllowedChildTypes =
                    new[] { "DocumentLibrary", "File", "Folder", "MemoList", "SystemFolder", "TaskList", "Workspace" }
                    .Select(ContentType.GetByName).ToArray();
                ts.Workspace1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var namesBefore = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car", "TaskList", "Workspace" };

                // ACTION
                var content = Content.Create(ts.Workspace1);
                var httpContext = new DefaultHttpContext();
                GenericContent.RemoveAllowedChildTypesAsync(content, httpContext, additionalNames).GetAwaiter().GetResult();

                // ASSERT
                ts.Workspace1 = Node.Load<Workspace>(ts.Workspace1.Id);
                var namesAfter = ts.Workspace1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Workspace1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Except(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }


        [TestMethod]
        public void AllowedChildTypes_Security_NoPermissionForCtd()
        {
            const string parentPath = "/Root/System/Settings";

            Test(() =>
            {
                var parent = Node.Load<SystemFolder>(parentPath);
                var typesAdmin = parent.EffectiveAllowedChildTypes.ToArray();

                // create a user that does not see the Settings type
                var user = CreateTestUser();

                using (new CurrentUserBlock(user))
                {
                    parent = Node.Load<SystemFolder>(parentPath);
                    var typesUser = parent.EffectiveAllowedChildTypes.ToArray();

                    // the allowed list for the user must be shorter than the admin list
                    // and it must NOT contain the Settings type
                    Assert.IsTrue(typesUser.Length < typesAdmin.Length);
                    Assert.IsTrue(typesAdmin.Any(t => t.Name == "Settings"));
                    Assert.IsFalse(typesUser.Any(t => t.Name == "Settings"));
                }
            });
        }

        [TestMethod]
        public void AllowedChildTypes_AdditionalSystemFolder()
        {
            Test(() =>
            {
                // ACTION
                var names1 = Repository.ContentTypesFolder.GetAllowedChildTypeNames();
                var names2 = Node.Load<GenericContent>(Repository.SettingsFolderPath).GetAllowedChildTypeNames();
                var types1 = Repository.ContentTypesFolder.GetAllowedChildTypes();
                var types2 = Node.Load<GenericContent>(Repository.SettingsFolderPath).GetAllowedChildTypes();

                // ASSERT
                Assert.AreEqual("ContentType", string.Join(", ", names1));
                Assert.AreEqual("Settings, SystemFolder", string.Join(", ", names2));
                Assert.AreEqual("ContentType", string.Join(", ", types1.Select(x => x.Name)));
                Assert.AreEqual("Settings, SystemFolder", string.Join(", ", types2.Select(x => x.Name)));
            });
        }

        /* ================================================================================== */

        private class TestStructure
        {
            public Workspace Site1;
            public Workspace Workspace1;
            public Folder Folder1;
        }

        private static TestStructure CreateTestStructure()
        {
            InstallCarContentType();

            var sites = new Folder(Repository.Root) { Name = "Sites" };
            sites.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            var site = new Workspace(sites) { Name = "Site1" };
            site.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            var workspace = new Workspace(site) { Name = "Workspace1" };
            workspace.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            var folder = new Folder(workspace) {Name = "Folder1"};
            folder.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            return new TestStructure {Site1 = site, Workspace1 = workspace, Folder1 = folder};
        }

        private static User CreateTestUser()
        {
            using (new SystemAccount())
            {
                var user = new User(Node.LoadNode(Identifiers.PortalOrgUnitId))
                {
                    Name = "testusr123",
                    LoginName = "testusr123",
                    Email = "testusr123@example.com"
                };
                user.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                Group.Administrators.AddMember(user);

                // deny permissions for the Settings CTD
                var settingsCtd = Node.LoadNode("/Root/System/Schema/ContentTypes/GenericContent/File/Settings");

                Providers.Instance.SecurityHandler.SecurityContext.CreateAclEditor()
                    .Deny(settingsCtd.Id, user.Id, false, PermissionType.See)
                    .ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();

                return user;
            }
        }
    }
}
