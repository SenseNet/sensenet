using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.Clients;
using Formatting = Newtonsoft.Json.Formatting;

namespace SenseNet.OpenApi
{
    public partial class OpenApiGenerator
    {
        /// <summary>
        /// Generates OpenApi v3 definition of the current sensenet API.
        /// WARNING: this version contains only the basic (CRUD) operations.
        /// </summary>
        /// <snCategory>Content and Schema</snCategory>
        /// <param name="content"></param>
        /// <param name="httpContext"></param>
        /// <returns>OpenApi definition.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.All)]
        public static string GetOpenApiDocument(Content content, HttpContext httpContext)
        {
            var clientStoreOptions = httpContext.RequestServices.GetRequiredService<IOptions<ClientStoreOptions>>().Value;
            var api = CreateOpenApiDocument(clientStoreOptions.RepositoryUrl.AddUrlSchema());

            var settings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented};
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                JsonSerializer.CreateDefault(settings).Serialize(writer, api);

            httpContext.Response.Headers.Add("content-type", "application/json");
            return sb.ToString();
        }
    }
}
