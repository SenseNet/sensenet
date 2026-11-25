using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search.Indexing;

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    public partial class MsSqlDataProvider
    {
        public override async Task<IndexingActivityMetadata[]> LoadIndexingActivityMetadataAsync(
            int fromId, int toId, int count, CancellationToken cancellationToken)
        {
            const string sql = @"SELECT TOP (@Count)
    IndexingActivityId, ActivityType, NodeId, VersionId, [Path], Extension
FROM dbo.IndexingActivities
WHERE IndexingActivityId >= @From AND IndexingActivityId <= @To
ORDER BY IndexingActivityId";

            using var context = CreateDataContext(cancellationToken);
            return await context.ExecuteReaderAsync(sql, cmd =>
            {
                cmd.Parameters.Add(context.CreateParameter("@From", DbType.Int32, fromId));
                cmd.Parameters.Add(context.CreateParameter("@To", DbType.Int32, toId));
                cmd.Parameters.Add(context.CreateParameter("@Count", DbType.Int32, count));
            }, async (reader, cancel) =>
            {
                var result = new List<IndexingActivityMetadata>();
                while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                {
                    result.Add(new IndexingActivityMetadata(reader.GetInt32(0),
                        Enum.Parse<IndexingActivityType>(reader.GetString(1)),
                        reader.GetInt32(2), reader.GetInt32(3), reader.GetString(4),
                        reader.IsDBNull(5) ? null : reader.GetString(5)));
                }
                return result.ToArray();
            }).ConfigureAwait(false);
        }

        public override async Task<int[]> LoadIndexingSubtreeVersionIdsAsync(
            string path, CancellationToken cancellationToken)
        {
            const string sql = @"SELECT V.VersionId
FROM dbo.Nodes N
INNER JOIN dbo.Versions V ON V.NodeId = N.NodeId
WHERE N.[Path] = @Path COLLATE Latin1_General_CI_AS
   OR N.[Path] LIKE @Descendants COLLATE Latin1_General_CI_AS ESCAPE '~'";

            var descendants = path.Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~[") + "/%";
            using var context = CreateDataContext(cancellationToken);
            return await context.ExecuteReaderAsync(sql, cmd =>
            {
                cmd.Parameters.Add(context.CreateParameter("@Path", DbType.String, PathMaxLength, path));
                cmd.Parameters.Add(context.CreateParameter("@Descendants", DbType.String, 2 * PathMaxLength + 2, descendants));
            }, async (reader, cancel) =>
            {
                var result = new List<int>();
                while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    result.Add(reader.GetInt32(0));
                return result.ToArray();
            }).ConfigureAwait(false);
        }
    }
}
