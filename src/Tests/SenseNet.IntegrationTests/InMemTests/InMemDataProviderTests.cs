using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemDataProviderTests : IntegrationTest<InMemPlatform, DataProviderTestCases>
    {
        InMemoryDataBase GetDatabase()
        {
            return ((InMemoryDataProvider)Providers.Instance.DataProvider).DB;
        }

        private int[] GetReferencesFromDb(int versionId, int propertyTypeId)
        {
            var refData = GetDatabase().ReferenceProperties
                .FirstOrDefault(x => x.VersionId == versionId && x.PropertyTypeId == propertyTypeId);
            return refData?.Value.ToArray();
        }

        //[TestMethod] public void IntT_InMem_DP_RefProp_Install() { TestCase.DP_RefProp_Install(GetReferencesFromDb); }
        //[TestMethod] public void IntT_InMem_DP_RefProp_Insert() { TestCase.DP_RefProp_Insert(GetReferencesFromDb); }
        //[TestMethod] public void IntT_InMem_DP_RefProp_Update() { TestCase.DP_RefProp_Update(GetReferencesFromDb); }
        //[TestMethod] public void IntT_InMem_DP_RefProp_Delete() { TestCase.DP_RefProp_Delete(GetReferencesFromDb); }
    }
}
