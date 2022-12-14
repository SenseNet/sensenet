using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataContextTests : TestBase
    {
        #region Infrastructure
        private class TestTransaction : DbTransaction
        {
            public bool IsRollbackCalled { get; private set; }

            public override void Commit()
            {
                throw new NotImplementedException();
            }

            public override void Rollback()
            {
                IsRollbackCalled = true;
            }

            protected override DbConnection DbConnection { get; }
            public override IsolationLevel IsolationLevel { get; }
        }
        private class TestConnection : DbConnection
        {
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                return new TestTransaction();
            }

            public override void Close()
            {
                // do nothing
            }

            public override void ChangeDatabase(string databaseName)
            {
                throw new NotImplementedException();
            }

            public override void Open()
            {
                // do nothing
            }

            public override string ConnectionString { get; set; }
            public override string Database { get; }
            public override ConnectionState State { get; }
            public override string DataSource { get; }
            public override string ServerVersion { get; }

            protected override DbCommand CreateDbCommand()
            {
                throw new NotImplementedException();
            }
        }
        private class TestDataContext : SnDataContext
        {
            public TestDataContext(DataOptions options, CancellationToken cancellationToken) : base(options, null, cancellationToken)
            {
                // do nothing
            }

            public override DbConnection CreateConnection()
            {
                return new TestConnection();
            }

            public override DbCommand CreateCommand()
            {
                throw new NotImplementedException();
            }

            public override DbParameter CreateParameter()
            {
                throw new NotImplementedException();
            }

            public override TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction, CancellationToken cancellationToken,
                TimeSpan timeout = default(TimeSpan))
            {
                return null;
            }
        }
        #endregion

        // The prefix DC_ means: DataContext.

        [TestMethod]
        public void DC_CancellationToken_Default()
        {
            var dataContext = new TestDataContext(new DataOptions(), CancellationToken.None);
            Assert.AreEqual(CancellationToken.None, dataContext.CancellationToken);
        }
        [TestMethod]
        public void DC_CancellationToken()
        {
            var cancellationToken = new CancellationTokenSource(123456).Token;
            var dataContext = new TestDataContext(new DataOptions(), cancellationToken);
            Assert.AreEqual(cancellationToken, dataContext.CancellationToken);
        }
        [TestMethod]
        public void DC_Dispose()
        {
            SnDataContext dataContext = null;
            TestConnection testConnection = null;
            TestTransaction testTransaction = null;
            try
            {
                using (dataContext = new TestDataContext(new DataOptions(), CancellationToken.None))
                {
                    Assert.IsFalse(dataContext.IsDisposed);
                    using (var transaction = dataContext.BeginTransaction(IsolationLevel.ReadCommitted, TimeSpan.FromMinutes(10)))
                    {
                        testConnection = (TestConnection) dataContext.Connection;
                        testTransaction = (TestTransaction)dataContext.Transaction.Transaction;
                        throw new SnNotSupportedException();
                    }
                }
            }
            catch(SnNotSupportedException)
            {
                // do nothing
            }
            Assert.IsTrue(testConnection.State == ConnectionState.Closed);
            Assert.IsTrue(testTransaction.IsRollbackCalled);
            Assert.IsTrue(dataContext.IsDisposed);
        }

        [TestMethod]
        public void DC_MSSQL_Construction_ConnectionString()
        {
            var connectionString = "ConnectionString1";
            var dataContext = new MsSqlDataContext(connectionString, DataOptions.GetLegacyConfiguration(), null, CancellationToken.None);
            Assert.AreEqual(connectionString, dataContext.ConnectionString);
        }
        [TestMethod]
        public void DC_MSSQL_Construction_ConnectionInfo()
        {
            var connectionInfo = new ConnectionInfo
            {
                DataSource = "DataSource1",
                InitialCatalog = InitialCatalog.Initial,
                InitialCatalogName = "InitialCatalog1"
            };

            // ACTION
            var connectionStrings = new ConnectionStringOptions
                {Repository = "Data Source=ds;Initial Catalog=ic;Integrated Security=True"};
            var connectionString = MsSqlDataContext.GetConnectionString(connectionInfo, connectionStrings);

            // ASSERT
            var expected = "Data Source=DataSource1;Initial Catalog=InitialCatalog1;Integrated Security=True";
            Assert.AreEqual(expected, connectionString);
        }
        [TestMethod]
        public void DC_MSSQL_Construction_ConnectionInfo_Master()
        {
            var connectionInfo = new ConnectionInfo
            {
                DataSource = "DataSource1",
                InitialCatalog = InitialCatalog.Master,
                InitialCatalogName = "InitialCatalog1"
            };

            // ACTION
            var connectionStrings = new ConnectionStringOptions
                { Repository = "Data Source=ds;Initial Catalog=ic;Integrated Security=True" };
            var connectionString = MsSqlDataContext.GetConnectionString(connectionInfo, connectionStrings);

            // ASSERT
            var expected = "Data Source=DataSource1;Initial Catalog=master;Integrated Security=True";
            Assert.AreEqual(expected, connectionString);
        }
        [TestMethod]
        public void DC_MSSQL_Construction_ConnectionInfo_UserPassword()
        {
            var connectionInfo = new ConnectionInfo
            {
                DataSource = "DataSource1",
                InitialCatalog = InitialCatalog.Initial,
                InitialCatalogName = "InitialCatalog1",
                UserName = "User1",
                Password = "123"
            };

            // ACTION
            var connectionStrings = new ConnectionStringOptions
                { Repository = "Data Source=ds;Initial Catalog=ic;Persist Security Info=False;Integrated Security=True" };
            var connectionString = MsSqlDataContext.GetConnectionString(connectionInfo, connectionStrings);

            // ASSERT
            var expected = "Data Source=DataSource1;Initial Catalog=InitialCatalog1;Integrated Security=False;Persist Security Info=False;User ID=User1;Password=123";
            Assert.AreEqual(expected, connectionString);
        }
        [TestMethod]
        public void DC_MSSQL_Construction_ConnectionInfo_Named_Master()
        {
            var connectionInfo = new ConnectionInfo
            {
                ConnectionName = "CustomCnStr",
                InitialCatalog = InitialCatalog.Master,
            };

            // ACTION
            var connectionStrings = new ConnectionStringOptions
            {
                Repository = "Data Source=ds;Initial Catalog=ic;Integrated Security=True",
                AllConnectionStrings = new Dictionary<string, string>
                {
                    {"CustomCnStr", "Data Source=CustomServer;Initial Catalog=InitialCatalog1;Integrated Security=True"}
                }
            };
            var connectionString = MsSqlDataContext.GetConnectionString(connectionInfo, connectionStrings);

            // ASSERT
            var expected = "Data Source=CustomServer;Initial Catalog=master;Integrated Security=True";
            Assert.AreEqual(expected, connectionString);
        }
        [TestMethod]
        public void DC_MSSQL_Construction_ConnectionInfo_Named_CustomDb()
        {
            var connectionInfo = new ConnectionInfo
            {
                ConnectionName = "CustomCnStr",
                InitialCatalog = InitialCatalog.Initial,
                InitialCatalogName = "CustomDb"
            };

            // ACTION
            var connectionStrings = new ConnectionStringOptions
            {
                Repository = "Data Source=ds;Initial Catalog=ic;Integrated Security=True",
                AllConnectionStrings = new Dictionary<string, string>
                {
                    {"CustomCnStr", "Data Source=CustomServer;Initial Catalog=InitialCatalog1;Integrated Security=True"}
                }
            };
            var connectionString = MsSqlDataContext.GetConnectionString(connectionInfo, connectionStrings);

            // ASSERT
            var expected = "Data Source=CustomServer;Initial Catalog=CustomDb;Integrated Security=True";
            Assert.AreEqual(expected, connectionString);
        }
    }
}
