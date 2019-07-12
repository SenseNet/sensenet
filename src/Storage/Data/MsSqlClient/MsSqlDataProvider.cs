using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    //UNDONE:DB: ASYNC API: CancellationToken is not used in this class.
    public partial class MsSqlDataProvider : RelationalDataProviderBase
    {
        public override DateTime DateTimeMinValue { get; } = new DateTime(1753, 1, 1, 12, 0, 0);

        public override IDataPlatform<DbConnection, DbCommand, DbParameter> GetPlatform()
        {
            return new MsSqlDataContext();
        }

        /* =========================================================================================== Platform specific implementations */

        /* =============================================================================================== Nodes */
        /* =============================================================================================== NodeHead */
        /* =============================================================================================== NodeQuery */

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

            //using (var ctx = new MsSqlDataContext(cancellationToken))
            using(var ctx = new MsSqlDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(sql.ToString(), async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    var result = new List<int>();
                    while (await reader.ReadAsync(cancel))
                    {
                        cancel.ThrowIfCancellationRequested();
                        result.Add(reader.GetSafeInt32(0));
                    }
                    return (IEnumerable<int>)result;
                });
            }
        }

        public override async Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart, bool orderByPath,
            List<QueryPropertyData> properties, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new MsSqlDataContext(cancellationToken))
            {
                var typeCount = nodeTypeIds?.Length ?? 0;
                var onlyNodes = true;
                (bool IsNodeTable, bool IsColumn, string Column, DbType DataType, object Value)[] propertyMapping = null;

                if (properties != null && properties.Any())
                {
                    propertyMapping = properties.Select(GetPropertyMappingForQuery).ToArray();
                    onlyNodes = propertyMapping.All(x => x.IsNodeTable);
                }

                var parameters = new List<DbParameter>();
                var sqlBuilder = new StringBuilder();
                sqlBuilder.AppendLine("-- MsSqlDataProvider.QueryNodesByTypeAndPathAndProperty");

                if (typeCount > 1)
                {
                    sqlBuilder.AppendLine("DECLARE @TypeIdTable AS TABLE(Id INT)");
                    sqlBuilder.AppendLine("INSERT INTO @TypeIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@TypeIds, ',')");
                }

                sqlBuilder.AppendLine("SELECT n.NodeId FROM Nodes n");
                if (!onlyNodes)
                    sqlBuilder.AppendLine("    JOIN Versions V ON V.NodeId = n.NodeId");

                sqlBuilder.Append("WHERE ");
                var first = true;

                if (!string.IsNullOrEmpty(pathStart))
                {
                    sqlBuilder.AppendLine("(/*[Path] = @Path OR*/ [Path] LIKE REPLACE(@Path, '_', '[_]') + '/%' COLLATE Latin1_General_CI_AS)");
                    parameters.Add(ctx.CreateParameter("@Path", DbType.String, pathStart));
                    first = false;
                }

                if (typeCount == 1)
                {
                    if (!first)
                        sqlBuilder.Append("    AND ");

                    sqlBuilder.AppendLine("n.NodeTypeId = @TypeId");
                    // ReSharper disable once AssignNullToNotNullAttribute
                    parameters.Add(ctx.CreateParameter("@TypeId", DbType.Int32, nodeTypeIds.First()));
                    first = false;
                }
                else if (typeCount > 1)
                {
                    if (!first)
                        sqlBuilder.Append("    AND ");
                    sqlBuilder.AppendLine("n.NodeTypeId IN (SELECT Id FROM @TypeIdTable)");
                    // ReSharper disable once AssignNullToNotNullAttribute
                    parameters.Add(ctx.CreateParameter("@TypeIds", DbType.String, string.Join(",", nodeTypeIds.Select(x => x.ToString()))));
                    first = false;
                }

                if (propertyMapping != null)
                {
                    var index = 1;
                    foreach (var item in propertyMapping)
                    {
                        if (!first)
                            sqlBuilder.Append("    AND ");

                        var paramName = "@Property" + index++;

                        if (item.IsColumn)
                            sqlBuilder.Append(item.IsNodeTable ? "n." : "v.").AppendLine($"[{item.Column}] = {paramName}");
                        else
                            sqlBuilder.AppendLine($"v.DynamicProperties LIKE '%' + {paramName} + '%'");

                        parameters.Add(ctx.CreateParameter(paramName, item.DataType, item.Value));

                        first = false;
                    }
                }

                if (orderByPath && !string.IsNullOrEmpty(pathStart))
                    sqlBuilder.AppendLine("ORDER BY n.[Path]");

                return await ctx.ExecuteReaderAsync(sqlBuilder.ToString(),
                    cmd => { cmd.Parameters.AddRange(parameters.ToArray()); },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var result = new List<int>();
                        while (await reader.ReadAsync(cancel))
                        {
                            cancel.ThrowIfCancellationRequested();
                            result.Add(reader.GetInt32(0));
                        }
                        return result;
                    });
            }
        }
        private (bool IsNodeTable, bool IsColumn, string Column, DbType DataType, object Value) GetPropertyMappingForQuery(QueryPropertyData property)
        {
            bool isNodeTable;
            string column = null;
            var isColumn = true;
            DbType dataType;

            switch (property.PropertyName)
            {
                case "NodeId": isNodeTable = true; dataType = DbType.Int32; break;
                case "NodeTypeId": isNodeTable = true; dataType = DbType.Int32; break;
                case "ContentListTypeId": isNodeTable = true; dataType = DbType.Int32; break;
                case "ContentListId": isNodeTable = true; dataType = DbType.Int32; break;
                case "CreatingInProgress": isNodeTable = true; dataType = DbType.Byte; break;
                case "IsDeleted": isNodeTable = true; dataType = DbType.Byte; break;
                case "IsInherited": isNodeTable = true; dataType = DbType.Byte; break;
                case "ParentNodeId": isNodeTable = true; dataType = DbType.Int32; break;
                case "Name": isNodeTable = true; dataType = DbType.String; break;
                case "Path": isNodeTable = true; dataType = DbType.String; break;
                case "Index": isNodeTable = true; dataType = DbType.Int32; break;
                case "Locked": isNodeTable = true; dataType = DbType.Byte; break;
                case "LockedById": isNodeTable = true; dataType = DbType.Int32; break;
                case "ETag": isNodeTable = true; dataType = DbType.AnsiString; break;
                case "LockType": isNodeTable = true; dataType = DbType.Int32; break;
                case "LockTimeout": isNodeTable = true; dataType = DbType.Int32; break;
                case "LockDate": isNodeTable = true; dataType = DbType.DateTime2; break;
                case "LockToken": isNodeTable = true; dataType = DbType.AnsiString; break;
                case "LastLockUpdate": isNodeTable = true; dataType = DbType.DateTime2; break;
                case "LastMinorVersionId": isNodeTable = true; dataType = DbType.Int32; break;
                case "LastMajorVersionId": isNodeTable = true; dataType = DbType.Int32; break;
                case "NodeCreationDate": isNodeTable = true; column = "CreationDate"; dataType = DbType.DateTime2; break;
                case "NodeCreatedById": isNodeTable = true; column = "CreatedById"; dataType = DbType.Int32; break;
                case "NodeModificationDate": isNodeTable = true; column = "ModificationDate"; dataType = DbType.DateTime2; break;
                case "NodeModifiedById": isNodeTable = true; column = "ModifiedById"; dataType = DbType.Int32; break;
                case "DisplayName": isNodeTable = true; dataType = DbType.String; break;
                case "IsSystem": isNodeTable = true; dataType = DbType.Byte; break;
                case "OwnerId": isNodeTable = true; dataType = DbType.Int32; break;
                case "SavingState": isNodeTable = true; dataType = DbType.Int32; break;
                case "VersionId": isNodeTable = false; dataType = DbType.Int32; break;
                case "MajorNumber": isNodeTable = false; dataType = DbType.Int16; break;
                case "MinorNumber": isNodeTable = false; dataType = DbType.Int16; break;
                case "VersionCreationDate": isNodeTable = false; column = "CreationDate"; dataType = DbType.DateTime2; break;
                case "VersionCreatedById": isNodeTable = false; column = "CreatedById"; dataType = DbType.Int32; break;
                case "VersionModificationDate": isNodeTable = false; column = "ModificationDate"; dataType = DbType.DateTime2; break;
                case "VersionModifiedById": isNodeTable = false; column = "ModifiedById"; dataType = DbType.Int32; break;
                case "Status": isNodeTable = false; dataType = DbType.Int32; break;
                default: isNodeTable = false; isColumn = false; dataType = DbType.String; break;
            }
            if (isColumn && column == null)
                column = property.PropertyName;

            var propertyValue = isColumn
                ? property.Value
                : $"\r\n{property.PropertyName}:{property.Value}\r\n";

            return (isNodeTable, isColumn, column, dataType, propertyValue);
        }

        private static string EscapeForLikeOperator(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            text = text.Replace("[", "[[]").Replace("_", "[_]").Replace("%", "[%]");

            return text;
        }

        /* =============================================================================================== Tree */
        /* =============================================================================================== TreeLock */
        /* =============================================================================================== IndexDocument */
        /* =============================================================================================== IndexingActivity */
        /* =============================================================================================== Schema */

        public override SchemaWriter CreateSchemaWriter()
        {
            return new MsSqlSchemaWriter();
        }

        /* =============================================================================================== Logging */
        /* =============================================================================================== Provider Tools */

        public override DateTime RoundDateTime(DateTime d)
        {
            return new DateTime(d.Ticks / 100000 * 100000);
        }

        /* =============================================================================================== Installation */

        public override async Task InstallInitialDataAsync(InitialData data, CancellationToken cancellationToken = default(CancellationToken))
        {
            await MsSqlDataInstaller.InstallInitialDataAsync(data, this, ConnectionStrings.ConnectionString);
        }

        /* =============================================================================================== Tools */

        protected override Exception GetException(Exception innerException, string message = null)
        {
            if (innerException is ContentNotFoundException)
                return innerException;
            if (innerException is NodeIsOutOfDateException)
                return innerException;

            if (!(innerException is SqlException sqlEx))
                return null;

            if (message == null)
                message = "A database exception occured during execution of the operation." +
                          " See InnerException for details.";

            // https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors?view=sql-server-2017
            if (sqlEx.Number == 2601)
                return new NodeAlreadyExistsException(message, sqlEx);

            if (sqlEx.Message.StartsWith("Cannot update a deleted Node"))
                return new ContentNotFoundException(message, sqlEx);
            if (sqlEx.Message.StartsWith("Cannot copy a deleted Version"))
                return new ContentNotFoundException(message, sqlEx);
            if (sqlEx.Message.StartsWith("Cannot move a deleted node"))
                return new ContentNotFoundException(message, sqlEx);
            if (sqlEx.Message.StartsWith("Cannot move under a deleted node"))
                return new ContentNotFoundException(message, sqlEx);
            if (sqlEx.Message.StartsWith("Node is out of date"))
                return new NodeIsOutOfDateException(message, sqlEx);
            if (sqlEx.Message.StartsWith("Source node is out of date"))
                return new NodeIsOutOfDateException(message, sqlEx);

            return null;
        }

        public override bool IsDeadlockException(Exception exception)
        {
            // Avoid [SqlException (0x80131904): Transaction (Process ID ??) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction.
            // CAUTION: Using e.ErrorCode and testing for HRESULT 0x80131904 will not work! you should use e.Number not e.ErrorCode
            if (!(exception is SqlException sqlEx))
                return false;
            var sqlExNumber = sqlEx.Number;
            var sqlExErrorCode = sqlEx.ErrorCode;
            var isDeadLock = sqlExNumber == 1205;

            // assert
            var messageParts = new[]
            {
                "was deadlocked on lock",
                "resources with another process and has been chosen as the deadlock victim. rerun the transaction"
            };
            var currentMessage = exception.Message.ToLower();
            var isMessageDeadlock = messageParts.All(msgPart => currentMessage.Contains(msgPart));

            if (sqlEx != null && isMessageDeadlock != isDeadLock)
                throw new DataException(string.Concat("Incorrect deadlock analysis",
                    ". Number: ", sqlExNumber,
                    ". ErrorCode: ", sqlExErrorCode,
                    ". Errors.Count: ", sqlEx.Errors.Count,
                    ". Original message: ", exception.Message), exception);

            return isDeadLock;
        }

        protected override long ConvertTimestampToInt64(object timestamp)
        {
            if (timestamp == null)
                return 0L;
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
