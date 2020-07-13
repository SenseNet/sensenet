using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SenseNet.OData.Metadata.Model;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Operations;

namespace SenseNet.OData.Writers
{
    /// <summary>
    /// Defines an inherited <see cref="ODataWriter"/> class for writing OData metadata in XML format.
    /// </summary>
    public class ODataXmlWriter : ODataWriter
    {
        /// <inheritdoc />
        /// <remarks>Returns with "xml" in this case.</remarks>
        public override string FormatName => "xml";

        /// <inheritdoc />
        /// <remarks>Returns with "application/xml" in this case.</remarks>
        public override string MimeType => "application/xml";

        /// <inheritdoc />
        protected override async Task WriteMetadataAsync(HttpContext httpContext, Edmx edmx)
        {
            string result;
            using (var writer = new StringWriter())
            {
                edmx.WriteXml(writer);
                result = writer.GetStringBuilder().ToString();
            }
            await WriteRawAsync(result, httpContext).ConfigureAwait(false);
        }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteServiceDocumentAsync(HttpContext httpContext, IEnumerable<string> names) { throw new SnNotSupportedException(); }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteSingleContentAsync(HttpContext httpContext, ODataEntity fields) { throw new SnNotSupportedException(); }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteActionsPropertyAsync(HttpContext httpContext, ODataActionItem[] actions, bool raw) { throw new SnNotSupportedException(); }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteErrorAsync(HttpContext context, Error error) { throw new SnNotSupportedException(); }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteOperationCustomResultAsync(HttpContext httpContext, object result, int? allCount) { throw new SnNotSupportedException(); }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteMultipleContentAsync(HttpContext httpContext, IEnumerable<ODataEntity> contents, int count) { throw new SnNotSupportedException(); }
        /// <inheritdoc />
        protected override Task WriteCountAsync(HttpContext httpContext, int count)
        {
            return WriteRawAsync(count, httpContext);
        }
    }
}
