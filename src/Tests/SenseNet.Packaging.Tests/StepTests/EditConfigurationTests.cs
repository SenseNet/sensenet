using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Packaging.Steps;
using SenseNet.Packaging.Steps;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Tests;

namespace SenseNet.Packaging.Tests.StepTests
{
    [TestClass]
    public class EditConfigurationTests : TestBase
    {
        // <EditConfiguration>
        //   <Move>
        //     <Element sourceSetion="section1"
        //              targetSection="sensenet/section2" />
        //     <Element sourceSetion="appSettings"
        //              sourceKey="asdf"
        //              targetSection="sensenet/section3"
        //              deleteIfValueIs="42" />
        //     <Element sourceSetion="appSettings"
        //              sourceKey="LuceneActivityTimeoutInSeconds"
        //              targetSection="sensenet/indexing"
        //              targetKey="IndexingActivityTimeoutInSeconds" />
        //     ...
        //   </Move>
        //   <Delete>
        //     <Element section="appSettings"
        //              key="RestoreIndex" />
        //     ...
        //   </Delete>
        // </EditConfiguration>

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

        /* ============================================================================================== */

        [TestMethod]
        public void Step_EditConfiguration_Parse_MissingFile()
        {
            try
            {
                var step = CreateStep(@"<EditConfiguration />");
                var file = step.File;
                Assert.Fail("InvalidStepParameterException exception was thrown.");
            }
            catch (InvalidStepParameterException)
            {
                // do nothing
            }
        }
        [TestMethod]
        public void Step_EditConfiguration_Parse_OperationNull()
        {
            var step = CreateStep(@"<EditConfiguration file='./web.config' />");
            Assert.AreEqual("./web.config", step.File);
            Assert.IsNull(step.Move);
            Assert.IsNull(step.Delete);
        }
        [TestMethod]
        public void Step_EditConfiguration_Parse_OperationEmpty()
        {
            var step = CreateStep(@"<EditConfiguration file='./web.config'><Move/><Delete/></EditConfiguration>");
            Assert.AreEqual(0, step.Move.Count());
            Assert.AreEqual(0, step.Delete.Count());
        }

