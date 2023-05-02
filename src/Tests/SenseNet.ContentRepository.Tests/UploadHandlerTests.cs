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
using SenseNet.ContentRepository.Schema;
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

        /* ========================================================================= Upload CTD */

        private readonly string _ctd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ContentType name=""TestContent"" parentType=""Folder"" handler=""SenseNet.ContentRepository.Folder"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields><Field name=""AppInfo"" type=""ShortText""><DisplayName>AppInfo</DisplayName></Field></Fields>
</ContentType>";

        [TestMethod]
        public void UploadHandler_Create_ContentType()
        {
            Test(() =>
            {
                var parent = Content.Load("/Root/System/Schema/ContentTypes/GenericContent");

                // ACT
                UploadCtd(_ctd, parent, "ContentType", "TestContent");

                // ASSERT
                Assert.IsNotNull(ContentType.GetByName("TestContent"));

                var createdContent = Content.Load("/Root/System/Schema/ContentTypes/GenericContent/Folder/TestContent");
                Assert.IsNotNull(createdContent);
                Assert.AreEqual("ContentType", createdContent.ContentType.Name);

                var createdText =
                    RepositoryTools.GetStreamString(createdContent.ContentHandler.GetBinary("Binary").GetStream());
                Assert.AreEqual(_ctd, createdText);
            });
        }
        [TestMethod]
        public void UploadHandler_Create_ContentType_AsFile()
        {
            Test(() =>
            {
                var parent = Content.Load("/Root/System/Schema/ContentTypes/GenericContent");

                // ACT
                try
                {
                    UploadCtd(_ctd, parent, "File", "TestContent");
                    Assert.Fail("The expected exception was not thrown.");
                }
                catch (Exception ex)
                {
                    // do nothing
                }

                // ASSERT
                Assert.IsNull(ContentType.GetByName("TestContent"));

                var createdContent = Content.Load("/Root/System/Schema/ContentTypes/GenericContent/Folder/TestContent");
                Assert.IsNull(createdContent);
                createdContent = Content.Load("/Root/System/Schema/ContentTypes/GenericContent/TestContent");
                Assert.IsNull(createdContent);
                createdContent = Content.Load("/Root/System/Schema/ContentTypes/TestContent");
                Assert.IsNull(createdContent);
            });
        }

        private readonly string _rootCtd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ContentType name=""TestContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields><Field name=""AppInfo"" type=""ShortText""><DisplayName>AppInfo</DisplayName></Field></Fields>
</ContentType>";

        [TestMethod]
        public void UploadHandler_Create_RootContentType()
        {
            Test(() =>
            {
                var parent = Content.Load("/Root/System/Schema/ContentTypes/GenericContent");

                // ACT
                try
                {
                    UploadCtd(_rootCtd, parent, "ContentType", "TestContent");
                    Assert.Fail("The expected exception was not thrown.");
                }
                catch (Exception ex)
                {
                    // do nothing
                }

                // ASSERT
                Assert.IsNull(ContentType.GetByName("TestContent"));

                var createdContent = Content.Load("/Root/System/Schema/ContentTypes/GenericContent/TestContent");
                Assert.IsNull(createdContent);
                createdContent = Content.Load("/Root/System/Schema/ContentTypes/TestContent");
                Assert.IsNull(createdContent);

            });
        }


        private readonly string _wrongParentCtd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ContentType name=""TestContent"" parentType=""ContentType"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields><Field name=""AppInfo"" type=""ShortText""><DisplayName>AppInfo</DisplayName></Field></Fields>
</ContentType>";

        [TestMethod]
        public void UploadHandler_Create_InheritFromContentType()
        {
            Test(() =>
            {
                var parent = Content.Load("/Root/System/Schema/ContentTypes/GenericContent");

                // ACT
                try
                {
                    UploadCtd(_wrongParentCtd, parent, "ContentType", "TestContent");
                    Assert.Fail("The expected exception was not thrown.");
                }
                catch (Exception ex)
                {
                    // do nothing
                }

                // ASSERT
                Assert.IsNull(ContentType.GetByName("TestContent"));

                var createdContent = Content.Load("/Root/System/Schema/ContentTypes/GenericContent/TestContent");
                Assert.IsNull(createdContent);
                createdContent = Content.Load("/Root/System/Schema/ContentTypes/TestContent");
                Assert.IsNull(createdContent);

            });
        }

        private object UploadCtd(string text, Content parent, string contentType, string fileName)
        {
            var httpContext = CreateHttpContext("/Root/Content/DocLib");

            var handler = new UploadHandler(parent, httpContext)
            {
                FileName = fileName,
                ContentTypeName = contentType,
                PropertyName = "Binary",
                Overwrite = true,
                ChunkToken = "0*0*False*False",
                FileText = text,
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
