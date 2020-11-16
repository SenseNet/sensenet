using System;
using SenseNet.ContentRepository;

namespace SenseNet.IntegrationTests
{
    internal class RepositoryManager
    {
        public IDisposable StartRepository(TestCaseBase testCaseClass)
        {
            RepositoryBuilder builder = testCaseClass.GetRepositoryBuilder();
            return Repository.Start(builder);
        }
    }

    /* =========================================================================== TEST CASES */

    public abstract class TestCaseBase
    {
        public void IntegrationTest(Action callback)
        {
            using (new RepositoryManager().StartRepository(this))
            {
                callback();
            }
        }

        public abstract RepositoryBuilder GetRepositoryBuilder();
    }
    public class TestCases1 : TestCaseBase
    {
        public override RepositoryBuilder GetRepositoryBuilder()
        {
            throw new NotImplementedException();
        }

        public void TestCase_Experimental1()
        {
            IntegrationTest(() =>
            {
                // ASSIGN

                // ACTION

                //ASSERT
            });
        }
    }

    /* =========================================================================== TEST IMPLEMENTATIONS */

    public abstract class IntegrationTestBase
    {
        public abstract TestCaseBase TestCases { get; }

        public abstract RepositoryBuilder GetRepositoryBuilder();
    }
}
