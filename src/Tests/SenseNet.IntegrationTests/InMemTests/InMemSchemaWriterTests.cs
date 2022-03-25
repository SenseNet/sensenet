﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemSchemaWriterTests : IntegrationTest<InMemPlatform, SchemaWriterTestCases>
    {
        /* ============================================================================== PropertyType */

        [TestMethod]
        public void IntT_InMem_SchemaWriter_CreatePropertyType() { TestCase.SchemaWriter_CreatePropertyType(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_CreateContentListPropertyType() { TestCase.SchemaWriter_CreateContentListPropertyType(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_DeletePropertyType() { TestCase.SchemaWriter_DeletePropertyType(); }

        /* ============================================================================== NodeType */

        [TestMethod]
        public void IntT_InMem_SchemaWriter_CreateRootNodeType_WithoutClassName() { TestCase.SchemaWriter_CreateRootNodeType_WithoutClassName(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_CreateRootNodeType_WithClassName() { TestCase.SchemaWriter_CreateRootNodeType_WithClassName(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_CreateNodeType_WithParent() { TestCase.SchemaWriter_CreateNodeType_WithParent(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_ModifyNodeType() { TestCase.SchemaWriter_ModifyNodeType(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_DeleteNodeType() { TestCase.SchemaWriter_DeleteNodeType(); }

        /* ============================================================================== ContentListType */

        [TestMethod]
        public void IntT_InMem_SchemaWriter_CreateContentListType() { TestCase.SchemaWriter_CreateContentListType(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_DeleteContentListType() { TestCase.SchemaWriter_DeleteContentListType(); }

        /* ============================================================================== PropertyType assignment */

        [TestMethod]
        public void IntT_InMem_SchemaWriter_AddPropertyTypeToNodeType_Declared() { TestCase.SchemaWriter_AddPropertyTypeToNodeType_Declared(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_AddPropertyTypeToNodeType_Inherited() { TestCase.SchemaWriter_AddPropertyTypeToNodeType_Inherited(); }
        [TestMethod, TestCategory("Services")]
        public void IntT_InMem_SchemaWriter_AddPropertyTypeToContentListType_CSrv() { TestCase.SchemaWriter_AddPropertyTypeToContentListType(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_RemovePropertyTypeFromNodeType() { TestCase.SchemaWriter_RemovePropertyTypeFromNodeType(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_RemovePropertyTypeFromContentListType() { TestCase.SchemaWriter_RemovePropertyTypeFromContentListType(); }
        [TestMethod]
        public void IntT_InMem_SchemaWriter_RemovePropertyTypeFromBaseNodeType() { TestCase.SchemaWriter_RemovePropertyTypeFromBaseNodeType(); }
    }
}
