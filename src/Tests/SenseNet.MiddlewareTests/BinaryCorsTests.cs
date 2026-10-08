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
        [DataRow("https://one.example.com", false, 200, false)]
        [DataRow("https://two.example.com", true, 200, false)]
        [DataRow("https://denied.example.net", false, 200, false)]
        [DataRow(null, false, 200, false)]
        [DataRow("https://one.example.com", false, 304, false)]
        [DataRow("https://two.example.com", true, 304, false)]
        [DataRow(null, true, 304, false)]
        [DataRow("https://one.example.com", false, 200, true)]
        [DataRow("https://two.example.com", true, 200, true)]
        [DataRow("https://denied.example.net", false, 200, true)]
        [DataRow(null, false, 200, true)]
        [DataRow("https://one.example.com", false, 304, true)]
        [DataRow("https://two.example.com", true, 304, true)]
        [DataRow(null, true, 304, true)]
        public async Task BinaryCors_ResponseAndLogging(string origin, bool directPath, int statusCode,
            bool frameworkCors)
        {
            await Test(async () =>
            {
                var file = await PrepareFile();
                var logger = new BinaryLogger();
                using var server = CreateServer(logger, frameworkCors);
                using var client = server.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    directPath ? file.Path : $"/binaryhandler.ashx?nodeid={file.Id}&unused=secret-query");
                if (origin != null)
                    request.Headers.Add("Origin", origin);
                request.Headers.Add("Cookie", "secret-request-cookie");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "secret-token");
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
                Assert.IsTrue(response.Headers.Vary.Contains("Accept-Encoding"));
                Assert.IsFalse(response.Headers.Vary.Contains("Origin"), "The scoped fix must not add global cache variation.");
                Assert.AreEqual(statusCode == 200 ? "binary body" : "", await response.Content.ReadAsStringAsync());

                await logger.Completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
                var entries = logger.Entries.ToArray();
                Assert.AreEqual(1, entries.Count(entry => entry.Level == LogLevel.Information));
                Assert.AreEqual(1, entries.Count(entry => entry.Level == LogLevel.Trace));
                Assert.IsFalse(entries.Any(entry => entry.Level >= LogLevel.Warning));
                Assert.IsFalse(entries.Any(entry => entry.Message.Contains("secret-")));
                var summary = entries.Single(entry => entry.Id.Name == "BinaryRequestCompleted");
                Assert.AreEqual(statusCode, summary.State["StatusCode"]);
                Assert.AreEqual(origin == null ? "NoOrigin" : allowed ? "AllowOriginPresent" : "AllowOriginAbsent", summary.State["CorsOutcome"]);
            });
        }

        [DataTestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task BinaryCors_OriginalWildcardAndExposedHeaders(bool allowAnyOrigin, bool credentials)
        {
            await Test(async () =>
            {
                var file = await PrepareFile();
                var policy = new CorsPolicy { SupportsCredentials = credentials };
                policy.Origins.Add(allowAnyOrigin ? "*" : "https://one.example.com");
                policy.ExposedHeaders.Add("Content-Disposition");
                var logger = new BinaryLogger(LogLevel.Information);
                using var server = CreateServer(logger, policy: policy);
                using var client = server.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/binaryhandler.ashx?nodeid={file.Id}");
                request.Headers.Add("Origin", "https://one.example.com");
                using var response = await client.SendAsync(request);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(allowAnyOrigin && !credentials ? "*" : "https://one.example.com",
                    response.Headers.GetValues("Access-Control-Allow-Origin").Single());
                Assert.AreEqual(credentials, response.Headers.Contains("Access-Control-Allow-Credentials"));
                Assert.AreEqual("Content-Disposition", response.Headers.GetValues("Access-Control-Expose-Headers").Single());
                await response.Content.ReadAsStringAsync();
                await logger.Completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.AreEqual(1, logger.Entries.Count, "Trace diagnostics must be opt-in.");
            });
        }

        [DataTestMethod]
        [DataRow("https://one.example.com", true)]
        [DataRow("https://denied.example.net", false)]
        public async Task BinaryCors_FrameworkPreflightDoesNotEnterBinaryBranch(string origin, bool allowed)
        {
            await Test(async () =>
            {
                await PrepareFile();
                var logger = new BinaryLogger();
                using var server = CreateServer(logger, frameworkCors: true, failIfBinaryEntered: true);
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
        public async Task BinaryCors_DisabledLoggingKeepsFallbackHeaders()
        {
            await Test(async () =>
            {
                var file = await PrepareFile();
                var logger = new BinaryLogger(LogLevel.Warning);
                using var server = CreateServer(logger);
                using var client = server.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/binaryhandler.ashx?nodeid={file.Id}");
                request.Headers.Add("Origin", "https://one.example.com");
                using var response = await client.SendAsync(request);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual("https://one.example.com", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
                Assert.AreEqual("binary body", await response.Content.ReadAsStringAsync());
                Assert.AreEqual(0, logger.Entries.Count);
            });
        }

        [TestMethod]
        public void BinaryCors_PrePrConstructorRemainsAvailable()
        {
            var constructor = typeof(BinaryMiddleware).GetConstructor(new[] { typeof(RequestDelegate) });
            Assert.IsNotNull(constructor, "Previously compiled clients require the exact one-argument constructor.");
            Assert.IsNotNull(constructor.Invoke(new object[] { null }));
            using var services = new ServiceCollection().BuildServiceProvider();
            Assert.IsNotNull(ActivatorUtilities.CreateInstance<BinaryMiddleware>(services,
                new RequestDelegate(_ => Task.CompletedTask)));

            var logger = new BinaryLogger();
            var policyProvider = new FixedPolicyProvider(new CorsPolicy());
            using var configuredServices = new ServiceCollection()
                .AddSingleton<ILogger<BinaryMiddleware>>(logger)
                .AddSingleton<ICorsPolicyProvider>(policyProvider)
                .BuildServiceProvider();
            var middleware = ActivatorUtilities.CreateInstance<BinaryMiddleware>(configuredServices,
                new RequestDelegate(_ => Task.CompletedTask));
            var fields = typeof(BinaryMiddleware).GetFields(System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.AreSame(logger, fields.Single(field => field.Name == "_logger").GetValue(middleware));
            Assert.AreSame(policyProvider, fields.Single(field => field.Name == "_corsPolicyProvider").GetValue(middleware));
        }

        private static TestServer CreateServer(BinaryLogger logger, bool frameworkCors = false,
            bool failIfBinaryEntered = false, CorsPolicy policy = null)
        {
            return new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    if (policy == null)
                        services.AddSenseNetCors();
                    else
                        services.AddSingleton<ICorsPolicyProvider>(new FixedPolicyProvider(policy));
                    services.AddSingleton<ILogger<BinaryMiddleware>>(logger);
                    services.AddSingleton(new WebTransferRegistrator(null));
                })
                .Configure(app =>
                {
                    if (frameworkCors)
                        app.UseSenseNetCors();
                    app.Use((context, next) =>
                    {
                        User.Current = User.Administrator;
                        context.Response.Headers.Append("Vary", "Accept-Encoding");
                        context.Response.Headers.Append("Set-Cookie", "secret-response-cookie");
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

        private sealed class FixedPolicyProvider : ICorsPolicyProvider
        {
            private readonly CorsPolicy _policy;
            public FixedPolicyProvider(CorsPolicy policy) => _policy = policy;
            public Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
            {
                Assert.AreEqual("sensenet", policyName);
                return Task.FromResult(_policy);
            }
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
