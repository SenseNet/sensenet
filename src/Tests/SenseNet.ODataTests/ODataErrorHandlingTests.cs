using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.OData;
using SenseNet.ODataTests.Accessors;
using SenseNet.ODataTests.Responses;
using SenseNet.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests;

[TestClass]
public class ODataErrorHandlingTests : ODataTestBase
{
    private class TestEventLogger : IEventLogger
    {
        public List<string> Entries { get; } = new();
        public void Write(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title,
            IDictionary<string, object> properties)
        {
            Entries.Add($"{severity}: {message}");
        }
    }
    private class TestSnTracer : ISnTracer
    {
        public List<string> Lines { get; } = new List<string>();
        public void Write(string line) { Lines.Add(line); }
        public void Flush() { /* do nothing */ }
    }

    [ODataAction]
    [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators, N.R.Developers, N.R.Visitor)]
    public static object ErrorHandling_NoError(Content content, string a)
    {
        return new {response = MethodBase.GetCurrentMethod().Name + "-" + a};
    }
    [ODataAction]
    [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators, N.R.Developers)]
    public static object ErrorHandling_OperationNotAccessible(Content content, string a)
    {
        return new { response = MethodBase.GetCurrentMethod().Name + "-" + a };
    }
    [ODataAction]
    [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators, N.R.Developers, N.R.Visitor)]
    public static object ErrorHandling_NotSupportedException(Content content, string a)
    {
        throw new NotSupportedException("not-supported-message");
    }
    [ODataAction]
    [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators, N.R.Developers, N.R.Visitor)]
    public static object ErrorHandling_AccessDeniedException(Content content, string a)
    {
        throw new AccessDeniedException("message", "path", 42, User.Current,
            new[] { PermissionType.Custom01 });
    }
    [ODataAction]
    [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators, N.R.Developers, N.R.Visitor)]
    public static object ErrorHandling_SenseNetSecurityException(Content content, string a)
    {
        throw new SenseNetSecurityException("message", new NotSupportedException("message2"));
    }

    [TestMethod]
    public async Task OD_ErrorHandling_NoError()
    {
        await ODataTestAsync(async () =>
        {
            ODataResponse response;
            var paramValue = "magicValue";

            // ACTION-1: admin
            using (new CurrentUserBlock(User.Administrator))
                response = await ODataPostAsync($"/OData.svc/('Root')/{nameof(ErrorHandling_NoError)}", "",
                    $"{{a:\"{paramValue}\"}}").ConfigureAwait(false);
            // ASSERT-1
            AssertNoError(response);
            Assert.AreEqual($"{{\r\n  \"response\": \"{nameof(ErrorHandling_NoError)}-{paramValue}\"\r\n}}", response.Result);

            // ACTION-2: visitor
            using (new CurrentUserBlock(User.Visitor))
                response = await ODataPostAsync($"/OData.svc/('Root')/{nameof(ErrorHandling_NoError)}", "",
                    $"{{a:\"{paramValue}\"}}").ConfigureAwait(false);
            // ASSERT-2
            AssertNoError(response);
            Assert.AreEqual($"{{\r\n  \"response\": \"{nameof(ErrorHandling_NoError)}-{paramValue}\"\r\n}}", response.Result);
        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task OD_ErrorHandling_OperationNotAccessible()
    {
        await ODataTestAsync(async () =>
        {
            ODataResponse response;

            // ACTION
            using (new CurrentUserBlock(User.Visitor))
                response = await ODataPostAsync($"/OData.svc/('Root')/{nameof(ErrorHandling_OperationNotAccessible)}", "",
                    $"{{a:\"magicValue\"}}").ConfigureAwait(false);

            // ASSERT
            var error = GetError(response, false);
            //UNDONE:ErrorHandling: ?? Simple "access denied" instead (see OD_ErrorHandling_MethodThrows_AccessDeniedException)
            Assert.AreEqual($"Operation not accessible: {nameof(ErrorHandling_OperationNotAccessible)}(a)", error.Message);
        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task OD_ErrorHandling_InvisibleContent()
    {
        await ODataTestAsync(async () =>
        {
            ODataResponse response;

            await ErrorHandlingTest(async (logger, tracer) =>
            {
                // ACTION
                using (new CurrentUserBlock(User.Visitor))
                    response = await ODataPostAsync($"/OData.svc/Root/('IMS')/{nameof(ErrorHandling_OperationNotAccessible)}", "",
                        $"{{a:\"magicValue\"}}").ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual(404, response.StatusCode);
                Assert.AreEqual(string.Empty, response.Result);
                Assert.IsFalse(logger.Entries.Any(x => x.StartsWith("Error:")));
                Assert.IsTrue(tracer.Lines.Any(x => x.Contains("Access denied. Path: /Root/IMS ")));
            }).ConfigureAwait(false);

            var user = new User(Node.LoadNode("/Root/IMS/BuiltIn/Portal"))
            {
                Name = "User-1",
                Enabled = true,
                Email = "user1@example.com"
            };
            user.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            await ErrorHandlingTest(async (logger, tracer) =>
            {
                // ACTION
                using (new CurrentUserBlock(user))
                    response = await ODataPostAsync($"/OData.svc/Root/('IMS')/{nameof(ErrorHandling_OperationNotAccessible)}", "",
                        $"{{a:\"magicValue\"}}").ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual(404, response.StatusCode);
                Assert.AreEqual(string.Empty, response.Result);
                Assert.IsFalse(logger.Entries.Any(x => x.StartsWith("Error:")));
                Assert.IsTrue(tracer.Lines.Any(x => x.Contains("Access denied. Path: /Root/IMS ")));
            }).ConfigureAwait(false);

            Providers.Instance.SecurityHandler.CreateAclEditor()
                .Allow((await NodeHead.GetAsync("/Root/IMS", CancellationToken.None)).Id, user.Id, false,
                    PermissionType.See)
                .Apply();

            await ErrorHandlingTest(async (logger, tracer) =>
            {
                // ACTION
                using (new CurrentUserBlock(user))
                    response = await ODataPostAsync($"/OData.svc/Root/('IMS')/{nameof(ErrorHandling_OperationNotAccessible)}", "",
                        $"{{a:\"magicValue\"}}").ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual(403, response.StatusCode);
                var error = GetError(response);
                Assert.IsTrue(error.Message.Contains("Operation not accessible:"));
                Assert.IsFalse(logger.Entries.Any(x => x.StartsWith("Error:")), "Unexpected log entry.");
                Assert.IsFalse(logger.Entries.Any(x => x.StartsWith("Warning:")), "Unexpected log entry.");
                Assert.IsTrue(tracer.Lines.Any(x => x.Contains("Operation not accessible:")), "Missing trace line");
            }).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OD_ErrorHandling_MethodThrows_NotSupportedException()
    {
        async Task TestWorker(IUser user, TestEventLogger logger, TestSnTracer tracer)
        {
            ODataResponse response;
            // ACTION
            using (new CurrentUserBlock(user))
                response = await ODataPostAsync(
                    $"/OData.svc/('Root')/{nameof(ErrorHandling_NotSupportedException)}", "",
                    $"{{a:\"magicValue\"}}").ConfigureAwait(false);
            // ASSERT
            var message = "not-supported-message";
            Assert.AreEqual(500, response.StatusCode);
            var error = GetError(response, false);
            Assert.AreEqual(ODataExceptionCode.NotSpecified, error.Code);
            Assert.AreEqual(nameof(NotSupportedException), error.ExceptionType);
            Assert.AreEqual(message, error.Message);
            Assert.IsTrue(logger.Entries.Any(x => x.StartsWith("Error:")), "Missing log entry");
            Assert.IsTrue(logger.Entries.Any(x => x.StartsWith("Error: " + message)), "Wrong log message");
            Assert.IsTrue(tracer.Lines.Any(x => x.Contains("\tERROR ")), "Missing trace line");
            Assert.IsTrue(tracer.Lines.Any(x => x.Contains(message)), "Wrong trace message");
        }
        await ODataTestAsync(async () =>
        {
            await ErrorHandlingTest((logger, tracer) => TestWorker(User.Administrator, logger, tracer)).ConfigureAwait(false);
            await ErrorHandlingTest((logger, tracer) => TestWorker(User.Visitor, logger, tracer)).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task OD_ErrorHandling_MethodThrows_AccessDeniedException()
    {
        async Task TestWorker(IUser user, TestEventLogger logger, TestSnTracer tracer)
        {
            ODataResponse response;
            // ACTION
            using (new CurrentUserBlock(User.Administrator))
                response = await ODataPostAsync($"/OData.svc/('Root')/{nameof(ErrorHandling_AccessDeniedException)}", "",
                    $"{{a:\"magicValue\"}}").ConfigureAwait(false);
            // ASSERT
            var message = "message Path: path EntityId: 42 UserId: 1 PermissionTypes: Custom01";
            Assert.AreEqual(403, response.StatusCode);
            var error = GetError(response, false);
            Assert.AreEqual(ODataExceptionCode.Forbidden, error.Code);
            //Assert.AreEqual(nameof(AccessDeniedException), error.ExceptionType);
            Assert.AreEqual(nameof(ODataException), error.ExceptionType);
            Assert.AreEqual("Access denied.", error.Message);
            Assert.IsFalse(logger.Entries.Any(x => x.StartsWith("Error:")), "Unexpected log entry");
            Assert.IsTrue(tracer.Lines.Any(x => x.Contains("\tERROR")), "Missing trace line");
            Assert.IsTrue(tracer.Lines.Any(x => x.Contains(message)), "Wrong trace message");
        }

        await ODataTestAsync(async () =>
        {
            await ErrorHandlingTest((logger, tracer) => TestWorker(User.Administrator, logger, tracer)).ConfigureAwait(false);
            await ErrorHandlingTest((logger, tracer) => TestWorker(User.Visitor, logger, tracer)).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task OD_ErrorHandling_MethodThrows_SenseNetSecurityException()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> {{"ASPNETCORE_ENVIRONMENT", "Development"}})
            .Build();

        async Task TestWorker(IUser user, TestEventLogger logger, TestSnTracer tracer)
        {
            ODataResponse response;
            // ACTION
            using (new CurrentUserBlock(User.Administrator))
                response = await ODataPostAsync($"/OData.svc/('Root')/{nameof(ErrorHandling_SenseNetSecurityException)}", "",
                    $"{{a:\"magicValue\"}}").ConfigureAwait(false);
            // ASSERT
            var message = "message";
            Assert.AreEqual(403, response.StatusCode);
            var error = GetError(response, false);
            Assert.AreEqual(ODataExceptionCode.Forbidden, error.Code);
            Assert.AreEqual(nameof(SenseNetSecurityException), error.ExceptionType);
            Assert.AreEqual(message, error.Message);
            Assert.IsFalse(logger.Entries.Any(x => x.StartsWith("Error:")), "Unexpected log entry");
            Assert.IsTrue(tracer.Lines.Any(x => x.Contains("\tERROR")), "Missing trace line");
            Assert.IsTrue(tracer.Lines.Any(x => x.Contains(message)), "Wrong trace message");
        }

        await ODataTestAsync(async () =>
        {
            await ErrorHandlingTest((logger, tracer) => TestWorker(User.Administrator, logger, tracer)).ConfigureAwait(false);
            await ErrorHandlingTest((logger, tracer) => TestWorker(User.Visitor, logger, tracer)).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OD_ErrorHandling_StackTrace()
    {
        async Task TestWorker(IUser user, IConfiguration config, bool expectedStackTraceExistence, string failedMessage)
        {
            ODataResponse response;
            // ACTION
            using (new CurrentUserBlock(user))
                response = await ODataPostAsync($"/OData.svc/('Root')/{nameof(ErrorHandling_NotSupportedException)}", "",
                    $"{{a:\"magicValue\"}}", null, config).ConfigureAwait(false);
            // ASSERT
            var error = GetError(response, false);
            var isStackTraceExist = !string.IsNullOrEmpty(error.StackTrace);
            Assert.AreEqual(expectedStackTraceExistence, isStackTraceExist, failedMessage);
        }

        IConfiguration development = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "ASPNETCORE_ENVIRONMENT", Environments.Development } })
            .Build();
        IConfiguration production = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "ASPNETCORE_ENVIRONMENT", Environments.Production } })
            .Build();
        IConfiguration staging = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "ASPNETCORE_ENVIRONMENT", Environments.Staging } })
            .Build();

        await ODataTestAsync(async () =>
        {
            var user = User.Administrator;
            await ErrorHandlingTest((logger, tracer) => TestWorker(user, development, true, "config: development")).ConfigureAwait(false);
            await ErrorHandlingTest((logger, tracer) => TestWorker(user, staging, false, "config: staging")).ConfigureAwait(false);
            await ErrorHandlingTest((logger, tracer) => TestWorker(user, production, false, "config: production")).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private async Task ErrorHandlingTest(Func<TestEventLogger, TestSnTracer, Task> callback)
    {
        using (new Tests.Core.Tools.LoggerSwindler<TestEventLogger>())
        {
            var logger = (TestEventLogger)SnLog.Instance;
            using (new Swindler<bool>(true,
                       () => SnTrace.Security.Enabled,
                       value => { SnTrace.Security.Enabled = value; }))
            {
                var tracer = new TestSnTracer();
                try
                {
                    SnTrace.SnTracers.Add(tracer);
                    tracer.Lines.Clear();
                    await callback(logger, tracer);
                }
                finally
                {
                    SnTrace.SnTracers.Remove(tracer);
                }
            }
        }
    }

}