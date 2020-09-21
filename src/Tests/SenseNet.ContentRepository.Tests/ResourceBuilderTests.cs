using System;
using System.Drawing.Text;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Packaging.Tools;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ResourceBuilderTests : TestBase
    {
        [TestMethod]
        public async System.Threading.Tasks.Task ResourceBuilder_AddNewContent()
        {
            await Test(async () =>
            {
                EnsureLocalizationParent();

                var contentName = "Resource-" + Guid.NewGuid() + ".xml";
                var contentPath = RepositoryPath.Combine(RepositoryStructure.ResourceFolderPath, contentName);
                var rb = new ResourceBuilder();

                rb.Content(contentName)
                    .Class("TestClass1")
                    .CultureEn()
                    .AddResource("DisplayName", "disp lay name")
                    .AddResource("Resource2", "value2");

                using (new SystemAccount())
                {
                    rb.Apply();
                }

                var resourceContent = await Node.LoadAsync<Resource>(contentPath, CancellationToken.None);

                Assert.IsNotNull(resourceContent);

                // check the actual resource (English culture)
                Assert.AreEqual("disp lay name", SR.GetString("TestClass1", "DisplayName"));
                Assert.AreEqual("value2", SR.GetString("TestClass1", "Resource2"));
            });
        }
        [TestMethod]
        public async System.Threading.Tasks.Task ResourceBuilder_MultiContent()
        {
            await Test(async () =>
            {
                EnsureLocalizationParent();

                var contentName1 = "Resource-" + Guid.NewGuid() + ".xml";
                var contentName2 = "Resource-" + Guid.NewGuid() + ".xml";
                var contentPath1 = RepositoryPath.Combine(RepositoryStructure.ResourceFolderPath, contentName1);
                var contentPath2 = RepositoryPath.Combine(RepositoryStructure.ResourceFolderPath, contentName2);
                var rb = new ResourceBuilder();

                rb.Content(contentName1)
                    .Class("TestClassAbc")
                    .CultureEn()
                    .AddResource("DisplayName", "disp lay name");

                rb.Content(contentName2)
                    .Class("TestClassDef")
                    .CultureEn()
                    .AddResource("MyResource123", "mystring");

                using (new SystemAccount())
                {
                    rb.Apply();
                }

                var resourceContent1 = await Node.LoadAsync<Resource>(contentPath1, CancellationToken.None);
                var resourceContent2 = await Node.LoadAsync<Resource>(contentPath2, CancellationToken.None);

                Assert.IsNotNull(resourceContent1);
                Assert.IsNotNull(resourceContent2);

                // check the actual resource (English culture)
                Assert.AreEqual("disp lay name", SR.GetString("TestClassAbc", "DisplayName"));
                Assert.AreEqual("mystring", SR.GetString("TestClassDef", "MyResource123"));
            });
        }

        [TestMethod]
        public void ResourceBuilder_MultiClass()
        {
            Test(() =>
            {
                EnsureLocalizationParent();

                var contentName = "Resource-" + Guid.NewGuid() + ".xml";
                var rb = new ResourceBuilder();

                rb.Content(contentName)
                    .Class("TestClass1")
                    .CultureEn()
                    .AddResource("DisplayName", "disp lay name");
                rb.Content(contentName)
                    .Class("TestClass2")
                    .CultureEn()
                    .AddResource("DisplayName", "Different value");

                using (new SystemAccount())
                {
                    rb.Apply();
                }
                
                // check the actual resource (English culture)
                Assert.AreEqual("disp lay name", SR.GetString("TestClass1", "DisplayName"));
                Assert.AreEqual("Different value", SR.GetString("TestClass2", "DisplayName"));
            });
        }
        [TestMethod]
        public void ResourceBuilder_MultiLanguage()
        {
            Test(() =>
            {
                EnsureLocalizationParent();

                var contentName = "Resource-" + Guid.NewGuid() + ".xml";
                var rb = new ResourceBuilder();

                rb.Content(contentName)
                    .Class("TestClass1")
                    .CultureEn()
                    .AddResource("DisplayName", "disp lay name")
                    .CultureHu()
                    .AddResource("DisplayName", "ugyanez magyarul");

                using (new SystemAccount())
                {
                    rb.Apply();
                }

                // check the actual resource in English
                Assert.AreEqual("disp lay name", SR.GetString("TestClass1", "DisplayName"));

                // check the same resource in Hungarian
                var hunValue = SenseNetResourceManager.Current.GetString("TestClass1", "DisplayName", 
                    CultureInfo.GetCultureInfo("hu"));

                Assert.AreEqual("ugyanez magyarul", hunValue);
            });
        }
        [TestMethod]
        public void ResourceBuilder_Overwrite()
        {
            Test(() =>
            {
                EnsureLocalizationParent();

                var contentName = "Resource-" + Guid.NewGuid() + ".xml";
                var rb = new ResourceBuilder();

                rb.Content(contentName)
                    .Class("TestClass1")
                    .CultureEn()
                    .AddResource("DisplayName", "value1")
                    .AddResource("DisplayName", "value2");
                
                using (new SystemAccount()) { rb.Apply(); }

                // check the actual resource (English culture)
                Assert.AreEqual("value2", SR.GetString("TestClass1", "DisplayName"));

                // continue editing using the same builder
                rb.Content(contentName)
                    .Class("TestClass1")
                    .CultureEn()
                    .AddResource("DisplayName", "value3");

                using (new SystemAccount()) { rb.Apply(); }

                Assert.AreEqual("value3", SR.GetString("TestClass1", "DisplayName"));

                // continue editing using a new builder
                rb = new ResourceBuilder();
                rb.Content(contentName)
                    .Class("TestClass1")
                    .CultureEn()
                    .AddResource("DisplayName", "value4");

                using (new SystemAccount()) { rb.Apply(); }

                Assert.AreEqual("value4", SR.GetString("TestClass1", "DisplayName"));
            });
        }

        private static void EnsureLocalizationParent()
        {
            using (new SystemAccount())
            {
                if (Node.Exists(RepositoryStructure.ResourceFolderPath)) 
                    return;

                var localizationFolder = new SystemFolder(Repository.Root) { Name = "Localization" };
                localizationFolder.Save();
            }
        }
    }
}
