using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests;

[TestClass]
public class ODataPasswordValidationTests : ODataTestBase
{
    [TestMethod]
    public Task AnonymousPasswordValidationIsLimitedWithoutLocalAuthentication() => ODataTestAsync(async () =>
    {
        await Providers.Instance.SecurityHandler.CreateAclEditor()
            .Allow(Repository.Root.Id, User.Visitor.Id, false, PermissionType.See, PermissionType.Open)
            .ApplyAsync(CancellationToken.None);
        using var caller = new CurrentUserBlock(User.Visitor);
        const string path = "/OData.svc/('Root')/ValidateCredentials";
        for (var i = 0; i < 5; i++)
        {
            var response = await ODataPostAsync(path, "", "{\"userName\":\"builtin\\\\admin\",\"password\":\"wrong\"}");
            Assert.AreEqual(403, response.StatusCode, response.Result);
        }

        var limited = await ODataProcessRequestEmptyResponseAsync(path, "",
            "{\"userName\":\"BuiltIn\\\\Admin\",\"password\":\"admin\"}", "POST", null);
        Assert.AreEqual(429, limited.Response.StatusCode);
        Assert.IsTrue(int.TryParse(limited.Response.Headers.RetryAfter, out var seconds) && seconds is > 0 and <= 60);
        Assert.AreEqual("no-store", limited.Response.Headers.CacheControl.ToString());
    });
}
