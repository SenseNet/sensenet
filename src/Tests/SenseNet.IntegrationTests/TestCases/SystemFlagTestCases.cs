using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Search;
using STT = System.Threading.Tasks;

namespace SenseNet.IntegrationTests.TestCases
{
    public class SystemFlagTestCases : TestCaseBase
    {
        [ContentHandler]
        public class TestSystemFolder : SystemFolder
        {
            public static string ContentTypeDefinition =
                "<?xml version='1.0' encoding='utf-8'?>" +
                "<ContentType name='TestSystemFolder' parentType='SystemFolder' " +
                $"handler='{typeof(TestSystemFolder).FullName}' " +
                "xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition' />";

            public TestSystemFolder(Node parent) : this(parent, "TestSystemFolder") { }
            public TestSystemFolder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
            protected TestSystemFolder(NodeToken nt) : base(nt) { }
        }

        /* ================================================================== IsSystem flag tests */

        public void SystemFlag_OnFolder()
        {
            IntegrationTest(() =>
            {
                var root = new Folder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                root.Save();
                try
                {
                    var x = Node.LoadNode(root.Id);
                    Assert.IsFalse(x.IsSystem);
                }
                finally
                {
                    root.ForceDelete();
                }
            });
        }
        public void SystemFlag_OnSystemFolder()
        {
            IntegrationTest(() =>
            {
                var root = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                root.Save();
                try
                {
                    var x = Node.LoadNode(root.Id);
                    Assert.IsTrue(x.IsSystem);
                }
                finally
                {
                    root.ForceDelete();
                }
            });
        }
        public void SystemFlag_OnFolderUnderSystemFolder()
        {
            IntegrationTest(() =>
            {
                var root = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                root.Save();
                var folder = new Folder(root) { Name = Guid.NewGuid().ToString() };
                folder.Save();
                try
                {
                    var x = Node.LoadNode(folder.Id);
                    Assert.IsTrue(x.IsSystem);
                }
                finally
                {
                    root.ForceDelete();
                }
            });
        }

