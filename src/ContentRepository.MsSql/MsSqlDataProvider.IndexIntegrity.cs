using Microsoft.Data.SqlClient;

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient;

public partial class MsSqlDataProvider : IIndexIntegrityDataSource
{
    private const string IntegrityVersionsSql = """
        SELECT V.VersionId, CONVERT(bigint, V.[Timestamp]), N.NodeId, CONVERT(bigint, N.[Timestamp]),
               N.LastMajorVersionId, N.LastMinorVersionId, N.ModificationDate, V.ModificationDate, N.NodeTypeId
        FROM dbo.Versions AS V INNER JOIN dbo.Nodes AS N ON N.NodeId = V.NodeId
        """;

    string IIndexIntegrityDataSource.IntegrityDatabaseIdentity
    {
        get
        {
            var connection = new SqlConnectionStringBuilder(ConnectionStrings.Value.Repository);
            return $"{connection.DataSource}\n{connection.InitialCatalog}\n{connection.AttachDBFilename}";
        }
    }

    protected override string ExportIntegrityVersionsScript => IntegrityVersionsSql + "\nORDER BY V.VersionId;";

    protected override string ReadIntegrityVersionsScript => IntegrityVersionsSql + """

        WHERE V.VersionId IN (SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@VersionIds, ','))
        ORDER BY V.VersionId;
        """;
}
