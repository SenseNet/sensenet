using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SnBenchmark;

/// <summary>
/// Lightweight HTTP client wrapping sensenet OData REST API calls.
/// Thread-safe – a single instance is shared across all workers.
/// </summary>
public sealed class SenseNetClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _basePath;
    private readonly string _contentType;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public SenseNetClient(BenchmarkOptions opts)
    {
        _baseUrl = opts.RepositoryUrl.TrimEnd('/');
        _basePath = opts.BasePath;
        _contentType = opts.ContentTypeName;

        var handler = new HttpClientHandler();
        if (opts.SkipTlsValidation)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(opts.RequestTimeoutSeconds),
        };
        _http.DefaultRequestHeaders.Add("apikey", opts.ApiKey);
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ── Ensure base folder ──────────────────────────────────────────

    /// <summary>
    /// Ensures the /Root/Content/Benchmark folder exists.
    /// Creates it recursively if needed.
    /// </summary>
    public async Task EnsureBaseFolderAsync(CancellationToken ct)
    {
        var segments = _basePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = string.Empty;

        for (var i = 0; i < segments.Length; i++)
        {
            var parent = current == string.Empty ? "/" + segments[0] : current;
            current = "/" + string.Join("/", segments.Take(i + 1));

            if (i < 1) continue; // skip /Root – always exists

            // Check if exists
            var checkUrl = $"/OData.svc{current}?metadata=no&$select=Id";
            var resp = await _http.GetAsync(checkUrl, ct);
            if (resp.StatusCode == HttpStatusCode.OK) continue;

            // Create folder under parent
            var parentOData = $"/OData.svc{parent}";
            var body = new
            {
                __ContentType = "Folder",
                Name = segments[i],
                DisplayName = segments[i]
            };
            var json = JsonSerializer.Serialize(body, JsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var createResp = await _http.PostAsync(parentOData, content, ct);
            createResp.EnsureSuccessStatusCode();
        }
    }

    // ── Create content ──────────────────────────────────────────────

    /// <summary>
    /// Creates a content node under the benchmark folder.
    /// Returns (success, elapsedMs).
    /// </summary>
    public async Task<RequestResult> CreateContentAsync(string name, int workerIndex,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var url = $"/OData.svc{_basePath}";
            var body = new
            {
                __ContentType = _contentType,
                Name = name,
                DisplayName = $"Bench-{workerIndex}-{name}",
                Description = $"Benchmark content created at {DateTime.UtcNow:O} by worker {workerIndex}"
            };
            var json = JsonSerializer.Serialize(body, JsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(url, content, ct);
            sw.Stop();

            return new RequestResult
            {
                Operation = OperationType.Create,
                Success = resp.IsSuccessStatusCode,
                StatusCode = (int)resp.StatusCode,
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
                Timestamp = DateTime.UtcNow,
                WorkerIndex = workerIndex,
                ErrorMessage = resp.IsSuccessStatusCode
                    ? null
                    : await ReadErrorAsync(resp)
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestResult
            {
                Operation = OperationType.Create,
                Success = false,
                StatusCode = 0,
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
                Timestamp = DateTime.UtcNow,
                WorkerIndex = workerIndex,
                ErrorMessage = ex.Message
            };
        }
    }

    // ── Query content ───────────────────────────────────────────────

    /// <summary>
    /// Queries content under the benchmark folder with OData.
    /// Returns (success, elapsedMs).
    /// </summary>
    public async Task<RequestResult> QueryContentAsync(int workerIndex, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var skip = Random.Shared.Next(0, 50);
            var url = $"/OData.svc{_basePath}" +
                      $"?metadata=no&$orderby=CreationDate desc&$top=10&$skip={skip}" +
                      $"&$select=Id,Name,DisplayName,CreationDate";
            var resp = await _http.GetAsync(url, ct);
            sw.Stop();

            return new RequestResult
            {
                Operation = OperationType.Query,
                Success = resp.IsSuccessStatusCode,
                StatusCode = (int)resp.StatusCode,
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
                Timestamp = DateTime.UtcNow,
                WorkerIndex = workerIndex,
                ErrorMessage = resp.IsSuccessStatusCode
                    ? null
                    : await ReadErrorAsync(resp)
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestResult
            {
                Operation = OperationType.Query,
                Success = false,
                StatusCode = 0,
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
                Timestamp = DateTime.UtcNow,
                WorkerIndex = workerIndex,
                ErrorMessage = ex.Message
            };
        }
    }

    // ── Health check ────────────────────────────────────────────────

    public async Task<bool> IsAliveAsync(CancellationToken ct)
    {
        try
        {
            var resp = await _http.GetAsync("/OData.svc/Root?metadata=no&$select=Id", ct);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static async Task<string> ReadErrorAsync(HttpResponseMessage resp)
    {
        try
        {
            var body = await resp.Content.ReadAsStringAsync();
            return body.Length > 300 ? body[..300] + "…" : body;
        }
        catch
        {
            return $"HTTP {(int)resp.StatusCode}";
        }
    }

    public void Dispose() => _http.Dispose();
}

// ── DTOs ────────────────────────────────────────────────────────────

public enum OperationType { Create, Query }

public sealed class RequestResult
{
    public OperationType Operation { get; init; }
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public double ElapsedMs { get; init; }
    public DateTime Timestamp { get; init; }
    public int WorkerIndex { get; init; }
    public string? ErrorMessage { get; init; }
}
