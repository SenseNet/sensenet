using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlDataProviderTests : IntegrationTest<MsSqlDataProviderPlatform, DataProviderTestCases>
    {
        private MsSqlDataContext CreateDataContext(CancellationToken cancellation)
        {
            return new MsSqlDataContext(ConnectionStrings.ConnectionString, new DataOptions(), cancellation);
        }
        private async Task<int[]> GetReferencesFromDbAsync(int versionId, int propertyTypeId, CancellationToken cancellation)
        {
            var sql = "SELECT ReferredNodeId FROM ReferenceProperties " +
                      "WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId";

            using (var ctx = CreateDataContext(cancellation))
            {
                var resultArray = await ctx.ExecuteReaderAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                        ctx.CreateParameter("@PropertyTypeId", DbType.Int32, propertyTypeId)

                    });
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    var result = new List<int>();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        result.Add(reader.GetSafeInt32(0));
                    }
                    return result.ToArray();
                }).ConfigureAwait(false);

                return resultArray.Length == 0 ? null : resultArray;
            }
        }
        private int[] GetReferencesFromDb(int versionId, int propertyTypeId)
        {
            return GetReferencesFromDbAsync(versionId, propertyTypeId, CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task CleanupAsync(IEnumerable<int> nodeIds, IEnumerable<int> versionIds, CancellationToken cancellation)
        {
            var nodeIdString = string.Join(", ", nodeIds);
            var versionIdString = string.Join(", ", versionIds);
            var sql = @$"DELETE FROM Nodes WHERE NodeId IN ({nodeIdString})
DELETE FROM Versions WHERE VersionId IN ({versionIdString})
DELETE FROM LongTextProperties WHERE VersionId IN ({versionIdString})
DELETE FROM ReferenceProperties WHERE VersionId IN ({versionIdString})
DELETE FROM Files WHERE FileId IN (SELECT FileId FROM BinaryProperties WHERE VersionId IN ({versionIdString}))
DELETE FROM BinaryProperties WHERE VersionId IN ({versionIdString})
";
            using (var ctx = CreateDataContext(cancellation))
            {
                var _ = await ctx.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
            }
        }
        private void Cleanup(IEnumerable<int> nodeIds, IEnumerable<int> versionIds)
        {
            CleanupAsync(nodeIds, versionIds, CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }


        [TestMethod] public void UT_MsSql_DP_Node_InsertDraft() { TestCase.UT_Node_InsertDraft(Cleanup); }
        [TestMethod] public void UT_MsSql_DP_Node_InsertPublic() { TestCase.UT_Node_InsertPublic(Cleanup); }
        [TestMethod] public void UT_MsSql_DP_Node_UpdateFirstDraft() { TestCase.UT_Node_UpdateFirstDraft(Cleanup); }

        [TestMethod] public void UT_MsSql_DP_RefProp_Insert() { TestCase.UT_RefProp_Insert(GetReferencesFromDb, Cleanup); }
        [TestMethod] public void UT_MsSql_DP_RefProp_Load() { TestCase.UT_RefProp_Load(GetReferencesFromDb, Cleanup); }
        [TestMethod] public void UT_MsSql_DP_RefProp_Update() { TestCase.UT_RefProp_Update(GetReferencesFromDb, Cleanup); }
        [TestMethod] public void UT_MsSql_DP_RefProp_Update3to0() { TestCase.UT_RefProp_Update3to0(GetReferencesFromDb, Cleanup); }
        [TestMethod] public void UT_MsSql_DP_RefProp_Update0to3() { TestCase.UT_RefProp_Update0to3(GetReferencesFromDb, Cleanup); }
        [TestMethod] public void UT_MsSql_DP_RefProp_NewVersionAndUpdate() { TestCase.UT_RefProp_NewVersionAndUpdate(GetReferencesFromDb, Cleanup); }
    }
}
