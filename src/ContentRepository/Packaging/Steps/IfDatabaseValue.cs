using System;
using System.Data;
using System.IO;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;

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
            using var op = SnTrace.Database.StartOperation("IfDatabaseValue: ExecuteSql: {0}",
                script.ToTrace());
            var result = Execute(script, context);
            op.Successful = true;
            return result;
        }
        private bool Execute(string script, ExecutionContext context)
        {
            var connectionInfo = new ConnectionInfo
            {
                DataSource = (string)context.ResolveVariable(DataSource),
                InitialCatalogName = (string)context.ResolveVariable(InitialCatalogName),
                UserName = (string)context.ResolveVariable(UserName),
                Password = (string)context.ResolveVariable(Password)
            };
            var connectionString = MsSqlDataContext.GetConnectionString(connectionInfo, context.ConnectionStrings)
                                   ?? context.ConnectionStrings.Repository;

            //TODO: [DIREF] get options from DI through constructor
            using var ctx = new MsSqlDataContext(connectionString, DataOptions.GetLegacyConfiguration(), CancellationToken.None);

            object result;
            try
            {
                result = ctx.ExecuteScalarAsync(script).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new PackagingException("Error during SQL script execution. " + ex);
            }

            if (result == null || Convert.IsDBNull(result))
                return false;

            switch (result)
            {
                case bool @bool:
                    return @bool;
                case byte @byte:
                    return @byte > 0;
                case decimal @decimal:
                    return @decimal > 0;
                case double @double:
                    return @double > 0;
                case float @float:
                    return @float > 0;
                case int @int:
                    return @int > 0;
                case long @long:
                    return @long > 0;
                case sbyte @sbyte:
                    return @sbyte > 0;
                case short @short:
                    return @short > 0;
                case uint @uint:
                    return @uint > 0;
                case ulong @ulong:
                    return @ulong > 0;
                case ushort @ushort:
                    return @ushort > 0;
                case string @string:
                    return !string.IsNullOrEmpty(@string);
            }

            return false;
        }
    }
}
