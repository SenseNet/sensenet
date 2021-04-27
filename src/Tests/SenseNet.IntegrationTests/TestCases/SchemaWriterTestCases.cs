using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.IntegrationTests.Infrastructure;

namespace SenseNet.IntegrationTests.TestCases
{
    public class SchemaWriterTestCases : TestCaseBase
    {
        // ReSharper disable once InconsistentNaming
        protected DataProvider DP => DataStore.DataProvider;

        /* ============================================================================== PropertyType */

        public void SchemaWriter_CreatePropertyType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var propertyTypeCountBefore = ed.PropertyTypes.Count;
                var lastPropertyTypeId = ed.PropertyTypes.Max(x => x.Id);
                var propertyTypeName = "PT1-" + Guid.NewGuid();
                var mapping = GetNextMapping(ed, false);

                // ACTION
                ed.CreatePropertyType(propertyTypeName, DataType.String, mapping);
                ed.Register();

                // ASSERT
                ActiveSchema.Reload();

                Assert.AreEqual(propertyTypeCountBefore + 1, ActiveSchema.PropertyTypes.Count);
                var propType = ActiveSchema.PropertyTypes[propertyTypeName];
                Assert.AreEqual(propertyTypeName, propType.Name);
                Assert.AreEqual(lastPropertyTypeId + 1, propType.Id);
                Assert.AreEqual(DataType.String, propType.DataType);
                Assert.AreEqual(mapping, propType.Mapping);
                Assert.AreEqual(false, propType.IsContentListProperty);
            });
        }
        public void SchemaWriter_CreateContentListPropertyType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var propertyTypeCountBefore = ed.PropertyTypes.Count;
                var lastPropertyTypeId = ed.PropertyTypes.Max(x => x.Id);
                var mapping = GetNextMapping(ed, true);

                // ACTION
                var created = ed.CreateContentListPropertyType(DataType.String, mapping);
                var propertyTypeName = created.Name;
                ed.Register();

                // ASSERT
                Assert.AreEqual(propertyTypeCountBefore + 1, ActiveSchema.PropertyTypes.Count);
                var propType = ActiveSchema.PropertyTypes[propertyTypeName];
                Assert.AreEqual(lastPropertyTypeId + 1, propType.Id);
                Assert.AreEqual(propertyTypeName, propType.Name);
                Assert.AreEqual(DataType.String, propType.DataType);
                Assert.AreEqual(800000000 + mapping, propType.Mapping);
                Assert.AreEqual(true, propType.IsContentListProperty);
            });
        }
        public void SchemaWriter_DeletePropertyType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var propertyTypeCountBefore = ed.PropertyTypes.Count;
                var propertyTypeName = "PT1-" + Guid.NewGuid();
                var mapping = GetNextMapping(ed, false);

                ed.CreatePropertyType(propertyTypeName, DataType.String, mapping);
                ed.Register();
                Assert.AreEqual(propertyTypeCountBefore + 1, ActiveSchema.PropertyTypes.Count);
                Assert.IsNotNull(ActiveSchema.PropertyTypes[propertyTypeName]);

                // ACTION
                ed = new SchemaEditor();
                ed.Load();
                ed.DeletePropertyType(ed.PropertyTypes[propertyTypeName]);
                ed.Register();

                // ASSERT
                Assert.AreEqual(propertyTypeCountBefore, ActiveSchema.PropertyTypes.Count);
                Assert.IsNull(ActiveSchema.PropertyTypes[propertyTypeName]);
            });
        }

        /* ============================================================================== NodeType */

        public void SchemaWriter_CreateRootNodeType_WithoutClassName()
        {
            IntegrationTest(() =>
            {
                try
                {
                    var ed = new SchemaEditor();
                    ed.Load();

                    // ACTION
                    ed.CreateNodeType(null, "NT1-" + Guid.NewGuid(), null);
                    ed.Register();

                    Assert.Fail();
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    Assert.IsTrue(e.Message.Contains("ClassName"));
                    // ignored
                }
            });
        }
        public void SchemaWriter_CreateRootNodeType_WithClassName()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var nodeTypeCountBefore = ActiveSchema.NodeTypes.Count;
                var lastNodeTypeId = ActiveSchema.NodeTypes.Max(x => x.Id);
                var nodeTypeName = "NT1-" + Guid.NewGuid();
                var className = "NT1Class";

                // ACTION
                ed.CreateNodeType(null, nodeTypeName, className);
                ed.Register();

                // ASSERT
                Assert.AreEqual(nodeTypeCountBefore + 1, ActiveSchema.NodeTypes.Count);
                var nodeType = ActiveSchema.NodeTypes[nodeTypeName];
                Assert.AreEqual(lastNodeTypeId + 1, nodeType.Id);
                Assert.AreEqual(nodeTypeName, nodeType.Name);
                Assert.AreEqual(null, nodeType.Parent);
                Assert.AreEqual(className, nodeType.ClassName);
            });
        }
        public void SchemaWriter_CreateNodeType_WithParent()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var nodeTypeCountBefore = ActiveSchema.NodeTypes.Count;
                var lastNodeTypeId = ActiveSchema.NodeTypes.Max(x => x.Id);
                var nodeTypeName1 = "NT1-" + Guid.NewGuid();
                var nodeTypeName2 = "NT2-" + Guid.NewGuid();
                var className1 = "NT0Class";
                var className2 = "NT1Class";

                // ACTION
                var nt1 = ed.CreateNodeType(null, nodeTypeName1, className1);
                ed.CreateNodeType(nt1, nodeTypeName2, className2);
                ed.Register();

                // ASSERT
                Assert.AreEqual(nodeTypeCountBefore + 2, ActiveSchema.NodeTypes.Count);
                var nodeType1 = ActiveSchema.NodeTypes[nodeTypeName1];
                Assert.AreEqual(lastNodeTypeId + 1, nodeType1.Id);
                Assert.AreEqual(nodeTypeName1, nodeType1.Name);
                Assert.AreEqual(null, nodeType1.Parent);
                Assert.AreEqual(className1, nodeType1.ClassName);
                var nodeType2 = ActiveSchema.NodeTypes[nodeTypeName2];
                Assert.AreEqual(lastNodeTypeId + 2, nodeType2.Id);
                Assert.AreEqual(nodeTypeName2, nodeType2.Name);
                Assert.AreEqual(nodeType1, nodeType2.Parent);
                Assert.AreEqual(className2, nodeType2.ClassName);
            });
        }
        public void SchemaWriter_ModifyNodeType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var nodeTypeName = "NT1-" + Guid.NewGuid();
                ed.CreateNodeType(null, nodeTypeName, "NT1Class");
                ed.Register();
                var nodeTypeCountBefore = ActiveSchema.NodeTypes.Count;
                var nodeTypeId = ActiveSchema.NodeTypes[nodeTypeName].Id;

                // ACTION
                ed = new SchemaEditor();
                ed.Load();
                var nt = ed.NodeTypes[nodeTypeName];
                ed.ModifyNodeType(nt, "NT1Class_modified");
                ed.Register();

                // ASSERT
                Assert.AreEqual(nodeTypeCountBefore, ActiveSchema.NodeTypes.Count);
                var nodeType = ActiveSchema.NodeTypes[nodeTypeName];
                Assert.AreEqual(nodeTypeId, nodeType.Id);
                Assert.AreEqual(nodeTypeName, nodeType.Name);
                Assert.AreEqual("NT1Class_modified", nodeType.ClassName);
            });
        }
        public void SchemaWriter_DeleteNodeType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var nodeTypeName = "NT1-" + Guid.NewGuid();
                ed.CreateNodeType(null, nodeTypeName, "NT1Class");
                ed.Register();
                var nodeTypeCountBefore = ActiveSchema.NodeTypes.Count;
                var nodeTypeId = ActiveSchema.NodeTypes[nodeTypeName].Id;

                // ACTION
                ed = new SchemaEditor();
                ed.Load();
                var nt = ed.NodeTypes[nodeTypeName];
                ed.DeleteNodeType(nt);
                ed.Register();

                // ASSERT
                Assert.AreEqual(nodeTypeCountBefore - 1, ActiveSchema.NodeTypes.Count);
                Assert.IsNull(ActiveSchema.NodeTypes[nodeTypeName]);
                Assert.IsNull(ActiveSchema.NodeTypes.GetItemById(nodeTypeId));
            });
        }

        /* ============================================================================== ContentListType */

        public void SchemaWriter_CreateContentListType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var contentListTypeCountBefore = ed.ContentListTypes.Count;
                var lastContentListTypeId = ed.ContentListTypes.Any() ? ed.ContentListTypes.Max(x => x.Id) : 0;
                var contentListTypeName = "LT1-" + Guid.NewGuid();

                // ACTION
                ed.CreateContentListType(contentListTypeName);
                ed.Register();

                // ASSERT
                Assert.AreEqual(contentListTypeCountBefore + 1, ActiveSchema.ContentListTypes.Count);
                var listType = ActiveSchema.ContentListTypes[contentListTypeName];
                Assert.IsTrue(lastContentListTypeId < listType.Id);
                Assert.AreEqual(contentListTypeName, listType.Name);
            });
        }
        public void SchemaWriter_DeleteContentListType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var contentListTypeCountBefore = ed.ContentListTypes.Count;
                var contentListTypeName1 = "LT1-" + Guid.NewGuid();
                var contentListTypeName2 = "LT2-" + Guid.NewGuid();
                var contentListTypeName3 = "LT3-" + Guid.NewGuid();
                var contentListTypeName4 = "LT4-" + Guid.NewGuid();
                ed.CreateContentListType(contentListTypeName1);
                ed.CreateContentListType(contentListTypeName2);
                ed.CreateContentListType(contentListTypeName3);
                ed.CreateContentListType(contentListTypeName4);
                ed.Register();
                Assert.AreEqual(contentListTypeCountBefore + 4, ActiveSchema.ContentListTypes.Count);

                // ACTION
                ed = new SchemaEditor();
                ed.Load();
                ed.DeleteContentListType(ed.ContentListTypes[contentListTypeName4]); // last
                ed.DeleteContentListType(ed.ContentListTypes[contentListTypeName2]); // middle
                ed.DeleteContentListType(ed.ContentListTypes[contentListTypeName1]); // first
                ed.Register();

                // ASSERT
                Assert.AreEqual(contentListTypeCountBefore + 1, ActiveSchema.ContentListTypes.Count);
                var listType = ActiveSchema.ContentListTypes[contentListTypeName3];
                Assert.AreEqual(contentListTypeName3, listType.Name);
            });
        }

        /* ============================================================================== PropertyType assignment */

        public void SchemaWriter_AddPropertyTypeToNodeType_Declared()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var nodeTypeName = "NT1-" + Guid.NewGuid();
                var ptName1 = "PT1-" + Guid.NewGuid();
                var ptName2 = "PT2-" + Guid.NewGuid();
                ed.CreateNodeType(null, nodeTypeName, "NT0Class");
                ed.Register();

                // ACTION
                ed = new SchemaEditor();
                ed.Load();
                var pt1 = ed.CreatePropertyType(ptName1, DataType.String, GetNextMapping(ed, false));
                var pt2 = ed.CreatePropertyType(ptName2, DataType.String, GetNextMapping(ed, false));
                var nt = ed.NodeTypes[nodeTypeName];
                nt.AddPropertyType(pt1);
                nt.AddPropertyType(pt2);
                ed.Register();

                // ASSERT
                nt = ActiveSchema.NodeTypes[nodeTypeName];
                AssertSequenceEqual(
                    new[] { ptName1, ptName2 },
                    nt.PropertyTypes.Select(x => x.Name));
            });
        }
        public void SchemaWriter_AddPropertyTypeToNodeType_Inherited()
        {
            //Assert.Inconclusive();
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var nodeTypeName1 = "NT1-" + Guid.NewGuid();
                var nodeTypeName2 = "NT2-" + Guid.NewGuid();
                var ptName1 = "PT1-" + Guid.NewGuid();
                var ptName2 = "PT2-" + Guid.NewGuid();
                var nt1 = ed.CreateNodeType(null, nodeTypeName1, "NT0Class");
                var ___ = ed.CreateNodeType(nt1, nodeTypeName2, "NT1Class");
                var pt1 = ed.CreatePropertyType(ptName1, DataType.String, GetNextMapping(ed, false));
                nt1.AddPropertyType(pt1);
                ed.Register();
                // ASSERT-BEFORE
                Assert.AreEqual(1, ActiveSchema.NodeTypes[nodeTypeName2].PropertyTypes.Count);
                var schema = DP.LoadSchemaAsync(CancellationToken.None).GetAwaiter().GetResult();
                var ntData2 = schema.NodeTypes.First(x => x.Name == nodeTypeName2);
                Assert.AreEqual(0, ntData2.Properties.Count);

                // ACTION
                ed = new SchemaEditor();
                ed.Load();
                var nt2 = ed.NodeTypes[nodeTypeName2];
                pt1 = ed.PropertyTypes[ptName1];
                nt2.AddPropertyType(pt1);
                ed.Register();

                // ASSERT-AFTER
                Assert.AreEqual(1, ActiveSchema.NodeTypes[nodeTypeName2].PropertyTypes.Count);
                schema = DP.LoadSchemaAsync(CancellationToken.None).GetAwaiter().GetResult();
                ntData2 = schema.NodeTypes.First(x => x.Name == nodeTypeName2);
                Assert.AreEqual(1, ntData2.Properties.Count);
            });
        }
        public void SchemaWriter_AddPropertyTypeToContentListType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var lt = ed.CreateContentListType("LT0" + Guid.NewGuid());

                // ACTION
                var pt0 = ed.CreateContentListPropertyType(DataType.String, GetNextMapping(ed, true));
                var pt1 = ed.CreateContentListPropertyType(DataType.Int, GetNextMapping(ed, true));
                var pt2 = ed.CreateContentListPropertyType(DataType.String, GetNextMapping(ed, true));
                var pt3 = ed.CreateContentListPropertyType(DataType.Int, GetNextMapping(ed, true));
                ed.AddPropertyTypeToPropertySet(pt0, lt);
                ed.AddPropertyTypeToPropertySet(pt1, lt);
                ed.AddPropertyTypeToPropertySet(pt2, lt);
                ed.AddPropertyTypeToPropertySet(pt3, lt);
                ed.Register();

                // ASSERT
                var schema = DP.LoadSchemaAsync(CancellationToken.None).GetAwaiter().GetResult();
                var ltData = schema.ContentListTypes.First(x => x.Name == lt.Name);
                AssertSequenceEqual(new[] { pt0.Name, pt1.Name, pt2.Name, pt3.Name }, ltData.Properties);
            });
        }

        public void SchemaWriter_RemovePropertyTypeFromNodeType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var nt = ed.CreateNodeType(null, "NT1-" + Guid.NewGuid(), "ClassName1");
                var pt0 = ed.CreatePropertyType("PT0-" + Guid.NewGuid(), DataType.String, GetNextMapping(ed, false));
                var pt1 = ed.CreatePropertyType("PT1-" + Guid.NewGuid(), DataType.String, GetNextMapping(ed, false));
                var pt2 = ed.CreatePropertyType("PT2-" + Guid.NewGuid(), DataType.String, GetNextMapping(ed, false));
                var pt3 = ed.CreatePropertyType("PT3-" + Guid.NewGuid(), DataType.String, GetNextMapping(ed, false));
                var pt4 = ed.CreatePropertyType("PT4-" + Guid.NewGuid(), DataType.String, GetNextMapping(ed, false));
                nt.AddPropertyType(pt0);
                nt.AddPropertyType(pt1);
                nt.AddPropertyType(pt2);
                nt.AddPropertyType(pt3);
                nt.AddPropertyType(pt4);
                ed.Register();

                // ACTION
                ed = new SchemaEditor();
                ed.Load();
                nt = ed.NodeTypes[nt.Name];
                pt0 = ed.PropertyTypes[pt0.Name];
                pt1 = ed.PropertyTypes[pt1.Name];
                pt2 = ed.PropertyTypes[pt2.Name];
                pt3 = ed.PropertyTypes[pt3.Name];
                pt4 = ed.PropertyTypes[pt4.Name];
                var ptX = ed.CreatePropertyType("PTX-" + Guid.NewGuid(), DataType.String, GetNextMapping(ed, false));
                ed.RemovePropertyTypeFromPropertySet(pt4, nt); // last
                ed.RemovePropertyTypeFromPropertySet(pt2, nt); // middle
                ed.RemovePropertyTypeFromPropertySet(pt0, nt); // first
                ed.RemovePropertyTypeFromPropertySet(pt0, nt); // first
                ed.RemovePropertyTypeFromPropertySet(ptX, nt); // not a member (without error)
                ed.Register();

                // ASSERT
                var schema = DP.LoadSchemaAsync(CancellationToken.None).GetAwaiter().GetResult();
                var ltData = schema.NodeTypes.First(x => x.Name == nt.Name);
                AssertSequenceEqual(new[] { pt1.Name, pt3.Name }, ltData.Properties);
            });
        }
        public void SchemaWriter_RemovePropertyTypeFromContentListType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var lt = ed.CreateContentListType("LT1-" + Guid.NewGuid());
                var pt0 = ed.CreateContentListPropertyType(DataType.Int, GetNextMapping(ed, true));
                var pt1 = ed.CreateContentListPropertyType(DataType.Int, GetNextMapping(ed, true));
                var pt2 = ed.CreateContentListPropertyType(DataType.Int, GetNextMapping(ed, true));
                var pt3 = ed.CreateContentListPropertyType(DataType.Int, GetNextMapping(ed, true));
                var pt4 = ed.CreateContentListPropertyType(DataType.Int, GetNextMapping(ed, true));
                lt.AddPropertyType(pt0);
                lt.AddPropertyType(pt1);
                lt.AddPropertyType(pt2);
                lt.AddPropertyType(pt3);
                lt.AddPropertyType(pt4);
                ed.Register();

                // ACTION
                ed = new SchemaEditor();
                ed.Load();
                lt = ed.ContentListTypes[lt.Name];
                lt.RemovePropertyType(ed.PropertyTypes[pt4.Name]); // last
                lt.RemovePropertyType(ed.PropertyTypes[pt2.Name]); // middle
                lt.RemovePropertyType(ed.PropertyTypes[pt0.Name]); // first
                ed.Register();

                // ASSERT
                var schema = DP.LoadSchemaAsync(CancellationToken.None).GetAwaiter().GetResult();
                var ltData = schema.ContentListTypes.First(x => x.Name == lt.Name);
                AssertSequenceEqual(new[] { pt1.Name, pt3.Name }, ltData.Properties);
            });
        }

        public void SchemaWriter_RemovePropertyTypeFromBaseNodeType()
        {
            IntegrationTest(() =>
            {
                var ed = new SchemaEditor();
                ed.Load();
                var nt0 = ed.CreateNodeType(null, "NT0-" + Guid.NewGuid(), "ClassName1");
                var nt1 = ed.CreateNodeType(nt0, "NT1-" + Guid.NewGuid(), "ClassName2");
                var pt0 = ed.CreatePropertyType("PT0-" + Guid.NewGuid(), DataType.String, GetNextMapping(ed, false));
                var pt1 = ed.CreatePropertyType("PT1-" + Guid.NewGuid(), DataType.String, GetNextMapping(ed, false));
                var pt2 = ed.CreatePropertyType("PT2-" + Guid.NewGuid(), DataType.String, GetNextMapping(ed, false));
                nt0.AddPropertyType(pt0);
                nt0.AddPropertyType(pt1);
                nt1.AddPropertyType(pt2);
                ed.Register();
                Assert.AreEqual(3, ActiveSchema.NodeTypes[nt1.Name].PropertyTypes.Count);

                // ACTION
                ed = new SchemaEditor();
                ed.Load();
                nt0 = ed.NodeTypes[nt0.Name];
                nt0.RemovePropertyType(ed.PropertyTypes[pt0.Name]);
                ed.Register();

                // ASSERT
                Assert.AreEqual(2, ActiveSchema.NodeTypes[nt1.Name].PropertyTypes.Count);
            });
        }

        /* ============================================================================== Tools */

        private static int _lastMapping = 1000;
        int GetNextMapping(SchemaRoot schema, bool contentList)
        {
            return Interlocked.Increment(ref _lastMapping);
        }
    }
}
