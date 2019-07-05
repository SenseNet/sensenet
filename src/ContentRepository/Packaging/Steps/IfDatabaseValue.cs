using System;
using System.Data;
using System.IO;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

namespace SenseNet.Packaging.Steps
{
    public class IfDatabaseValue : ConditionalStep
    {
        public string Query { get; set; }
        public string DataSource { get; set; }
        public string InitialCatalogName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

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
            //UNDONE:DB: not tested
            var connectionInfo = new ConnectionInfo
            {
                DataSource = (string)context.ResolveVariable(DataSource),
                InitialCatalogName = (string)context.ResolveVariable(InitialCatalogName),
                UserName = (string)context.ResolveVariable(UserName),
                Password = (string)context.ResolveVariable(Password)
            };
            using (var ctx = new MsSqlDataContext(connectionInfo))
            {
                object result;
                try
                {
                    result = ctx.ExecuteScalarAsync(script).Result;
                }
                catch (Exception ex)
                {
                    throw new PackagingException("Error during SQL script execution. " + ex);
                }

                if (result == null || Convert.IsDBNull(result))
                    return false;

                if (result is bool @bool)
                    return @bool;
                if (result is byte @bybte)
                    return @bybte > 0;
                if (result is decimal @decimal)
                    return @decimal > 0;
                if (result is double @double)
                    return @double > 0;
                if (result is float @float)
                    return @float > 0;
                if (result is int @int)
                    return @int > 0;
                if (result is long @long)
                    return @long > 0;
                if (result is sbyte @sbyte)
                    return @sbyte > 0;
                if (result is short @short)
                    return @short > 0;
                if (result is uint @uint)
                    return @uint > 0;
                if (result is ulong @ulong)
                    return @ulong > 0;
                if (result is ushort @ushort)
                    return @ushort > 0;
                if (result is string @string)
                    return !string.IsNullOrEmpty(@string);
            }
            return false;
        }
    }
}
