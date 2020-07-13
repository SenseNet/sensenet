using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SenseNet.OData.Metadata.Model;
using SenseNet.OData.Typescript;
using Schema = SenseNet.ContentRepository.Schema.Metadata.Schema;

// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

namespace SenseNet.OData.Writers
{
    /// <summary>
    /// Defines an inherited <see cref="ODataWriter"/> class for writing OData metadata in TypeScript format.
    /// </summary>
    public class ODataTypescriptWriter : ODataJsonWriter
    {
        /// <inheritdoc />
        /// <remarks>Returns with "typescript" in this case.</remarks>
        public override string FormatName => "typescript";

        /// <inheritdoc />
        /// <remarks>Returns with "text/x-typescript" in this case.</remarks>
        public override string MimeType => "text/x-typescript";

        /// <inheritdoc />
        protected override async Task WriteMetadataAsync(HttpContext httpContext, Edmx edmx)
        {
            var requestedModule = httpContext.Request.Query["module"].ToString().ToLowerInvariant();
            if (string.IsNullOrEmpty(requestedModule))
                requestedModule = "classes";

            var schema0 = new Schema(TypescriptGenerationContext.DisabledContentTypeNames);
            var context = new TypescriptGenerationContext();
            var schema1 = new TypescriptTypeCollectorVisitor(context).Visit(schema0);

            var writer = new StringWriter();
            switch (requestedModule)
            {
                case "enums":
                    new TypescriptEnumsVisitor(context, writer).Visit(schema1);
                    break;
                case "complextypes":
                    new TypescriptComplexTypesVisitor(context, writer).Visit(schema1);
                    break;
                case "contenttypes":
                    new TypescriptClassesVisitor(context, writer).Visit(schema1);
                    break;
                case "resources":
                    ResourceWriter.WriteResourceClasses(writer);
                    break;
                case "schemas":
                    new TypescriptCtdVisitor(context, writer).Visit(schema1);
                    break;
                case "fieldsettings":
                    new TypescriptFieldSettingsVisitor(context, writer).Visit(schema1);
                    break;
                default:
                    throw new InvalidOperationException("Unknown module name: " + requestedModule
                        + ". Valid names: enums, complextypes, contenttypes, resources, schemas, fieldsettings.");
            }

            await WriteRawAsync(writer.GetStringBuilder().ToString(), httpContext);
        }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteServiceDocumentAsync(HttpContext httpContext, IEnumerable<string> names) { throw new SnNotSupportedException(); }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteSingleContentAsync(HttpContext httpContext, ODataEntity fields) { throw new SnNotSupportedException(); }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteActionsPropertyAsync(HttpContext httpContext, ODataActionItem[] actions, bool raw) { throw new SnNotSupportedException(); }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteOperationCustomResultAsync(HttpContext httpContext, object result, int? allCount) { throw new SnNotSupportedException(); }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteMultipleContentAsync(HttpContext httpContext, IEnumerable<ODataEntity> content, int count) { throw new SnNotSupportedException(); }
    }
}
