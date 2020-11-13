using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class FileTests : GenericContentTests
    {
        [TestMethod]
        public void MultiStep_Existing_Incremental()
        {
            Test(() =>
            {
                var root = CreateTestRoot();
                var file = new File(root) { Name = "test.txt" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString("test file"));

                file.Save();

                // create a new file with the same name
                var file2 = new File(root) { Name = file.Name };
                file2.Binary.SetStream(RepositoryTools.GetStreamFromString("new file content"));

                // this should prevent the exception and simply generate a new name
                file2.AllowIncrementalNaming = true;

                // this is normal in case of chunk upload, it should not throw an exception
                file2.Save(SavingMode.StartMultistepSave);
            });
        }

        [TestMethod]
        public void File_Create_TXT()
        {
            CreateFileTest("file.txt", "text/plain", "Lorem ipcum dolor sit amet.", true);
        }
        [TestMethod]
        public void File_Create_PDF()
        {
            CreateFileTest("file.pdf", "application/pdf");
        }
        [TestMethod]
        public void File_Create_DOC()
        {
            CreateFileTest("file.doc", "application/msword");
        }
        [TestMethod]
        public void File_Create_DOCX()
        {
            CreateFileTest("file.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        }
        [TestMethod]
        public void File_Create_XLS()
        {
            CreateFileTest("file.xls", "application/vnd.ms-excel");
        }
        [TestMethod]
        public void File_Create_XLSX()
        {
            CreateFileTest("file.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        [TestMethod]
        public void File_Create_PPT()
        {
            CreateFileTest("file.ppt", "application/vnd.ms-powerpoint");
        }
        [TestMethod]
        public void File_Create_PPTX()
        {
            CreateFileTest("file.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");
        }
        [TestMethod]
        public void File_Create_NoExtension()
        {
            CreateFileTest("FILE", MimeTable.DefaultMimeType, "Lorem ipcum dolor sit amet.", false);
        }
        [TestMethod]
        public void File_Create_NoExtension_ExplicitMime()
        {
            Test(() =>
            {
                var root = CreateTestRoot();
                var file = new File(root)
                {
                    Name = "FILE",
                    Binary = {ContentType = "text/plain"}
                };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString("Lorem ipsum dolor sit amet."));
                file.Save();

                // check mime type
                file = Node.Load<File>(file.Id);
                Assert.AreEqual("text/plain", file.Binary.ContentType);

                // check searchability
                var queryResult = CreateSafeContentQuery("dolor").Execute();
                Assert.AreEqual(0, queryResult.Count);
            });
        }

        private void CreateFileTest(string fileName, string expectedMimeType, string fileContent = null, bool searchable = false)
        {
            Test(() =>
            {
                var root = CreateTestRoot();
                var file = new File(root) { Name = fileName };

                file.Save();

                if (fileContent != null)
                {
                    file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
                    file.Save();
                }

                // check mime type
                file = Node.Load<File>(file.Id);
                Assert.AreEqual(expectedMimeType, file.Binary.ContentType);

                // check searchability
                if (fileContent != null && searchable)
                {
                    var queryResult = CreateSafeContentQuery(fileContent.Split(' ')[1]).Execute();
                    Assert.AreEqual(1, queryResult.Count);
                }
            });
        }
    }
}
