using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SenseNet.Services.Core.Diagnostics;

/// <summary>Compatibility API for callers that consume a health response object.</summary>
/// <remarks>The HTTP middleware uses IHealthSnapshotStore directly to avoid JSON work per request.
/// Customize background collectors or the snapshot store to change the endpoint response.</remarks>
public interface IHealthHandler
{
    Task<object> GetHealthResponseAsync(HttpContext httpContext);
}

internal sealed class HealthHandler(IHealthSnapshotStore snapshots, IHealthAccessEvaluator access, TimeProvider clock) : IHealthHandler
{
    public Task<object> GetHealthResponseAsync(HttpContext httpContext) =>
        Task.FromResult(snapshots.Current.ReadResponse(access.CanReadDetails(httpContext), clock));
}
