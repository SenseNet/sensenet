using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Common.Storage.Data.MsSqlClient;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.DataModel;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    public partial class MsSqlDataProvider : RelationalDataProviderBase
    {
        public override DateTime DateTimeMinValue { get; } = new DateTime(1753, 1, 1, 12, 0, 0);

        /* =============================================================================================== Factory methods */

        public override DbConnection CreateConnection()
        {
            return new SqlConnection(ConnectionStrings.ConnectionString);
        }
        public override DbCommand CreateCommand()
        {
            return new SqlCommand();
        }
        public override DbParameter CreateParameter()
        {
            return new SqlParameter();
        }

        /* =========================================================================================== Platform specific implementations */

        public override async Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var sql = new StringBuilder("SELECT NodeId FROM Nodes WHERE ");
            var first = true;

            if (pathStart != null && pathStart.Length > 0)
            {
                for (int i = 0; i < pathStart.Length; i++)
                    if (pathStart[i] != null)
                        pathStart[i] = pathStart[i].Replace("'", "''");

                sql.AppendLine("(");
                for (int i = 0; i < pathStart.Length; i++)
                {
                    if (i > 0)
                        sql.AppendLine().Append(" OR ");
                    sql.Append(" Path LIKE N'");
                    sql.Append(EscapeForLikeOperator(pathStart[i]));
                    if (!pathStart[i].EndsWith(RepositoryPath.PathSeparator))
                        sql.Append(RepositoryPath.PathSeparator);
                    sql.Append("%' COLLATE Latin1_General_CI_AS");
                }
                sql.AppendLine(")");
                first = false;
            }

            if (name != null)
            {
                name = name.Replace("'", "''");
                if (!first)
                    sql.Append(" AND");
                sql.Append(" Name = '").Append(name).Append("'");
                first = false;
            }

            if (nodeTypeIds != null)
            {
                if (!first)
                    sql.Append(" AND");
                sql.Append(" NodeTypeId");
                if (nodeTypeIds.Length == 1)
                    sql.Append(" = ").Append(nodeTypeIds[0]);
                else
                    sql.Append(" IN (").Append(string.Join(", ", nodeTypeIds)).Append(")");
            }

            if (orderByPath)
                sql.AppendLine().Append("ORDER BY Path");

            return await MsSqlProcedure.ExecuteReaderAsync(sql.ToString(), reader =>
            {
                var result = new List<int>();
                while (reader.Read())
                    result.Add(reader.GetSafeInt32(0));
                return (IEnumerable<int>) result;
            });
        }
        private static string EscapeForLikeOperator(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            text = text.Replace("[", "[[]").Replace("_", "[_]").Replace("%", "[%]");

            return text;
        }

        public override DateTime RoundDateTime(DateTime d)
        {
            return new DateTime(d.Ticks / 100000 * 100000);
        }

        public override async Task InstallInitialDataAsync(InitialData data, CancellationToken cancellationToken = default(CancellationToken))
        {
            await MsSqlDataInstaller.InstallInitialDataAsync(data, this, ConnectionStrings.ConnectionString);
        }

        protected override long ConvertTimestampToInt64(object timestamp)
        {
            var bytes = (byte[]) timestamp;
            var @long = 0L;
            for (int i = 0; i < bytes.Length; i++)
                @long = (@long << 8) + bytes[i];
            return @long;
        }

        protected override object ConvertInt64ToTimestamp(long timestamp)
        {
            var bytes = new byte[8];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[7 - i] = (byte)(timestamp & 0xFF);
                timestamp = timestamp >> 8;
            }
            return bytes;
        }

    }
}
