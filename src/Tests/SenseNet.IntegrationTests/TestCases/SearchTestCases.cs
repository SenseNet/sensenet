﻿using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.IntegrationTests.Infrastructure;

namespace SenseNet.IntegrationTests.TestCases
{
    public class SearchTestCases : TestCaseBase
    {
        private const string CtdSearchTestReference = @"<ContentType name=""GenericContentWithReferenceTest"" 
parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" 
xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields>
    <Field name=""SearchTestReference"" type=""Reference"">
        <Configuration>
        <AllowMultiple>true</AllowMultiple>
        </Configuration>
    </Field>
  </Fields>
</ContentType>
";
        public void Search_ReferenceField()
        {
            IntegrationTest(() =>
            {
                ContentTypeInstaller.InstallContentType(CtdSearchTestReference);

                var parent = RepositoryTools.CreateStructure("/Root/" + Guid.NewGuid(), "SystemFolder");
                var folder1 = Content.CreateNew("Folder", parent.ContentHandler, "f1");
                folder1.SaveSameVersionAsync(CancellationToken.None).GetAwaiter().GetResult();
                var folder2 = Content.CreateNew("Folder", parent.ContentHandler, "f2");
                folder2.SaveSameVersionAsync(CancellationToken.None).GetAwaiter().GetResult();
                var folder3 = Content.CreateNew("Folder", parent.ContentHandler, "f3");
                folder3.SaveSameVersionAsync(CancellationToken.None).GetAwaiter().GetResult();

                var content1 = Content.CreateNew("GenericContentWithReferenceTest", parent.ContentHandler, "content1");

                content1["SearchTestReference"] = new[]
                {
                    folder1.ContentHandler, folder2.ContentHandler, folder3.ContentHandler
                };
                content1.SaveSameVersionAsync(CancellationToken.None).GetAwaiter().GetResult();

                // find content by the reference field
                var result1 = CreateSafeContentQuery($"+SearchTestReference:{folder1.Id}").Execute().Nodes.FirstOrDefault();
                var result2 = CreateSafeContentQuery($"+SearchTestReference:{folder2.Id}").Execute().Nodes.FirstOrDefault();
                var result3 = CreateSafeContentQuery($"+SearchTestReference:{folder3.Id}").Execute().Nodes.FirstOrDefault();

                Assert.IsNotNull(result1);
                Assert.IsNotNull(result2);
                Assert.IsNotNull(result3);
                Assert.AreEqual(content1.Id, result1.Id);
                Assert.AreEqual(content1.Id, result2.Id);
                Assert.AreEqual(content1.Id, result3.Id);
            });
        }
    }
}
