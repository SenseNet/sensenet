using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class AutoNamingTests : TestBase
    {
        private static string ContentType_Car1Name = "Car1_AutoNamingTests";
        private static string ContentType_Car2Name = "Car2_AutoNamingTests";
        private static ContentType ContentType_Car1;
        private static ContentType ContentType_Car2;
        public static void InstallContentTypes()
        {
            var ctdformat = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <ContentType name=""{0}"" parentType=""Car"" handler=""SenseNet.ContentRepository.GenericContent""
                             xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                  <AllowIncrementalNaming>{1}</AllowIncrementalNaming>
                </ContentType>";
            var ctd1 = String.Format(ctdformat, ContentType_Car1Name, "true");
            var ctd2 = String.Format(ctdformat, ContentType_Car2Name, "false");
            ContentTypeInstaller.InstallContentType(CarContentType, ctd1, ctd2);
            ContentType_Car1 = ContentType.GetByName(ContentType_Car1Name);
            ContentType_Car2 = ContentType.GetByName(ContentType_Car2Name);
        }

        [TestMethod]
        public void ContentNaming_FromDisplayName()
        {
            var providers = new[] {
                (ContentNamingProvider)new CharReplacementContentNamingProvider(),
                (ContentNamingProvider)new Underscore5FContentNamingProvider()
            };

            var contentNamingProviderAcc = new PrivateType(typeof(ContentNamingProvider));
            var originalProvider = contentNamingProviderAcc.GetStaticField("__instance");
            try
            {
                foreach (var provider in providers)
                {
                    contentNamingProviderAcc.SetStaticField("__instance", provider);
                    var providerName = provider.GetType().Name;

                    Assert.AreEqual("árvíztűrőtükörfúrógép", ContentNamingProvider.GetNameFromDisplayName("árvíztűrőtükörfúrógép"));
                    Assert.AreEqual("ÁRVÍZTŰRŐTÜKÖRFÚRÓGÉP", ContentNamingProvider.GetNameFromDisplayName("ÁRVÍZTŰRŐTÜKÖRFÚRÓGÉP"));
                    Assert.AreEqual("ÁrvíztűrőTükörfúrógép", ContentNamingProvider.GetNameFromDisplayName("ÁrvíztűrőTükörfúrógép"));
                    Assert.AreEqual("árvíztűrőtükörfúrógép.txt", ContentNamingProvider.GetNameFromDisplayName("árvíztűrőtükörfúrógép.txt"));
                    Assert.AreEqual("árvíztűrőtükörfúrógép.doc.txt", ContentNamingProvider.GetNameFromDisplayName("árvíztűrőtükörfúrógép.txt", "árvíztűrőtükörfúrógép.doc"));
                    Assert.AreEqual("árvíztűrőtükörfúrógép.doc.txt", ContentNamingProvider.GetNameFromDisplayName(".txt", "árvíztűrőtükörfúrógép.doc"));

                    var name =  ContentNamingProvider.GetNameFromDisplayName("!*_~@#$%a^&()b{}+\"'|:<>?c/.,");
                    if (providerName == typeof(CharReplacementContentNamingProvider).Name)
                    {
                        Assert.AreEqual("!-_-a-()b-c", name);
                    }
                    else if (providerName == typeof(Underscore5FContentNamingProvider).Name)
                    {
                        Assert.AreEqual("!_2a_5f_7e_40_23_24_25a_5e_26()b_7b_7d_2b_22_27_7c_3a_3c_3e_3fc_2f._2c", name);
                    }
                    else
                    {
                        Assert.Inconclusive("Unknown ContentNamingProvider: " + providerName);
                    }
                }
            }
            finally
            {
                contentNamingProviderAcc.SetStaticField("__instance", originalProvider);
            }
        }
        [TestMethod]
        public void ContentNaming_AllowIncrementalNaming_Allowed()
        {
            Test(() =>
            {
                InstallContentTypes();
                var testRoot = new SystemFolder(Repository.Root);
                testRoot.Save();

                Content content1, content2;
                do
                {
                    content1 = Content.CreateNew(ContentType_Car1Name, testRoot, null);
                    content2 = Content.CreateNew(ContentType_Car1Name, testRoot, null);
                } while (content1.Name != content2.Name);
                content1.Save();
                content2.Save();
            });
        }
        [TestMethod]
        public void ContentNaming_AllowIncrementalNaming_Disallowed()
        {
            Test(() =>
            {
                var thrown = false;
                try
                {
                    InstallContentTypes();
                    var testRoot = new SystemFolder(Repository.Root);
                    testRoot.Save();

                    Content content1, content2;
                    do
                    {
                        content1 = Content.CreateNew(ContentType_Car2Name, testRoot, null);
                        content2 = Content.CreateNew(ContentType_Car2Name, testRoot, null);
                    } while (content1.Name != content2.Name);
                    content1.Save();
                    content2.Save();
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
        [TestMethod]
        public void ContentNaming_IncrementNameSuffixOnSave()
        {
            Test(() =>
            {
                InstallContentTypes();
                var testRoot = new SystemFolder(Repository.Root);
                testRoot.Save();

                var root = new SystemFolder(testRoot) { Name = Guid.NewGuid().ToString() };
                root.Save();

                // increment check
                var content1 = Content.CreateNew(ContentType_Car1Name, root, "mycar");
                content1.Save();
                var content2 = Content.CreateNew(ContentType_Car1Name, root, "mycar");
                content2.Save();
                var content3 = Content.CreateNew(ContentType_Car1Name, root, "mycar");
                content3.Save();
                Assert.AreEqual("mycar", content1.Name);    // if mycar does not exist, name does not change
                Assert.AreEqual("mycar(1)", content2.Name); // first increment
                Assert.AreEqual("mycar(2)", content3.Name); // second increment

                // 9 - 10 order problem: if mycar(9) and mycar(10) exists, mycar(11) is the next even though 10 is smaller than 9 if compared as strings
                Content.CreateNew(ContentType_Car1Name, root, "mycar(9)").Save();
                Content.CreateNew(ContentType_Car1Name, root, "mycar(10)").Save();
                var content4 = Content.CreateNew(ContentType_Car1Name, root, "mycar");
                content4.Save();
                Assert.AreEqual("mycar(11)", content4.Name);

                // (string) suffix problem 1: string(test) should be incremented to string(test)(1)
                var content5 = Content.CreateNew(ContentType_Car1Name, root, "string(test)");
                content5.Save();
                var content6 = Content.CreateNew(ContentType_Car1Name, root, "string(test)");
                content6.Save();
                Assert.AreEqual("string(test)", content5.Name);    // if string(test) does not exist, name does not change
                Assert.AreEqual("string(test)(1)", content6.Name); // first increment

                // (string) suffix problem 2: string should be incremented to string(guid), since string(test) already exists
                var content7 = Content.CreateNew(ContentType_Car1Name, root, "string");
                content7.Save();
                var content8 = Content.CreateNew(ContentType_Car1Name, root, "string");
                content8.Save();
                Assert.AreEqual("string", content7.Name);       // did not exist yet
                Assert.AreEqual("string(1)", content8.Name);       // did not exist yet
            });
        }
        [TestMethod]
        public void ContentNaming_IncrementNameSuffixOnSaveWithExtension()
        {
            Test(() =>
            {
                InstallContentTypes();
                var testRoot = new SystemFolder(Repository.Root);
                testRoot.Save();

                var root = new SystemFolder(testRoot) { Name = Guid.NewGuid().ToString() };
                root.Save();

                // increment check
                var content1 = Content.CreateNew(ContentType_Car1Name, root, "mycar.xml");
                content1.Save();
                var content2 = Content.CreateNew(ContentType_Car1Name, root, "mycar.xml");
                content2.Save();
                var content3 = Content.CreateNew(ContentType_Car1Name, root, "mycar.xml");
                content3.Save();
                Assert.AreEqual("mycar.xml", content1.Name);    // if mycar does not exist, name does not change
                Assert.AreEqual("mycar(1).xml", content2.Name); // first increment
                Assert.AreEqual("mycar(2).xml", content3.Name); // second increment

                // 9 - 10 order problem: if mycar(9) and mycar(10) exists, mycar(11) is the next even though 10 is smaller than 9 if compared as strings
                Content.CreateNew(ContentType_Car1Name, root, "mycar(9).xml").Save();
                Content.CreateNew(ContentType_Car1Name, root, "mycar(10).xml").Save();
                var content4 = Content.CreateNew(ContentType_Car1Name, root, "mycar.xml");
                content4.Save();
                Assert.IsTrue(content4.Name == "mycar(11).xml");

                // (string) suffix problem 1: string(test) should be incremented to string(test)(1)
                var content5 = Content.CreateNew(ContentType_Car1Name, root, "string(test).xml");
                content5.Save();
                var content6 = Content.CreateNew(ContentType_Car1Name, root, "string(test).xml");
                content6.Save();
                Assert.AreEqual("string(test).xml", content5.Name);    // if string(test) does not exist, name does not change
                Assert.AreEqual("string(test)(1).xml", content6.Name); // first increment

                // (string) suffix problem 2: string should be incremented to string(guid), since string(test) already exists
                var content7 = Content.CreateNew(ContentType_Car1Name, root, "string.xml");
                content7.Save();
                var content8 = Content.CreateNew(ContentType_Car1Name, root, "string.xml");
                content8.Save();
                Assert.AreEqual("string.xml", content7.Name);
                Assert.AreEqual("string(1).xml", content8.Name);
            });
        }
        [TestMethod]
        public void ContentNaming_AutoIncrementOnDemand()
        {
            Test(() =>
            {
                InstallContentTypes();
                var testRoot = new SystemFolder(Repository.Root);
                testRoot.Save();

                var root = new SystemFolder(testRoot) { Name = Guid.NewGuid().ToString() };
                root.Save();

                var content1 = Content.CreateNew(ContentType_Car2Name, root, "ondemand");
                content1.Save();

                // take a non-autoincrement type, and save it with autoincrement
                bool typeNotAutoIncrement = false;
                var content2 = Content.CreateNew(ContentType_Car2Name, root, "ondemand");
                try
                {
                    content2.Save();
                }
                catch (NodeAlreadyExistsException)
                {
                    typeNotAutoIncrement = true;
                }
                catch(AggregateException ae)
                {
                    typeNotAutoIncrement = ae.InnerException is NodeAlreadyExistsException;
                }
                Assert.IsTrue(typeNotAutoIncrement);    // the type is non-autoincremental

                content2.ContentHandler.AllowIncrementalNaming = true;
                content2.Save();
                Assert.AreEqual("ondemand(1)", content2.Name);  // non-autoincremental type is saved autoincrementally
            });
        }
        [TestMethod]
        public void ContentNaming_NameOnlyChangesAfterSave()
        {
            Test(() =>
            {
                InstallContentTypes();
                var testRoot = new SystemFolder(Repository.Root);
                testRoot.Save();

                var content1 = Content.CreateNew(ContentType_Car1Name, testRoot, "changeonsave");
                content1.Save();

                var content2 = Content.CreateNew(ContentType_Car1Name, testRoot, "changeonsave");
                Assert.IsTrue(content2.Name == "changeonsave");     // name is not changed before saving, no queries are run (queries would be slow)
                content2.Save();
                Assert.IsTrue(content2.Name == "changeonsave(1)");  // name is changed after saving - we check here that name should actually be changed, so previous assert is correct
            });
        }
        [TestMethod]
        public void ContentNaming_IncrementNameSuffix()
        {
            string msg;

            msg = IncrementNameSuffixTest("Car", "Car(1)"); Assert.IsNull(msg, msg);
            msg = IncrementNameSuffixTest("Car(12)", "Car(13)"); Assert.IsNull(msg, msg);
            msg = IncrementNameSuffixTest("Car.xml", "Car(1).xml"); Assert.IsNull(msg, msg);
            msg = IncrementNameSuffixTest("Car(8).xml", "Car(9).xml"); Assert.IsNull(msg, msg);
            msg = IncrementNameSuffixTest("Car((8).xml", "Car((9).xml"); Assert.IsNull(msg, msg);
            msg = IncrementNameSuffixTest("(1)", "(2)"); Assert.IsNull(msg, msg); Assert.IsNull(msg, msg);
            msg = IncrementNameSuffixTest(")", ")(1)"); Assert.IsNull(msg, msg); Assert.IsNull(msg, msg);

            msg = IncrementNameSuffixTest("Car(string)(12)", "Car(string)(13)"); Assert.IsNull(msg, msg);

            // suffices are guids for the following. 
            // if the last suffixed name from db is in the form 'name(x)' where x is not a number, we are not able to decide the next suffix, so it is guid
            var actual = ContentNamingProvider.IncrementNameSuffix("Car(string)");
            Assert.AreEqual("Car(string)(1)", actual);
            actual = ContentNamingProvider.IncrementNameSuffix("Car(8)).xml");
            Assert.AreEqual("Car(8))(1).xml", actual);
            actual = ContentNamingProvider.IncrementNameSuffix("Car()");
            Assert.AreEqual("Car()(1)", actual);
        }
        [TestMethod]
        public void ContentNaming_ParseSuffix()
        {
            string msg;
            msg = ParseSuffixTest("Car(1)", 1); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car(1).xml", 1); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car(test)(1)", 1); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car(test)(1).xml", 1); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car", 0); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car(test)", 0); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car(test)(1", 0); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car.xml", 0); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car(test).xml", 0); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car(test)(1.xml", 0); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car((8).xml", 8); Assert.IsNull(msg, msg);
            msg = ParseSuffixTest("Car(8)).xml", 0); Assert.IsNull(msg, msg);
        }
        private string IncrementNameSuffixTest(string name, string expected)
        {
            var actual = ContentNamingProvider.IncrementNameSuffix(name);
            if(actual == expected)
                return null;
            return String.Format("Name is {0}, expected: {1}", actual, expected);
        }
        private string ParseSuffixTest(string name, int expected)
        {
            name = System.IO.Path.GetFileNameWithoutExtension(name);
            string nameBase;
            var actual = ContentNamingProvider.ParseSuffix(name, out nameBase);
            if (actual == expected)
                return null;
            return String.Format("Result is {0}, expected: {1}", actual, expected);
        }
        //private bool SuffixIsGuid(string name)
        //{
        //    name = Path.GetFileNameWithoutExtension(name);
        //    Guid guid;
        //    var guidstr = name.Substring(name.Length - 36 - 1, 36);
        //    return Guid.TryParse(guidstr, out guid);
        //}
    }
}
