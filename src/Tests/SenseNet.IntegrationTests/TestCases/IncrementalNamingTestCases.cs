﻿using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.IntegrationTests.Infrastructure;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.TestCases
{
    public class IncrementalNamingTestCases : TestCaseBase
    {
        private static string ContentType_IncrementalNamingAllowedName = "ContentType_IncrementalNamingAllowed";
        private static string ContentType_IncrementalNamingDisallowedName = "ContentType_IncrementalNamingDisallowed";
        private static ContentType ContentType_IncrementalNamingAllowed;
        private static ContentType ContentType_IncrementalNamingDisallowed;
        public static void InstallContentTypes()
        {
            var ctdformat = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <ContentType name=""{0}"" parentType=""Car"" handler=""SenseNet.ContentRepository.GenericContent""
                             xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                  <AllowIncrementalNaming>{1}</AllowIncrementalNaming>
                </ContentType>";
            var ctd1 = String.Format(ctdformat, ContentType_IncrementalNamingAllowedName, "true");
            var ctd2 = String.Format(ctdformat, ContentType_IncrementalNamingDisallowedName, "false");
            ContentTypeInstaller.InstallContentType(CarContentType, ctd1, ctd2);
            ContentType_IncrementalNamingAllowed = ContentType.GetByName(ContentType_IncrementalNamingAllowedName);
            ContentType_IncrementalNamingDisallowed = ContentType.GetByName(ContentType_IncrementalNamingDisallowedName);
        }

        /* ================================================================================= TEST CASES */

        public void ContentNaming_AllowIncrementalNaming_Allowed()
        {
            IntegrationTest(() =>
            {
                InstallContentTypes();
                var testRoot = new SystemFolder(Repository.Root);
                testRoot.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                Content content1, content2;
                do
                {
                    content1 = Content.CreateNew(ContentType_IncrementalNamingAllowedName, testRoot, null);
                    content2 = Content.CreateNew(ContentType_IncrementalNamingAllowedName, testRoot, null);
                } while (content1.Name != content2.Name);
                content1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                content2.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            });

        }
        public void ContentNaming_AllowIncrementalNaming_Disallowed()
        {
            IntegrationTest(() =>
            {
                var thrown = false;
                try
                {
                    InstallContentTypes();
                    var testRoot = new SystemFolder(Repository.Root);
                    testRoot.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                    Content content1, content2;
                    do
                    {
                        content1 = Content.CreateNew(ContentType_IncrementalNamingDisallowedName, testRoot, null);
                        content2 = Content.CreateNew(ContentType_IncrementalNamingDisallowedName, testRoot, null);
                    } while (content1.Name != content2.Name);
                    content1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    content2.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (NodeAlreadyExistsException)
                {
                    thrown = true;
                }
                catch (AggregateException ae)
                {
                    thrown = ae.InnerException is NodeAlreadyExistsException;
                }
                if (!thrown)
                    Assert.Fail("The expected NodeAlreadyExistsException was not thrown.");
            });
        }
    }
}
