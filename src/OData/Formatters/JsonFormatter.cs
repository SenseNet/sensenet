using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SenseNet.ContentRepository.OData;

namespace SenseNet.OData.Formatters
{
    /// <summary>
    /// Defines an inherited <see cref="ODataFormatter"/> class for writing any OData response in JSON format.
    /// </summary>
    public class JsonFormatter : ODataFormatter
    {
        /// <inheritdoc />
        /// <remarks>Returns with "json" in this case.</remarks>
        public override string FormatName => "json";

        /// <inheritdoc />
        /// <remarks>Returns with "application/json" in this case.</remarks>
        public override string MimeType => "application/json";

        /// <inheritdoc />
        protected override async Task WriteMetadataAsync(HttpContext httpContext, Metadata.Edmx edmx)
        {
            string result;
            using (var writer = new StringWriter())
            {
                edmx.WriteJson(writer);
                result = writer.GetStringBuilder().ToString();
            }
            await httpContext.Response.WriteAsync(result).ConfigureAwait(false);
        }
        /// <inheritdoc />
        protected override async Task WriteServiceDocumentAsync(HttpContext httpContext, IEnumerable<string> names)
        {
            using (var writer = new StringWriter())
            {
                var x = new { d = new { EntitySets = names } };
                JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                    .Serialize(writer, x);

                await httpContext.Response.WriteAsync(writer.GetStringBuilder().ToString());
            }
        }
        /// <inheritdoc />
        protected override async Task WriteSingleContentAsync(HttpContext httpContext, ODataEntity fields)
        {
            await WriteAsync(new ODataSingleContent { FieldData = fields }, httpContext).ConfigureAwait(false);
        }
        /// <inheritdoc />
        protected override async Task WriteMultipleContentAsync(HttpContext httpContext, IEnumerable<ODataEntity> contents, int count)
        {
            await WriteAsync(ODataMultipleContent.Create(contents, count), httpContext).ConfigureAwait(false);
        }
        /// <inheritdoc />
        protected override async Task WriteActionsPropertyAsync(HttpContext httpContext, ODataActionItem[] actions, bool raw)
        {
            if(raw)
                await WriteAsync(actions, httpContext)
                .ConfigureAwait(false);
            else
                await WriteAsync(new ODataSingleContent { FieldData = new ODataEntity { { ODataMiddleware.ActionsPropertyName, actions } } }, httpContext)
                .ConfigureAwait(false);
        }
        /// <inheritdoc />
        protected override async Task WriteOperationCustomResultAsync(HttpContext httpContext, object result, int? allCount)
        {
            if (result is IEnumerable<ODataEntity> dictionaryList)
            {
                await WriteAsync(ODataMultipleContent.Create(dictionaryList, allCount ?? dictionaryList.Count()), httpContext)
                    .ConfigureAwait(false);
                return;
            }
            if (result is IEnumerable<ODataObject> customContentList)
            {
                await WriteAsync(ODataMultipleContent.Create(customContentList, allCount ?? 0), httpContext)
                    .ConfigureAwait(false);
                return;
            }

            await WriteAsync(result, httpContext)
                .ConfigureAwait(false);
        }
        /// <inheritdoc />
        protected override Task WriteCountAsync(HttpContext httpContext, int count)
        {
            return WriteRawAsync(count, httpContext);
        }

        /// <inheritdoc />
        protected override async Task WriteErrorAsync(HttpContext httpContext, Error error)
        {
            var settings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Formatting = Formatting.Indented,
                Converters = ODataMiddleware.JsonConverters
            };

            var serializer = JsonSerializer.Create(settings);
            string text;
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, new Dictionary<string, object> {{"error", error}});
                text = writer.GetStringBuilder().ToString();
            }

            httpContext.Response.ContentType = "application/json;odata=verbose;charset=utf-8";
            await httpContext.Response.WriteAsync(text);
        }

        protected async Task WriteAsync(object response, HttpContext httpContext)
        {
            var resp = httpContext.Response;

            switch (response)
            {
                case null:
                    resp.StatusCode = 204;
                    return;
                case string _:
                    await WriteRawAsync(response, httpContext).ConfigureAwait(false);
                    return;
            }

            var settings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Formatting = Formatting.Indented,
                Converters = ODataMiddleware.JsonConverters
            };
            var serializer = JsonSerializer.Create(settings);
            string text;
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, response);
                text = writer.GetStringBuilder().ToString();
            }
            resp.ContentType = "application/json;odata=verbose;charset=utf-8";
            await resp.WriteAsync(text).ConfigureAwait(false);
        }

    }
}
