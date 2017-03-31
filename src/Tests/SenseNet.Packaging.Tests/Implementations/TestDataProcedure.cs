using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Core.Tests.Implementations
{
    public class TestDataProcedure : IDataProcedure
    {
        public List<TestDataProcedure> TraceList { get; set; }
        public object ExpectedCommandResult { get; set; }

        public string ExecutorMethod { get; private set; }
        public void Dispose()
        {
            // do nothing
        }

        public CommandType CommandType { get; set; }
        public string CommandText { get; set; }
        public DbParameterCollection Parameters { get; } = new TestDbParameterCollection();
        public void DeriveParameters()
        {
            throw new NotImplementedException();
        }

        public DbDataReader ExecuteReader()
        {
            ExecutorMethod = "ExecuteReader";
            TraceList.Add(this);
            return (TestDataReader)ExpectedCommandResult;
        }

        public DbDataReader ExecuteReader(CommandBehavior behavior)
        {
            ExecutorMethod = $"ExecuteReader(CommandBehavior.{behavior})";
            TraceList.Add(this);
            return (TestDataReader)ExpectedCommandResult;
        }

        public object ExecuteScalar()
        {
            ExecutorMethod = "ExecuteScalar";
            TraceList.Add(this);
            return ExpectedCommandResult;
        }

        public int ExecuteNonQuery()
        {
            ExecutorMethod = "ExecuteNonQuery";
            TraceList.Add(this);
            return 0;
        }
    }
}
