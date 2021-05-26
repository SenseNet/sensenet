using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Versioning;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;
using IsolationLevel = System.Data.IsolationLevel;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataProviderTests : TestBase
    {
        private class TestLogger : IEventLogger
        {
            public List<string> Errors { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
            public List<string> Infos { get; } = new List<string>();

            public void Write(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title,
                IDictionary<string, object> properties)
            {
                switch (severity)
                {
                    case TraceEventType.Information: 
                        Infos.Add((string)message);
                        break;
                    case TraceEventType.Warning:
                        Warnings.Add((string)message);
                        break;
                    case TraceEventType.Critical:
                    case TraceEventType.Error:
                        Errors.Add((string)message);
                        break;
                }
            }
        }

        // ReSharper disable once InconsistentNaming
        private static DataProvider DP => Providers.Instance.DataStore.DataProvider;
        // ReSharper disable once InconsistentNaming
        private static ITestingDataProviderExtension TDP => Providers.Instance.DataProvider.GetExtension<ITestingDataProviderExtension>();

        /* ================================================================================================== Transaction */

        /// <summary>
        /// Designed for testing the Rollback opration of the transactionality.
        /// An instance of this class is almost like a NodeHeadData but throws an exception
        /// when the setter of the Timestamp property is called. This call probably is always after all database operation
        /// so using this object helps the testing of the full rolling-back operation.
        /// </summary>
        private class ErrorGenNodeHeadData : NodeHeadData
        {
            private bool _isDeadlockSimulation;
            private long _timestamp;
            public override long Timestamp
            {
                get => _timestamp;
                set => throw new Exception(_isDeadlockSimulation
                    ? "Transaction was deadlocked."
                    : "Something went wrong.");
            }

            public static NodeHeadData Create(NodeHeadData src, bool isDeadlockSimulation = false)
            {
                return new ErrorGenNodeHeadData
                {
                    _isDeadlockSimulation = isDeadlockSimulation,
                    NodeId = src.NodeId,
                    NodeTypeId = src.NodeTypeId,
                    ContentListTypeId = src.ContentListTypeId,
                    ContentListId = src.ContentListId,
                    CreatingInProgress = src.CreatingInProgress,
                    IsDeleted = src.IsDeleted,
                    ParentNodeId = src.ParentNodeId,
                    Name = src.Name,
                    DisplayName = src.DisplayName,
                    Path = src.Path,
                    Index = src.Index,
                    Locked = src.Locked,
                    LockedById = src.LockedById,
                    ETag = src.ETag,
                    LockType = src.LockType,
                    LockTimeout = src.LockTimeout,
                    LockDate = src.LockDate,
                    LockToken = src.LockToken,
                    LastLockUpdate = src.LastLockUpdate,
                    LastMinorVersionId = src.LastMinorVersionId,
                    LastMajorVersionId = src.LastMajorVersionId,
                    CreationDate = src.CreationDate,
                    CreatedById = src.CreatedById,
                    ModificationDate = src.ModificationDate,
                    ModifiedById = src.ModifiedById,
                    IsSystem = src.IsSystem,
                    OwnerId = src.OwnerId,
                    SavingState = src.SavingState,
                    _timestamp = src.Timestamp
                };
            }
        }
        private class NodeDataForDeadlockTest : NodeData
        {
            public NodeDataForDeadlockTest(NodeData data) : base(data.NodeTypeId, data.ContentListTypeId)
            {
                data.CopyData(this);
            }

            internal override NodeHeadData GetNodeHeadData()
            {
                return ErrorGenNodeHeadData.Create(base.GetNodeHeadData(), true);
            }
        }

        private async STT.Task<(int Nodes, int Versions, int Binaries, int Files, int LongTexts, string AllCounts, string AllCountsExceptFiles)> GetDbObjectCountsAsync(string path, DataProvider DP, ITestingDataProviderExtension tdp)
        {
            var nodes = await DP.GetNodeCountAsync(path, CancellationToken.None);
            var versions = await DP.GetVersionCountAsync(path, CancellationToken.None);
            var binaries = await TDP.GetBinaryPropertyCountAsync(path);
            var files = await TDP.GetFileCountAsync(path);
            var longTexts = await TDP.GetLongTextCountAsync(path);
            var all = $"{nodes},{versions},{binaries},{files},{longTexts}";
            var allExceptFiles = $"{nodes},{versions},{binaries},{longTexts}";

            var result =  (Nodes: nodes, Versions: versions, Binaries: binaries, Files: files, LongTexts: longTexts, AllCounts: all, AllCountsExceptFiles: allExceptFiles);
            return await STT.Task.FromResult(result);
        }

        [TestMethod]
        public async STT.Task DP_Transaction_Deadlock()
        {
            var testLogger = new TestLogger();
            await Test(builder =>
            {
                builder.UseLogger(testLogger);
            }, async () =>
            {
                var testNode = new SystemFolder(Repository.Root) { Name = "TestNode" };

                // Prepare for this test
                var nodeAcc = new ObjectAccessor(testNode, typeof(Node));
                nodeAcc.SetField("_data", new NodeDataForDeadlockTest(testNode.Data));

                var countsBefore = (await GetDbObjectCountsAsync(null, DP, TDP)).AllCounts;
                testLogger.Warnings.Clear();

                // ACTION
                try
                {
                    testNode.Save();
                    Assert.Fail("Teh expected exception was not thrown.");
                }
                catch (AggregateException ae)
                {
                    if (!(ae.InnerException is TransactionDeadlockedException))
                        throw;
                    // ignored
                }
                catch (DataException de)
                {
                    if (!(de.InnerException is TransactionDeadlockedException))
                        throw;
                    // ignored
                }

                // ASSERT
                Assert.IsTrue(testLogger.Warnings.Count >= 2);
                Assert.IsTrue(testLogger.Warnings[0].ToLowerInvariant().Contains("deadlock"));
                Assert.IsTrue(testLogger.Warnings[1].ToLowerInvariant().Contains("deadlock"));
                var countsAfter = (await GetDbObjectCountsAsync(null, DP, TDP)).AllCounts;
                Assert.AreEqual(countsBefore, countsAfter);
            });
        }

        #region Infrastructure for DP_Transaction_Timeout
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
        private class TestTransaction : DbTransaction
        {
            public override void Commit() { }
            public override void Rollback() { }
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
            public TestDataContext(DataOptions options, CancellationToken cancellationToken) : base(options, cancellationToken)
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
            public override DbParameter CreateParameter(){throw new NotImplementedException();}
            public override TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction,
                CancellationToken cancellationToken, TimeSpan timeout = default(TimeSpan)){return null;}
        }
        #endregion
        [TestMethod]
        public async STT.Task DP_Transaction_Timeout()
        {
            async STT.Task<TransactionStatus> TestTimeout(double timeoutInSeconds)
            {
                TransactionWrapper transactionWrapper;

                //TODO: check default configuration timeout values
                using (var ctx = new TestDataContext(new DataOptions(), CancellationToken.None))
                {
                    using (var transaction = ctx.BeginTransaction(timeout: TimeSpan.FromSeconds(timeoutInSeconds)))
                    {
                        transactionWrapper = ctx.Transaction;
                        try
                        {
                            await ctx.ExecuteNonQueryAsync(" :) ").ConfigureAwait(false);
                            transaction.Commit();
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
                return transactionWrapper.Status;
            }

            Assert.AreEqual(TransactionStatus.Committed, await TestTimeout(2.5));
            Assert.AreEqual(TransactionStatus.Aborted, await TestTimeout(0.5));

        }


    }
}
