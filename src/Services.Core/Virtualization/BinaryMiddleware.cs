using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SenseNet.Services.Core.Diagnostics;

namespace SenseNet.Services.Core.Virtualization
{
    /// <summary>
    /// ASP.NET Core middleware to process binary requests.
    /// Register UseSenseNetCors before UseSenseNetFiles to handle CORS, including preflight requests.
    /// </summary>
    public class BinaryMiddleware
    {
        private static readonly EventId RequestCompleted = new EventId(1001, "BinaryRequestCompleted");
        private static readonly EventId CorsResponse = new EventId(1002, "BinaryCorsResponse");
        private readonly RequestDelegate _next;
        private readonly ILogger<BinaryMiddleware> _logger;

        public BinaryMiddleware(RequestDelegate next, ILogger<BinaryMiddleware> logger = null)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext, WebTransferRegistrator statistics)
        {
            if (_logger?.IsEnabled(LogLevel.Information) == true || _logger?.IsEnabled(LogLevel.Trace) == true)
            {
                var started = Stopwatch.GetTimestamp();
                // CORS uses OnStarting. Read the final headers/status after the entire response,
                // including any downstream middleware or exception handler, has completed.
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
                            "Binary CORS response: Origin {Origin}; AllowOrigin {AllowOrigin}; AllowCredentials {AllowCredentials}; ExposeHeaders {ExposeHeaders}; Vary {Vary}; TraceId {TraceId}",
                            httpContext.Request.Headers[HeaderNames.Origin].ToString(),
                            headers[HeaderNames.AccessControlAllowOrigin].ToString(),
                            headers[HeaderNames.AccessControlAllowCredentials].ToString(),
                            headers[HeaderNames.AccessControlExposeHeaders].ToString(),
                            headers[HeaderNames.Vary].ToString(), httpContext.TraceIdentifier);

                    return Task.CompletedTask;
                });
            }

            var statData = statistics?.RegisterWebRequest(httpContext);
            var bh = new BinaryHandler(httpContext);
            await bh.ProcessRequestCore().ConfigureAwait(false);
            statistics?.RegisterWebResponse(statData, httpContext);

            if (_next != null)
                await _next(httpContext).ConfigureAwait(false);
        }
    }
}
