using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Diagnostics;

public interface IHealthHandler
{
    Task<HealthResponse> GetHealthResponseAsync(HttpContext httpContext);
}
internal class HealthHandler : IHealthHandler
{
    public Task<HealthResponse> GetHealthResponseAsync(HttpContext httpContext)
    {
        return Task.FromResult(HealthResponse.NotAvailable);
    }
}

public class HealthResponse
{
    internal static readonly HealthResponse NotRegistered = new() { Status = "Service not registered." };
    internal static readonly HealthResponse NotAvailable = new() { Status = "Service not available." };

    public string Status { get; set; }
}