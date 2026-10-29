using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository.Security.ApiKeys;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core.Authentication;

namespace SenseNet.Services.Core.Diagnostics;

/// <summary>Serves anonymous and dedicated health-key requests before repository-dependent middleware.</summary>
internal sealed class HealthEarlyMiddleware(RequestDelegate next, IHealthSnapshotStore snapshots,
    HealthCheckerKeyValidator healthKey, HealthRepositoryLifecycle lifecycle, TimeProvider clock,
    IOptions<HealthMonitoringOptions> healthOptions, IOptions<AuthenticationOptions> authentication)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context).ConfigureAwait(false);
            return;
        }
        SetCors(context);
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }
        var key = context.GetApiKey();
        // An explicit, recognized health key takes precedence over other credentials.
        if (healthKey.Matches(key))
        {
            await snapshots.Current.WriteAsync(context, healthKey.IsValid(key), clock).ConfigureAwait(false);
            return;
        }
        var hasCredentials = !string.IsNullOrEmpty(key) || context.Request.Headers.ContainsKey("Authorization") ||
            authentication.Value.AddJwtCookie && context.Request.Cookies.ContainsKey(IdentityConstants.JwtCookieName) ||
            context.User.Identity?.IsAuthenticated == true;
        if (!hasCredentials || !lifecycle.Ready)
        {
            await snapshots.Current.WriteAsync(context, false, clock).ConfigureAwait(false);
            return;
        }
        try
        {
            await next(context).ConfigureAwait(false);
            // Authentication/authorization middleware may short-circuit an invalid credential.
            if (!context.Response.HasStarted && context.Response.StatusCode is 401 or 403)
            {
                context.Response.Clear();
                SetCors(context);
                await snapshots.Current.WriteAsync(context, false, clock).ConfigureAwait(false);
            }
        }
        catch (Exception) when (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
        {
            context.Response.Clear();
            SetCors(context);
            await snapshots.Current.WriteAsync(context, false, clock).ConfigureAwait(false);
        }
    }

    private void SetCors(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin) || !(healthOptions.Value.AllowedOrigins ?? Array.Empty<string>()).Contains(origin, StringComparer.Ordinal)) return;
        context.Response.Headers.AccessControlAllowOrigin = origin;
        context.Response.Headers.Vary = "Origin";
        context.Response.Headers.AccessControlAllowMethods = "GET, HEAD, OPTIONS";
        context.Response.Headers.AccessControlAllowHeaders = "Authorization, apikey";
    }
}
