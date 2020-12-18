using System;
using System.Collections.Generic;
using SenseNet.BackgroundOperations;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;

namespace SenseNet.IntegrationTests.TestCases
{
    public abstract class TestCase
    {
        public IPlatform Platform { get; set; }

        /* ==================================================================== */

        private static string _lastPlatformName;
        private static RepositoryInstance _repositoryInstance;
        public void IntegrationTest(Action callback)
        {
            IntegrationTest(false, callback, null);
        }
        public void IntegrationTest(Action<SystemFolder> callback)
        {
            IntegrationTest(false, null, callback);
        }
        public void IsolatedIntegrationTest(Action callback)
        {
            IntegrationTest(true, callback, null);
        }
        public void IsolatedIntegrationTest(Action<SystemFolder> callback)
        {
            IntegrationTest(true, null, callback);
        }
        private void IntegrationTest(bool isolated, Action callback, Action<SystemFolder> callbackWithSandbox)
        {
            var platformName = Platform.GetType().Name;
            var brandNew = isolated || _repositoryInstance == null || platformName != _lastPlatformName;

            if (brandNew)
            {
                Logger.Log("  (cleanup repository)");
                _repositoryInstance?.Dispose();
                _repositoryInstance = null;
                _lastPlatformName = null;

                var builder = Platform.GetRepositoryBuilder();

                Logger.Log("  start new repository");
                _repositoryInstance = Repository.Start(builder);
                _lastPlatformName = platformName;

                //PrepareRepository();
            }

            SystemFolder sandbox = null;
            try
            {
                using (new SystemAccount())
                    if (callback != null) callback(); else callbackWithSandbox(sandbox = CreateSandbox());

            }
            finally
            {
                if(sandbox!=null)
                    using (new SystemAccount())
                        sandbox.ForceDelete();

                if (isolated)
                {
                    Logger.Log("  cleanup repository");
                    _repositoryInstance?.Dispose();
                    _repositoryInstance = null;
                    _lastPlatformName = null;
                }
            }
        }

        public static void CleanupClass()
        {
            Logger.Log("  ((cleanup repository))");
            _repositoryInstance?.Dispose();
            _repositoryInstance = null;
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

        /* ==================================================================== */

        protected SystemFolder CreateSandbox()
        {
            var sandbox = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
            sandbox.Save();
            return sandbox;
        }
    }
}
