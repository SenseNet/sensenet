using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using STT=System.Threading.Tasks;
using Npgsql;
using SenseNet.ContentRepository.Storage.DataModel;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{
    internal class PgSqlSchemaInstaller
    {
        private static readonly byte Yes = 1;
        private static readonly byte No = 0;

        private readonly string _connectionString;

        public PgSqlSchemaInstaller(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async STT.Task InstallSchemaAsync(RepositorySchemaData schema)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                await UpsertPropertyTypesAsync(schema.PropertyTypes, connection, transaction).ConfigureAwait(false);
                await UpsertNodeTypesAsync(schema.NodeTypes, connection, transaction).ConfigureAwait(false);
                await UpsertContentListTypesAsync(schema.ContentListTypes, connection, transaction).ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }

        private async STT.Task UpsertPropertyTypesAsync(List<PropertyTypeData> propertyTypes,
            NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            foreach (var pt in propertyTypes)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""PropertyTypes"" (""PropertyTypeId"", ""Name"", ""DataType"", ""Mapping"", ""IsContentListProperty"")
                      VALUES (@Id, @Name, @DataType, @Mapping, @IsContentListProperty)
                      ON CONFLICT (""PropertyTypeId"") DO UPDATE SET
                          ""Name"" = EXCLUDED.""Name"",
                          ""DataType"" = EXCLUDED.""DataType"",
                          ""Mapping"" = EXCLUDED.""Mapping"",
                          ""IsContentListProperty"" = EXCLUDED.""IsContentListProperty""",
                    connection, transaction);
                cmd.Parameters.AddWithValue("@Id", pt.Id);
                cmd.Parameters.AddWithValue("@Name", pt.Name);
                cmd.Parameters.AddWithValue("@DataType", pt.DataType.ToString());
                cmd.Parameters.AddWithValue("@Mapping", pt.Mapping);
                cmd.Parameters.AddWithValue("@IsContentListProperty", pt.IsContentListProperty ? Yes : No);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async STT.Task UpsertNodeTypesAsync(List<NodeTypeData> nodeTypes,
            NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            foreach (var nt in nodeTypes)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""NodeTypes"" (""NodeTypeId"", ""ParentId"", ""Name"", ""ClassName"", ""Properties"")
                      VALUES (@Id, @ParentId, @Name, @ClassName, @Properties)
                      ON CONFLICT (""NodeTypeId"") DO UPDATE SET
                          ""ParentId"" = EXCLUDED.""ParentId"",
                          ""Name"" = EXCLUDED.""Name"",
                          ""ClassName"" = EXCLUDED.""ClassName"",
                          ""Properties"" = EXCLUDED.""Properties""",
                    connection, transaction);
                cmd.Parameters.AddWithValue("@Id", nt.Id);
                var parentId = nodeTypes.FirstOrDefault(x => x.Name == nt.ParentName)?.Id;
                cmd.Parameters.AddWithValue("@ParentId", (object)parentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Name", nt.Name);
                cmd.Parameters.AddWithValue("@ClassName", nt.ClassName);
                cmd.Parameters.AddWithValue("@Properties", string.Join(" ", nt.Properties));
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async STT.Task UpsertContentListTypesAsync(List<ContentListTypeData> contentListTypes,
            NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            foreach (var clt in contentListTypes)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO ""ContentListTypes"" (""ContentListTypeId"", ""Name"", ""Properties"")
                      VALUES (@Id, @Name, @Properties)
                      ON CONFLICT (""ContentListTypeId"") DO UPDATE SET
                          ""Name"" = EXCLUDED.""Name"",
                          ""Properties"" = EXCLUDED.""Properties""",
                    connection, transaction);
                cmd.Parameters.AddWithValue("@Id", clt.Id);
                cmd.Parameters.AddWithValue("@Name", clt.Name);
                cmd.Parameters.AddWithValue("@Properties", string.Join(" ", clt.Properties));
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
