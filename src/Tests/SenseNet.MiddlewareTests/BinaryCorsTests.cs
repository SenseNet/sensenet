using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Portal;
using SenseNet.Services.Core.Diagnostics;
using SenseNet.Services.Core.Virtualization;
using SenseNet.Tests.Core;
using File = SenseNet.ContentRepository.File;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.MiddlewareTests
{
    [TestClass]
    public class BinaryCorsTests : TestBase
    {
        [DataTestMethod]
        [DataRow("https://one.example.com", false, 200)]
        [DataRow("https://two.example.com", true, 200)]
        [DataRow("https://denied.example.net", false, 200)]
        [DataRow(null, false, 200)]
        [DataRow("https://one.example.com", false, 304)]
        [DataRow("https://two.example.com", true, 304)]
        [DataRow(null, true, 304)]
        public async Task BinaryCors_ResponseAndLogging(string origin, bool directPath, int statusCode)
        {
            await Test(async () =>
            {
                var file = await PrepareFile();
                var logger = new BinaryLogger();
                using var server = CreateServer(logger);
                using var client = server.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    directPath ? file.Path : $"/binaryhandler.ashx?nodeid={file.Id}");
                if (origin != null)
                    request.Headers.Add("Origin", origin);
                if (statusCode == 304)
                    request.Headers.IfModifiedSince = DateTimeOffset.UtcNow.AddMinutes(1);

                using var response = await client.SendAsync(request);
                Assert.AreEqual(statusCode, (int)response.StatusCode);
                var allowed = origin != null && origin.EndsWith(".example.com", StringComparison.Ordinal);
                Assert.AreEqual(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
                if (allowed)
                {
                    CollectionAssert.AreEqual(new[] { origin }, response.Headers.GetValues("Access-Control-Allow-Origin").ToArray());
                    Assert.AreEqual("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
                }
                Assert.AreEqual(1, response.Headers.Vary.Count(value => value.Equals("Origin", StringComparison.OrdinalIgnoreCase)));
                Assert.IsTrue(response.Headers.Vary.Contains("Accept-Encoding"));
                Assert.AreEqual(statusCode == 200 ? "binary body" : "", await response.Content.ReadAsStringAsync());

                await logger.Completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
                var entries = logger.Entries.ToArray();
                Assert.AreEqual(1, entries.Count(entry => entry.Level == LogLevel.Information));
                Assert.AreEqual(1, entries.Count(entry => entry.Level == LogLevel.Trace));
                Assert.IsFalse(entries.Any(entry => entry.Level >= LogLevel.Warning));
                Assert.IsFalse(entries.Any(entry => entry.Message.Contains("secret-cookie")));
                var summary = entries.Single(entry => entry.Id.Name == "BinaryRequestCompleted");
                Assert.AreEqual(statusCode, summary.State["StatusCode"]);
                Assert.AreEqual(origin == null ? "NoOrigin" : allowed ? "AllowOriginPresent" : "AllowOriginAbsent", summary.State["CorsOutcome"]);
            });
        }

        [DataTestMethod]
        [DataRow("https://one.example.com", true)]
        [DataRow("https://denied.example.net", false)]
        public async Task BinaryCors_PreflightDoesNotEnterBinaryBranch(string origin, bool allowed)
        {
            await Test(async () =>
            {
                await PrepareFile();
                var logger = new BinaryLogger();
                using var server = CreateServer(logger, failIfBinaryEntered: true);
                using var client = server.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Options, "/binaryhandler.ashx");
                request.Headers.Add("Origin", origin);
                request.Headers.Add("Access-Control-Request-Method", "GET");
                request.Headers.Add("Access-Control-Request-Headers", "Authorization");
                using var response = await client.SendAsync(request);
                Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
                Assert.AreEqual(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
                if (allowed)
                {
                    Assert.IsTrue(response.Headers.GetValues("Access-Control-Allow-Methods").Any(value => value.Contains("GET")));
                    Assert.IsTrue(response.Headers.GetValues("Access-Control-Allow-Headers").Any(value => value.Contains("Authorization")));
                }
                Assert.AreEqual(0, logger.Entries.Count);
            });
        }

        [TestMethod]
        public async Task BinaryCors_CustomOriginPredicateIsHonored()
        {
            await Test(async () =>
            {
                var file = await PrepareFile();
                var logger = new BinaryLogger(LogLevel.Information);
                using var server = CreateServer(logger, customPolicy: true);
                using var client = server.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/binaryhandler.ashx?nodeid={file.Id}");
                request.Headers.Add("Origin", "https://custom.example.net");
                using var response = await client.SendAsync(request);
                Assert.AreEqual("https://custom.example.net", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
                Assert.AreEqual(1, response.Headers.Vary.Count(value => value == "Origin"));
                await logger.Completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.AreEqual(1, logger.Entries.Count);
            });
        }

        private static TestServer CreateServer(BinaryLogger logger, bool failIfBinaryEntered = false, bool customPolicy = false)
        {
            return new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    if (customPolicy)
                        services.AddCors(options => options.AddPolicy("sensenet", policy => policy
                            .SetIsOriginAllowed(origin => origin == "https://custom.example.net")
                            .AllowCredentials()));
                    else
                        services.AddSenseNetCors();
                    services.AddSingleton<ILogger<BinaryMiddleware>>(logger);
                    services.AddSingleton(new WebTransferRegistrator(null));
                })
                .Configure(app =>
                {
                    app.UseSenseNetCors();
                    app.Use((context, next) =>
                    {
                        User.Current = User.Administrator;
                        context.Response.Headers.Append("Vary", "Accept-Encoding");
                        context.Response.Headers.Append("Set-Cookie", "secret-cookie");
                        return next();
                    });
                    app.UseSenseNetFiles(buildAppBranchBefore: branch =>
                    {
                        if (failIfBinaryEntered)
                            branch.Run(_ => throw new AssertFailedException("Preflight reached binary processing."));
                    });
                }));
        }

        private static async Task<File> PrepareFile()
        {
            var settings = await Node.LoadAsync<Settings>("/Root/System/Settings/Portal.settings", CancellationToken.None);
            var json = JObject.Parse(RepositoryTools.GetStreamString(settings.Binary.GetStream()));
            json[PortalSettings.SETTINGS_ALLOWEDORIGINDOMAINS] = new JArray("*.example.com");
            json[PortalSettings.SETTINGS_CACHEHEADERS] = new JArray(new JObject { ["Extension"] = "txt", ["MaxAge"] = 60 });
            settings.Binary.SetStream(RepositoryTools.GetStreamFromString(json.ToString()));
            await settings.SaveAsync(CancellationToken.None);
            var folder = new SystemFolder(Repository.Root) { Name = "CorsTests" };
            await folder.SaveAsync(CancellationToken.None);
            var file = new File(folder) { Name = "cors-test.txt" };
            file.Binary.SetStream(RepositoryTools.GetStreamFromString("binary body"));
            await file.SaveAsync(CancellationToken.None);
            return file;
        }

        private sealed class BinaryLogger : ILogger<BinaryMiddleware>
        {
            private readonly LogLevel _minimum;
            public readonly ConcurrentQueue<(LogLevel Level, EventId Id, string Message, Dictionary<string, object> State)> Entries = new();
            public readonly TaskCompletionSource<bool> Completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public BinaryLogger(LogLevel minimum = LogLevel.Trace) => _minimum = minimum;
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimum;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(level)) return;
                Entries.Enqueue((level, id, formatter(state, exception),
                    ((IEnumerable<KeyValuePair<string, object>>)state).ToDictionary(pair => pair.Key, pair => pair.Value)));
                if (id.Name == "BinaryCorsResponse" || !IsEnabled(LogLevel.Trace))
                    Completed.TrySetResult(true);
            }
        }
    }
}
