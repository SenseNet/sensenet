using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.Tests.Core;

namespace SenseNet.IntegrationTests.CustomTests
{
    [TestClass]
    public class MsSqlDataProviderTests : TestBase
    {
        private MsSqlDataProvider CreateDataProvider()
        {
            var connOptions = Options.Create(ConnectionStringOptions.GetLegacyConnectionStrings());
            var dbInstallerOptions = Options.Create(new MsSqlDatabaseInstallationOptions());
            return new MsSqlDataProvider(Options.Create(DataOptions.GetLegacyConfiguration()), connOptions,
                dbInstallerOptions,
                new MsSqlDatabaseInstaller(dbInstallerOptions,
                    NullLoggerFactory.Instance.CreateLogger<MsSqlDatabaseInstaller>()),
                new MsSqlDataInstaller(connOptions, NullLoggerFactory.Instance.CreateLogger<MsSqlDataInstaller>()),
                NullLoggerFactory.Instance.CreateLogger<MsSqlDataProvider>());
        }

        [TestMethod]
        public async Task MsSqlDataProvider_ShortText_Escape()
        {
            await Test(() =>
            {
                var dp = CreateDataProvider();

                var properties = new PropertyType[]
                {
                    PropertyType.GetByName("Domain"),
                    PropertyType.GetByName("FullName"),
                    PropertyType.GetByName("Email"),
                    PropertyType.GetByName("LoginName"),
                };
                var inputValues = new[]
                {
                    "Domain1",
                    "LastName\tFirstName",
                    "a@b.c \\ d@e.f",
                    "asdf\\\r\nqwer",
                };
                var data = new Dictionary<PropertyType, object>();
                for (int i = 0; i < inputValues.Length; i++)
                {
                    data.Add(properties[i], inputValues[i]);
                }

                // ACTION
                var serialized = dp.SerializeDynamicProperties(data);
                var deserialized = dp.DeserializeDynamicProperties(serialized);

                // ASSERT
                var keys = deserialized.Keys.ToArray();
                var values = deserialized.Values.ToArray();
                for (int i = 0; i < inputValues.Length; i++)
                {
                    Assert.AreEqual(properties[i], keys[i]);
                    Assert.AreEqual(inputValues[i], values[i]);
                }

                return Task.CompletedTask;
            });
        }
    }
}
