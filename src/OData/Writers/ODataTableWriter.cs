using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SenseNet.OData.Metadata.Model;
using SenseNet.Services.Core.Operations;

namespace SenseNet.OData.Writers
{
    /// <summary>
    /// Defines an inherited <see cref="ODataWriter"/> class for writing OData objects in a simple HTML TABLE format.
    /// Designed for debug and test purposes only.
    /// </summary>
    public class ODataTableWriter : ODataWriter
    {
        /// <inheritdoc />
        /// <remarks>Returns with "table" in this case.</remarks>
        public override string FormatName => "table";

        /// <inheritdoc />
        /// <remarks>Returns with "application/html" in this case.</remarks>
        public override string MimeType => "text/html";

        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteMetadataAsync(HttpContext httpContext, ODataRequest odataRequest, Edmx edmx)
        {
            throw new SnNotSupportedException("Table writer does not support metadata writing.");
        }
        /// <inheritdoc />
        protected override async Task WriteServiceDocumentAsync(HttpContext httpContext, ODataRequest odataRequest, IEnumerable<string> names)
        {
            using (var writer = new StringWriter())
            {
                WriteStart(writer);
                writer.Write("      <tr><td>Service document</td></tr>\n");
                foreach (var name in names)
                {
                    writer.Write("      <tr><td>");
                    writer.Write(name);
                    writer.Write("</td></tr>\n");
                }
                WriteEnd(writer);

                var resp = httpContext.Response;
                resp.ContentType = "text/html";
                await WriteRawAsync(writer.GetStringBuilder().ToString(), httpContext, odataRequest);
            }
        }
        /// <inheritdoc />
        protected override async Task WriteSingleContentAsync(HttpContext httpContext, ODataRequest odataRequest, ODataEntity fields)
        {
            using (var writer = new StringWriter())
            {
                WriteStart(writer);

                writer.Write("      <tr><td>Name</td><td>Value</td></tr>\n");

                foreach (var item in fields.OrderBy(x => x.Key))
                {
                    if (item.Key == "__metadata")
                    {
                        if (item.Value is ODataSimpleMeta simpleMeta)
                        {
                            writer.Write("      <tr><td>__metadata.Uri</td><td>");
                            writer.Write(simpleMeta.Uri);
                            writer.Write("</td></tr>\n");
                            writer.Write("      <tr><td>__metadata.Type</td><td>");
                            writer.Write(simpleMeta.Type);
                            writer.Write("</td></tr>\n");

                            if (simpleMeta is ODataFullMeta fullMeta)
                            {
                                writer.Write("      <tr><td>__metadata.Actions</td><td>");
                                WriteValue(writer, fullMeta.Actions.Where(x => !x.Forbidden).Select(x => x.Name));
                                writer.Write("</td></tr>\n");
                                writer.Write("      <tr><td>__metadata.Functions</td><td>");
                                WriteValue(writer, fullMeta.Functions.Where(x => !x.Forbidden).Select(x => x.Name));
                                writer.Write("</td></tr>\n");
                            }
                        }
                    }
                    else
                    {
                        writer.Write("      <tr><td>");
                        writer.Write(item.Key);
                        writer.Write("</td><td>");
                        WriteValue(writer, item.Value);
                        writer.Write("</td></tr>\n");
                    }
                }
                WriteEnd(writer);

                var resp = httpContext.Response;
                resp.ContentType = "text/html";
                await WriteRawAsync(writer.GetStringBuilder().ToString(), httpContext, odataRequest);
            }
        }
        /// <inheritdoc />
        protected override async Task WriteMultipleContentAsync(HttpContext httpContext, ODataRequest odataRequest, IEnumerable<ODataEntity> contents, int count)
        {
            //var resp = httpContext.Response;
            var colNames = new List<string> { "Nr." };

            ODataSimpleMeta simpleMeta;
            ODataFullMeta fullMeta;
            var contentArray = contents?.ToArray();
            if (contentArray != null && contentArray.Length > 0)
            {
                var firstContent = contentArray[0];
                if (firstContent.Count > 0)
                {
                    simpleMeta = firstContent.First().Value as ODataSimpleMeta;
                    if (simpleMeta != null)
                    {
                        colNames.Add("__metadata.Uri");
                        colNames.Add("__metadata.Type");
                        fullMeta = simpleMeta as ODataFullMeta;
                        if (fullMeta != null)
                        {
                            colNames.Add("__metadata.Actions");
                            colNames.Add("__metadata.Functions");
                        }
                    }
                }
            }

            if (contentArray != null)
                foreach (var content in contentArray)
                    foreach (var item in content)
                        if (!colNames.Contains(item.Key) && item.Key != "__metadata")
                            colNames.Add(item.Key);

            using (var writer = new StringWriter())
            {
                WriteStart(writer);

                foreach (var colName in colNames)
                    if (colName != "__metadata")
                        writer.Write("<td>" + colName + "</td>");
                writer.Write("</tr>\n");

                var localCount = 0;
                if (contentArray != null)
                {
                    foreach (var content in contentArray)
                    {
                        var row = new object[colNames.Count];
                        row[0] = ++localCount;

                        var colIndex = 0;
                        object meta;
                        if (content.TryGetValue("__metadata", out meta))
                        {
                            simpleMeta = meta as ODataSimpleMeta;
                            if (simpleMeta != null)
                            {
                                colIndex = colNames.IndexOf("__metadata.Uri");
                                if (colIndex >= 0)
                                    row[colIndex] = simpleMeta.Uri;
                                colIndex = colNames.IndexOf("__metadata.Type");
                                if (colIndex >= 0)
                                    row[colIndex] = simpleMeta.Type;
                                fullMeta = simpleMeta as ODataFullMeta;
                                if (fullMeta != null)
                                {
                                    colIndex = colNames.IndexOf("__metadata.Actions");
                                    if (colIndex >= 0)
                                        row[colIndex] =
                                            FormatValue(fullMeta.Actions.Where(x => !x.Forbidden).Select(x => x.Name));
                                    colIndex = colNames.IndexOf("__metadata.Functions");
                                    if (colIndex >= 0)
                                        row[colIndex] = FormatValue(fullMeta.Functions.Where(x => !x.Forbidden)
                                            .Select(x => x.Name));
                                }
                            }
                        }

                        foreach (var item in content)
                        {
                            colIndex = colNames.IndexOf(item.Key);
                            if (colIndex >= 0)
                                row[colIndex] = item.Value;
                        }
                        writer.Write("      <tr>\n");

                        for (var i = 0; i < row.Length; i++)
                        {
                            writer.Write("        <td>");
                            WriteValue(writer, row[i]);
                            writer.Write("</td>\n");
                        }
                        writer.Write("      </tr>\n");
                    }
                }
                writer.Write("    </table>\n");
                writer.Write("  </div>\n");
                if (contentArray != null && contentArray.Length != count)
                    writer.Write("  <div>Total count of contents: " + count + "</div>\n");
                writer.Write("</body>\n");
                writer.Write("</html>\n");

                var resp = httpContext.Response;
                resp.ContentType = "text/html";
                await WriteRawAsync(writer.GetStringBuilder().ToString(), httpContext, odataRequest);
            }
        }
        /// <inheritdoc />
        protected override async Task WriteActionsPropertyAsync(HttpContext httpContext, ODataRequest odataRequest, ODataActionItem[] actions, bool raw)
        {
            // raw parameter isn't used
            var data = actions.Select(x => new ODataEntity{
                {"Name", x.Name},
                {"DisplayName", x.DisplayName},
                {"Index", x.Index},
                {"Icon", x.Icon},
                {"Url", x.Url},
                {"Forbidden", x.Forbidden}
            }).ToList();

            await WriteMultipleContentAsync(httpContext, odataRequest, data, actions.Length)
                .ConfigureAwait(false);
        }
        /// <summary>This method is not supported in this writer.</summary>
        protected override Task WriteOperationCustomResultAsync(HttpContext httpContext, ODataRequest odataRequest, object result, int? allCount)
        {
            throw new NotSupportedException("ODataTableWriter supports only a Content or an IEnumerable<Content> as an operation result.");
        }
        /// <inheritdoc />
        protected override Task WriteCountAsync(HttpContext httpContext, ODataRequest odataRequest, int count)
        {
            return WriteRawAsync(count, httpContext, odataRequest);
        }
        /// <inheritdoc />
        protected override async Task WriteErrorAsync(HttpContext httpContext, ODataRequest odataRequest, Error error)
        {
            using (var writer = new StringWriter())
            {
                WriteStartError(writer);

                writer.Write("      <tr><td>ERROR</td><td>" + error.Message.Value.Replace("<", "&lt;").Replace(">", "&gt;") + "</td></tr>\n");
                writer.Write("      <tr><td>Code</td><td>" + error.Code + "</td></tr>\n");
                writer.Write("      <tr><td>Exception type</td><td>" + error.ExceptionType + "</td></tr>\n");
                writer.Write("      <tr><td>Message (lang: " + error.Message.Lang + ")</td><td>" + error.Message.Value + "</td></tr>\n");
                if (error.InnerError != null)
                    writer.Write("      <tr><td>Inner error</td><td>" + error.InnerError.Trace.Replace("<", "&lt;").Replace(">", "&gt;").Replace(Environment.NewLine, "<br/>") + "</td></tr>\n");

                WriteEnd(writer);

                var resp = httpContext.Response;
                resp.ContentType = "text/html";
                await WriteRawAsync(writer.GetStringBuilder().ToString(), httpContext, odataRequest);
            }
        }

