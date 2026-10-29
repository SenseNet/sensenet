using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SenseNet.Services.Core.Diagnostics;

/// <summary>Writes an already serialized snapshot without invoking a health provider.</summary>
public class HealthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext, IHealthSnapshotStore snapshots,
        IHealthAccessEvaluator access, TimeProvider clock)
    {
        await snapshots.Current.WriteAsync(httpContext, access.CanReadDetails(httpContext), clock).ConfigureAwait(false);
        // Preserve the existing branch-after hook. The built-in branch terminates immediately.
        await next(httpContext).ConfigureAwait(false);
    }
}
