using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.Testing;
using SenseNet.Tests.Core;

namespace SenseNet.IntegrationTests.MsSql.CustomTests
{
    [TestClass]
    public class MsSqlDataProviderTests : TestBase
    {
        private string __connectionString;
        private string GetConnectionString()
        {
            if (__connectionString == null)
            {
                var builder = new ConfigurationBuilder();
                builder.AddJsonFile("appsettings.json");
                builder.AddUserSecrets("86cf56af-3ef2-46f4-afba-503609b83378");
                var appConfig = builder.Build(); ;
                __connectionString = appConfig.GetConnectionString("SnCrMsSql");
            }
            return __connectionString;
        }

        private MsSqlDataProvider CreateDataProvider()
        {
            var connectionString = GetConnectionString();
            var connOptions = Options.Create(new ConnectionStringOptions{Repository = connectionString});
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
            await Test(builder =>
                {
                    //UNDONE:CNSTR: Avoid connectionString problems in any other tests in this assembly.
                    var cnstr = GetConnectionString();
                    var builtInBlobProvider = (BuiltInBlobProvider) Providers.Instance.BlobProviders.BuiltInBlobProvider;
                    var builtInBlobProviderAcc = new ObjectAccessor(builtInBlobProvider);
                    builtInBlobProviderAcc.SetField("_connectionString", cnstr);
                },
                () =>
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
