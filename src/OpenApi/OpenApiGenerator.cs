using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using Formatting = Newtonsoft.Json.Formatting;

namespace SenseNet.OpenApi
{
    public partial class OpenApiGenerator
    {
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static string GetOpenApiDocument(Content content, HttpContext httpContext)
        {
            var thisUri = new Uri(httpContext.Request.GetDisplayUrl());
            var thisUrl = $"{thisUri.Scheme}://{thisUri.Authority}";

            var api = CreateOpenApiDocument(thisUrl);

            var settings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented};
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                JsonSerializer.CreateDefault(settings).Serialize(writer, api);

            httpContext.Response.Headers.Add("content-type", "application/json");
            return sb.ToString();
        }
    }
}
