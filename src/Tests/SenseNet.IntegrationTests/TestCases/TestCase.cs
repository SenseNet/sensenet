using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.IntegrationTests.Platforms;

namespace SenseNet.IntegrationTests.TestCases
{
    public abstract class TestCase
    {
        public IPlatform Platform { get; set; }

        /* ==================================================================== */

        public void IntegrationTest(Action callback)
        {
            var builder = Platform.GetRepositoryBuilder();

            using (var _ = Repository.Start(builder))
            using (new SystemAccount())
                callback();
        }
        //UNDONE:<?: Consider the instructions in the following block
        //public void IntegrationTest(Action callback)
        //{
        //    Cache.Reset();
        //    ContentTypeManager.Reset();
        //    //Providers.Instance.NodeTypeManeger = null;

        //    var builder = _implementation.GetRepositoryBuilder();

        //    Indexing.IsOuterSearchEngineEnabled = true;

        //    Cache.Reset();
        //    ContentTypeManager.Reset();

        //    using (Repository.Start(builder))
        //    using (new SystemAccount())
        //        callback();
        //}

    }
}
