using System;
using System.Reflection.Metadata.Ecma335;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public abstract class TestCaseBase
    {
        public IPlatform Platform { get; set; }
        //UNDONE:<?IntT: Call from every base control-method.
        public Action<RepositoryBuilder> TestInitializer { get; set; }

        /* ==================================================================== */

        //private static string _lastPlatformName;
        //private static RepositoryInstance _repositoryInstance;

        public void NoRepoIntegrationTest(Action callback)
        {
            var platformName = Platform.GetType().Name;
            var builder = Platform.CreateRepositoryBuilder();
            TestInitializer?.Invoke(builder);
            callback();
        }

        public void IntegrationTest(Action callback)
        {
            IntegrationTest(null, callback, null);
        }
        public void IntegrationTest(Action<RepositoryBuilder> initialize, Action callback)
        {
            IntegrationTest(initialize, callback, null);
        }
        public void IntegrationTest(Action<SystemFolder> callback)
        {
            IntegrationTest(null, null, callback);
        }
        public void IntegrationTest(Action<RepositoryBuilder> initialize, Action<SystemFolder> callback)
        {
            IntegrationTest(initialize, null, callback);
        }
        private void IntegrationTest(Action<RepositoryBuilder> initialize,
            Action callback, Action<SystemFolder> callbackWithSandbox)
        {
            var builder = Platform.CreateRepositoryBuilder();
            initialize?.Invoke(builder);

            using (var repository = Repository.Start(builder))
            {
                SystemFolder sandbox = null;
                try
                {
                    using (new SystemAccount())
                    {
                        if (callback != null)
                            callback();
                        else
                            callbackWithSandbox(sandbox = CreateSandbox());
                    }
                }
                finally
                {
                    if(sandbox!=null)
                        using (new SystemAccount())
                            sandbox.ForceDelete();
                }
            }
        }

        public void IntegrationTest<T>(Action<T> callback) where T : GenericContent
        {
            IntegrationTest(null, null, callback);
        }
        public void IntegrationTest<T>(Action<RepositoryBuilder> initialize, Action<T> callback) where T : GenericContent
        {
            IntegrationTest(initialize, null, callback);
        }
        private void IntegrationTest<T>(Action<RepositoryBuilder> initialize,
            Action callback, Action<T> callbackWithSandbox) where T : GenericContent
        {
            var builder = Platform.CreateRepositoryBuilder();
            initialize?.Invoke(builder);

            using (var repository = Repository.Start(builder))
            {
                T sandbox = null;
                try
                {
                    using (new SystemAccount())
                    {
                        if (callback != null)
                            callback();
                        else
                            callbackWithSandbox(sandbox = CreateSandbox<T>());
                    }

                }
                finally
                {
                    if (sandbox != null)
                        using (new SystemAccount())
                            sandbox.ForceDelete();
                }
            }
        }

        public Task IntegrationTestAsync(Func<Task> callback)
        {
            return IntegrationTestAsync(null, callback, null);
        }
        public Task IntegrationTestAsync(Action<RepositoryBuilder> initialize, Func<Task> callback)
        {
            return IntegrationTestAsync(initialize, callback, null);
        }
        public Task IntegrationTestAsync(Func<SystemFolder, Task> callback)
        {
            return IntegrationTestAsync(null, null, callback);
        }
        public Task IntegrationTestAsync(Action<RepositoryBuilder> initialize, Func<SystemFolder, Task> callback)
        {
            return IntegrationTestAsync(initialize, null, callback);
        }
        private async Task IntegrationTestAsync(Action<RepositoryBuilder> initialize,
            Func<Task> callback, Func<SystemFolder, Task> callbackWithSandbox)
        {
            var builder = Platform.CreateRepositoryBuilder();
            initialize?.Invoke(builder);

            using (var repository = Repository.Start(builder))
            {
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
                }
            }
        }

        [Obsolete("##", true)]
        public static void CleanupClass()
        {
        }


        //UNDONE:<?IntT: Consider the instructions in the following block
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
        protected T CreateSandbox<T>() where T : GenericContent
        {
            var sandbox = Content.CreateNew(typeof(T).Name, Repository.Root, Guid.NewGuid().ToString());
            sandbox.Save();
            return (T)sandbox.ContentHandler;
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
