using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Testing;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public abstract class TestCaseBase
    {
        public IPlatform Platform { get; set; }
        //UNDONE:<?IntT: Call from every base control-method.
        public Action<RepositoryBuilder> TestInitializer { get; set; }

        /* ==================================================================== */

        public void NoRepoIntegrationTest(Action callback)
        {
            var platformName = Platform.GetType().Name;
            var builder = Platform.CreateRepositoryBuilder();
            TestInitializer?.Invoke(builder);
            callback();
        }
        public async Task NoRepoIntegrationTestAsync(Func<Task> callback)
        {
            var platformName = Platform.GetType().Name;
            var builder = Platform.CreateRepositoryBuilder();
            TestInitializer?.Invoke(builder);
            await callback().ConfigureAwait(false);
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
                Cache.Reset();
                ContentTypeManager.Reset();

                this.Platform.OnAfterRepositoryStart(repository);

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
                Cache.Reset();
                ContentTypeManager.Reset();

                this.Platform.OnAfterRepositoryStart(repository);

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
                Cache.Reset();
                ContentTypeManager.Reset();

                this.Platform.OnAfterRepositoryStart(repository);

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

        protected void AssertSequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var e = string.Join(", ", expected.Select(x => x.ToString()));
            var a = string.Join(", ", actual.Select(x => x.ToString()));
            Assert.AreEqual(e, a);
        }
        protected static ContentQuery CreateSafeContentQuery(string qtext, QuerySettings settings = null)
        {
            var cquery = ContentQuery.CreateQuery(qtext, settings ?? QuerySettings.AdminSettings);
            var cqueryAcc = new ObjectAccessor(cquery);
            cqueryAcc.SetFieldOrProperty("IsSafe", true);
            return cquery;
        }

        protected static readonly string CarContentType = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='Car' parentType='ListItem' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>Car,DisplayName</DisplayName>
  <Description>Car,Description</Description>
  <Icon>Car</Icon>
  <AllowIncrementalNaming>true</AllowIncrementalNaming>
  <Fields>
    <Field name='Name' type='ShortText'/>
    <Field name='Make' type='ShortText'/>
    <Field name='Model' type='ShortText'/>
    <Field name='Style' type='Choice'>
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <AllowExtraValue>true</AllowExtraValue>
        <Options>
          <Option value='Sedan' selected='true'>Sedan</Option>
          <Option value='Coupe'>Coupe</Option>
          <Option value='Cabrio'>Cabrio</Option>
          <Option value='Roadster'>Roadster</Option>
          <Option value='SUV'>SUV</Option>
          <Option value='Van'>Van</Option>
        </Options>
      </Configuration>
    </Field>
    <Field name='StartingDate' type='DateTime'/>
    <Field name='Color' type='Color'>
      <Configuration>
        <DefaultValue>#ff0000</DefaultValue>
        <Palette>#ff0000;#f0d0c9;#e2a293;#d4735e;#65281a</Palette>
      </Configuration>
    </Field>
    <Field name='EngineSize' type='ShortText'/>
    <Field name='Power' type='ShortText'/>
    <Field name='Price' type='Number'/>
    <Field name='Description' type='LongText'/>
  </Fields>
</ContentType>
";
        protected static void InstallCarContentType()
        {
            ContentTypeInstaller.InstallContentType(CarContentType);
        }

    }
}
