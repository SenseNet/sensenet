using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    //UNDONE: SQL: Develop SqlQueryExecutor
    internal class SqlQueryExecutor
    {
        //public IPermissionFilter PermissionChecker { get; private set; }
        //public LucQuery LucQuery { get; private set; }

        //public void Initialize(LucQuery lucQuery, IPermissionFilter permissionChecker)
        //{
        //    this.LucQuery = lucQuery;
        //    this.PermissionChecker = permissionChecker;
        //}

        //public string QueryString
        //{
        //    get
        //    {
        //        return _sqlQueryText + "-- Parameters: "
        //            + String.Join(", ", _sqlParameters.Select(p => p.Name + " = " + ValueToString(p.Value)).ToArray());
        //    }
        //}
        //private string ValueToString(object p)
        //{
        //    if (p is DateTime)
        //        return ((DateTime)p).ToString("yyyy-MM-dd HH:mm:ss.fff");
        //    return p.ToString();
        //}

        //public int TotalCount { get; internal set; }


        //private NodeQueryParameter[] _sqlParameters;
        //private string _sqlQueryText;

        //public SqlQueryExecutor(string sqlQueryText, NodeQueryParameter[] sqlParameters)
        //{
        //    _sqlQueryText = sqlQueryText;
        //    _sqlParameters = sqlParameters;
        //}

        //public IEnumerable<LucObject> Execute()
        //{
        //    using (var op = SnTrace.Query.StartOperation("SqlQueryExcutor. SQL:'{0}', parameters:{1}", _sqlQueryText, String.Join(", ", _sqlParameters.Select(x => x.Name + "=" + x.Value))))
        //    using (var proc = DataProvider.CreateDataProcedure(_sqlQueryText))
        //    {
        //        proc.CommandType = System.Data.CommandType.Text;
        //        foreach (var sqlParam in _sqlParameters)
        //        {
        //            var param = DataProvider.CreateParameter();
        //            param.ParameterName = sqlParam.Name;
        //            param.Value = sqlParam.Value;
        //            proc.Parameters.Add(param);
        //        }

        //        var sqlResult = new List<LucObject>();
        //        var totalCount = 0;

        //        using (var reader = proc.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                var nodeId = reader.GetInt32(0);
        //                var isLastPublic = reader.GetValue(1) != DBNull.Value;
        //                if (PermissionChecker.IsPermitted(nodeId, isLastPublic, true))
        //                {
        //                    var lucObj = new LucObject();
        //                    lucObj[IndexFieldName.NodeId] = nodeId.ToString();
        //                    sqlResult.Add(lucObj);
        //                    totalCount++;
        //                }
        //            }
        //        }

        //        op.Successful = true;
        //        this.TotalCount = totalCount;

        //        return sqlResult;
        //    }
        //}
    }
}
