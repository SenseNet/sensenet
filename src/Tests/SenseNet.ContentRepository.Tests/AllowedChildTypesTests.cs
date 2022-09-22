using System;
using System.Linq;
using System.Threading;
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
                ts.Workspace1.AllowChildType(additionalNames[0], true, true, true);

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
                ts.Workspace1.AllowChildType(additionalNames[0], true, true, true);

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
                ts.Workspace1.AllowChildTypes(additionalNames, true, true, true);

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
                ts.Workspace1.AllowChildTypes(additionalNames, true, true, true);

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
        public void AllowedChildTypes_Folder_NoLocalItems_AddOne()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                var namesBefore = ts.Folder1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Folder1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car" };

                // ACTION
                ts.Folder1.AllowChildType(additionalNames[0], true, true, true);

                // ASSERT
                ts.Folder1 = Node.Load<Folder>(ts.Folder1.Id);
                var namesAfter = ts.Folder1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Folder1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Union(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Folder_LocalItems_AddOne()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                ts.Folder1.AllowedChildTypes =
                    new[] { "DocumentLibrary", "File", "Folder", "MemoList", "SystemFolder", "TaskList", "Workspace" }
                    .Select(ContentType.GetByName).ToArray();
                ts.Folder1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var namesBefore = ts.Folder1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Folder1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car" };

                // ACTION
                ts.Folder1.AllowChildType(additionalNames[0], true, true, true);

                // ASSERT
                ts.Folder1 = Node.Load<Folder>(ts.Folder1.Id);
                var namesAfter = ts.Folder1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Folder1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Union(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Folder_NoLocalItems_AddMore()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                var namesBefore = ts.Folder1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Folder1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car", "File", "Memo" };

                // ACTION
                ts.Folder1.AllowChildTypes(additionalNames, true, true, true);

                // ASSERT
                ts.Folder1 = Node.Load<Folder>(ts.Folder1.Id);
                var namesAfter = ts.Folder1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Folder1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Union(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
            });
        }
        [TestMethod]
        public void AllowedChildTypes_Folder_LocalItems_AddMore()
        {
            Test(() =>
            {
                var ts = CreateTestStructure();
                ts.Folder1.AllowedChildTypes =
                    new[] { "DocumentLibrary", "File", "Folder", "MemoList", "SystemFolder", "TaskList", "Workspace" }
                    .Select(ContentType.GetByName).ToArray();
                ts.Folder1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var namesBefore = ts.Folder1.GetAllowedChildTypeNames().OrderBy(x => x).ToArray();
                var localNamesBefore = ts.Folder1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var additionalNames = new[] { "Car", "File", "Memo" };

                // ACTION
                ts.Folder1.AllowChildTypes(additionalNames, true, true, true);

                // ASSERT
                ts.Folder1 = Node.Load<Folder>(ts.Folder1.Id);
                var namesAfter = ts.Folder1.GetAllowedChildTypeNames().OrderBy(x => x);
                var localNamesAfter = ts.Folder1.AllowedChildTypes.Select(x => x.Name).OrderBy(x => x).ToArray();
                var expected = namesBefore.Union(additionalNames).Distinct().OrderBy(x => x);
                Assert.AreEqual(string.Join(", ", expected), string.Join(", ", namesAfter));
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

        [TestMethod]
        public void AllowedChildTypes_Bug1607_Transitive_Page()
        {
            const string ctd = @"<ContentType name=""{0}"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <AllowedChildTypes>{1}</AllowedChildTypes>
  <Fields><Field name=""TestCount"" type=""Integer""></Field></Fields>
</ContentType>";

            Test(() =>
            {
                // Create 3 CTDs:		MyTypeA, Page, MyTypeC
                // AllowChildType:		MyTypeA allows Page
                // AllowChildType:		Page allows MyTypeC
                ContentTypeInstaller.InstallContentType(
                    string.Format(ctd, "MyTypeA", "Page"),
                    string.Format(ctd, "Page", "MyTypeC"),
                    string.Format(ctd, "MyTypeC", ""));

                var genericContentType = ContentType.GetByName("GenericContent");
                var myTypeAContentType = ContentType.GetByName("MyTypeA");
                var pageContentType = ContentType.GetByName("Page");
                var myTypeCContentType = ContentType.GetByName("MyTypeC");
                Assert.AreEqual(genericContentType.Name, myTypeAContentType.ParentTypeName);
                Assert.AreEqual(genericContentType.Name, pageContentType.ParentTypeName);
                Assert.AreEqual(genericContentType.Name, myTypeCContentType.ParentTypeName);

                // AllowChildType:		/Root/Content allows MyTypeA
                var rootContent = Content.Load("/Root/Content");
                var gc = (GenericContent) rootContent.ContentHandler;
                gc.SetAllowedChildTypes(new[] {ContentType.GetByName("MyTypeA")});
                gc.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                // New content:		/Root/Content/MyTypeA-1
                var myTypeA1 = Content.CreateNew("MyTypeA", rootContent.ContentHandler, "MyTypeA-1");
                myTypeA1.Save();

                // New content:		/Root/Content/MyTypeA-1/Page-1
                var page1 = Content.CreateNew("Page", myTypeA1.ContentHandler, "Page-1");
                page1.Save();

                // ACTION: Try new content:	/Root/Content/MyTypeA-1/Page-1/MyTypeC-1
                var myTypeC1 = Content.CreateNew("MyTypeC", page1.ContentHandler, "MyTypeC-1");
                Exception expectedException = null;
                try
                {
                    myTypeC1.Save();
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
                Assert.IsTrue(expectedException.Message.Contains("Allowed types: Page, SystemFolder"));
            });
        }
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
                myTypeA1.Save();

                // New content:		/Root/Content/MyTypeA-1/Folder-1
                var folder1 = Content.CreateNew("Folder", myTypeA1.ContentHandler, "Folder-1");
                folder1.Save();

                // ACTION: Try new content:	/Root/Content/MyTypeA-1/Folder-1/MyTypeC-1
                var myTypeC1 = Content.CreateNew("MyTypeC", folder1.ContentHandler, "MyTypeC-1");
                Exception expectedException = null;
                try
                {
                    myTypeC1.Save();
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
                ts.Workspace1.SetAllowedChildTypes(namesToSet.Select(ContentType.GetByName), true, true, true);

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
                ts.Workspace1.SetAllowedChildTypes(namesToSet.Select(ContentType.GetByName), true, true, true);

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
                ts.Workspace1.SetAllowedChildTypes(setOnContentType, true, true, true);

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
                    .Apply();

                return user;
            }
        }
    }
}
