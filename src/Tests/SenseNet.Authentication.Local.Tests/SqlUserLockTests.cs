using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SnWebApplication.Api.Sql.LocalAuth;

namespace SenseNet.Authentication.Local.Tests;

[TestClass]
public class SqlUserLockTests
{
    [TestMethod]
    public async Task IndependentSqlConnectionsSerializeTheSameUserAndReleaseOnDisposal()
    {
        var connection = Environment.GetEnvironmentVariable("SB167_TEST_SQL");
        if (string.IsNullOrEmpty(connection)) Assert.Inconclusive("Set SB167_TEST_SQL to a disposable SQL database.");
        var config = Options.Create(new ConnectionStringOptions { Repository = connection });
        var first = new SqlLocalAuthenticationUserLock(config);
        var second = new SqlLocalAuthenticationUserLock(config);
        await using (var held = await first.TryAcquireAsync(123, CancellationToken.None))
        {
            Assert.IsNotNull(held);
            Assert.IsNull(await second.TryAcquireAsync(123, CancellationToken.None));
            await using var otherUser = await second.TryAcquireAsync(456, CancellationToken.None);
            Assert.IsNotNull(otherUser);
        }
        await using var again = await second.TryAcquireAsync(123, CancellationToken.None);
        Assert.IsNotNull(again);
    }
}
