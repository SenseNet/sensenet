using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public abstract class TestCaseBase
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

                var builder = Platform.CreateRepositoryBuilder();

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

        public Task IntegrationTestAsync(Func<Task> callback)
        {
            return IntegrationTestAsync(false, callback, null);
        }
        public Task IntegrationTestAsync(Func<SystemFolder, Task> callback)
        {
            return IntegrationTestAsync(false, null, callback);
        }
        public Task IsolatedIntegrationTestAsync(Func<Task> callback)
        {
            return IntegrationTestAsync(true, callback, null);
        }
        public Task IsolatedIntegrationTestAsync(Func<SystemFolder, Task> callback)
        {
            return IntegrationTestAsync(true, null, callback);
        }
        private async Task IntegrationTestAsync(bool isolated, Func<Task> callback, Func<SystemFolder, Task> callbackWithSandbox)
        {
            var platformName = Platform.GetType().Name;
            var brandNew = isolated || _repositoryInstance == null || platformName != _lastPlatformName;

            if (brandNew)
            {
                Logger.Log("  (cleanup repository)");
                _repositoryInstance?.Dispose();
                _repositoryInstance = null;
                _lastPlatformName = null;

                var builder = Platform.CreateRepositoryBuilder();

                Logger.Log("  start new repository");
                _repositoryInstance = Repository.Start(builder);
                _lastPlatformName = platformName;

                //PrepareRepository();
            }

            SystemFolder sandbox = null;
            try
            {
                using (new SystemAccount())
                {
                    if (callback != null)
                        await callback();
                    else
                        await callbackWithSandbox(sandbox = CreateSandbox());
                }
            }
            finally
            {
                if (sandbox != null)
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

        //    var builder = _implementation.CreateRepositoryBuilder();

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

        private class UserBlock : IDisposable
        {
            private IUser _originalUser;
            public UserBlock(IUser user)
            {
                _originalUser = User.Current;
                User.Current = user;
            }
            public void Dispose()
            {
                User.Current = _originalUser;
            }
        }
        public IDisposable CurrentUserBlock(IUser user)
        {
            return new UserBlock(user);
        }

    }
}
