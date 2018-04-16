using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
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
            public void CopyFields(Content source, Content target)
            {
                _original.Invoke(nameof(CopyFields), source, target);
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
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' contentTypeName='Type2' />");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.ContentTypeName);
            Assert.IsNull(step.FieldMapping);
        }
        [TestMethod]
        public void Step_ChangeContentType_ParseMapping()
        {
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' contentTypeName='Type2'>
                                      <FieldMapping>
                                        <ContentType name='Type1'>
                                          <Field source='Field1' target='Field3' />
                                        </ContentType>
                                        <Field source='Field1' target='Field2' />
                                      </FieldMapping>
                                    </ChangeContentType>");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.ContentTypeName);
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
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' contentTypeName='Type2'>
                                      <FieldMapping>
                                        <Field source='Field1' target='Field3' />
                                      </FieldMapping>
                                    </ChangeContentType>");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.ContentTypeName);

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
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' contentTypeName='Type2'>
                                      <FieldMapping>
                                        <UnknownElement />
                                      </FieldMapping>
                                    </ChangeContentType>");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.ContentTypeName);

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
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' contentTypeName='Type2'>
                                      <FieldMapping>
                                        <ContentType name='Type1'>
                                          <ContentType name='Type2' />
                                        </ContentType>
                                      </FieldMapping>
                                    </ChangeContentType>");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.ContentTypeName);

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
            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' contentTypeName='Type2' />");
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
            Assert.Inconclusive("The test is not finished but the code need to be committed.");

            var step = CreateStep(@"<ChangeContentType contentQuery='TypeIs:Type1 .AUTOFILTERS:OFF' contentTypeName='Type2'>
                                      <FieldMapping>
                                        <ContentType name='Type2'>
                                          <Field source='Field2' target='Field3' />
                                        </ContentType>
                                        <Field source='Field1' target='Field3' />
                                      </FieldMapping>
                                    </ChangeContentType>");
            Assert.AreEqual("TypeIs:Type1 .AUTOFILTERS:OFF", step.ContentQuery);
            Assert.AreEqual("Type2", step.ContentTypeName);
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
    </Fields>
</ContentType>",
@"<?xml version='1.0' encoding='utf-8'?><ContentType name='Type3' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='Field1' type='ShortText'/>
        <Field name='Field4' type='ShortText'/>
    </Fields>
</ContentType>");

                var stepAcc = new ChangeContentTypeAccessor(step);

                // test
                var mapping = stepAcc.ParseMapping(step.FieldMapping, ContentType.GetByName("Type2"));
// not finished but the code need to be committed.
Assert.Inconclusive();
            });
        }

        /* =========================================================================== Tools */

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

    }
}
