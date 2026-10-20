using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SenseNet.Services.Core.Diagnostics;

namespace SenseNet.Services.Core.Virtualization
{
    /// <summary>
    /// ASP.NET Core middleware to process binary requests.
    /// </summary>
    public class BinaryMiddleware
    {
        private static readonly EventId RequestCompleted = new EventId(1001, "BinaryRequestCompleted");
        private static readonly EventId CorsResponse = new EventId(1002, "BinaryCorsResponse");
        private readonly RequestDelegate _next;
        private readonly ICorsPolicyProvider _corsPolicyProvider;
        private readonly ILogger<BinaryMiddleware> _logger;

        // The legacy ASP.NET Core middleware activator chooses the first matching constructor.
        // Keep the DI constructor first; the attribute also selects it for current activators.
        [ActivatorUtilitiesConstructor]
        public BinaryMiddleware(RequestDelegate next, ICorsPolicyProvider corsPolicyProvider = null,
            ILogger<BinaryMiddleware> logger = null)
        {
            _next = next;
            _corsPolicyProvider = corsPolicyProvider;
            _logger = logger;
        }

        public BinaryMiddleware(RequestDelegate next)
            : this(next, null, null)
        {
        }

        public async Task InvokeAsync(HttpContext httpContext, WebTransferRegistrator statistics)
        {
            if (_logger?.IsEnabled(LogLevel.Information) == true || _logger?.IsEnabled(LogLevel.Trace) == true)
            {
                var started = Stopwatch.GetTimestamp();
                httpContext.Response.OnCompleted(() =>
                {
                    var headers = httpContext.Response.Headers;
                    var corsOutcome = !httpContext.Request.Headers.ContainsKey(HeaderNames.Origin)
                        ? "NoOrigin"
                        : headers.ContainsKey(HeaderNames.AccessControlAllowOrigin)
                            ? "AllowOriginPresent"
                            : "AllowOriginAbsent";

                    _logger.LogInformation(RequestCompleted,
                        "Binary request completed: {Method} {Path} {StatusCode}; CORS {CorsOutcome}; {DurationMs} ms; TraceId {TraceId}",
                        httpContext.Request.Method, httpContext.Request.Path, httpContext.Response.StatusCode,
                        corsOutcome, Stopwatch.GetElapsedTime(started).TotalMilliseconds, httpContext.TraceIdentifier);

                    if (_logger.IsEnabled(LogLevel.Trace))
                        _logger.LogTrace(CorsResponse,
                            "Binary CORS response: Origin {Origin}; AllowOrigin {AllowOrigin}; AllowCredentials {AllowCredentials}; ExposeHeaders {ExposeHeaders}; TraceId {TraceId}",
                            httpContext.Request.Headers[HeaderNames.Origin].ToString(),
                            headers[HeaderNames.AccessControlAllowOrigin].ToString(),
                            headers[HeaderNames.AccessControlAllowCredentials].ToString(),
                            headers[HeaderNames.AccessControlExposeHeaders].ToString(), httpContext.TraceIdentifier);

                    return Task.CompletedTask;
                });
            }

            // Preserve the original binary-specific CORS policy lookup and header injection.
            if (_corsPolicyProvider != null)
            {
                var corsPolicy = await _corsPolicyProvider.GetPolicyAsync(httpContext, "sensenet");
                if (corsPolicy != null)
                {
                    var origin = httpContext.Request.Headers["Origin"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(origin))
                    {
                        var isAllowed = corsPolicy.Origins.Contains(origin) ||
                                        corsPolicy.Origins.Contains("*") ||
                                        corsPolicy.AllowAnyOrigin;

                        if (isAllowed)
                        {
                            if (corsPolicy.AllowAnyOrigin || corsPolicy.Origins.Contains("*"))
                            {
                                if (!corsPolicy.SupportsCredentials)
                                    httpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                                else
                                    httpContext.Response.Headers.Append("Access-Control-Allow-Origin", origin);
                            }
                            else
                            {
                                httpContext.Response.Headers.Append("Access-Control-Allow-Origin", origin);
                            }

                            if (corsPolicy.SupportsCredentials)
                                httpContext.Response.Headers.Append("Access-Control-Allow-Credentials", "true");

                            if (corsPolicy.ExposedHeaders.Count > 0)
                            {
                                var exposedHeaders = string.Join(", ", corsPolicy.ExposedHeaders);
                                httpContext.Response.Headers.Append("Access-Control-Expose-Headers", exposedHeaders);
                            }
                        }
                    }
                }
            }

            var statData = statistics?.RegisterWebRequest(httpContext);

            var bh = new BinaryHandler(httpContext);

            await bh.ProcessRequestCore().ConfigureAwait(false);

            statistics?.RegisterWebResponse(statData, httpContext);

            // Call next middleware in the chain if exists
            if (_next != null)
                await _next(httpContext).ConfigureAwait(false);
        }
    }
}
