using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.OpenApi;
using SenseNet.OpenApi.Model;
using SenseNet.Tests.Core;

namespace SenseNet.OpenApiTests
{
    [TestClass]
    public class GetSchemaTests : OpenApiTestBase
    {
        [TestMethod]
        public void OpenApi_GetSchema_String()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(string));

            // ASSERT
            var expected = new ObjectSchema() { Type = "string" };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Int()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(int));

            // ASSERT
            var expected = new ObjectSchema() { Type = "integer", Format = "int32"};
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Bool()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(bool));

            // ASSERT
            var expected = new ObjectSchema() { Type = "boolean" };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Object()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(object));

            // ASSERT
            var expected = new ObjectSchema() { Type = "object", Description = "TODO: EMPTY OBJECT" };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Array_String()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(string[]));

            // ASSERT
            var expected = new ObjectSchema() { Type = "array", Items = new ObjectSchema{Type = "string"}};
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Array_Int()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(int[]));

            // ASSERT
            var expected = new ObjectSchema() { Type = "array", Items = new ObjectSchema { Type = "integer", Format = "int32"} };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Array_Bool()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(bool[]));

            // ASSERT
            var expected = new ObjectSchema() { Type = "array", Items = new ObjectSchema { Type = "boolean" } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Array_Object()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(object[]));

            // ASSERT
            var expected = new ObjectSchema {Type = "array", Items = new ObjectSchema {Type = "object", Description = "TODO: EMPTY OBJECT"}};
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Enumerable_String()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(IEnumerable<string>));

            // ASSERT
            var expected = new ObjectSchema() { Type = "array", Items = new ObjectSchema { Type = "string" } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Enumerable_Int()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(IEnumerable<int>));

            // ASSERT
            var expected = new ObjectSchema() { Type = "array", Items = new ObjectSchema { Type = "integer", Format = "int32" } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Enumerable_Bool()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(IEnumerable<bool>));

            // ASSERT
            var expected = new ObjectSchema() { Type = "array", Items = new ObjectSchema { Type = "boolean" } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Enumerable_Object()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(IEnumerable<object>));

            // ASSERT
            var expected = new ObjectSchema() { Type = "array", Items = new ObjectSchema { Type = "object", Description = "TODO: EMPTY OBJECT" } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }

        [TestMethod]
        public void OpenApi_GetSchema_Content()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(SenseNet.ContentRepository.Content));

            // ASSERT
            var expected = new ObjectSchema() { Ref = "#/components/schemas/ODataEntity" };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Enumerable_Content()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(IEnumerable<SenseNet.ContentRepository.Content>));

            // ASSERT
            var expected = new ObjectSchema() { Type = "array", Items = new ObjectSchema { Ref = "#/components/schemas/ODataEntity" } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Async_Content()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(Task<SenseNet.ContentRepository.Content>));

            // ASSERT
            var expected = new ObjectSchema() { Ref = "#/components/schemas/ODataEntity" };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Async_Enumerable_Content()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(Task<IEnumerable<SenseNet.ContentRepository.Content>>));

            // ASSERT
            var expected = new ObjectSchema() { Type = "array", Items = new ObjectSchema { Ref = "#/components/schemas/ODataEntity" } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Void()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(Task));

            // ASSERT
            Assert.IsNull(schema);
        }
        [TestMethod]
        public void OpenApi_GetSchema_Async_Void()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(void));

            // ASSERT
            Assert.IsNull(schema);
        }

        [TestMethod]
        public void OpenApi_GetSchema_IDictionary_Int()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(IDictionary<string, int>));

            // ASSERT
            var expected = new DictionarySchema { Type = "object", AdditionalProperties = new ObjectSchema { Type = "integer", Format = "int32" } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Dictionary_Int()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(IDictionary<string, int>));

            // ASSERT
            var expected = new DictionarySchema { Type = "object", AdditionalProperties = new ObjectSchema { Type = "integer", Format = "int32" } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_IDictionary_IList_Int()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(IDictionary<string, IList<int>>));

            // ASSERT
            var expected = new DictionarySchema { Type = "object", AdditionalProperties = new ObjectSchema { Type = "array", Items = new ObjectSchema { Type = "integer", Format = "int32" } } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_IDictionary_List_Int()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(IDictionary<string, List<int>>));

            // ASSERT
            var expected = new DictionarySchema { Type = "object", AdditionalProperties = new ObjectSchema { Type = "array", Items = new ObjectSchema { Type = "integer", Format = "int32" } } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Dictionary_IList_Int()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(Dictionary<string, IList<int>>));

            // ASSERT
            var expected = new DictionarySchema { Type = "object", AdditionalProperties = new ObjectSchema { Type = "array", Items = new ObjectSchema { Type = "integer", Format = "int32" } } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Dictionary_List_Int()
        {
            var builder = new OpenApiBuilder(null);

            // ACTION
            var schema = builder.GetSchema(typeof(Dictionary<string, List<int>>));

            // ASSERT
            var expected = new DictionarySchema { Type = "object", AdditionalProperties = new ObjectSchema { Type = "array", Items = new ObjectSchema { Type = "integer", Format = "int32" } } };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
        }

        [TestMethod]
        public void OpenApi_GetSchema_Infer_ClientSecret()
        {
            var api = OpenApiGenerator.CreateOpenApiDocument("test");
            var builder = new OpenApiBuilder(api);
            var type = typeof(ClientSecret);
            // ACTION
            var schema = builder.GetSchema(type);
            // ASSERT
            var expected = new ObjectSchema {Ref = "#/components/schemas/" + type.Name};
            Assert.AreEqual(Serialize(expected), Serialize(schema));
            Assert.IsTrue(api.Components.Schemas.ContainsKey(type.Name));

            expected = new ObjectSchema { Type = "object", Properties = new Dictionary<string, Schema>
                {
                    {"id", new ObjectSchema{Type = "string"}},
                    {"value", new ObjectSchema{Type = "string"}},
                    {"creationDate", new ObjectSchema{Type = "string", Format = "date-time"}},
                    {"validTill", new ObjectSchema{Type = "string", Format = "date-time"}},
                }
            };
            var inferred = api.Components.Schemas[type.Name];
            Assert.AreEqual(Serialize(expected), Serialize(inferred));
        }
        [TestMethod]
        public void OpenApi_GetSchema_Infer_IdentityInfo()
        {
            var api = OpenApiGenerator.CreateOpenApiDocument("test");
            var builder = new OpenApiBuilder(api);
            var type = typeof(PermissionQueryForRest.IdentityInfo);
            // ACTION
            var schema = builder.GetSchema(type);
            // ASSERT
            var expected = new ObjectSchema { Ref = "#/components/schemas/" + type.Name };
            Assert.AreEqual(Serialize(expected), Serialize(schema));
            Assert.IsTrue(api.Components.Schemas.ContainsKey(type.Name));

            expected = new ObjectSchema
            {
                Type = "object",
                Properties = new Dictionary<string, Schema>
                {
                    {"path", new ObjectSchema{Type = "string"}},
                    {"name", new ObjectSchema{Type = "string"}},
                    {"displayName", new ObjectSchema{Type = "string"}},
                    {"groups", new ObjectSchema
                        {
                            Type = "array", Items = new ObjectSchema{Ref = "#/components/schemas/GroupInfo"}
                        }
                    },
                }
            };
            var inferred = api.Components.Schemas[type.Name];
            Assert.AreEqual(Serialize(expected), Serialize(inferred));
        }

        [TestMethod]
        public void OpenApi_GetSchema_Infer_TwoEnumsSameName()
        {
            var api = OpenApiGenerator.CreateOpenApiDocument("test");
            var builder = new OpenApiBuilder(api);
            var type1 = typeof(SenseNet.OpenApiTests.TestTypes1.TestEnum);
            var type2 = typeof(SenseNet.OpenApiTests.TestTypes2.TestEnum);
            // ACTION
            var schema1 = builder.GetSchema(type1);
            var schema2 = builder.GetSchema(type2);
            // ASSERT
            var expected1 = new ObjectSchema { Ref = "#/components/schemas/TestEnum" };
            Assert.AreEqual(Serialize(expected1), Serialize(schema1));
            var expected2 = new ObjectSchema { Ref = "#/components/schemas/TestEnum2" };
            Assert.AreEqual(Serialize(expected2), Serialize(schema2));
            var component1 = new ObjectSchema { Type = "string", Enum = new[] { "item1", "item2" } };
            Assert.AreEqual(Serialize(component1), Serialize(api.Components.Schemas["TestEnum"]));
            var component2 = new ObjectSchema { Type = "string", Enum = new[] { "itemA", "itemB", "itemC" } };
            Assert.AreEqual(Serialize(component2), Serialize(api.Components.Schemas["TestEnum2"]));
        }

        [TestMethod]
        public void OpenApi_GetSchema_Inheritance()
        {
            var api = OpenApiGenerator.CreateOpenApiDocument("test");
            var builder = new OpenApiBuilder(api);

            // ACTION
            var schema = builder.GetSchema(typeof(SenseNet.OpenApiTests.TestTypes3.Class2));

            // ASSERT
            var schemas = new[]
            {
                schema,
                api.Components.Schemas["BaseClass"],
                api.Components.Schemas["Class1"],
                api.Components.Schemas["Class2"],
            };
            var expected = new ObjectSchema[]
            {
                new ObjectSchema {Ref = "#/components/schemas/Class2"},
                new ObjectSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, Schema> {{"Property1", new ObjectSchema {Type = "string"}}}
                },
                new ObjectSchema
                {
                    Type = "object",
                    AllOf = new []{new ObjectSchema{Ref = "#/components/schemas/BaseClass" } },
                    Properties = new Dictionary<string, Schema> {{"Property2", new ObjectSchema {Type = "string"}}}
                },
                new ObjectSchema
                {
                    Type = "object",
                    AllOf = new []{new ObjectSchema{Ref = "#/components/schemas/Class1" } },
                    Properties = new Dictionary<string, Schema> {{"Property3", new ObjectSchema {Type = "string"}}}
                },
            };

            Assert.AreEqual(Serialize(expected), Serialize(schemas));
        }

    }
}