        [TestMethod]
        public void Step_EditConfiguration_Parse_Delete_MissingSection()
        {
            var step = CreateStep(@"<EditConfiguration file='./web.config'>
    <Delete>
        <Element/>
    </Delete>
</EditConfiguration>");

            string message = null;
            Assert.AreEqual(1, step.Delete.Count());
            try
            {
                var acc = new PrivateObject(step);
                var deletes = (EditConfiguration.DeleteOperation[])acc.Invoke("ParseDeleteElements");
            }
            catch (Exception e)
            {
                message = (e.InnerException?.Message ?? e.Message).ToLowerInvariant();
            }
            Assert.IsTrue(message.Contains("invalid delete"));
            Assert.IsTrue(message.Contains("missing 'section'"));
        }
        [TestMethod]
        public void Step_EditConfiguration_Parse_Delete()
        {
            var step = CreateStep(@"<EditConfiguration file='./web.config'>
    <Delete>
        <Element section='section1' key='key1'/>
        <Element section='section2'/>
    </Delete>
</EditConfiguration>");

            Assert.AreEqual(2, step.Delete.Count());
            var acc = new PrivateObject(step);
            var deletes = (EditConfiguration.DeleteOperation[]) acc.Invoke("ParseDeleteElements");
            Assert.AreEqual(2, deletes.Length);
            Assert.AreEqual("section1", deletes[0].Section);
            Assert.AreEqual("key1", deletes[0].Key);
            Assert.AreEqual("section2", deletes[1].Section);
            Assert.AreEqual(null, deletes[1].Key);
        }

        [TestMethod]
        public void Step_EditConfiguration_Parse_Move_MissingSource()
        {
            var step = CreateStep(@"<EditConfiguration file='./web.config'>
    <Move>
        <Element targetSection='section1'/>
    </Move>
</EditConfiguration>");

            string message = null;
            Assert.AreEqual(1, step.Move.Count());
            try
            {
                var acc = new PrivateObject(step);
                var moves = (EditConfiguration.DeleteOperation[])acc.Invoke("ParseMoveElements");
            }
            catch (Exception e)
            {
                message = (e.InnerException?.Message ?? e.Message).ToLowerInvariant();
            }
            Assert.IsTrue(message.Contains("invalid move"));
            Assert.IsTrue(message.Contains("missing 'sourcesection'"));
        }
        [TestMethod]
        public void Step_EditConfiguration_Parse_Move_MissingTarget()
        {
            var step = CreateStep(@"<EditConfiguration file='./web.config'>
    <Move>
        <Element sourceSection='section1'/>
    </Move>
</EditConfiguration>");

            string message = null;
            Assert.AreEqual(1, step.Move.Count());
            try
            {
                var acc = new PrivateObject(step);
                var moves = (EditConfiguration.DeleteOperation[])acc.Invoke("ParseMoveElements");
            }
            catch (Exception e)
            {
                message = (e.InnerException?.Message ?? e.Message).ToLowerInvariant();
            }
            Assert.IsTrue(message.Contains("invalid move"));
            Assert.IsTrue(message.Contains("missing 'targetsection'"));
        }
        [TestMethod]
        public void Step_EditConfiguration_Parse_Move_MissingSourceKeyIfTargetGiven()
        {
            var step = CreateStep(@"<EditConfiguration file='./web.config'>
    <Move>
        <Element sourceSection='section1' targetSection='section2' targetKey='key2'/>
    </Move>
</EditConfiguration>");

            string message = null;
            Assert.AreEqual(1, step.Move.Count());
            try
            {
                var acc = new PrivateObject(step);
                var moves = (EditConfiguration.DeleteOperation[])acc.Invoke("ParseMoveElements");
            }
            catch (Exception e)
            {
                message = (e.InnerException?.Message ?? e.Message).ToLowerInvariant();
            }
            Assert.IsTrue(message.Contains("invalid move"));
            Assert.IsTrue(message.Contains("'sourcekey' is required"));
        }
        [TestMethod]
        public void Step_EditConfiguration_Parse_Move_MissingSourceKeyIfDeleteValueGiven()
        {
            var step = CreateStep(@"<EditConfiguration file='./web.config'>
    <Move>
        <Element sourceSection='section1' targetSection='section2' deleteIfValueIs='42'/>
    </Move>
</EditConfiguration>");

            string message = null;
            Assert.AreEqual(1, step.Move.Count());
            try
            {
                var acc = new PrivateObject(step);
                var moves = (EditConfiguration.DeleteOperation[])acc.Invoke("ParseMoveElements");
            }
            catch (Exception e)
            {
                message = (e.InnerException?.Message ?? e.Message).ToLowerInvariant();
            }
            Assert.IsTrue(message.Contains("invalid move"));
            Assert.IsTrue(message.Contains("'sourcekey' is required"));
        }
        [TestMethod]
        public void Step_EditConfiguration_Parse_Move()
        {
            void CheckMoveOperation(EditConfiguration.MoveOperation op,
                string sourceSection, string targetSection, string sourceKey, string targetKey, string deleteIfValueIs)
            {
                Assert.AreEqual(sourceSection, op.SourceSection);
                Assert.AreEqual(targetSection, op.TargetSection);
                Assert.AreEqual(sourceKey, op.SourceKey);
                Assert.AreEqual(targetKey, op.TargetKey);
                Assert.AreEqual(deleteIfValueIs, op.DeleteIfValueIs);
            }

            var step = CreateStep(@"<EditConfiguration file='./web.config'>
    <Move>
        <Element sourceSection='section1' targetSection='section2'/>
        <Element sourceSection='section1' targetSection='section2' sourceKey='key1'/>
        <Element sourceSection='section1' targetSection='section2' sourceKey='key1' targetKey='key2'/>
        <Element sourceSection='section1' targetSection='section2' sourceKey='key1' targetKey='key2' deleteIfValueIs='42'/>
    </Move>
</EditConfiguration>");

            Assert.IsNull(step.Delete);
            Assert.AreEqual(4, step.Move.Count());
            var acc = new PrivateObject(step);
            var moves = (EditConfiguration.MoveOperation[]) acc.Invoke("ParseMoveElements");
            Assert.AreEqual(4, moves.Length);
            CheckMoveOperation(moves[0], "section1", "section2", null, null, null);
            CheckMoveOperation(moves[1], "section1", "section2", "key1", null, null);
            CheckMoveOperation(moves[2], "section1", "section2", "key1", "key2", null);
            CheckMoveOperation(moves[3], "section1", "section2", "key1", "key2", "42");
        }

        /* ---------------------------------------------------------------------------------------------- */

        [TestMethod]
        public void Step_EditConfiguration_MoveSimpleKeyToExisting()
        {
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    <sectionGroup name='sectionsA'>
      <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <section1>
    <add key='key1' value='value1' />
  </section1>
  <sectionsA>
    <section1>
      <add key='key2' value='value2' />
    </section1>
  </sectionsA>
  <appSettings>
    <add key='key3' value='value3' />
    <add key='key4' value='value4' />
    <add key='key5' value='value5' />
  </appSettings>
</configuration>";

            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    <sectionGroup name='sectionsA'>
      <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <section1>
    <add key='key1' value='value1' />
    <add key='key3' value='value3' />
  </section1>
  <sectionsA>
    <section1>
      <add key='key2' value='value2' />
      <add key='key4' value='value4' />
    </section1>
  </sectionsA>
  <appSettings>
    <add key='key5' value='value5' />
  </appSettings>
</configuration>";

            MoveOperationTest(config, expected, new[]
            {
                new EditConfiguration.MoveOperation
                {
                    SourceSection = "appSettings",
                    SourceKey = "key3",
                    TargetSection = "section1",
                },
                new EditConfiguration.MoveOperation
                {
                    SourceSection = "appSettings",
                    SourceKey = "key4",
                    TargetSection = "sectionsA/section1",
                },
            });
        }
        [TestMethod]
        public void Step_EditConfiguration_MoveSimpleKeyToExistingAndRename()
        {
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    <sectionGroup name='sectionsA'>
      <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <section1>
    <add key='key1' value='value1' />
  </section1>
  <sectionsA>
    <section1>
      <add key='key2' value='value2' />
    </section1>
  </sectionsA>
  <appSettings>
    <add key='key3' value='value3' />
    <add key='key4' value='value4' />
    <add key='key5' value='value5' />
  </appSettings>
</configuration>";

            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    <sectionGroup name='sectionsA'>
      <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <section1>
    <add key='key1' value='value1' />
    <add key='key3_renamed' value='value3' />
  </section1>
  <sectionsA>
    <section1>
      <add key='key2' value='value2' />
      <add key='key4_renamed' value='value4' />
    </section1>
  </sectionsA>
  <appSettings>
    <add key='key5' value='value5' />
  </appSettings>
</configuration>";

            MoveOperationTest(config, expected, new[]
            {
                new EditConfiguration.MoveOperation
                {
                    SourceSection = "appSettings",
                    SourceKey = "key3",
                    TargetSection = "section1",
                    TargetKey = "key3_renamed"
                },
                new EditConfiguration.MoveOperation
                {
                    SourceSection = "appSettings",
                    SourceKey = "key4",
                    TargetSection = "sectionsA/section1",
                    TargetKey = "key4_renamed"
                },
            });
        }
        [TestMethod]
        public void Step_EditConfiguration_MoveSimpleKeyAndFullCreate()
        {
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <appSettings>
    <add key='key1' value='value1' />
    <add key='key2' value='value2' />
  </appSettings>
</configuration>";

            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    <sectionGroup name='sectionsA'>
      <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <appSettings>
  </appSettings>
  <section1>
    <add key='key1' value='value1' />
  </section1>
  <sectionsA>
    <section1>
      <add key='key2' value='value2' />
    </section1>
  </sectionsA>
</configuration>";

            MoveOperationTest(config, expected, new[]
            {
                new EditConfiguration.MoveOperation
                {
                    SourceSection = "appSettings",
                    SourceKey = "key1",
                    TargetSection = "section1",
                },
                new EditConfiguration.MoveOperation
                {
                    SourceSection = "appSettings",
                    SourceKey = "key2",
                    TargetSection = "sectionsA/section1",
                },
            });
        }
        [TestMethod]
        public void Step_EditConfiguration_MoveSimpleKeyAndPartiallyCreate()
        {
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <sectionGroup name='sectionsA'>
      <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <appSettings>
    <add key='key1' value='value1' />
    <add key='key2' value='value2' />
  </appSettings>
  <sectionsA>
    <add key='key9' value='value9' />
  </sectionsA>
</configuration>";

            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <sectionGroup name='sectionsA'>
      <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
    <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
  </configSections>
  <appSettings>
  </appSettings>
  <sectionsA>
    <add key='key9' value='value9' />
    <section1>
      <add key='key2' value='value2' />
    </section1>
  </sectionsA>
  <section1>
    <add key='key1' value='value1' />
  </section1>
</configuration>";

            MoveOperationTest(config, expected, new[]
            {
                new EditConfiguration.MoveOperation
                {
                    SourceSection = "appSettings",
                    SourceKey = "key1",
                    TargetSection = "section1",
                },
                new EditConfiguration.MoveOperation
                {
                    SourceSection = "appSettings",
                    SourceKey = "key2",
                    TargetSection = "sectionsA/section1",
                },
            });
        }
        [TestMethod]
        public void Step_EditConfiguration_MoveSimpleKeyAndRenameAndPartiallyCreate()
        {
            var config = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <sectionGroup name='sectionsA'>
      <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
  </configSections>
  <appSettings>
    <add key='key1' value='value1' />
    <add key='key2' value='value2' />
  </appSettings>
</configuration>";

            var expected = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <configSections>
    <sectionGroup name='sectionsA'>
      <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
    </sectionGroup>
    <section name='section1' type='System.Configuration.NameValueFileSectionHandler' />
  </configSections>
  <appSettings>
  </appSettings>
  <section1>
    <add key='key1_renamed' value='value1' />
  </section1>
  <sectionsA>
    <section1>
      <add key='key2_renamed' value='value2' />
    </section1>
  </sectionsA>
</configuration>";

            MoveOperationTest(config, expected, new[]
            {
                new EditConfiguration.MoveOperation
                {
                    SourceSection = "appSettings",
                    SourceKey = "key1",
                    TargetSection = "section1",
                    TargetKey = "key1_renamed"
                },
                new EditConfiguration.MoveOperation
                {
                    SourceSection = "appSettings",
                    SourceKey = "key2",
                    TargetSection = "sectionsA/section1",
                    TargetKey = "key2_renamed"
                },
            });
        }
        [TestMethod]
        public void Step_EditConfiguration_Section()
        {
            Assert.Inconclusive();
        }

        /* ============================================================================================== */

        private EditConfiguration CreateStep(string stepElementString)
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
            var stepElement = (XmlElement)manifestXml.SelectSingleNode("/Package/Steps/EditConfiguration");
            var result = (EditConfiguration)Step.Parse(stepElement, 0, executionContext);
            return result;
        }

        private void MoveOperationTest(string config, string expectedConfig, EditConfiguration.MoveOperation[] moves)
        {
            var xml = new XmlDocument();
            xml.LoadXml(config);

            var step = CreateStep("<EditConfiguration file='./web.config' />");
            if (!step.Edit(xml, moves, null, "[path]"))
                Assert.Fail("Not executed.");

            var expected = expectedConfig.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "").Replace("\"", "'");
            var actual = xml.OuterXml.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "").Replace("\"", "'");

            Assert.IsTrue(expected == actual, $"Actual: {xml.OuterXml}");
        }

    }
}
