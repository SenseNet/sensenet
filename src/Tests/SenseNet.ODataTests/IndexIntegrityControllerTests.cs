#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search.Indexing.Integrity;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.OData;
using SenseNet.OData.Operations;
using SenseNet.OData.Operations.IndexIntegrity;
using SenseNet.Search.Indexing.Integrity;
using Task = System.Threading.Tasks.Task;
using IoFile = System.IO.File;

namespace SenseNet.ODataTests;

[TestClass]
public class IndexIntegrityControllerTests : ODataTestBase
{
    private const string Route = "/OData.svc/('Root')/IndexIntegrity/";

    [TestMethod]
    public void IndexIntegrity_IsOptIn()
    {
        var services = new ServiceCollection();
        services.AddSenseNetIndexIntegrityChecker<FakeProvider>(new ConfigurationBuilder().Build());
        using var container = services.BuildServiceProvider();
        Assert.IsNull(container.GetService<IIndexIntegrityProvider>());
        Assert.IsNull(container.GetService<IndexIntegrityWorker>());
        Assert.IsNull(container.GetService<IndexIntegrityController>());
    }

    [TestMethod]
    public Task IndexIntegrity_ReportsRepairsAndRetiresPreviousCheck()
    {
        var root = Path.Combine(Path.GetTempPath(), "sensenet-integrity-test-" + Guid.NewGuid());
        return ODataTest2Async(services =>
        {
            services.AddSingleton<IWebHostEnvironment>(new WebEnvironment { ContentRootPath = root });
            services.AddSenseNetIndexIntegrityChecker<FakeProvider>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Enabled"] = "true",
                    ["MinimumFreeBytes"] = "0"
                }).Build());
        }, async () =>
        {
            var services = Providers.Instance.Services;
            var fileStore = services.GetRequiredService<IndexIntegrityFileStore>();
            var worker = services.GetRequiredService<IndexIntegrityWorker>();
            var previousUser = User.Current;
            try
            {
                await worker.StartAsync(CancellationToken.None);
                User.Current = User.Visitor;
                var anonymous = await ODataPostAsync(Route + "StartCheck", null, "{}");
                Assert.IsTrue(anonymous.StatusCode is 403 or 404, anonymous.Result);
                var anonymousRuns = await ODataGetAsync(Route + "GetRuns", null);
                Assert.IsTrue(anonymousRuns.StatusCode is 403 or 404, anonymousRuns.Result);

                User.Current = User.Administrator;
                Assert.AreEqual(0, (await GetRunsAsync()).Length);

                var cookie = await RequestAsync(HttpMethods.Post, Route + "StartCheck", body: "{}",
                    setup: request =>
                    {
                        request.Headers.Cookie = ".sn.auth=Bearer%20token";
                        request.Headers.Authorization = "Bearer token";
                    });
                Assert.AreEqual(403, cookie.Response.StatusCode);
                Assert.AreEqual("invalid_antiforgery_token", Body(cookie).GetProperty("error").GetString());

                var start = await RequestAsync(HttpMethods.Post, Route + "StartCheck", body: "{}",
                    setup: request => request.Headers["Idempotency-Key"] = "check");
                Assert.AreEqual(202, start.Response.StatusCode, ResponseText(start));
                var checkId = Body(start).GetProperty("checkId").GetGuid();
                await WaitForCompletion(fileStore, checkId, checkId);

                var retry = await RequestAsync(HttpMethods.Post, Route + "StartCheck", body: "{}",
                    setup: request => request.Headers["Idempotency-Key"] = "check");
                Assert.AreEqual(checkId, Body(retry).GetProperty("checkId").GetGuid());

                var statusQuery = $"?checkId={checkId:D}&view=status";
                var location = start.Response.Headers.Location.ToString();
                Assert.AreEqual($"/odata.svc/('Root')/IndexIntegrity/GetReport{statusQuery}", location);
                var status = await ODataGetAsync(location[..location.IndexOf('?')], statusQuery);
                Assert.AreEqual(200, status.StatusCode, status.Result);
                using var statusJson = JsonDocument.Parse(status.Result);
                Assert.AreEqual("completed", statusJson.RootElement.GetProperty("status").GetString());
                Assert.AreEqual(1L, statusJson.RootElement.GetProperty("summary").GetProperty("differences").GetInt64());

                var download = await RequestAsync(HttpMethods.Get, Route + "GetReport", $"?checkId={checkId:D}&$format=table");
                Assert.AreEqual(200, download.Response.StatusCode, ResponseText(download));
                Assert.AreEqual("application/x-ndjson", download.Response.ContentType);
                Assert.AreEqual(download.Response.Body.Length, download.GetODataRequest().ResponseSize);
                Assert.AreEqual($"attachment; filename=\"comparison-{checkId:D}.jsonl\"",
                    download.Response.Headers.ContentDisposition.ToString());
                var lines = ResponseText(download)
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries);
                using var difference = JsonDocument.Parse(lines[0]);
                Assert.AreEqual("9007199254740993",
                    difference.RootElement.GetProperty("sqlNodeTimestamp").GetString());
                Assert.IsTrue(difference.RootElement.TryGetProperty("nodeModificationDateUtc", out _));
                Assert.IsFalse(difference.RootElement.TryGetProperty("docId", out _));
                using var summary = JsonDocument.Parse(lines[1]);
                Assert.AreEqual(1L, summary.RootElement.GetProperty("differences").GetInt64());

                var invalid = await ODataPostAsync(Route + "Repair", null,
                    $$"""{"checkId":"{{checkId:D}}","differenceIds":[1.4]}""");
                Assert.AreEqual(400, invalid.StatusCode, invalid.Result);

                var repairBody = $$"""{"checkId":"{{checkId:D}}","differenceIds":[1]}""";
                var repair = await RequestAsync(HttpMethods.Post, Route + "Repair", body: repairBody);
                Assert.AreEqual(202, repair.Response.StatusCode, ResponseText(repair));
                var repairId = Body(repair).GetProperty("repairId").GetGuid();
                await WaitForCompletion(fileStore, checkId, repairId);
                var repairResultsPath = fileStore.RepairPath(fileStore.Read(checkId, repairId)!);
                var repairResults = await IoFile.ReadAllTextAsync(repairResultsPath);
                Assert.IsFalse(string.IsNullOrEmpty(repairResults));
                var repairStatus = await ODataGetAsync(Route + "GetReport", $"{statusQuery}&repairId={repairId:D}");
                using var repairJson = JsonDocument.Parse(repairStatus.Result);
                Assert.AreEqual(1, repairJson.RootElement.GetProperty("summary").GetProperty("repairedVersions").GetInt32());

                var repairByKind = await ODataPostAsync(Route + "Repair", null,
                    $$"""{"checkId":"{{checkId:D}}","kinds":["NotInIndex"]}""");
                Assert.AreEqual(202, repairByKind.StatusCode, repairByKind.Result);
                using var byKind = JsonDocument.Parse(repairByKind.Result);
                var repairByKindId = byKind.RootElement.GetProperty("repairId").GetGuid();
                await WaitForCompletion(fileStore, checkId, repairByKindId);

                Assert.AreNotEqual(repairId, repairByKindId);
                var secondResultsPath = fileStore.RepairPath(fileStore.Read(checkId, repairByKindId)!);
                Assert.IsTrue(IoFile.Exists(secondResultsPath));
                Assert.AreEqual(repairResults, await IoFile.ReadAllTextAsync(repairResultsPath));
                var previousStatus = await ODataGetAsync(Route + "GetReport", $"{statusQuery}&repairId={repairId:D}");
                Assert.AreEqual(200, previousStatus.StatusCode, previousStatus.Result);
                Assert.AreEqual(repairStatus.Result, previousStatus.Result);

                var listed = (await GetRunsAsync()).Single();
                Assert.AreEqual(checkId, listed.CheckId);
                CollectionAssert.AreEquivalent(new[] { repairId, repairByKindId }, listed.RepairIds);

                var fake = (FakeProvider)services.GetRequiredService<IIndexIntegrityProvider>();
                fake.ProviderId = "replacement";
                var incompatible = await RequestAsync(HttpMethods.Post, Route + "Repair", body: repairBody);
                Assert.AreEqual(409, incompatible.Response.StatusCode);
                Assert.AreEqual("incompatible_provider", Body(incompatible).GetProperty("error").GetString());

                var use = fileStore.OpenReport(checkId)!;
                Guid nextId;
                try
                {
                    var next = await RequestAsync(HttpMethods.Post, Route + "StartCheck", body: "{}");
                    nextId = Body(next).GetProperty("checkId").GetGuid();
                    await WaitForCompletion(fileStore, nextId, nextId);
                    Assert.IsTrue(IoFile.Exists(fileStore.ReportPath(checkId)));
                    CollectionAssert.AreEqual(new[] { nextId, checkId }, (await GetRunsAsync())
                        .Select(run => run.CheckId).ToArray());
                }
                finally { use.Dispose(); }
                Assert.IsNull(fileStore.Read(checkId, checkId));
                var remaining = (await GetRunsAsync()).Single();
                Assert.AreEqual(nextId, remaining.CheckId);
                Assert.AreEqual(0, remaining.RepairIds.Length);
            }
            finally
            {
                User.Current = previousUser;
                await worker.StopAsync(CancellationToken.None);
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        });
    }

    private async System.Threading.Tasks.Task<(Guid CheckId, Guid[] RepairIds)[]> GetRunsAsync()
    {
        var response = await RequestAsync(HttpMethods.Get, Route + "GetRuns");
        Assert.AreEqual(200, response.Response.StatusCode, ResponseText(response));
        Assert.AreEqual("no-store", response.Response.Headers.CacheControl.ToString());
        return Body(response).GetProperty("checks").EnumerateArray()
            .Select(check => (check.GetProperty("checkId").GetGuid(),
                check.GetProperty("repairIds").EnumerateArray().Select(id => id.GetGuid()).ToArray()))
            .ToArray();
    }

    private async System.Threading.Tasks.Task<HttpContext> RequestAsync(string method, string path,
        string? query = null, string? body = null, Action<HttpRequest>? setup = null)
    {
        var context = CreateHttpContext(path, query);
        var responseBody = context.Response.Body;
        var response = new ResponseFeature();
        context.Features.Set<IHttpResponseFeature>(response);
        context.Features.Set<IHttpResponseBodyFeature>(new ResponseBodyFeature(responseBody, response));
        context.Request.Method = method;
        if (body != null)
        {
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Request.ContentLength = context.Request.Body.Length;
            context.Request.ContentType = "application/json";
        }
        setup?.Invoke(context.Request);
        var middleware = new ODataMiddleware(null, null, null, NullLogger<ODataMiddleware>.Instance);
        await middleware.ProcessRequestAsync(context, ODataRequest.Parse(context));
        return context;
    }

    private static async Task WaitForCompletion(IndexIntegrityFileStore files, Guid checkId, Guid runId)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var run = files.Read(checkId, runId);
        while (run?.Active == true)
        {
            await Task.Delay(10, timeout.Token);
            run = files.Read(checkId, runId);
        }
        Assert.AreEqual("completed", run?.Status, run?.Error);
    }

    private static string ResponseText(HttpContext context) =>
        Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());

    private static JsonElement Body(HttpContext context)
    {
        using var doc = JsonDocument.Parse(ResponseText(context));
        return doc.RootElement.Clone();
    }

    private sealed class ResponseFeature : HttpResponseFeature
    {
        public bool Started { get; set; }
        public override bool HasStarted => Started;
    }

    private sealed class ResponseBodyFeature(Stream stream, ResponseFeature response) : StreamResponseBodyFeature(stream)
    {
        public override Task StartAsync(CancellationToken cancellationToken = default)
        {
            response.Started = true;
            return base.StartAsync(cancellationToken);
        }
    }

    private sealed class WebEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "IntegrityTests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeProvider : IIndexIntegrityProvider
    {
        public string ProviderId { get; set; } = "fake";
        public int RepairFormatVersion => 1;
        public IndexIntegrityCapabilities GetCapabilities() => new(true, true, "test-index");

        public System.Threading.Tasks.Task<IndexIntegrityCheckResult> CheckAsync(IndexIntegrityWork work,
            Action<Difference> report, CancellationToken cancellationToken)
        {
            var utc = DateTimeOffset.UtcNow;
            report(new(DifferenceKind.NotInIndex, 42, NodeId: 21)
            {
                Database = new DatabaseVersion(42, 17, 21, 9_007_199_254_740_993, 42, 42, utc.UtcDateTime, utc.UtcDateTime)
            });
            var phase = new IndexIntegrityPhase(utc, utc, 0, utc);
            return Task.FromResult(new IndexIntegrityCheckResult(new(1, 0, 0), 1, 2, 3, 2, [],
                phase, phase, phase));
        }

        public Task RepairAsync(IndexIntegrityWork work, IReadOnlyList<IndexIntegritySelection> selection,
            Action<IndexIntegrityRepairItem> report, CancellationToken cancellationToken)
        {
            var item = selection.Single();
            report(new(item.DifferenceId, item.VersionId, "repaired"));
            return Task.CompletedTask;
        }
    }
}
