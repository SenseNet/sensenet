using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using SenseNet.Services.Core.Diagnostics;

namespace SenseNet.Services.Core.Virtualization
{
    /// <summary>
    /// ASP.NET Core middleware to process binary requests.
    /// </summary>
    public class BinaryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ICorsPolicyProvider _corsPolicyProvider;
        private readonly ILogger<BinaryMiddleware> _logger;
        
        public BinaryMiddleware(RequestDelegate next, ICorsPolicyProvider corsPolicyProvider = null, ILogger<BinaryMiddleware> logger = null)
        {
            _next = next;
            _corsPolicyProvider = corsPolicyProvider;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext, WebTransferRegistrator statistics)
        {
            _logger?.LogInformation("=== BinaryMiddleware START ===");
            _logger?.LogInformation("Request Path: {Path}", httpContext.Request.Path);
            _logger?.LogInformation("Request Method: {Method}", httpContext.Request.Method);
            
            // Apply CORS policy manually since MapMiddlewareWhen creates a separate branch 
            // that bypasses the UseSenseNetCors middleware
            if (_corsPolicyProvider != null)
            {
                _logger?.LogInformation("CORS Policy Provider is available");
                
                var corsPolicy = await _corsPolicyProvider.GetPolicyAsync(httpContext, "sensenet");
                if (corsPolicy != null)
                {
                    _logger?.LogInformation("CORS Policy retrieved successfully");
                    _logger?.LogInformation("CORS Policy - AllowAnyOrigin: {AllowAnyOrigin}", corsPolicy.AllowAnyOrigin);
                    _logger?.LogInformation("CORS Policy - SupportsCredentials: {SupportsCredentials}", corsPolicy.SupportsCredentials);
                    _logger?.LogInformation("CORS Policy - Origins: {Origins}", string.Join(", ", corsPolicy.Origins));
                    
                    // Apply CORS headers based on the policy
                    var origin = httpContext.Request.Headers["Origin"].FirstOrDefault();
                    _logger?.LogInformation("Request Origin header: '{Origin}'", origin ?? "NULL");
                    
                    if (!string.IsNullOrEmpty(origin))
                    {
                        // Check if the origin is allowed (either matches exactly or policy allows any origin)
                        var isAllowed = corsPolicy.Origins.Contains(origin) || 
                                      corsPolicy.Origins.Contains("*") ||
                                      corsPolicy.AllowAnyOrigin;
                        
                        _logger?.LogInformation("Origin is allowed: {IsAllowed}", isAllowed);
                        
                        if (isAllowed)
                        {
                            // For any origin (*), we can only return * if credentials are NOT supported
                            if (corsPolicy.AllowAnyOrigin || corsPolicy.Origins.Contains("*"))
                            {
                                if (!corsPolicy.SupportsCredentials)
                                {
                                    httpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                                    _logger?.LogInformation("Added CORS header: Access-Control-Allow-Origin = *");
                                }
                                else
                                {
                                    httpContext.Response.Headers.Append("Access-Control-Allow-Origin", origin);
                                    _logger?.LogInformation("Added CORS header: Access-Control-Allow-Origin = {Origin}", origin);
                                }
                            }
                            else
                            {
                                httpContext.Response.Headers.Append("Access-Control-Allow-Origin", origin);
                                _logger?.LogInformation("Added CORS header: Access-Control-Allow-Origin = {Origin}", origin);
                            }
                            
                            if (corsPolicy.SupportsCredentials)
                            {
                                httpContext.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
                                _logger?.LogInformation("Added CORS header: Access-Control-Allow-Credentials = true");
                            }
                            
                            if (corsPolicy.ExposedHeaders.Count > 0)
                            {
                                var exposedHeaders = string.Join(", ", corsPolicy.ExposedHeaders);
                                httpContext.Response.Headers.Append("Access-Control-Expose-Headers", exposedHeaders);
                                _logger?.LogInformation("Added CORS header: Access-Control-Expose-Headers = {Headers}", exposedHeaders);
                            }
                        }
                        else
                        {
                            _logger?.LogWarning("Origin '{Origin}' is NOT allowed by CORS policy", origin);
                        }
                    }
                    else
                    {
                        _logger?.LogInformation("No Origin header in request");
                    }
                }
                else
                {
                    _logger?.LogWarning("CORS Policy is NULL - could not retrieve policy");
                }
            }
            else
            {
                _logger?.LogWarning("CORS Policy Provider is NULL - CORS headers will NOT be added");
            }

            var statData = statistics?.RegisterWebRequest(httpContext);

            var bh = new BinaryHandler(httpContext);

            await bh.ProcessRequestCore().ConfigureAwait(false);
            
            _logger?.LogInformation("Response Status Code: {StatusCode}", httpContext.Response.StatusCode);
            _logger?.LogInformation("Response Headers: {Headers}", 
                string.Join("; ", httpContext.Response.Headers.Select(h => $"{h.Key}={h.Value}")));

            statistics?.RegisterWebResponse(statData, httpContext);
            
            _logger?.LogInformation("=== BinaryMiddleware END ===");

            // Call next middleware in the chain if exists
            if (_next != null)
                await _next(httpContext).ConfigureAwait(false);
        }
    }
}
