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

        private int[] GetReferencesFromDb(Node node, PropertyType propertyType)
        {
            var version = GetDatabase().Versions.FirstOrDefault(x => x.VersionId == node.VersionId);
            if (version == null)
                return null;
            var value = version.DynamicProperties[propertyType.Name] as IEnumerable<int>;
            return value == null ? null : value.ToArray();
        }

        [TestMethod] public void IntT_InMem_DP_RefProp_Install() { TestCase.DP_RefProp_Install(GetReferencesFromDb); }
        [TestMethod] public void IntT_InMem_DP_RefProp_Insert() { TestCase.DP_RefProp_Insert(GetReferencesFromDb); }
        [TestMethod] public void IntT_InMem_DP_RefProp_Update() { TestCase.DP_RefProp_Update(GetReferencesFromDb); }
        [TestMethod] public void IntT_InMem_DP_RefProp_Delete() { TestCase.DP_RefProp_Delete(GetReferencesFromDb); }
    }
}
