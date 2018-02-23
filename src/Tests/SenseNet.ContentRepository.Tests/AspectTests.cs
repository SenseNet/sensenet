using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search;
using SenseNet.Services.ContentStore;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Fields;
using SenseNet.Portal.OData;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Xml;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tests;
using SenseNet.Search.Indexing;
using SenseNet.Configuration;
using SenseNet.Portal;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class AspectTests : TestBase
    {
        [TestMethod]
        public void Aspect_HasFieldIfHasAspect()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                Aspect aspect1 = null;
                Aspect aspect2 = null;
                try
                {
                    aspect1 = EnsureAspect("Aspect_HasFieldIfHasAspect_Aspect1");
                    aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field1' type='ShortText' />
  </Fields>
</AspectDefinition>";
                    aspect1.Save();

                    aspect2 = EnsureAspect("Aspect_HasFieldIfHasAspect_Aspect2");
                    aspect2.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field2' type='ShortText' />
  </Fields>
</AspectDefinition>";
                    aspect2.Save();

                    var fieldName1 = String.Concat(aspect1.Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");
                    var fieldName2 = String.Concat(aspect2.Name, Aspect.ASPECTFIELDSEPARATOR, "Field2");

                    var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#1");
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#2");

                    content.AddAspects(aspect1);
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#3");
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#4");

                    content.RemoveAspects(aspect1);
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#5");
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#6");

                    content.AddAspects(aspect1, aspect2);
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#7");
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName2), "#8");

                    content.RemoveAspects(aspect2.Path);
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#9");
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#10");

                    content.AddAspects(aspect2.Id);
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#11");
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName2), "#12");

                    content.RemoveAllAspects();
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#13");
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#14");
                }
                finally
                {
                    aspect1.ForceDelete();
                    aspect2.ForceDelete();
                }
            });
        }
        [TestMethod]
        public void Aspect_PropertySetAndPropertyNotCreated()
        {
            Test(() =>
            {
                var contentListCount = ActiveSchema.ContentListTypes.Count;
                var propertyCount = ActiveSchema.PropertyTypes.Count;

                var aspect = EnsureAspect(Guid.NewGuid().ToString());
                aspect.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field1' type='ShortText' />
    <AspectField name='Field2' type='ShortText' />
  </Fields>
</AspectDefinition>";
                aspect.Save();

                Assert.IsTrue(ActiveSchema.ContentListTypes.Count == contentListCount, "ContentListType is created.");
                Assert.IsTrue(ActiveSchema.PropertyTypes.Count == propertyCount, "PropertyTypes are created.");
            });
        }
        [TestMethod]
        public void Aspect_Searchable()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                Aspect aspect1 = null;
                Aspect aspect2 = null;
                try
                {
                    aspect1 = EnsureAspect("Aspect1");
                    aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field1' type='ShortText' />
  </Fields>
</AspectDefinition>";
                    aspect1.Save();

                    aspect2 = EnsureAspect("Aspect2");
                    aspect2.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field2' type='ShortText' />
  </Fields>
</AspectDefinition>";
                    aspect2.Save();

                    var fieldName1 = String.Concat(aspect1.Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");
                    var fieldName2 = String.Concat(aspect2.Name, Aspect.ASPECTFIELDSEPARATOR, "Field2");

                    var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content.AddAspects(aspect1, aspect2);
                    content[fieldName1] = "Value1";
                    content[fieldName2] = "Value2";
                    content.Save();
                    var id1 = content.Id;

                    content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content.AddAspects(aspect1, aspect2);
                    content[fieldName1] = "Value1a";
                    content[fieldName2] = "Value2";
                    content.Save();
                    var id2 = content.Id;

                    content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content.AddAspects(aspect1);
                    content[fieldName1] = "Value1";
                    content.Save();
                    var id3 = content.Id;

                    ContentTypeManager.Reset(); //---- must work with loaded indexing info table
                    content = Content.Load(content.Id);

                    var r1 = Content.All.DisableAutofilters().Where(c => (string)c[fieldName1] == "Value1").ToArray().Select(x => x.Id);
                    var r2 = Content.All.DisableAutofilters().Where(c => (string)c[fieldName2] == "Value2").ToArray().Select(x => x.Id);
                    var r3 = CreateSafeContentQuery(fieldName1 + ":Value1 .AUTOFILTERS:OFF").Execute().Identifiers;
                    var r4 = CreateSafeContentQuery(fieldName2 + ":Value2 .AUTOFILTERS:OFF").Execute().Identifiers;

                    var expected1 = String.Join(",", new[] { id1, id3 });
                    var expected2 = String.Join(",", new[] { id1, id2 });
                    var result1 = String.Join(",", r1);
                    var result2 = String.Join(",", r2);
                    var result3 = String.Join(",", r3);
                    var result4 = String.Join(",", r4);

                    Assert.AreEqual(expected1, result1);
                    Assert.AreEqual(expected2, result2);
                    Assert.AreEqual(expected1, result3);
                    Assert.AreEqual(expected2, result4);
                }
                finally
                {
                    aspect1.ForceDelete();
                    aspect2.ForceDelete();
                }
            });
        }
        [TestMethod]
        public void Aspect_Sortable()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                Aspect aspect1 = null;
                try
                {
                    aspect1 = EnsureAspect("Aspect_Sortable_Aspect1");
                    aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field1' type='ShortText' />
  </Fields>
</AspectDefinition>";
                    aspect1.Save();

                    var fieldName1 = String.Concat(aspect1.Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");

                    var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content.AddAspects(aspect1);
                    content[fieldName1] = "Aspect_Sortable1b";
                    content.Save();
                    var id1 = content.Id;

                    content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content.AddAspects(aspect1);
                    content[fieldName1] = "Aspect_Sortable1c";
                    content.Save();
                    var id2 = content.Id;

                    content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content.AddAspects(aspect1);
                    content[fieldName1] = "Aspect_Sortable1a";
                    content.Save();
                    var id3 = content.Id;

                    ContentTypeManager.Reset(); //---- must work with loaded indexing info table
                    content = Content.Load(content.Id);

                    var r0 = Content.All.DisableAutofilters().Where(c => c.InTree(testRoot) && ((string)c[fieldName1]).StartsWith("Aspect_Sortable1")).ToArray().Select(x => x.Id);
                    var r1 = Content.All.DisableAutofilters().Where(c => c.InTree(testRoot) && ((string)c[fieldName1]).StartsWith("Aspect_Sortable1")).OrderBy(c => c[fieldName1]).ToArray().Select(x => x.Id);
                    var r2 = Content.All.DisableAutofilters().Where(c => c.InTree(testRoot) && ((string)c[fieldName1]).StartsWith("Aspect_Sortable1")).OrderByDescending(c => c[fieldName1]).ToArray().Select(x => x.Id);
                    var r3 = CreateSafeContentQuery($"+InTree:'{testRoot.Path}' +{fieldName1}:Aspect_Sortable1* .AUTOFILTERS:OFF .SORT:{fieldName1}").Execute().Identifiers;
                    var r4 = CreateSafeContentQuery($"+InTree:'{testRoot.Path}' +{fieldName1}:Aspect_Sortable1* .AUTOFILTERS:OFF .REVERSESORT:{fieldName1}").Execute().Identifiers;

                    var expected1 = String.Join(",", new[] { id3, id1, id2 });
                    var expected2 = String.Join(",", new[] { id2, id1, id3 });
                    var result0 = String.Join(",", r0);
                    var result1 = String.Join(",", r1);
                    var result2 = String.Join(",", r2);
                    var result3 = String.Join(",", r3);
                    var result4 = String.Join(",", r4);

                    //Assert.AreEqual(expected1, result1);
                    //Assert.AreEqual(expected2, result2);
                    Assert.AreEqual(expected1, result3);
                    Assert.AreEqual(expected2, result4);
                }
                finally
                {
                    aspect1.ForceDelete();
                }
            });
        }
        [TestMethod]
        public void Aspect_UniqueName()
        {
            Test(() =>
            {
                var folder1 = new Folder(Repository.AspectsFolder) { Name = Guid.NewGuid().ToString() };
                folder1.Save();
                var folder2 = new Folder(Repository.AspectsFolder) { Name = Guid.NewGuid().ToString() };
                folder2.Save();

                var aspect1 = new Aspect(folder1) { Name = Guid.NewGuid().ToString() };
                aspect1.Save();
                var aspect2 = new Aspect(folder2) { Name = aspect1.Name };
                try
                {
                    aspect2.Save();
                    Assert.Fail("Exception was not thrown");
                }
                catch (InvalidOperationException)
                {
                }
            });
        }

        [TestMethod]
        public void Aspect_UniqueName_02()
        {
            Test(() =>
            {
                var aspectName = Guid.NewGuid().ToString();
                var aspect1 = new Aspect(Repository.AspectsFolder) { Name = aspectName };
                aspect1.Save();

                Assert.AreEqual(aspect1.Id, Aspect.LoadAspectByName(aspectName).Id, "#1 load newly created aspect by name failed: a different aspect was loaded.");

                //delete aspect to make its name available
                aspect1.ForceDelete();

                //create aspect with the same name
                var aspect2 = new Aspect(Repository.AspectsFolder) { Name = aspectName };
                aspect2.Save();
            });
        }

        [TestMethod]
        public void Aspect_SameFieldName()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                var folder1 = new Folder(testRoot) { Name = Guid.NewGuid().ToString() };
                folder1.Save();

                var aspect1 = EnsureAspect("Aspect_SameFieldName_Aspect1");
                aspect1.AddFields(new FieldInfo { Name = "Field1", Type = "ShortText" });
                var fn1 = aspect1.Name + Aspect.ASPECTFIELDSEPARATOR + "Field1";

                var aspect2 = EnsureAspect("Aspect_SameFieldName_Aspect2");
                aspect2.AddFields(new FieldInfo { Name = "Field1", Type = "Integer" });
                var fn2 = aspect2.Name + Aspect.ASPECTFIELDSEPARATOR + "Field1";

                var content1 = Content.CreateNew("Car", folder1, Guid.NewGuid().ToString());
                content1.AddAspects(aspect1);
                content1[fn1] = "TextValue";
                content1.Save();

                var content2 = Content.CreateNew("Car", folder1, Guid.NewGuid().ToString());
                content2.AddAspects(aspect2);
                content2[fn2] = 42;
                content2.Save();

                var result1 = Content.All.DisableAutofilters().Where(c => (string)c[fn1] == "TextValue").Count();
                var result2 = Content.All.DisableAutofilters().Where(c => (int)c[fn2] == 42).Count();

                Assert.IsTrue(result1 == 1, String.Format("Result1 is {0}, expected: 1", result1));
                Assert.IsTrue(result2 == 1, String.Format("Result2 is {0}, expected: 1", result2));
            });
        }
        [TestMethod]
        public void Aspect_CreateAddFieldsRemoveFields()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                Aspect aspect = null;
                try
                {
                    var aspectContent = Content.CreateNew("Aspect", Repository.AspectsFolder, "Aspect42");
                    aspectContent.Save();
                    aspect = (Aspect)aspectContent.ContentHandler;

                    var fieldName1 = String.Concat(aspect.Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");
                    var fieldName2 = String.Concat(aspect.Name, Aspect.ASPECTFIELDSEPARATOR, "Field2");

                    var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content.AddAspects((Aspect)aspect);
                    content.Save();

                    //var fs1 = new ShortTextFieldSetting { Name = "Field1", ShortName = "ShortText" };
                    //var fs2 = new ShortTextFieldSetting { Name = "Field2", ShortName = "ShortText" };
                    var fs1 = new FieldInfo { Name = "Field1", Type = "ShortText" };
                    var fs2 = new FieldInfo { Name = "Field2", Type = "ShortText" };

                    //-----------------------------------------------------------------------------------------------------

                    Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#1");
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#2");

                    aspect.AddFields(fs1);
                    content = Content.Load(content.Id);
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#11");
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#12");

                    aspect.AddFields(fs2);
                    content = Content.Load(content.Id);
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#21");
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName2), "#22");

                    aspect.RemoveFields(fieldName1);
                    content = Content.Load(content.Id);
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#31");
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName2), "#32");

                    aspect.RemoveFields(fieldName2);
                    content = Content.Load(content.Id);
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#41");
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#42");

                    aspect.AddFields(fs1, fs2);
                    content = Content.Load(content.Id);
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#51");
                    Assert.IsTrue(content.Fields.ContainsKey(fieldName2), "#52");

                    aspect.RemoveFields(fieldName1, fieldName2);
                    content = Content.Load(content.Id);
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#61");
                    Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#62");
                }
                finally
                {
                    aspect.ForceDelete();
                }
            });
        }

        [TestMethod]
        public void Aspect_ReferencePersistence()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                var aspectName = "ReferencePersistence";
                var aspectPath = Repository.AspectsFolderPath + "/" + aspectName;
                var fieldName = "References";
                var aspectFieldName = aspectName + Aspect.ASPECTFIELDSEPARATOR + fieldName;
                if (Node.Exists(aspectPath))
                    Node.ForceDelete(aspectPath);

                var aspectContent = Content.CreateNew("Aspect", Repository.AspectsFolder, aspectName);
                aspectContent.Save();
                var aspect = (Aspect)aspectContent.ContentHandler;
                aspect.AddFields(new FieldInfo
                {
                    Name = fieldName,
                    Type = "Reference",
                    Configuration = new ConfigurationInfo { FieldSpecific = new Dictionary<string, object> { { "AllowMultiple", true } } }
                });

                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.AddAspects((Aspect)aspect);
                content[aspectFieldName] = new NodeList<Node>(new[] { 1, 2, 3 });
                content.Save();

                content = Content.Load(content.Id);

                var gc = content.ContentHandler as GenericContent;
                var aspectData = gc.AspectData.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "").Replace("<?xmlversion=\"1.0\"encoding=\"utf-16\"?>", "");

                Assert.AreEqual("<AspectData><ReferencePersistence.References>1,2,3</ReferencePersistence.References></AspectData>", aspectData);
            });
        }
        [TestMethod]
        public void Aspect_ReferenceExportImport()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                var aspectName = "ReferencePersistence";
                var aspectPath = Repository.AspectsFolderPath + "/" + aspectName;
                var fieldName = "References";
                var aspectFieldName = aspectName + Aspect.ASPECTFIELDSEPARATOR + fieldName;
                if (Node.Exists(aspectPath))
                    Node.ForceDelete(aspectPath);

                var aspectContent = Content.CreateNew("Aspect", Repository.AspectsFolder, aspectName);
                aspectContent.Save();
                var aspect = (Aspect)aspectContent.ContentHandler;
                aspect.AddFields(new FieldInfo
                {
                    Name = fieldName,
                    Type = "Reference",
                    Configuration = new ConfigurationInfo { FieldSpecific = new Dictionary<string, object> { { "AllowMultiple", true } } }
                });

                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.AddAspects((Aspect)aspect);
                content[aspectFieldName] = new NodeList<Node>(new[] { 1, 2, 3 });
                content.Save();

                //---------------------------------------------------------------

                var sb = new StringBuilder();
                var writer = new XmlTextWriter(new StringWriter(sb));

                RepositoryEnvironment.WorkingMode = new RepositoryEnvironment.WorkingModeFlags
                {
                    Exporting = true,
                    Importing = false,
                    Populating = false,
                    SnAdmin = false
                };
                try
                {
                    content = Content.Load(content.Id);
                    content.ExportFieldData(writer, null);

                    var xml = new XmlDocument();
                    xml.LoadXml("<r>" + sb.ToString() + "</r>");
                    var xmlnode = xml.SelectSingleNode("//" + aspectFieldName);
                    var data = xmlnode.OuterXml.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "");

                    Assert.AreEqual(
                        "<" + aspectFieldName + ">"
                        + "<Path>/Root/IMS/BuiltIn/Portal/Admin</Path><Path>/Root</Path><Path>/Root/IMS</Path>"
                        + "</" + aspectFieldName + ">"
                        , data);
                }
                finally
                {
                    RepositoryEnvironment.WorkingMode = new RepositoryEnvironment.WorkingModeFlags
                    {
                        Exporting = false,
                        Importing = false,
                        Populating = false,
                        SnAdmin = false
                    };
                }
            });
        }

        [TestMethod]
        public void Aspect_FieldAppinfoContainsXmlCharacters()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                var appinfoValue = "asdf<qwer>yxcv";
                var fieldvalue = "Xy <b>asdf</b>.";
                var aspect = EnsureAspect("XmlCharTest");
                aspect.AddFields(
                    new FieldInfo
                    {
                        AppInfo = appinfoValue,
                        Name = "TestField",
                        Type = "ShortText"
                    });
                var content = Content.CreateNew("Car", testRoot, null);
                content.AddAspects(aspect);
                content["XmlCharTest.TestField"] = fieldvalue;
                content.Save();
                var id = content.Id;

                //--------

                content = Content.Load(id);
                Assert.AreEqual(appinfoValue, content.Fields["XmlCharTest.TestField"].FieldSetting.AppInfo);
                Assert.AreEqual(fieldvalue, (string)content["XmlCharTest.TestField"]);
            });
        }

        [TestMethod]
        public void Aspect_ReferenceFields()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                Aspect aspect1 = null;
                try
                {
                    var fields1 = new List<FieldInfo>();
                    fields1.Add(new FieldInfo()
                    {
                        Name = "MyField1",
                        Type = "ShortText",
                    });
                    fields1.Add(new FieldInfo()
                    {
                        Name = "MyField2",
                        Type = "Reference",
                    });

                    aspect1 = EnsureAspect("Aspect_ReferenceFields_Aspect1");
                    aspect1.AddFields(fields1.ToArray());
                    aspect1.Save();

                    var fn11 = aspect1.Name + Aspect.ASPECTFIELDSEPARATOR + fields1[0].Name;
                    var fn12 = aspect1.Name + Aspect.ASPECTFIELDSEPARATOR + fields1[1].Name;

                    // -----------

                    var content1 = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content1.AddAspects(aspect1);
                    content1[fn11] = "Hello world this is a nice summer afternoon!";
                    content1.Save();

                    var content2 = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content2.AddAspects(aspect1);
                    content2[fn11] = "Hello world this is a cold winter morning!";
                    content2[fn12] = new List<Node> { content1.ContentHandler };
                    content2.Save();

                    // Test reference property value after reload

                    content2 = Content.Load(content2.Id);
                    IEnumerable<Node> references = (IEnumerable<Node>)content2[fn12];

                    Assert.IsTrue(references.Any());
                    Assert.IsTrue(references.Count() == 1);
                    Assert.IsTrue(references.ElementAt(0).Id == content1.Id);

                    // Test if the field can be queried with CQL

                    var q = CreateSafeContentQuery(fn12 + ":" + content1.Id.ToString() + " .AUTOFILTERS:OFF");
                    IEnumerable<int> ids = q.Execute().Identifiers;

                    Assert.IsTrue(ids.Any());
                    Assert.IsTrue(ids.Contains(content2.Id));

                    // -----------

                    content1.ForceDelete();
                    content2.ForceDelete();
                }
                finally
                {
                    aspect1.ForceDelete();
                }
            });
        }

        [TestMethod]
        public void Aspect_AddingAspectDoesNotResetDefaultValuesOfTheOriginalFields()
        {
            Test(() =>
            {
                var testRoot = CreateTestRoot();

                var contentTypeName = "Aspect_AddingAspectDoesNotResetDefaultValuesOfTheOriginalFields";
                var shortTextFieldName = "ShortText";
                var displayName2Name = "DisplayName2";
                var int32FieldName = "Int32";
                var referenceFieldName = "GeneralReference";
                var shortText2FieldName = "ShortText2";
                var shortTextDefault = "Forty four";
                var displayName2Default = "Friendly name";
                var int32Default = 42;
                var referenceDefault = "/Root,/Root/IMS";
                ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='" + contentTypeName + @"' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' 
        xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <DisplayName>AddAspectAndFieldDefaultValueTest</DisplayName>
    <Description>AddAspectAndFieldDefaultValueTest</Description>
    <Fields>
        <Field name='" + shortTextFieldName + @"' type='ShortText'>
            <Configuration>
                <Compulsory>true</Compulsory>
                <DefaultValue>" + shortTextDefault + @"</DefaultValue>
            </Configuration>
        </Field>
		<Field name='" + int32FieldName + @"' type='Integer'>
            <Configuration>
                <Compulsory>true</Compulsory>
                <DefaultValue>" + int32Default + @"</DefaultValue>
            </Configuration>
        </Field>
		<Field name='" + referenceFieldName + @"' type='Reference'>
			<Configuration>
                <Compulsory>true</Compulsory>
				<AllowMultiple>true</AllowMultiple>
				<Compulsory>false</Compulsory>
                <DefaultValue>" + referenceDefault + @"</DefaultValue>
			</Configuration>
		</Field>
        <Field name='" + displayName2Name + @"' type='ShortText'>
            <Bind property='HiddenField'/>
            <Configuration>
                <Compulsory>true</Compulsory>
                <DefaultValue>" + displayName2Default + @"</DefaultValue>
            </Configuration>
        </Field>
        <Field name='" + shortText2FieldName + @"' type='ShortText' />
    </Fields>
</ContentType>");

                var aspect1Name = contentTypeName + "_Aspect1";
                var aspect2Name = contentTypeName + "_Aspect2";
                var aspect3Name = contentTypeName + "_Aspect3";
                var aspectShortTextFieldName = aspect1Name + ".ShortText";
                var aspectDisplayName2FieldName = aspect1Name + ".DisplayName2";
                var aspectInt32FieldName = aspect2Name + ".Int32";
                var aspectReferenceFieldName = aspect3Name + ".GeneralReference";
                var aspect1 = EnsureAspect(aspect1Name);
                aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
    <Fields>
        <AspectField name='" + shortTextFieldName + @"' type='ShortText'>
            <Configuration>
                <Compulsory>true</Compulsory>
                <DefaultValue>" + shortTextDefault + @"</DefaultValue>
            </Configuration>
        </AspectField>
        <AspectField name='" + displayName2Name + @"' type='ShortText'>
            <Bind property='HiddenField'/>
            <Configuration>
                <Compulsory>true</Compulsory>
                <DefaultValue>" + displayName2Default + @"</DefaultValue>
            </Configuration>
        </AspectField>
    </Fields>
</AspectDefinition>";
                aspect1.Save();
                var aspect2 = EnsureAspect(aspect2Name);
                aspect2.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
    <Fields>
	    <AspectField name='" + int32FieldName + @"' type='Integer'>
            <Configuration>
                <Compulsory>true</Compulsory>
                <DefaultValue>" + int32Default + @"</DefaultValue>
            </Configuration>
        </AspectField>
    </Fields>
</AspectDefinition>";
                aspect2.Save();
                var aspect3 = EnsureAspect(aspect3Name);
                aspect3.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
    <Fields>
	    <AspectField name='" + referenceFieldName + @"' type='Reference'>
		    <Configuration>
                <Compulsory>true</Compulsory>
			    <AllowMultiple>true</AllowMultiple>
			    <Compulsory>false</Compulsory>
                <DefaultValue>" + referenceDefault + @"</DefaultValue>
		    </Configuration>
	    </AspectField>
    </Fields>
</AspectDefinition>";
                aspect3.Save();

                Content content = null;

                try
                {
                    // #1 check not saved
                    content = Content.CreateNew(contentTypeName, testRoot, Guid.NewGuid().ToString());
                    Assert.AreEqual(shortTextDefault, content[shortTextFieldName]);
                    Assert.AreEqual(null, content[shortText2FieldName]);
                    Assert.AreEqual(null, content[displayName2Name]);
                    Assert.AreEqual(int32Default, content[int32FieldName]);
                    Assert.AreEqual(referenceDefault, String.Join(",", ((IEnumerable<Node>)content[referenceFieldName]).Select(n => n.Path)));
                    content.Save();
                    var contentId = content.Id;

                    // #2 check reloaded
                    content = Content.Load(contentId);
                    Assert.AreEqual(shortTextDefault, content[shortTextFieldName]);
                    Assert.AreEqual(null, content[shortText2FieldName]);
                    Assert.AreEqual(null, content[displayName2Name]);
                    Assert.AreEqual(int32Default, content[int32FieldName]);
                    Assert.AreEqual(referenceDefault, String.Join(",", ((IEnumerable<Node>)content[referenceFieldName]).Select(n => n.Path)));

                    // #3 add aspect1 and aspect2 with separated instruction without save
                    content.AddAspects(aspect1);
                    content.AddAspects(aspect2);

                    // #4 check with 2 aspects / not saved
                    Assert.AreEqual(shortTextDefault, content[shortTextFieldName]);
                    Assert.AreEqual(null, content[shortText2FieldName]);
                    Assert.AreEqual(null, content[displayName2Name]);
                    Assert.AreEqual(int32Default, content[int32FieldName]);
                    Assert.AreEqual(referenceDefault, String.Join(",", ((IEnumerable<Node>)content[referenceFieldName]).Select(n => n.Path)));
                    Assert.AreEqual(shortTextDefault, content[aspectShortTextFieldName]);
                    Assert.AreEqual(null, content[aspectDisplayName2FieldName]);
                    Assert.AreEqual(int32Default, content[aspectInt32FieldName]);
                    content.Save();

                    // #5 check with 2 aspects / reloaded
                    content = Content.Load(contentId);
                    Assert.AreEqual(shortTextDefault, content[shortTextFieldName]);
                    Assert.AreEqual(null, content[displayName2Name]);
                    Assert.AreEqual(null, content[shortText2FieldName]);
                    Assert.AreEqual(int32Default, content[int32FieldName]);
                    Assert.AreEqual(referenceDefault, String.Join(",", ((IEnumerable<Node>)content[referenceFieldName]).Select(n => n.Path)));
                    Assert.AreEqual(shortTextDefault, content[aspectShortTextFieldName]);
                    Assert.AreEqual(null, content[aspectDisplayName2FieldName]);
                    Assert.AreEqual(int32Default, content[aspectInt32FieldName]);

                    // #6 add aspect3 without save
                    content.AddAspects(aspect3);

                    // #7 check with all aspects / not saved
                    Assert.AreEqual(shortTextDefault, content[shortTextFieldName]);
                    Assert.AreEqual(null, content[displayName2Name]);
                    Assert.AreEqual(null, content[shortText2FieldName]);
                    Assert.AreEqual(int32Default, content[int32FieldName]);
                    Assert.AreEqual(referenceDefault, String.Join(",", ((IEnumerable<Node>)content[referenceFieldName]).Select(n => n.Path)));
                    Assert.AreEqual(shortTextDefault, content[aspectShortTextFieldName]);
                    Assert.AreEqual(null, content[aspectDisplayName2FieldName]);
                    Assert.AreEqual(int32Default, content[aspectInt32FieldName]);
                    Assert.AreEqual(referenceDefault, String.Join(",", ((IEnumerable<Node>)content[aspectReferenceFieldName]).Select(n => n.Path)));
                    content.Save();

                    // #8 check with all aspects / reloaded
                    content = Content.Load(contentId);
                    Assert.AreEqual(shortTextDefault, content[shortTextFieldName]);
                    Assert.AreEqual(null, content[displayName2Name]);
                    Assert.AreEqual(null, content[shortText2FieldName]);
                    Assert.AreEqual(int32Default, content[int32FieldName]);
                    Assert.AreEqual(referenceDefault, String.Join(",", ((IEnumerable<Node>)content[referenceFieldName]).Select(n => n.Path)));
                    Assert.AreEqual(shortTextDefault, content[aspectShortTextFieldName]);
                    Assert.AreEqual(null, content[aspectDisplayName2FieldName]);
                    Assert.AreEqual(int32Default, content[aspectInt32FieldName]);
                    Assert.AreEqual(referenceDefault, String.Join(",", ((IEnumerable<Node>)content[aspectReferenceFieldName]).Select(n => n.Path)));
                }
                finally
                {
                    content.ForceDelete();
                    if (ContentType.GetByName(contentTypeName) != null)
                        ContentTypeInstaller.RemoveContentType(contentTypeName);
                }
            });
        }


        [TestMethod]
        public void Aspect_DI9UserInviteBug()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                var firstAspect = CreateDI9Aspect("FirstAspect", new Dictionary<string, string> { { "Field1", "ShortText" }, });
                var aspect1 = CreateDI9Aspect("Aspect1", new Dictionary<string, string> { { "Field1", "ShortText" }, });
                var aspect2 = CreateDI9Aspect("Aspect2", new Dictionary<string, string> { { "Field1", "ShortText" }, });
                var aspect3 = CreateDI9Aspect("Aspect3", new Dictionary<string, string> { { "Field1", "ShortText" }, });
                var lastAspect = CreateDI9Aspect("LastAspect", new Dictionary<string, string> { { "Field1", "ShortText" }, });

                // #1: Adding initial aspects
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.AddAspects(firstAspect, aspect1, aspect2, aspect3);
                content.Save();
                var contentId = content.Id;

                var aspectsTrace1 = String.Join(",", content.ContentHandler.GetReferences("Aspects").Select(n => n.Name));
                Assert.AreEqual("FirstAspect,Aspect1,Aspect2,Aspect3", aspectsTrace1);

                // #2: Adding GlobalUserAspect one more time and UserAspectLast
                content = Content.Load(contentId);
                content.AddAspects(firstAspect);
                content.AddAspects(lastAspect);

                var aspectsTrace2 = String.Join(",", content.ContentHandler.GetReferences("Aspects").Select(n => n.Name));
                Assert.AreEqual("FirstAspect,Aspect1,Aspect2,Aspect3,LastAspect", aspectsTrace2);
            });
        }
        private Aspect CreateDI9Aspect(string name, IDictionary<string, string> fields)
        {
            var aspect = Aspect.LoadAspectByName(name);
            if (aspect == null)
                aspect = new Aspect(Repository.AspectsFolder) { Name = name };

            aspect.AspectDefinition = String.Format(@"<AspectDefinition " +
                    "xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>" +
                    "<Fields>{0}</Fields></AspectDefinition>",
                "\r\n  " + String.Join("\r\n  ", fields.Select(i => String.Format("<AspectField name='{0}' type='{1}' />",
                    i.Key, i.Value))));
            aspect.Save();

            return aspect;
        }

        // =============================================================================================

        private GenericContent CreateTestRoot(bool save = true)
        {
            var node = new SystemFolder(Repository.Root) { Name = "_AspectTests" };
            if (save)
                node.Save();
            return node;
        }
        private static Site CreateTestSite()
        {
            var sites = new Folder(Repository.Root, "Sites") { Name = "Sites" };
            sites.Save();

            var site = new Site(sites) { Name = "TestSite", UrlList = new Dictionary<string, string> { { "localhost", "None" } } };
            site.Save();

            return site;
        }

        internal static Aspect EnsureAspect(string name)
        {
            var r = CreateSafeContentQuery($"+TypeIs:Aspect +Name:{name} .AUTOFILTERS:OFF").Execute();
            if (r.Count > 0)
                return (Aspect)r.Nodes.First();
            var aspectContent = Content.CreateNew("Aspect", Repository.AspectsFolder, name);
            aspectContent.Save();
            return (Aspect)aspectContent.ContentHandler;
        }
        private string GetJson(object o)
        {
            var writer = new StringWriter();
            Newtonsoft.Json.JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                .Serialize(writer, o);
            return writer.GetStringBuilder().ToString();
        }
    }
}
