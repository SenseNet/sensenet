using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataProviderExtensionTests : TestBase
    {
        [TestMethod]
        public void DataProviderExtension_CallingInterfaceMethod()
        {
            // ARRANGE
            var dp = new InMemoryTestingDataProvider();
            var builder = CreateRepositoryBuilderForTest();
            builder.UseTestingDataProviderExtension(dp);
            dp.DB.LogEntries.AddRange(new[]
            {
                new InMemoryDataProvider.LogEntriesRow{Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-2.1d)},
                new InMemoryDataProvider.LogEntriesRow{Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-2.1d)},
                new InMemoryDataProvider.LogEntriesRow{Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-1.1d)},
                new InMemoryDataProvider.LogEntriesRow{Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-1.1d)},
                new InMemoryDataProvider.LogEntriesRow{Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-0.1d)},
                new InMemoryDataProvider.LogEntriesRow{Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-0.1d)},
            });

            var testingDataProvider = DataProvider.GetExtension<ITestingDataProviderExtension>();
            testingDataProvider.InitializeForTests();

            // ACTION
            // Call an interface method
            var actual = testingDataProvider.GetPermissionLogEntriesCountAfterMoment(DateTime.UtcNow.AddDays(-2));

            // ASSERT
            Assert.AreEqual(2, actual);
        }
        [TestMethod]
        public void DataProviderExtension_CallingNotInterfaceMethod()
        {
            var builder = CreateRepositoryBuilderForTest();
            builder.UseTestingDataProviderExtension(new InMemoryTestingDataProvider());

            // ACTION
            // Call a not interface method
            var actual = ((InMemoryTestingDataProvider)DataProvider.GetExtension<ITestingDataProviderExtension>())
                .TestMethodThatIsNotInterfaceMember("asdf");

            // ASSERT
            Assert.AreEqual("asdfasdf", actual);
        }

        [TestMethod]
        public void DataProviderExtension_CustomScript()
        {
            var builder = CreateRepositoryBuilderForTest();
            const string paramName = "Param1";
            const int paramValue = 42;

            // ACTION
            var proc = DataProvider.Instance.CreateDataProcedure(InMemoryDataProvider.MagicCommandText)
                .AddParameter(paramName, paramValue);

            // ASSERT
            Assert.AreEqual(1, proc.Parameters.Count);
            var paramByIndex = proc.Parameters[0];
            var paramByName = proc.Parameters[0];
            Assert.AreSame(paramByName, paramByIndex);
        }

        [TestMethod]
        public void DataProviderExtension_CustomScriptWithParameters()
        {
            CreateRepositoryBuilderForTest();

            // ACTION
            var proc = DataProvider.Instance.CreateDataProcedure(InMemoryDataProvider.MagicCommandText)
                .AddParameter("Param0", 42, DbType.Int64)
                .AddParameter("Param1", "asdf", DbType.AnsiString);

            // ASSERT
            var prms = proc.Parameters;
            Assert.AreEqual(2, prms.Count);

            var prm = prms[0];
            Assert.AreEqual("Param0", prm.ParameterName);
            Assert.AreEqual(DbType.Int64, prm.DbType);
            Assert.AreEqual(42, prm.Value);
            Assert.AreEqual(0, prm.Size);

            prm = prms[1];
            Assert.AreEqual("Param1", prm.ParameterName);
            Assert.AreEqual(DbType.AnsiString, prm.DbType);
            Assert.AreEqual("asdf", prm.Value);
            Assert.AreEqual(0, prm.Size);
        }

        [TestMethod]
        public void DataProviderExtension_CustomScriptDerivedParameters()
        {
            CreateRepositoryBuilderForTest();
            var values = new object[] {
                "Parameter value",  //  0
                true,               //  1
                (byte)42,           //  2
                (short)42,          //  3
                42,                 //  4
                42L,                //  5
                DateTime.UtcNow,    //  6
                (float)42,          //  7
                (double)42,         //  8
                (decimal)42,        //  9
                Guid.NewGuid(),     // 10
                new byte[] { 0xFF, 0xE0, 0x00, 0x42 } // 11
            };
            var expectedTypes = new[]
            {
                DbType.String,    //  0
                DbType.Boolean,   //  1
                DbType.Byte,      //  2
                DbType.Int16,     //  3
                DbType.Int32,     //  4
                DbType.Int64,     //  5
                DbType.DateTime,  //  6
                DbType.Single,    //  7
                DbType.Double,    //  8
                DbType.Decimal,   //  9
                DbType.Guid,      // 10
                DbType.Binary,    // 11
            };
            var expectedSizes = new[]
            {
                15, //  0
                0,  //  1
                0,  //  2
                0,  //  3
                0,  //  4
                0,  //  5
                0,  //  6
                0,  //  7
                0,  //  8
                0,  //  9
                0,  // 10
                4,  // 11
            };

            // ACTION
            var proc = DataProvider.Instance.CreateDataProcedure(InMemoryDataProvider.MagicCommandText)
                    .AddParameter("Param0", values[0])
                    .AddParameter("Param1", values[1])
                    .AddParameter("Param2", values[2])
                    .AddParameter("Param3", values[3])
                    .AddParameter("Param4", values[4])
                    .AddParameter("Param5", values[5])
                    .AddParameter("Param6", values[6])
                    .AddParameter("Param7", values[7])
                    .AddParameter("Param8", values[8])
                    .AddParameter("Param9", values[9])
                    .AddParameter("Param10", values[10])
                    .AddParameter("Param11", values[11])
                ;

            // ASSERT

            var prms = proc.Parameters;
            Assert.AreEqual(12, prms.Count);

            for (var i = 0; i < 12; i++)
            {
                var prm = prms[i];
                Assert.AreEqual($"Param{i}", prm.ParameterName);
                Assert.AreEqual(expectedTypes[i], prm.DbType);
                Assert.AreEqual(values[i], prm.Value);
                Assert.AreEqual(expectedSizes[i], prm.Size);
            }
        }
    }
}