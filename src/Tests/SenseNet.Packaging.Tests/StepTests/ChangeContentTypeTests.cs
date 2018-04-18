using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Packaging.Steps;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Tests;

namespace SenseNet.Packaging.Tests.StepTests
{
    [TestClass]
    public class ChangeContentTypeTests : TestBase
    {
        private class ChangeContentTypeAccessor
        {
            private readonly PrivateObject _original;
            public ChangeContentTypeAccessor(ChangeContentType original)
            {
                _original = new PrivateObject(original);
            }
            public void CopyFields(Content source, Content target, Dictionary<string, Dictionary<string, string>> fieldMapping)
            {
                _original.Invoke(nameof(CopyFields), source, target, fieldMapping);
            }
            public string TranslateFieldName(string sourceContentTypeName, string fieldName, string[] availableTargetNames, Dictionary<string, Dictionary<string, string>> mapping)
            {
                return (string)_original.Invoke(nameof(TranslateFieldName), sourceContentTypeName, fieldName, availableTargetNames, mapping);
            }
            public Dictionary<string, Dictionary<string, string>> ParseMapping(IEnumerable<XmlElement> fieldMapping, ContentType targetType)
            {
                return (Dictionary<string, Dictionary<string, string>>)_original.Invoke(nameof(ParseMapping),
                    fieldMapping, targetType);
            }
        }

