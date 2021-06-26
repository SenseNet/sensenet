using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Tests.Core;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class MsSqlDataContextTests : TestBase
    {
        private class TestCommand : DbCommand
        {
            public override void Prepare()
            {
                throw new NotImplementedException();
            }

            public override string CommandText { get; set; }
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection DbConnection { get; set; }
            protected override DbParameterCollection DbParameterCollection { get; }
            protected override DbTransaction DbTransaction { get; set; }
            public override bool DesignTimeVisible { get; set; }

            private bool _cancelled;
            public override void Cancel()
            {
                _cancelled = true;
            }

            protected override DbParameter CreateDbParameter()
            {
                throw new NotImplementedException();
            }

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                throw new NotImplementedException();
            }

            public override int ExecuteNonQuery()
            {
                for (var i = 0; i < 1000; i++)
                {
                    if (_cancelled)
                        return -1;
                    System.Threading.Thread.Sleep(1);
                }
                return 0;
            }
            public override STT.Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            {
                for (var i = 0; i < 10; i++)
                {
                    if (_cancelled)
                        return STT.Task.FromResult(-1);

                    Thread.Sleep(100);
                }
                return STT.Task.FromResult(0);
            }

            public override object ExecuteScalar()
            {
                throw new NotImplementedException();
            }
        }
        private class TestConnection : DbConnection
        {
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                throw new NotImplementedException();
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
        private class TestMsSqlDataContext : MsSqlDataContext
        {
            public TestMsSqlDataContext(string connectionString, DataOptions options, CancellationToken cancel) : base(connectionString, options, cancel)
            {
            }

            public override DbConnection CreateConnection()
            {
                return new TestConnection();
            }

            public override DbCommand CreateCommand()
            {
                return new TestCommand();
            }
        }

        [TestMethod]
        public async STT.Task MsSqlDataContext_ExecuteNonQueryWithGo()
        {
            var connectionString = "connectionstring";
            var dataOptions = DataOptions.GetLegacyConfiguration();
            var script = "DROP TABLE [dbo].[Troubles]\r\nGO\r\nCREATE TABLE [dbo].[Jokes]";

            using (var ctx = new TestMsSqlDataContext(connectionString, dataOptions, CancellationToken.None))
            {
                await ctx.ExecuteNonQueryAsync(script, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Id", DbType.Int32, 42),
                        ctx.CreateParameter("@Name", DbType.String, 42, "42"),
                    });
                }).ConfigureAwait(false);
            }

            Assert.Fail(":)");
        }
    }
}
