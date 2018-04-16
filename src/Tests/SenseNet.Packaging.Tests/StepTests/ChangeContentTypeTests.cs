using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Tests;

namespace SenseNet.Packaging.Tests.StepTests
{
    [TestClass]
    public class ChangeContentTypeTests : TestBase
    {
        private static StringBuilder _log;
        [TestInitialize]
        public void PrepareTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);
        }

        [TestMethod]
        public void Step_ChangeContentType_Parse()
        {
            
        }
    }
}
