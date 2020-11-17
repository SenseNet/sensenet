using System;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.IntegrationTests
{
    public abstract class TestCaseBase
    {
        private IntegrationTestBase _implementation;

        public void SetImplementation(IntegrationTestBase implementation)
        {
            _implementation = implementation;
        }

        public void IntegrationTest(Action callback)
        {
            Cache.Reset();
            ContentTypeManager.Reset();
            //Providers.Instance.NodeTypeManeger = null;

            var builder = _implementation.GetRepositoryBuilder();

            Indexing.IsOuterSearchEngineEnabled = true;

            Cache.Reset();
            ContentTypeManager.Reset();

            using (Repository.Start(builder))
                using (new SystemAccount())
                    callback();
        }
    }

}
