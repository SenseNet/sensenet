using System;
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
                throw new NotImplementedException();
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
            public TestDataContext(CancellationToken cancellationToken) : base(cancellationToken)
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
            var dataContext = new TestDataContext(CancellationToken.None);
            Assert.AreEqual(CancellationToken.None, dataContext.CancellationToken);
        }
        [TestMethod]
        public void DC_CancellationToken()
        {
            var cancellationToken = new CancellationTokenSource(123456).Token;
            var dataContext = new TestDataContext(cancellationToken);
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
                using (dataContext = new TestDataContext(CancellationToken.None))
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
        public void DC_MSSQL_Construction_Default()
        {
            var dataContext = new MsSqlDataContext(CancellationToken.None);
            Assert.AreEqual(ConnectionStrings.ConnectionString, dataContext.ConnectionString);
        }
        [TestMethod]
        public void DC_MSSQL_Construction_ConnectionString()
        {
            var connectionString = "ConnectionString1";
            var dataContext = new MsSqlDataContext(connectionString, CancellationToken.None);
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
            var dataContext = new MsSqlDataContext(connectionInfo, CancellationToken.None);

            // ASSERT
            var expected = "Data Source=DataSource1;Initial Catalog=InitialCatalog1;Integrated Security=True";
            Assert.AreEqual(expected, dataContext.ConnectionString);
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
            var dataContext = new MsSqlDataContext(connectionInfo, CancellationToken.None);

            // ASSERT
            var expected = "Data Source=DataSource1;Initial Catalog=master;Integrated Security=True";
            Assert.AreEqual(expected, dataContext.ConnectionString);
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
            var dataContext = new MsSqlDataContext(connectionInfo, CancellationToken.None);

            // ASSERT
            var expected = "Data Source=DataSource1;Initial Catalog=InitialCatalog1;Integrated Security=False;Persist Security Info=False;User ID=User1;Password=123";
            Assert.AreEqual(expected, dataContext.ConnectionString);
        }
    }
}