        public void SystemFlag_Copy_FromFolderToFolder()
        {
            IntegrationTest(() =>
            {
                var srcParent = new Folder(Repository.Root) { Name = "Source-" + Guid.NewGuid() };
                srcParent.Save();
                var target = new Folder(Repository.Root) { Name = "Target-" + Guid.NewGuid() };
                var targetName = target.Name;
                target.Save();
                var nodeIds = CreateSourceStructure(srcParent);
                Node f1, f2, s3, f4, s5, f6, f7, f8, f9, f10, f11, f12;
                try
                {
                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsFalse(f1.IsSystem, "F1");
                    Assert.IsFalse(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsFalse(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsFalse(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsFalse(f9.IsSystem, "F9");
                    Assert.IsFalse(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    f1.CopyTo(target);

                    f1 = Node.LoadNode($"/Root/{targetName}/F1");
                    f2 = Node.LoadNode($"/Root/{targetName}/F1/F2");
                    s3 = Node.LoadNode($"/Root/{targetName}/F1/S3");
                    f4 = Node.LoadNode($"/Root/{targetName}/F1/F4");
                    s5 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5");
                    f6 = Node.LoadNode($"/Root/{targetName}/F1/F2/F6");
                    f7 = Node.LoadNode($"/Root/{targetName}/F1/S3/F7");
                    f8 = Node.LoadNode($"/Root/{targetName}/F1/S3/F8");
                    f9 = Node.LoadNode($"/Root/{targetName}/F1/F4/F9");
                    f10 = Node.LoadNode($"/Root/{targetName}/F1/F4/F10");
                    f11 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5/F11");
                    f12 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5/F12");

                    Assert.IsFalse(f1.IsSystem, "F1");
                    Assert.IsFalse(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsFalse(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsFalse(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsFalse(f9.IsSystem, "F9");
                    Assert.IsFalse(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    Assert.AreEqual(12, CreateSafeContentQuery("+Description:SystemFlagTest .COUNTONLY", QuerySettings.Default).Execute().Count);
                    Assert.AreEqual(24, CreateSafeContentQuery("+Description:SystemFlagTest .COUNTONLY .AUTOFILTERS:OFF", QuerySettings.Default).Execute().Count);
                }
                finally
                {
                    srcParent.ForceDelete();
                    target.ForceDelete();
                }
            });
        }
        public void SystemFlag_Copy_FromSystemFolderToSystemFolder()
        {
            IntegrationTest(() =>
            {
                var srcParent = new SystemFolder(Repository.Root) { Name = "Source-" + Guid.NewGuid() };
                srcParent.Save();
                var target = new SystemFolder(Repository.Root) { Name = "Target-" + Guid.NewGuid() };
                var targetName = target.Name;
                target.Save();
                var nodeIds = CreateSourceStructure(srcParent);
                Node f1, f2, s3, f4, s5, f6, f7, f8, f9, f10, f11, f12;
                try
                {
                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsTrue(f1.IsSystem, "F1");
                    Assert.IsTrue(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsTrue(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsTrue(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsTrue(f9.IsSystem, "F9");
                    Assert.IsTrue(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    f1.CopyTo(target);

                    f1 = Node.LoadNode($"/Root/{targetName}/F1");
                    f2 = Node.LoadNode($"/Root/{targetName}/F1/F2");
                    s3 = Node.LoadNode($"/Root/{targetName}/F1/S3");
                    f4 = Node.LoadNode($"/Root/{targetName}/F1/F4");
                    s5 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5");
                    f6 = Node.LoadNode($"/Root/{targetName}/F1/F2/F6");
                    f7 = Node.LoadNode($"/Root/{targetName}/F1/S3/F7");
                    f8 = Node.LoadNode($"/Root/{targetName}/F1/S3/F8");
                    f9 = Node.LoadNode($"/Root/{targetName}/F1/F4/F9");
                    f10 = Node.LoadNode($"/Root/{targetName}/F1/F4/F10");
                    f11 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5/F11");
                    f12 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5/F12");

                    Assert.IsTrue(f1.IsSystem, "F1");
                    Assert.IsTrue(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsTrue(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsTrue(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsTrue(f9.IsSystem, "F9");
                    Assert.IsTrue(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    Assert.AreEqual(0, GetSystemFlagRelatedContentCount(false));
                    Assert.AreEqual(24, GetSystemFlagRelatedContentCount(true));
                }
                finally
                {
                    srcParent.ForceDelete();
                    target.ForceDelete();
                }
            });
        }
        public void SystemFlag_Copy_FromFolderToSystemFolder()
        {
            IntegrationTest(() =>
            {
                var srcParent = new Folder(Repository.Root) { Name = "Source-" + Guid.NewGuid() };
                srcParent.Save();
                var target = new SystemFolder(Repository.Root) { Name = "Target-" + Guid.NewGuid() };
                var targetName = target.Name;
                target.Save();
                var nodeIds = CreateSourceStructure(srcParent);
                Node f1, f2, s3, f4, s5, f6, f7, f8, f9, f10, f11, f12;
                try
                {
                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsFalse(f1.IsSystem, "F1");
                    Assert.IsFalse(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsFalse(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsFalse(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsFalse(f9.IsSystem, "F9");
                    Assert.IsFalse(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    f1.CopyTo(target);

                    f1 = Node.LoadNode($"/Root/{targetName}/F1");
                    f2 = Node.LoadNode($"/Root/{targetName}/F1/F2");
                    s3 = Node.LoadNode($"/Root/{targetName}/F1/S3");
                    f4 = Node.LoadNode($"/Root/{targetName}/F1/F4");
                    s5 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5");
                    f6 = Node.LoadNode($"/Root/{targetName}/F1/F2/F6");
                    f7 = Node.LoadNode($"/Root/{targetName}/F1/S3/F7");
                    f8 = Node.LoadNode($"/Root/{targetName}/F1/S3/F8");
                    f9 = Node.LoadNode($"/Root/{targetName}/F1/F4/F9");
                    f10 = Node.LoadNode($"/Root/{targetName}/F1/F4/F10");
                    f11 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5/F11");
                    f12 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5/F12");

                    Assert.IsTrue(f1.IsSystem, "F1");
                    Assert.IsTrue(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsTrue(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsTrue(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsTrue(f9.IsSystem, "F9");
                    Assert.IsTrue(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    Assert.AreEqual(6, GetSystemFlagRelatedContentCount(false));
                    Assert.AreEqual(24, GetSystemFlagRelatedContentCount(true));
                }
                finally
                {
                    srcParent.ForceDelete();
                    target.ForceDelete();
                }
            });
        }
        public void SystemFlag_Copy_FromSystemFolderToFolder()
        {
            IntegrationTest(() =>
            {
                var srcParent = new SystemFolder(Repository.Root) { Name = "Source-" + Guid.NewGuid() };
                srcParent.Save();
                var target = new Folder(Repository.Root) { Name = "Target-" + Guid.NewGuid() };
                var targetName = target.Name;
                target.Save();
                var nodeIds = CreateSourceStructure(srcParent);
                Node f1, f2, s3, f4, s5, f6, f7, f8, f9, f10, f11, f12;
                try
                {
                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsTrue(f1.IsSystem, "F1");
                    Assert.IsTrue(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsTrue(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsTrue(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsTrue(f9.IsSystem, "F9");
                    Assert.IsTrue(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    f1.CopyTo(target);

                    f1 = Node.LoadNode($"/Root/{targetName}/F1");
                    f2 = Node.LoadNode($"/Root/{targetName}/F1/F2");
                    s3 = Node.LoadNode($"/Root/{targetName}/F1/S3");
                    f4 = Node.LoadNode($"/Root/{targetName}/F1/F4");
                    s5 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5");
                    f6 = Node.LoadNode($"/Root/{targetName}/F1/F2/F6");
                    f7 = Node.LoadNode($"/Root/{targetName}/F1/S3/F7");
                    f8 = Node.LoadNode($"/Root/{targetName}/F1/S3/F8");
                    f9 = Node.LoadNode($"/Root/{targetName}/F1/F4/F9");
                    f10 = Node.LoadNode($"/Root/{targetName}/F1/F4/F10");
                    f11 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5/F11");
                    f12 = Node.LoadNode($"/Root/{targetName}/F1/F2/S5/F12");

                    Assert.IsFalse(f1.IsSystem, "F1");
                    Assert.IsFalse(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsFalse(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsFalse(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsFalse(f9.IsSystem, "F9");
                    Assert.IsFalse(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    Assert.AreEqual(6, GetSystemFlagRelatedContentCount(false));
                    Assert.AreEqual(24, GetSystemFlagRelatedContentCount(true));
                }
                finally
                {
                    srcParent.ForceDelete();
                    target.ForceDelete();
                }
            });
        }

        public void SystemFlag_Move_FromFolderToFolder()
        {
            IntegrationTest(() =>
            {
                var srcParent = new Folder(Repository.Root) { Name = "Source-" + Guid.NewGuid() };
                srcParent.Save();
                var target = new Folder(Repository.Root) { Name = "Target-" + Guid.NewGuid() };
                target.Save();
                var nodeIds = CreateSourceStructure(srcParent);
                Node f1, f2, s3, f4, s5, f6, f7, f8, f9, f10, f11, f12;
                try
                {
                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsFalse(f1.IsSystem, "F1");
                    Assert.IsFalse(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsFalse(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsFalse(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsFalse(f9.IsSystem, "F9");
                    Assert.IsFalse(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    f1.MoveTo(target);

                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsFalse(f1.IsSystem, "F1");
                    Assert.IsFalse(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsFalse(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsFalse(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsFalse(f9.IsSystem, "F9");
                    Assert.IsFalse(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    Assert.AreEqual(6, GetSystemFlagRelatedContentCount(false));
                    Assert.AreEqual(12, GetSystemFlagRelatedContentCount(true));
                }
                finally
                {
                    srcParent.ForceDelete();
                    target.ForceDelete();
                }
            });
        }
        public void SystemFlag_Move_FromSystemFolderToSystemFolder()
        {
            IntegrationTest(() =>
            {
                var srcParent = new SystemFolder(Repository.Root) { Name = "Source-" + Guid.NewGuid() };
                srcParent.Save();
                var target = new SystemFolder(Repository.Root) { Name = "Target-" + Guid.NewGuid() };
                target.Save();
                var nodeIds = CreateSourceStructure(srcParent);
                Node f1, f2, s3, f4, s5, f6, f7, f8, f9, f10, f11, f12;
                try
                {
                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsTrue(f1.IsSystem, "F1");
                    Assert.IsTrue(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsTrue(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsTrue(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsTrue(f9.IsSystem, "F9");
                    Assert.IsTrue(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    f1.MoveTo(target);

                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsTrue(f1.IsSystem, "F1");
                    Assert.IsTrue(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsTrue(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsTrue(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsTrue(f9.IsSystem, "F9");
                    Assert.IsTrue(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    Assert.AreEqual(0, GetSystemFlagRelatedContentCount(false));
                    Assert.AreEqual(12, GetSystemFlagRelatedContentCount(true));
                }
                finally
                {
                    srcParent.ForceDelete();
                    target.ForceDelete();
                }
            });
        }
        public void SystemFlag_Move_FromFolderToSystemFolder()
        {
            IntegrationTest(() =>
            {
                var srcParent = new Folder(Repository.Root) { Name = "Source-" + Guid.NewGuid() };
                srcParent.Save();
                var target = new SystemFolder(Repository.Root) { Name = "Target-" + Guid.NewGuid() };
                target.Save();
                var nodeIds = CreateSourceStructure(srcParent);
                Node f1, f2, s3, f4, s5, f6, f7, f8, f9, f10, f11, f12;
                try
                {
                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsFalse(f1.IsSystem, "F1");
                    Assert.IsFalse(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsFalse(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsFalse(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsFalse(f9.IsSystem, "F9");
                    Assert.IsFalse(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    f1.MoveTo(target);

                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsTrue(f1.IsSystem, "F1");
                    Assert.IsTrue(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsTrue(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsTrue(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsTrue(f9.IsSystem, "F9");
                    Assert.IsTrue(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    Assert.AreEqual(0, GetSystemFlagRelatedContentCount(false));
                    Assert.AreEqual(12, GetSystemFlagRelatedContentCount(true));
                }
                finally
                {
                    srcParent.ForceDelete();
                    target.ForceDelete();
                }
            });
        }
        public void SystemFlag_Move_FromSystemFolderToFolder()
        {
            IntegrationTest(() =>
            {
                var srcParent = new SystemFolder(Repository.Root) { Name = "Source-" + Guid.NewGuid() };
                srcParent.Save();
                var target = new Folder(Repository.Root) { Name = "Target-" + Guid.NewGuid() };
                target.Save();
                var nodeIds = CreateSourceStructure(srcParent);
                Node f1, f2, s3, f4, s5, f6, f7, f8, f9, f10, f11, f12;
                try
                {
                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsTrue(f1.IsSystem, "F1");
                    Assert.IsTrue(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsTrue(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsTrue(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsTrue(f9.IsSystem, "F9");
                    Assert.IsTrue(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    f1.MoveTo(target);

                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsFalse(f1.IsSystem, "F1");
                    Assert.IsFalse(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsFalse(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsFalse(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsFalse(f9.IsSystem, "F9");
                    Assert.IsFalse(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    Assert.AreEqual(6, GetSystemFlagRelatedContentCount(false));
                    Assert.AreEqual(12, GetSystemFlagRelatedContentCount(true));
                }
                finally
                {
                    srcParent.ForceDelete();
                    target.ForceDelete();
                }
            });
        }

        [Description("Tree root is folder but subtree contains system folder descendants.")]
        public void SystemFlag_Move_FromFolderToSystemFolder_Descendant()
        {
            IntegrationTest(() =>
            {
                var srcParent = new Folder(Repository.Root) { Name = "Source-" + Guid.NewGuid() };
                srcParent.Save();
                var target = new Folder(Repository.Root) { Name = "Target-" + Guid.NewGuid() };
                target.Save();

                if (ContentType.GetByName(nameof(TestSystemFolder)) == null)
                    ContentTypeInstaller.InstallContentType(TestSystemFolder.ContentTypeDefinition);

                var nodeIds = CreateSourceStructure2(srcParent);
                Node f1, f2, s3, f4, s5, f6, f7, f8, f9, f10, f11, f12;
                try
                {
                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsFalse(f1.IsSystem, "F1");
                    Assert.IsFalse(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsFalse(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsFalse(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsFalse(f9.IsSystem, "F9");
                    Assert.IsFalse(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    f1.MoveTo(target);

                    f1 = Node.LoadNode(nodeIds["F1"]);
                    f2 = Node.LoadNode(nodeIds["F2"]);
                    s3 = Node.LoadNode(nodeIds["S3"]);
                    f4 = Node.LoadNode(nodeIds["F4"]);
                    s5 = Node.LoadNode(nodeIds["S5"]);
                    f6 = Node.LoadNode(nodeIds["F6"]);
                    f7 = Node.LoadNode(nodeIds["F7"]);
                    f8 = Node.LoadNode(nodeIds["F8"]);
                    f9 = Node.LoadNode(nodeIds["F9"]);
                    f10 = Node.LoadNode(nodeIds["F10"]);
                    f11 = Node.LoadNode(nodeIds["F11"]);
                    f12 = Node.LoadNode(nodeIds["F12"]);

                    Assert.IsFalse(f1.IsSystem, "F1");
                    Assert.IsFalse(f2.IsSystem, "F2");
                    Assert.IsTrue(s3.IsSystem, "S3");
                    Assert.IsFalse(f4.IsSystem, "F4");
                    Assert.IsTrue(s5.IsSystem, "S5");
                    Assert.IsFalse(f6.IsSystem, "F6");
                    Assert.IsTrue(f7.IsSystem, "F7");
                    Assert.IsTrue(f8.IsSystem, "F8");
                    Assert.IsFalse(f9.IsSystem, "F9");
                    Assert.IsFalse(f10.IsSystem, "F10");
                    Assert.IsTrue(f11.IsSystem, "F11");
                    Assert.IsTrue(f12.IsSystem, "F12");

                    Assert.AreEqual(6, GetSystemFlagRelatedContentCount(false));
                    Assert.AreEqual(12, GetSystemFlagRelatedContentCount(true));
                }
                finally
                {
                    srcParent.ForceDelete();
                    target.ForceDelete();
                    ContentTypeInstaller.RemoveContentType(nameof(TestSystemFolder));
                }
            });
        }

        /* ===================================================================================== Tools */

        private Dictionary<string, int> CreateSourceStructure(Folder srcParent)
        {
            var ids = new Dictionary<string, int>();
            var f1 = new Folder(srcParent) { Name = "F1", Description = "SystemFlagTest" }; f1.Save(); ids.Add(f1.Name, f1.Id);
            {
                var f2 = new Folder(f1) { Name = "F2", Description = "SystemFlagTest" }; f2.Save(); ids.Add(f2.Name, f2.Id);
                {
                    var s5 = new SystemFolder(f2) { Name = "S5", Description = "SystemFlagTest" }; s5.Save(); ids.Add(s5.Name, s5.Id);
                    {
                        var f11 = new Folder(s5) { Name = "F11", Description = "SystemFlagTest" }; f11.Save(); ids.Add(f11.Name, f11.Id);
                        var f12 = new Folder(s5) { Name = "F12", Description = "SystemFlagTest" }; f12.Save(); ids.Add(f12.Name, f12.Id);
                    }
                    var f6 = new Folder(f2) { Name = "F6", Description = "SystemFlagTest" }; f6.Save(); ids.Add(f6.Name, f6.Id);
                }
                var s3 = new SystemFolder(f1) { Name = "S3", Description = "SystemFlagTest" }; s3.Save(); ids.Add(s3.Name, s3.Id);
                {
                    var f7 = new Folder(s3) { Name = "F7", Description = "SystemFlagTest" }; f7.Save(); ids.Add(f7.Name, f7.Id);
                    var f8 = new Folder(s3) { Name = "F8", Description = "SystemFlagTest" }; f8.Save(); ids.Add(f8.Name, f8.Id);
                }
                var f4 = new Folder(f1) { Name = "F4", Description = "SystemFlagTest" }; f4.Save(); ids.Add(f4.Name, f4.Id);
                {
                    var f9 = new Folder(f4) { Name = "F9", Description = "SystemFlagTest" }; f9.Save(); ids.Add(f9.Name, f9.Id);
                    var f10 = new Folder(f4) { Name = "F10", Description = "SystemFlagTest" }; f10.Save(); ids.Add(f10.Name, f10.Id);
                }
            }

            return ids;
        }
        private Dictionary<string, int> CreateSourceStructure2(Folder srcParent)
        {
            var ids = new Dictionary<string, int>();
            var f1 = new Folder(srcParent) { Name = "F1", Description = "SystemFlagTest" }; f1.Save(); ids.Add(f1.Name, f1.Id);
            {
                var f2 = new Folder(f1) { Name = "F2", Description = "SystemFlagTest" }; f2.Save(); ids.Add(f2.Name, f2.Id);
                {
                    f2.AllowChildType(nameof(TestSystemFolder), true, save: true);
                    var s5 = new TestSystemFolder(f2) { Name = "S5", Description = "SystemFlagTest" }; s5.Save(); ids.Add(s5.Name, s5.Id);
                    {
                        var f11 = new Folder(s5) { Name = "F11", Description = "SystemFlagTest" }; f11.Save(); ids.Add(f11.Name, f11.Id);
                        var f12 = new Folder(s5) { Name = "F12", Description = "SystemFlagTest" }; f12.Save(); ids.Add(f12.Name, f12.Id);
                    }
                    var f6 = new Folder(f2) { Name = "F6", Description = "SystemFlagTest" }; f6.Save(); ids.Add(f6.Name, f6.Id);
                }
                var s3 = new TestSystemFolder(f1) { Name = "S3", Description = "SystemFlagTest" }; s3.Save(); ids.Add(s3.Name, s3.Id);
                {
                    var f7 = new Folder(s3) { Name = "F7", Description = "SystemFlagTest" }; f7.Save(); ids.Add(f7.Name, f7.Id);
                    var f8 = new Folder(s3) { Name = "F8", Description = "SystemFlagTest" }; f8.Save(); ids.Add(f8.Name, f8.Id);
                }
                var f4 = new Folder(f1) { Name = "F4", Description = "SystemFlagTest" }; f4.Save(); ids.Add(f4.Name, f4.Id);
                {
                    var f9 = new Folder(f4) { Name = "F9", Description = "SystemFlagTest" }; f9.Save(); ids.Add(f9.Name, f9.Id);
                    var f10 = new Folder(f4) { Name = "F10", Description = "SystemFlagTest" }; f10.Save(); ids.Add(f10.Name, f10.Id);
                }
            }

            return ids;
        }

        private int GetSystemFlagRelatedContentCount(bool isSystem)
        {
            if (isSystem)
                return CreateSafeContentQuery(
                    "+Description:SystemFlagTest .COUNTONLY .AUTOFILTERS:OFF",
                    QuerySettings.Default).Execute().Count;
            return CreateSafeContentQuery(
                "+Description:SystemFlagTest .COUNTONLY",
                QuerySettings.Default).Execute().Count;
        }
    }
}
