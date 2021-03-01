using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Storage;
using SenseNet.Testing;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class FileSystemWrapperTests : TestBase
    {
        private string[] PathsForTest_GetFiles = new[]
        {
            @"c:\MyFolder\1.content",
            @"c:\MyFolder\1a.txt",
            @"c:\MyFolder\2.Content",
            @"c:\MyFolder\2a.TxT",
            @"c:\MyFolder\3.CoNtEnT",
            @"c:\MyFolder\3a.TXT",
            @"c:\MyFolder\4.CONTENT"
        };

        [TestMethod]
        public void FileSystemWrapper_GetFilesByExtension_content()
        {
            var acc = new TypeAccessor(typeof(FileSystemWrapper.Directory));

            // ACTION
            var result = (string[])acc.InvokeStatic("GetFilesByExtension", PathsForTest_GetFiles, "content");

            // ASSERT
            var actual = string.Join(", ", result.Select(Path.GetFileNameWithoutExtension));
            Assert.AreEqual("1, 2, 3, 4", actual);
        }
        [TestMethod]
        public void FileSystemWrapper_GetFilesByExtension_dotcontent()
        {
            var acc = new TypeAccessor(typeof(FileSystemWrapper.Directory));

            // ACTION
            var result = (string[])acc.InvokeStatic("GetFilesByExtension", PathsForTest_GetFiles, ".content");

            // ASSERT
            var actual = string.Join(", ", result.Select(Path.GetFileNameWithoutExtension));
            Assert.AreEqual("1, 2, 3, 4", actual);
        }
        [TestMethod]
        public void FileSystemWrapper_GetFiles_suffix_TXT()
        {
            var acc = new TypeAccessor(typeof(FileSystemWrapper.Directory));

            // ACTION
            var result = (string[])acc.InvokeStatic("GetFilesByExtension", PathsForTest_GetFiles, "TXT");

            // ASSERT
            var actual = string.Join(", ", result.Select(Path.GetFileNameWithoutExtension));
            Assert.AreEqual("1a, 2a, 3a", actual);
        }
        [TestMethod]
        public void FileSystemWrapper_GetFiles_suffix_dotTXT()
        {
            var acc = new TypeAccessor(typeof(FileSystemWrapper.Directory));

            // ACTION
            var result = (string[])acc.InvokeStatic("GetFilesByExtension", PathsForTest_GetFiles, ".TXT");

            // ASSERT
            var actual = string.Join(", ", result.Select(Path.GetFileNameWithoutExtension));
            Assert.AreEqual("1a, 2a, 3a", actual);
        }
    }
}
