using System;
using System.Data;
using System.IO;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Packaging.Steps
{
    public class IfDatabaseValue : ConditionalStep
    {
        public string Query { get; set; }
        public string DataSource { get; set; }
        public string InitialCatalogName { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(Query))
                throw new PackagingException("Query argument is missing.");

            string queryPath;
            try
            {
                queryPath = ResolvePackagePath(Query, context);
                if (!File.Exists(queryPath))
                    queryPath = null;
            }
            catch
            {
                queryPath = null;
            }

            return ExecuteSql(queryPath != null ? File.ReadAllText(queryPath) : Query, context);
        }

        private bool ExecuteSql(string script, ExecutionContext context)
        {
            using (var proc = CreateDataProcedure(script, context))
            {
                proc.CommandType = CommandType.Text;
                object result;

                try
                {
                    result = proc.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    throw new PackagingException("Error during SQL script execution. " + ex);
                }

                if (result == null || Convert.IsDBNull(result))
                    return false;

                if (result is bool)
                    return (bool)result;
                if (result is byte)
                    return (byte)result > 0;
                if (result is decimal)
                    return (decimal)result > 0;
                if (result is double)
                    return (double)result > 0;
                if (result is float)
                    return (float)result > 0;
                if (result is int)
                    return (int)result > 0;
                if (result is long)
                    return (long)result > 0;
                if (result is sbyte)
                    return (sbyte)result > 0;
                if (result is short)
                    return (short)result > 0;
                if (result is uint)
                    return (uint)result > 0;
                if (result is ulong)
                    return (ulong)result > 0;
                if (result is ushort)
                    return (ushort)result > 0;

                if (result is string)
                    return !string.IsNullOrEmpty((string) result);
            }

            return false;
        }

        private IDataProcedure CreateDataProcedure(string script, ExecutionContext context)
        {
            return DataProvider.CreateDataProcedure(script, new ConnectionInfo
            {
                DataSource = (string)context.ResolveVariable(DataSource),
                InitialCatalogName = (string)context.ResolveVariable(InitialCatalogName)
            });
        }
    }
}
