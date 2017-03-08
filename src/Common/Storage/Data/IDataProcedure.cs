using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;

namespace SenseNet.ContentRepository.Storage.Data
{
    public interface IDataProcedure : IDisposable
    {
        CommandType CommandType { get; set; }
        string CommandText { get; set; }
        DbParameterCollection Parameters { get; }

        void DeriveParameters();
        DbDataReader ExecuteReader();
        DbDataReader ExecuteReader(CommandBehavior behavior);
        object ExecuteScalar();
        int ExecuteNonQuery();
    }
}
