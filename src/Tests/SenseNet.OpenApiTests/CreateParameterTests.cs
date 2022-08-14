using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.OData;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Sharing;
using SenseNet.OData;
using SenseNet.OpenApi;
using SenseNet.OpenApi.Model;
using SenseNet.Tests.Core;

namespace SenseNet.OpenApiTests
{
    [TestClass]
    public class CreateParameterTests : OpenApiTestBase
    {
        [TestMethod]
        public void OpenApi_CreateParam_HttpContext()
        {
            var builder = new OpenApiBuilder(null);
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo {Name = "prm", Type = typeof(HttpContext)});
            // ASSERT
            Assert.IsNull(actual);
        }
        [TestMethod]
        public void OpenApi_CreateParam_OdataRequest()
        {
            var builder = new OpenApiBuilder(null);
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(ODataRequest) });
            // ASSERT
            Assert.IsNull(actual);
        }
        [TestMethod]
        public void OpenApi_CreateParam_Bool_Optional()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter { Name = "prm", In = "query", Required = false, Schema = new ObjectSchema { Type = "boolean" } };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", IsOptional = true, Type = typeof(bool) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_String()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter { Name = "prm", In = "query", Required = true, Schema = new ObjectSchema { Type = "string" } };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(string) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_StringArray()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "array", Items = new ObjectSchema { Type = "string" } }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(string[]) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }

        [TestMethod]
        public void OpenApi_CreateParam_ODataArray()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "array", Items = new ObjectSchema { Type = "string" } }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(ODataArray<string>) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }

        [TestMethod]
        public void OpenApi_CreateParam_Int_Nullable()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter { Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "integer", Format = "int32", Nullable = true } };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(int?) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_Bool_Nullable()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter { Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "boolean", Nullable = true } };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", IsOptional = false, Type = typeof(bool?) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_DateTime_Nullable()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter { Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "string", Format = "date-time", Nullable = true } };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(DateTime?) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }

        [TestMethod]
        public void OpenApi_CreateParam_Format_Int()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "integer", Format = "int32" }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(int) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_Format_Long()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "integer", Format = "int64" }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(long) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_Format_DateTime()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "string", Format = "date-time" }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(DateTime) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_Format_TimeSpan()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "string", Format = "time-span" }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(TimeSpan) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_Format_ULong()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "string", Format = "ulong" }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(ulong) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_Format_Decimal()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "number", Format = "decimal" }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(decimal) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_Format_Double()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "number", Format = "double" }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(double) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_Format_Float()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "number", Format = "float" }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(float) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }
        [TestMethod]
        public void OpenApi_CreateParam_Format_Guid()
        {
            var builder = new OpenApiBuilder(null);
            var expected = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Type = "string", Format = "guid" }
            };
            // ACTION
            var actual = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(Guid) });
            // ASSERT
            Assert.AreEqual(Serialize(expected), Serialize(actual));
        }

        [TestMethod]
        public void OpenApi_CreateParam_IndexRebuildLevel()
        {
            var api = OpenApiGenerator.CreateOpenApiDocument("test");
            var builder = new OpenApiBuilder(api);
            var expectedParameter = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Ref = "#/components/schemas/IndexRebuildLevel" }
            };
            var expectedSchema = new ObjectSchema { Type = "string", Enum = new[] { "indexOnly", "databaseAndIndex" } };
            // ACTION
            var actualParameter = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(IndexRebuildLevel) });
            // ASSERT
            Assert.AreEqual(Serialize(expectedParameter), Serialize(actualParameter));
            var actualSchema = api.Components.Schemas[actualParameter.Schema.Ref.Split('/').Last()];
            Assert.AreEqual(Serialize(expectedSchema), Serialize(actualSchema));
        }
        [TestMethod]
        public void OpenApi_CreateParam_ODataFormat()
        {
            var api = OpenApiGenerator.CreateOpenApiDocument("test");
            var builder = new OpenApiBuilder(api);
            var expectedParameter = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Ref = "#/components/schemas/OutputFormat" }
            };
            var expectedSchema = new ObjectSchema { Type = "string", Enum = new[] { "none", "json", "verboseJson", "atom", "xml" } };
            // ACTION
            var actualParameter = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(OutputFormat) });
            // ASSERT
            Assert.AreEqual(Serialize(expectedParameter), Serialize(actualParameter));
            var actualSchema = api.Components.Schemas[actualParameter.Schema.Ref.Split('/').Last()];
            Assert.AreEqual(Serialize(expectedSchema), Serialize(actualSchema));
        }
        [TestMethod]
        public void OpenApi_CreateParam_SharingMode()
        {
            var api = OpenApiGenerator.CreateOpenApiDocument("test");
            var builder = new OpenApiBuilder(api);
            var expectedParameter = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Ref = "#/components/schemas/SharingMode" }
            };
            var expectedSchema = new ObjectSchema { Type = "string", Enum = new[] { "public", "authenticated", "private" } };
            // ACTION
            var actualParameter = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(SharingMode) });
            // ASSERT
            Assert.AreEqual(Serialize(expectedParameter), Serialize(actualParameter));
            var actualSchema = api.Components.Schemas[actualParameter.Schema.Ref.Split('/').Last()];
            Assert.AreEqual(Serialize(expectedSchema), Serialize(actualSchema));
        }
        [TestMethod]
        public void OpenApi_CreateParam_SharingLevel()
        {
            var api = OpenApiGenerator.CreateOpenApiDocument("test");
            var builder = new OpenApiBuilder(api);
            var expectedParameter = new Parameter
            {
                Name = "prm",
                In = "query",
                Required = true,
                Schema = new ObjectSchema { Ref = "#/components/schemas/SharingLevel" }
            };
            var expectedSchema = new ObjectSchema { Type = "string", Enum = new[] { "open", "edit" } };
            // ACTION
            var actualParameter = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(SharingLevel) });
            // ASSERT
            Assert.AreEqual(Serialize(expectedParameter), Serialize(actualParameter));
            var actualSchema = api.Components.Schemas[actualParameter.Schema.Ref.Split('/').Last()];
            Assert.AreEqual(Serialize(expectedSchema), Serialize(actualSchema));
        }
        [TestMethod]
        public void OpenApi_CreateParam_MetadataFormat()
        {
            var api = OpenApiGenerator.CreateOpenApiDocument("test");
            var builder = new OpenApiBuilder(api);
            var expectedParameter = new Parameter
            {
                Name = "prm", In = "query", Required = true,
                Schema = new ObjectSchema { Ref = "#/components/schemas/MetadataFormat" }
            };
            var expectedSchema = new ObjectSchema { Type = "string", Enum = new[] { "no", "minimal", "full" } };
            // ACTION
            var actualParameter = builder.CreateParameter(new OperationParameterInfo { Name = "prm", Type = typeof(MetadataFormat) });
            // ASSERT
            Assert.AreEqual(Serialize(expectedParameter), Serialize(actualParameter));
            var actualSchema = api.Components.Schemas[actualParameter.Schema.Ref.Split('/').Last()];
            actualSchema.Description = null; // clear description for easy comparison.
            Assert.AreEqual(Serialize(expectedSchema), Serialize(actualSchema));
        }

    }
}
