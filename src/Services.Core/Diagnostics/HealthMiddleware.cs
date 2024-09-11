using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SenseNet.Services.Core.Diagnostics;

public class HealthMiddleware
{
    private readonly RequestDelegate _next;

    public HealthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    public async Task InvokeAsync(HttpContext httpContext, WebTransferRegistrator statistics)
    {
        var statData = statistics?.RegisterWebRequest(httpContext);

        // Get HealthResponse
        var healthHandler = httpContext.RequestServices.GetService<IHealthHandler>();
        var healthResponse = healthHandler == null
            ? "Service not registered: " + nameof(IHealthHandler)
            : await healthHandler.GetHealthResponseAsync(httpContext);

        // Write HealthResponse
        var webResponse = httpContext.Response;
        var output = JsonConvert.SerializeObject(healthResponse, SerializerSettings);
        var buffer = Encoding.UTF8.GetBytes(output);
        await webResponse.Body.WriteAsync(buffer, 0, buffer.Length)
            .ConfigureAwait(false);

        statistics?.RegisterWebResponse(statData, httpContext);

        // Call the next delegate/middleware in the pipeline.
        await _next(httpContext);
    }
}