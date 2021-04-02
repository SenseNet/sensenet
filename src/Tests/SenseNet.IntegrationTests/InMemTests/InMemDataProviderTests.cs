using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemDataProviderTests : IntegrationTest<InMemPlatform, DataProviderTestCases>
    {
        private InMemoryDataBase GetDatabase()
        {
            return ((InMemoryDataProvider)Providers.Instance.DataProvider).DB;
        }

        private int[] GetReferencesFromDb(int versionId, int propertyTypeId)
        {
            var refData = GetDatabase().ReferenceProperties
                .FirstOrDefault(x => x.VersionId == versionId && x.PropertyTypeId == propertyTypeId);
            return refData?.Value.ToArray();
        }
        private void Cleanup(IEnumerable<int> nodeIds, IEnumerable<int> versionIds)
        {
            var db = GetDatabase();
            db.Nodes.Clear();
            db.Versions.Clear();
            db.LongTextProperties.Clear();
            db.ReferenceProperties.Clear();
            db.BinaryProperties.Clear();
            db.Files.Clear();
        }

        [TestMethod] public void UT_InMem_DP_Node_InsertDraft() { TestCase.UT_Node_InsertDraft(Cleanup); }
        [TestMethod] public void UT_InMem_DP_Node_InsertPublic() { TestCase.UT_Node_InsertPublic(Cleanup); }
        [TestMethod] public void UT_InMem_DP_Node_UpdateFirstDraft() { TestCase.UT_Node_UpdateFirstDraft(Cleanup); }

        [TestMethod] public void UT_InMem_DP_RefProp_Insert() { TestCase.UT_RefProp_Insert(GetReferencesFromDb, Cleanup); }
        [TestMethod] public void UT_InMem_DP_RefProp_Load() { TestCase.UT_RefProp_Load(GetReferencesFromDb, Cleanup); }
        [TestMethod] public void UT_InMem_DP_RefProp_Update() { TestCase.UT_RefProp_Update(GetReferencesFromDb, Cleanup); }
        [TestMethod] public void UT_InMem_DP_RefProp_Update3to0() { TestCase.UT_RefProp_Update3to0(GetReferencesFromDb, Cleanup); }
        [TestMethod] public void UT_InMem_DP_RefProp_Update0to3() { TestCase.UT_RefProp_Update0to3(GetReferencesFromDb, Cleanup); }
        [TestMethod] public void UT_InMem_DP_RefProp_NewVersionAndUpdate() { TestCase.UT_RefProp_NewVersionAndUpdate(GetReferencesFromDb, Cleanup); }

        [TestMethod] public void UT_InMem_DP_RefProp_DeleteNode() { TestCase.UT_RefProp_DeleteNode(GetReferencesFromDb, Cleanup); }
    }
}
