using System;
using SenseNet.ContentRepository;

namespace SenseNet.IntegrationTests
{
    public abstract class MsSqlIntegrationTestBase<T> : IntegrationTestBase where T : TestCaseBase, new()
    {
        public T TestCases { get; }

        protected MsSqlIntegrationTestBase()
        {
            TestCases = new T();
            TestCases.SetImplementation(this);
        }

        public override RepositoryBuilder GetRepositoryBuilder()
        {
            throw new NotImplementedException();
        }
    }
}
