﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SenseNet.ContentRepository.OData;
using SenseNet.OData.Metadata.Model;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Operations;

namespace SenseNet.OData.Writers
{
    /// <summary>
    /// Defines an inherited <see cref="ODataWriter"/> class for writing any OData response in JSON format.
    /// </summary>
    public class ODataJsonWriter : ODataWriter
    {
        /// <inheritdoc />
        /// <remarks>Returns with "json" in this case.</remarks>
        public override string FormatName => "json";

        /// <inheritdoc />
        /// <remarks>Returns with "application/json" in this case.</remarks>
        public override string MimeType => "application/json";

        /// <inheritdoc />
        protected override async Task WriteMetadataAsync(HttpContext httpContext, ODataRequest odataRequest, Edmx edmx)
        {
            string result;
            using (var writer = new StringWriter())
            {
                edmx.WriteJson(writer);
                result = writer.GetStringBuilder().ToString();
            }
            await WriteRawAsync(result, httpContext, odataRequest).ConfigureAwait(false);
        }
        /// <inheritdoc />
        protected override async Task WriteServiceDocumentAsync(HttpContext httpContext, ODataRequest odataRequest, IEnumerable<string> names)
        {
            using (var writer = new StringWriter())
            {
                var x = new { d = new { EntitySets = names } };
                JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                    .Serialize(writer, x);

                await WriteRawAsync(writer.GetStringBuilder().ToString(), httpContext, odataRequest);
            }
        }
        /// <inheritdoc />
        protected override async Task WriteSingleContentAsync(HttpContext httpContext, ODataRequest odataRequest, ODataEntity fields)
        {
            await WriteAsync(new ODataSingleContent { FieldData = fields }, httpContext, odataRequest).ConfigureAwait(false);
        }
        /// <inheritdoc />
        protected override async Task WriteMultipleContentAsync(HttpContext httpContext, ODataRequest odataRequest, IEnumerable<ODataEntity> contents, int count)
        {
            await WriteAsync(ODataMultipleContent.Create(contents, count), httpContext, odataRequest).ConfigureAwait(false);
        }
        /// <inheritdoc />
        protected override async Task WriteActionsPropertyAsync(HttpContext httpContext, ODataRequest odataRequest, ODataActionItem[] actions, bool raw)
        {
            if(raw)
                await WriteAsync(actions, httpContext, odataRequest)
                .ConfigureAwait(false);
            else
                await WriteAsync(new ODataSingleContent { FieldData = new ODataEntity { { ODataMiddleware.ActionsPropertyName, actions } } }, httpContext, odataRequest)
                .ConfigureAwait(false);
        }
        /// <inheritdoc />
        protected override async Task WriteOperationCustomResultAsync(HttpContext httpContext, ODataRequest odataRequest, object result, int? allCount)
        {
            if (result is IEnumerable<ODataEntity> dictionaryList)
            {
                await WriteAsync(ODataMultipleContent.Create(dictionaryList, allCount ?? dictionaryList.Count()), httpContext, odataRequest)
                    .ConfigureAwait(false);
                return;
            }
            if (result is IEnumerable<ODataObject> customContentList)
            {
                await WriteAsync(ODataMultipleContent.Create(customContentList, allCount ?? 0), httpContext, odataRequest)
                    .ConfigureAwait(false);
                return;
            }

            await WriteAsync(result, httpContext, odataRequest)
                .ConfigureAwait(false);
        }
        /// <inheritdoc />
        protected override Task WriteCountAsync(HttpContext httpContext, ODataRequest odataRequest, int count)
        {
            return WriteRawAsync(count, httpContext, odataRequest);
        }

        /// <inheritdoc />
        protected override async Task WriteErrorAsync(HttpContext httpContext, ODataRequest odataRequest, Error error)
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
            await WriteRawAsync(text, httpContext, odataRequest);
        }

        protected async Task WriteAsync(object response, HttpContext httpContext, ODataRequest odataRequest)
        {
            var resp = httpContext.Response;

            if (httpContext.Response.HasStarted)
                return;

            switch (response)
            {
                case null:
                    resp.StatusCode = 204;
                    return;
                case string _:
                    await WriteRawAsync(response, httpContext, odataRequest).ConfigureAwait(false);
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

            odataRequest.ResponseSize = text.Length;
            ResponseLimiter.AssertResponseLength(resp, text.Length);

            resp.ContentType = "application/json;odata=verbose;charset=utf-8";
            await resp.WriteAsync(text).ConfigureAwait(false);
        }
    }
}
