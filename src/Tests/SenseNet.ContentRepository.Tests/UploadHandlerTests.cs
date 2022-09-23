using System;
using System.Collections.Generic;
using System.Net.Http;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Services.Core.Operations;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class UploadHandlerTests : TestBase
    {
        [TestMethod]
        public void UploadHandler_Create_Empty()
        {
            Test(() =>
            {
                var root = Content.Load("/Root/Content");
                var parent = Content.CreateNew("DocumentLibrary", root.ContentHandler, "DocLib");
                parent.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                UploadTextFile(null, parent, "Test.txt", false);

                var createdContent = Content.Load("/Root/Content/DocLib/Test.txt");
                var stream = createdContent.ContentHandler.GetBinary("Binary").GetStream();

                Assert.AreEqual(0, stream.Length);
            });
        }
        [TestMethod]
        public void UploadHandler_Create_Text()
        {
            Test(() =>
            {
                var firstText = "First content.";
                var root = Content.Load("/Root/Content");
                var parent = Content.CreateNew("DocumentLibrary", root.ContentHandler, "DocLib");
                parent.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                UploadTextFile(firstText, parent, "Test.txt", false);

                var createdContent = Content.Load("/Root/Content/DocLib/Test.txt");
                var createdText =
                    RepositoryTools.GetStreamString(createdContent.ContentHandler.GetBinary("Binary").GetStream());

                Assert.AreEqual(firstText, createdText);
            });
        }
        [TestMethod]
        public void UploadHandler_Update_EmptyToText()
        {
            Test(() =>
            {
                var modifiedText = "Modified content.";
                var root = Content.Load("/Root/Content");
                var parent = Content.CreateNew("DocumentLibrary", root.ContentHandler, "DocLib");
                parent.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                UploadTextFile(null, parent, "Test.txt", false);
                UploadTextFile(modifiedText, parent, "Test.txt", true);

                var content = Content.Load("/Root/Content/DocLib/Test.txt");
                var createdText =
                    RepositoryTools.GetStreamString(content.ContentHandler.GetBinary("Binary").GetStream());

                Assert.AreEqual(modifiedText, createdText);
            });
        }
        [TestMethod]
        public void UploadHandler_Update_TextToEmpty()
        {
            Test(() =>
            {
                var firstText = "First content.";
                var root = Content.Load("/Root/Content");
                var parent = Content.CreateNew("DocumentLibrary", root.ContentHandler, "DocLib");
                parent.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                UploadTextFile(firstText, parent, "Test.txt", false);
                UploadTextFile(null, parent, "Test.txt", true);

                var content = Content.Load("/Root/Content/DocLib/Test.txt");
                var stream = content.ContentHandler.GetBinary("Binary").GetStream();

                Assert.AreEqual(0, stream.Length);
            });
        }
        [TestMethod]
        public void UploadHandler_Update_TextToText()
        {
            Test(() =>
            {
                var firstText = "First content.";
                var modifiedText = "Modified content.";
                var root = Content.Load("/Root/Content");
                var parent = Content.CreateNew("DocumentLibrary", root.ContentHandler, "DocLib");
                parent.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                UploadTextFile(firstText, parent, "Test.txt", false);
                UploadTextFile(modifiedText, parent, "Test.txt", true);

                var content = Content.Load("/Root/Content/DocLib/Test.txt");
                var createdText =
                    RepositoryTools.GetStreamString(content.ContentHandler.GetBinary("Binary").GetStream());

                Assert.AreEqual(modifiedText, createdText);
            });
        }


        private object UploadTextFile(string text, Content parent, string fileName, bool overwrite)
        {
            var httpContext = CreateHttpContext("/Root/Content/DocLib");
            var stream = text == null
                ? new MemoryStream(Array.Empty<byte>())
                : RepositoryTools.GetStreamFromString(text);
            var formFile = new FormFile(stream, 0L, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary()
            };
            formFile.ContentType = "text/plain";
            var files = new FormFileCollection { formFile };
            IFormCollection form = new FormCollection(new Dictionary<string, StringValues>(), files);
            httpContext.Request.Form = form;

            var handler = new UploadHandler(parent, httpContext)
            {
                FileName = fileName,
                PropertyName = "Binary",
                Overwrite = overwrite,
                ChunkToken = "0*0*False*False"
            };

            var response = handler.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
            return response;
        }
        private HttpContext CreateHttpContext(string resource, string queryString = null, IServiceProvider services = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services ?? new ServiceCollection().BuildServiceProvider();
            var request = httpContext.Request;
            request.Method = "GET";
            request.Path = resource;
            request.QueryString = queryString == null ? new QueryString() : new QueryString(queryString);
            httpContext.Response.Body = new MemoryStream();
            return httpContext;
        }

    }
}