        private static void WriteStart(TextWriter writer)
        {
            writer.Write("<!DOCTYPE html>\n");
            writer.Write("<html>\n");
            writer.Write("<head>\n");
            writer.Write("<style>\n");

            writer.Write(@"
.MainTable { margin:0px;padding:0px; width:100%; border:1px solid #7f7f7f; }
.MainTable table { border-collapse: collapse; border-spacing: 0; width:100%; /*height:100%;*/ margin:0px;padding:0px; }
.MainTable tr:nth-child(odd)  { background-color:#e5e5e5; }
.MainTable tr:nth-child(even) { background-color:#ffffff; }
.MainTable td                            { border-width:0px 1px 1px 0px; vertical-align:middle; border:1px solid #7f7f7f; text-align:left; padding:4px; font-size:12px; font-family:Arial; font-weight:normal; color:#000000;}
.MainTable tr:last-child td              { border-width:0px 1px 0px 0px;}
.MainTable tr td:last-child              { border-width:0px 0px 1px 0px;}
.MainTable tr:last-child td:last-child   { border-width:0px 0px 0px 0px;}
.MainTable tr:first-child td             { border-width:0px 0px 1px 1px; background-color:#4c4c4c; border:0px solid #7f7f7f; font-size:16px; font-family:Arial; font-weight:bold; color:#ffffff;}
.MainTable tr:first-child td:first-child { border-width:0px 0px 1px 0px;}
.MainTable tr:first-child td:last-child  { border-width:0px 0px 1px 1px;}
");
            writer.Write("</style>\n");
            writer.Write("</head>\n");

            writer.Write("<body>\n");
            writer.Write("  <div class=\"MainTable\">\n");
            writer.Write("    <table>\n");
        }
        private static void WriteStartError(TextWriter writer)
        {
            writer.Write("<!DOCTYPE html>\n");
            writer.Write("<html>\n"
                            + "<head>\n"
                            + "<style>"
                            + @"
.MainTable { margin:0px;padding:0px; width:100%; border:1px solid #7c0202; }
.MainTable table { border-collapse: collapse; border-spacing: 0; width:100%; /*height:100%;*/ margin:0px;padding:0px; }
/*.MainTable tr:nth-child(odd)  { background-color:#fce3e3; }*/
/*.MainTable tr:nth-child(even) { background-color:#ffffff; }*/
.MainTable tr { background-color:#ffffff; }
.MainTable td                            { border-width:0px 1px 1px 0px; vertical-align:middle; border:1px solid #7c0202; text-align:left; padding:4px; font-size:12px; font-family:Arial; font-weight:normal; color:#000000; vertical-align:top;}
.MainTable tr:last-child td              { border-width:0px 1px 0px 0px;}
.MainTable tr td:last-child              { border-width:0px 0px 1px 0px;}
.MainTable tr:last-child td:last-child   { border-width:0px 0px 0px 0px;}
.MainTable tr:first-child td             { border-width:0px 0px 1px 1px; background-color:#ff0000; border:0px solid #7c0202; text-align:center; font-size:16px; font-family:Arial; font-weight:bold; color:#ffffff; width:150px;}
.MainTable tr:first-child td:first-child { border-width:0px 0px 1px 0px;}
.MainTable tr:first-child td:last-child  { border-width:0px 0px 1px 1px;}
"
                            + "</style>\n"
                            + "</head>\n"
                            + "<body>\n"
                            + "  <div class=\"MainTable\">\n"
                            + "    <table>\n");
        }
        private static void WriteEnd(TextWriter writer)
        {
            writer.Write("    </table>\n");
            writer.Write("  </div>\n");
            writer.Write("</body>\n");
            writer.Write("</html>\n");
        }
        private void WriteValue(TextWriter writer, object value)
        {
            writer.Write(FormatValue(value));
        }
        private string FormatValue(object value)
        {
            if (value == null)
                return "[null]";

            var deferred = value as ODataReference;
            if (deferred != null)
                return String.Format("<a href=\"{0}?$format=table\" title=\"{0}\">deferred<a>", deferred.Reference.Uri);

            var stringValue = value as string;
            if (stringValue != null)
                return stringValue;

            var enumerable = value as System.Collections.IEnumerable;
            if (enumerable != null)
                return String.Join(", ", enumerable.Cast<object>());

            return value.ToString();
        }
        private List<string> GetPropertyNames(object obj, Dictionary<Type, List<string>> cache)
        {
            List<string> names;
            var type = obj.GetType();
            if (cache.TryGetValue(type, out names))
                return names;

            names = type.GetProperties().Select(x => x.Name).ToList();
            cache.Add(type, names);
            return names;
        }
    }
}
