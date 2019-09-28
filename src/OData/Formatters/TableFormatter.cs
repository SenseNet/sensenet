using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SenseNet.OData.Formatters
{
    /// <summary>
    /// Defines an inherited <see cref="ODataFormatter"/> class for writing OData objects in a simple HTML TABLE format.
    /// Designed for debug and test purposes only.
    /// </summary>
    public class TableFormatter : ODataFormatter
    {
        /// <inheritdoc />
        /// <remarks>Returns with "table" in this case.</remarks>
        public override string FormatName => "table";

        /// <inheritdoc />
        /// <remarks>Returns with "application/html" in this case.</remarks>
        public override string MimeType => "text/html";

        /// <summary>This method is not supported in this formatter.</summary>
        protected override Task WriteMetadataAsync(HttpContext httpContext, Metadata.Edmx edmx)
        {
            throw new SnNotSupportedException("Table formatter does not support metadata writing.");
        }
        /// <inheritdoc />
        protected override Task WriteServiceDocumentAsync(HttpContext httpContext, IEnumerable<string> names)
        {
            //var resp = httpContext.Response;

            //WriteStart(resp);
            //resp.Write("      <tr><td>Service document</td></tr>\n");
            //foreach (var name in names)
            //{
            //    resp.Write("      <tr><td>");
            //    resp.Write(name);
            //    resp.Write("</td></tr>\n");
            //}
            //WriteEnd(resp);
            throw new NotImplementedException(); //UNDONE:ODATA: Not implemented.
        }
        /// <inheritdoc />
        protected override void WriteSingleContent(HttpContext httpContext, ODataEntity fields)
        {
            //var resp = httpContext.Response;
            //resp.ContentType = "text/html";

            //WriteStart(resp);

            //resp.Write("      <tr><td>Name</td><td>Value</td></tr>\n");

            //foreach (var item in fields.OrderBy(x => x.Key))
            //{
            //    if (item.Key == "__metadata")
            //    {
            //        if (item.Value is ODataSimpleMeta simpleMeta)
            //        {
            //            resp.Write("      <tr><td>__metadata.Uri</td><td>");
            //            resp.Write(simpleMeta.Uri);
            //            resp.Write("</td></tr>\n");
            //            resp.Write("      <tr><td>__metadata.Type</td><td>");
            //            resp.Write(simpleMeta.Type);
            //            resp.Write("</td></tr>\n");

            //            if (simpleMeta is ODataFullMeta fullMeta)
            //            {
            //                resp.Write("      <tr><td>__metadata.Actions</td><td>");
            //                WriteValue(httpContext, fullMeta.Actions.Where(x => !x.Forbidden).Select(x => x.Name));
            //                resp.Write("</td></tr>\n");
            //                resp.Write("      <tr><td>__metadata.Functions</td><td>");
            //                WriteValue(httpContext, fullMeta.Functions.Where(x => !x.Forbidden).Select(x => x.Name));
            //                resp.Write("</td></tr>\n");
            //            }
            //        }
            //    }
            //    else
            //    {
            //        resp.Write("      <tr><td>");
            //        resp.Write(item.Key);
            //        resp.Write("</td><td>");
            //        WriteValue(httpContext, item.Value);
            //        resp.Write("</td></tr>\n");
            //    }
            //}
            //WriteEnd(resp);
            throw new NotImplementedException(); //UNDONE:ODATA: Not implemented.
        }
        /// <inheritdoc />
        protected override void WriteMultipleContent(HttpContext httpContext, IEnumerable<ODataEntity> contents, int count)
        {
            //var resp = httpContext.Response;
            //var colNames = new List<string> {"Nr."};

            //ODataSimpleMeta simpleMeta;
            //ODataFullMeta fullMeta;
            //if (contents != null && contents.Count > 0)
            //{
            //    var firstContent = contents[0];
            //    if (firstContent.Count > 0)
            //    {
            //        simpleMeta = firstContent.First().Value as ODataSimpleMeta;
            //        if (simpleMeta != null)
            //        {
            //            colNames.Add("__metadata.Uri");
            //            colNames.Add("__metadata.Type");
            //            fullMeta = simpleMeta as ODataFullMeta;
            //            if (fullMeta != null)
            //            {
            //                colNames.Add("__metadata.Actions");
            //                colNames.Add("__metadata.Functions");
            //            }
            //        }
            //    }
            //}

            //if (contents != null)
            //    foreach (var content in contents)
            //        foreach (var item in content)
            //            if (!colNames.Contains(item.Key) && item.Key != "__metadata")
            //                colNames.Add(item.Key);

            //WriteStart(resp);

            //foreach (var colName in colNames)
            //    if(colName!="__metadata")
            //        resp.Write("<td>" + colName + "</td>");
            //resp.Write("</tr>\n");

            //var localCount = 0;
            //if (contents != null)
            //{
            //    foreach (var content in contents)
            //    {
            //        var row = new object[colNames.Count];
            //        row[0] = ++localCount;

            //        var colIndex = 0;
            //        object meta;
            //        if (content.TryGetValue("__metadata", out meta))
            //        {
            //            simpleMeta = meta as ODataSimpleMeta;
            //            if (simpleMeta != null)
            //            {
            //                colIndex = colNames.IndexOf("__metadata.Uri");
            //                if (colIndex >= 0)
            //                    row[colIndex] = simpleMeta.Uri;
            //                colIndex = colNames.IndexOf("__metadata.Type");
            //                if (colIndex >= 0)
            //                    row[colIndex] = simpleMeta.Type;
            //                fullMeta = simpleMeta as ODataFullMeta;
            //                if (fullMeta != null)
            //                {
            //                    colIndex = colNames.IndexOf("__metadata.Actions");
            //                    if (colIndex >= 0)
            //                        row[colIndex] =
            //                            FormatValue(fullMeta.Actions.Where(x => !x.Forbidden).Select(x => x.Name));
            //                    colIndex = colNames.IndexOf("__metadata.Functions");
            //                    if (colIndex >= 0)
            //                        row[colIndex] = FormatValue(fullMeta.Functions.Where(x => !x.Forbidden)
            //                            .Select(x => x.Name));
            //                }
            //            }
            //        }

            //        foreach (var item in content)
            //        {
            //            colIndex = colNames.IndexOf(item.Key);
            //            if (colIndex >= 0)
            //                row[colIndex] = item.Value;
            //        }
            //        resp.Write("      <tr>\n");

            //        for (var i = 0; i < row.Length; i++)
            //        {
            //            resp.Write("        <td>");
            //            WriteValue(httpContext, row[i]);
            //            resp.Write("</td>\n");
            //        }
            //        resp.Write("      </tr>\n");
            //    }
            //}
            //resp.Write("    </table>\n");
            //resp.Write("  </div>\n");
            //if (contents != null && contents.Count != count)
            //    resp.Write("  <div>Total count of contents: " + count + "</div>\n");
            //resp.Write("</body>\n");
            //resp.Write("</html>\n");
            throw new NotImplementedException(); //UNDONE:ODATA: Not implemented.
        }
        /// <inheritdoc />
        protected override void WriteActionsProperty(HttpContext httpContext, ODataActionItem[] actions, bool raw)
        {
            // raw parameter isn't used
            var data = actions.Select(x => new ODataEntity{
                {"Name", x.Name},
                {"DisplayName", x.DisplayName},
                {"Index", x.Index},
                {"Icon", x.Icon},
                {"Url", x.Url},
                {"IncludeBackUrl", x.IncludeBackUrl},
                {"ClientAction", x.ClientAction},
                {"Forbidden", x.Forbidden}
            }).ToList();

            WriteMultipleContent(httpContext, data, actions.Length);
        }
        /// <summary>This method is not supported in this formatter.</summary>
        protected override void WriteOperationCustomResult(HttpContext httpContext, object result, int? allCount)
        {
            throw new NotSupportedException("TableFormatter supports only a Content or an IEnumerable<Content> as an operation result.");
        }
        /// <inheritdoc />
        protected override void WriteCount(HttpContext httpContext, int count)
        {
            /*await*/
            WriteRawAsync(count, httpContext).ConfigureAwait(false)
      .GetAwaiter().GetResult();
        }
        /// <inheritdoc />
        protected override Task WriteErrorAsync(HttpContext context, Error error)
        {
            //var resp = context.Response;
            //resp.ContentType = "text/html";

            //WriteStartError(resp);

            //resp.Write("      <tr><td>ERROR</td><td>" + error.Message.Value.Replace("<", "&lt;").Replace(">", "&gt;") + "</td></tr>\n");
            //resp.Write("      <tr><td>Code</td><td>" + error.Code + "</td></tr>\n");
            //resp.Write("      <tr><td>Exception type</td><td>" + error.ExceptionType + "</td></tr>\n");
            //resp.Write("      <tr><td>Message (lang: " + error.Message.Lang + ")</td><td>" + error.Message.Value + "</td></tr>\n");
            //if (error.InnerError != null)
            //    resp.Write("      <tr><td>Inner error</td><td>" + error.InnerError.Trace.Replace("<", "&lt;").Replace(">", "&gt;").Replace(Environment.NewLine, "<br/>") + "</td></tr>\n");

            //WriteEnd(resp);
            throw new NotImplementedException(); //UNDONE:ODATA: Not implemented.
        }

        private static void WriteStart(HttpResponse resp)
        {
            //            resp.Write("<!DOCTYPE html>\n");
            //            resp.Write("<html>\n");
            //            resp.Write("<head>\n");
            //            resp.Write("<style>\n");

            //            resp.Write(@"
            //.MainTable { margin:0px;padding:0px; width:100%; border:1px solid #7f7f7f; }
            //.MainTable table { border-collapse: collapse; border-spacing: 0; width:100%; /*height:100%;*/ margin:0px;padding:0px; }
            //.MainTable tr:nth-child(odd)  { background-color:#e5e5e5; }
            //.MainTable tr:nth-child(even) { background-color:#ffffff; }
            //.MainTable td                            { border-width:0px 1px 1px 0px; vertical-align:middle; border:1px solid #7f7f7f; text-align:left; padding:4px; font-size:12px; font-family:Arial; font-weight:normal; color:#000000;}
            //.MainTable tr:last-child td              { border-width:0px 1px 0px 0px;}
            //.MainTable tr td:last-child              { border-width:0px 0px 1px 0px;}
            //.MainTable tr:last-child td:last-child   { border-width:0px 0px 0px 0px;}
            //.MainTable tr:first-child td             { border-width:0px 0px 1px 1px; background-color:#4c4c4c; border:0px solid #7f7f7f; font-size:16px; font-family:Arial; font-weight:bold; color:#ffffff;}
            //.MainTable tr:first-child td:first-child { border-width:0px 0px 1px 0px;}
            //.MainTable tr:first-child td:last-child  { border-width:0px 0px 1px 1px;}
            //");
            //            resp.Write("</style>\n");
            //            resp.Write("</head>\n");

            //            resp.Write("<body>\n");
            //            resp.Write("  <div class=\"MainTable\">\n");
            //            resp.Write("    <table>\n");
            throw new NotImplementedException(); //UNDONE:ODATA: Not implemented.
        }
        private static void WriteStartError(HttpResponse resp)
        {
            resp.WriteAsync("<html>\n"
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
        private static void WriteEnd(HttpResponse resp)
        {
            //resp.Write("    </table>\n");
            //resp.Write("  </div>\n");
            //resp.Write("</body>\n");
            //resp.Write("</html>\n");
            throw new NotImplementedException(); //UNDONE:ODATA: Not implemented.
        }
        private void WriteValue(HttpContext httpContext, object value)
        {
            //httpContext.Response.Write(FormatValue(value));
            throw new NotImplementedException(); //UNDONE:ODATA: Not implemented.
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