        private static StringBuilder _log;
        [TestInitialize]
        public void PrepareTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);
        }

        [TestMethod]
        public void Step_ChangeContentType_Parse()
        {
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' targetType='Type2' />");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.TargetType);
            Assert.IsNull(step.FieldMapping);
        }
        [TestMethod]
        public void Step_ChangeContentType_ParseMapping()
        {
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' targetType='Type2'>
                                      <FieldMapping>
                                        <ContentType name='Type1'>
                                          <Field source='Field1' target='Field3' />
                                        </ContentType>
                                        <Field source='Field1' target='Field2' />
                                      </FieldMapping>
                                    </ChangeContentType>");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.TargetType);
            Assert.IsNotNull(step.FieldMapping);
            Assert.AreEqual(2, step.FieldMapping.Count());

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type1' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field1' type='ShortText'/>
    </Fields>
</ContentType>",
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type2' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field2' type='ShortText'/>
        <Field name='Field3' type='ShortText'/>
    </Fields>
</ContentType>");

                var stepAcc = new ChangeContentTypeAccessor(step);

                // test
                var mapping = stepAcc.ParseMapping(step.FieldMapping, ContentType.GetByName("Type2"));

                // check
                Assert.AreEqual("Field3", mapping["Type1"]["Field1"]);
                Assert.AreEqual("Field2", mapping[""]["Field1"]);
            });
        }
        [TestMethod]
        public void Step_ChangeContentType_ParseMapping_InvalidTargetField()
        {
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' targetType='Type2'>
                                      <FieldMapping>
                                        <Field source='Field1' target='Field3' />
                                      </FieldMapping>
                                    </ChangeContentType>");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.TargetType);

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type1' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field1' type='ShortText'/>
    </Fields>
</ContentType>",
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type2' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field2' type='ShortText'/>
    </Fields>
</ContentType>");

                var stepAcc = new ChangeContentTypeAccessor(step);

                try
                {
                    // test
                    stepAcc.ParseMapping(step.FieldMapping, ContentType.GetByName("Type2"));
                    Assert.Fail("The expected InvalidStepParameterException was not thrown.");
                }
                catch (Exception e)
                {
                    e = e.InnerException;
                    Assert.IsNotNull(e);
                    Assert.AreEqual("The Field3 is not a field of the Type2 content type.", e.Message);
                }
            });
        }
        [TestMethod]
        public void Step_ChangeContentType_ParseMapping_InvalidRootElement()
        {
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' targetType='Type2'>
                                      <FieldMapping>
                                        <UnknownElement />
                                      </FieldMapping>
                                    </ChangeContentType>");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.TargetType);

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type1' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field1' type='ShortText'/>
    </Fields>
</ContentType>",
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type2' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field2' type='ShortText'/>
    </Fields>
</ContentType>");

                var stepAcc = new ChangeContentTypeAccessor(step);

                try
                {
                    // test
                    stepAcc.ParseMapping(step.FieldMapping, ContentType.GetByName("Type2"));
                    Assert.Fail("The expected InvalidStepParameterException was not thrown.");
                }
                catch (Exception e)
                {
                    e = e.InnerException;
                    Assert.IsNotNull(e);
                    Assert.IsTrue(e.Message.StartsWith("Unknown element in the FieldMapping: UnknownElement."));
                }
            });
        }
        [TestMethod]
        public void Step_ChangeContentType_ParseMapping_InvalidChildElement()
        {
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' targetType='Type2'>
                                      <FieldMapping>
                                        <ContentType name='Type1'>
                                          <ContentType name='Type2' />
                                        </ContentType>
                                      </FieldMapping>
                                    </ChangeContentType>");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.TargetType);

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type1' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field1' type='ShortText'/>
    </Fields>
</ContentType>",
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type2' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field2' type='ShortText'/>
    </Fields>
</ContentType>");

                var stepAcc = new ChangeContentTypeAccessor(step);

                try
                {
                    // test
                    stepAcc.ParseMapping(step.FieldMapping, ContentType.GetByName("Type2"));
                    Assert.Fail("The expected InvalidStepParameterException was not thrown.");
                }
                catch (Exception e)
                {
                    e = e.InnerException;
                    Assert.IsNotNull(e);
                    Assert.IsTrue(e.Message.StartsWith("Invalid child element in the FieldMapping/ContentType."));
                }
            });
        }

        [TestMethod]
        public void Step_ChangeContentType_TranslateFieldName()
        {
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' targetType='Type2' />");
            var stepAcc = new ChangeContentTypeAccessor(step);

            var availableTargetNames = new[] {"Field1", "Field4"};
            var mapping = new Dictionary<string, Dictionary<string, string>>
            {
                {"", new Dictionary<string, string> {{"Field2", "Field4"}}},
                {"Type1", new Dictionary<string, string> {{"Field3", "Field4"}}},
            };

            // implicit tests

            // non-existent type in the mapping
            Assert.AreEqual("Field1", stepAcc.TranslateFieldName("Type0", "Field1", availableTargetNames, mapping));
            Assert.AreEqual("Field4", stepAcc.TranslateFieldName("Type0", "Field2", availableTargetNames, mapping));
            Assert.AreEqual(null, stepAcc.TranslateFieldName("Type0", "Field3", availableTargetNames, mapping));
            Assert.AreEqual("Field4", stepAcc.TranslateFieldName("Type0", "Field4", availableTargetNames, mapping));

            // existing type in the mapping
            Assert.AreEqual("Field1", stepAcc.TranslateFieldName("Type1", "Field1", availableTargetNames, mapping));
            Assert.AreEqual(null, stepAcc.TranslateFieldName("Type1", "Field2", availableTargetNames, mapping));
            Assert.AreEqual("Field4", stepAcc.TranslateFieldName("Type1", "Field3", availableTargetNames, mapping));
            Assert.AreEqual("Field4", stepAcc.TranslateFieldName("Type1", "Field4", availableTargetNames, mapping));
        }

        [TestMethod]
        public void Step_ChangeContentType_CopyFields()
        {
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:(Type1 Type2) .AUTOFILTERS:OFF' targetType='Type2'>
                                      <FieldMapping>
                                        <ContentType name='Type2'>
                                          <Field source='Field2' target='Field3' />
                                        </ContentType>
                                        <Field source='Field1' target='Field3' />
                                      </FieldMapping>
                                    </ChangeContentType>");

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type1' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field1' type='ShortText'/>
        <Field name='Field4' type='ShortText'/>
   </Fields>
</ContentType>",
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type2' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field2' type='ShortText'/>
        <Field name='Field4' type='ShortText'/>
   </Fields>
</ContentType>",
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type3' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field3' type='ShortText'/>
        <Field name='Field4' type='ShortText'/>
    </Fields>
</ContentType>");

                var testRoot = CreateTestRoot();

                var content1 = Content.CreateNew("Type1", testRoot, "Content-1");
                content1["Field1"] = "Value-1";
                content1["Field4"] = "Value-4";
                content1.Save();

                var content2 = Content.CreateNew("Type2", testRoot, "Content-2");
                content2["Field2"] = "Value-2";
                content2["Field4"] = "Value-4";
                content2.Save();

                // do not save: filled by the test below
                var contentFrom1 = Content.CreateNew("Type3", testRoot, "Content-3");
                var contentFrom2 = Content.CreateNew("Type3", testRoot, "Content-4");

                var stepAcc = new ChangeContentTypeAccessor(step);
                var mapping = stepAcc.ParseMapping(step.FieldMapping, ContentType.GetByName("Type3"));

                // test
                stepAcc.CopyFields(content1, contentFrom1, mapping);
                stepAcc.CopyFields(content2, contentFrom2, mapping);

                // check
                Assert.AreEqual("Value-1", contentFrom1["Field3"]);
                Assert.AreEqual("Value-4", contentFrom1["Field4"]);
                Assert.AreEqual("Value-2", contentFrom2["Field3"]);
                Assert.AreEqual("Value-4", contentFrom2["Field4"]);
            });
        }

        [TestMethod]
        public void Step_ChangeContentType_Execute()
        {
            var step = CreateStep(@"<ChangeContentType sourceType='Type1 Type2' targetType='Type3' change='force'>
                                      <FieldMapping>
                                        <ContentType name='Type2'>
                                          <Field source='Field2' target='Field3' />
                                        </ContentType>
                                        <Field source='Field1' target='Field3' />
                                      </FieldMapping>
                                    </ChangeContentType>");

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type1' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field1' type='ShortText'/>
        <Field name='Field4' type='ShortText'/>
   </Fields>
</ContentType>",
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type2' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field2' type='ShortText'/>
        <Field name='Field4' type='ShortText'/>
   </Fields>
</ContentType>",
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type3' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field3' type='ShortText'/>
        <Field name='Field4' type='ShortText'/>
    </Fields>
</ContentType>");

                var root = Repository.Root;
                root.AllowChildTypes(root.GetAllowedChildTypes()
                    .Union(new[] {ContentType.GetByName("Type1"), ContentType.GetByName("Type2")}));
                root.Save();

                var testRoot = new Folder(Repository.Root) { Name = "TestRoot" };
                testRoot.Save();

                var content1 = Content.CreateNew("Type1", testRoot, "Content-1");
                content1["Field1"] = "Value-1";
                content1["Field4"] = "Value-4";
                content1.Save();

                var content2 = Content.CreateNew("Type2", testRoot, "Content-2");
                content2["Field2"] = "Value-2";
                content2["Field4"] = "Value-4";
                content2.Save();

                // test
                step.Execute(GetExecutionContext());

                // check
                content1 = Content.Load(content1.Path);
                Assert.AreEqual("Type3", content1.ContentType.Name);
                Assert.AreEqual("Value-1", content1["Field3"]);
                Assert.AreEqual("Value-4", content1["Field4"]);
                content2 = Content.Load(content2.Path);
                Assert.AreEqual("Type3", content2.ContentType.Name);
                Assert.AreEqual("Value-2", content2["Field3"]);
                Assert.AreEqual("Value-4", content2["Field4"]);
            });
        }

        /* =========================================================================== Tools */

        private Node CreateTestRoot()
        {
            var node = new SystemFolder(Repository.Root) { Name = "TestRoot" };
            node.Save();
            return node;
        }
        private ChangeContentType CreateStep(string stepElementString)
        {
            var manifestXml = new XmlDocument();
            manifestXml.LoadXml($@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>MyCompany.MyComponent</Id>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            {stepElementString}
                        </Steps>
                    </Package>");
            var manifest = Manifest.Parse(manifestXml, 0, true, new PackageParameter[0]);
            var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, 0, manifest.CountOfPhases, null, null);
            var stepElement = (XmlElement)manifestXml.SelectSingleNode("/Package/Steps/ChangeContentType");
            var result = (ChangeContentType)Step.Parse(stepElement, 0, executionContext);
            return result;
        }
        private ExecutionContext GetExecutionContext()
        {
            var manifestXml = new XmlDocument();
            manifestXml.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>MyCompany.MyComponent</Id>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");

            var phase = 0;
            var console = new StringWriter();
            var manifest = Manifest.Parse(manifestXml, phase, true, new PackageParameter[0]);
            var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, null, console);
            executionContext.RepositoryStarted = true;
            return executionContext;
        }

    }
}
