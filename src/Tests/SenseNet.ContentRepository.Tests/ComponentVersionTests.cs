using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ComponentVersionTests
    {
        [TestMethod]
        public void VersionChecker_Repository_Correct()
        {
            var rvc = new RepositoryVersionChecker();
            var av = rvc.GetType().Assembly.GetName().Version;

            var cv1 = new Version(av.Major, av.Minor, av.Build + 1);
            var cv2 = new Version(av.Major, av.Minor + 1, av.Build);
            var cv3 = new Version(av.Major + 1, av.Minor, av.Build);
            var cv4 = new Version(av.Major - 1, av.Minor, av.Build);
            
            Assert.IsTrue(rvc.IsComponentAllowed(cv1));
            Assert.IsTrue(rvc.IsComponentAllowed(cv2));
            Assert.IsTrue(rvc.IsComponentAllowed(cv3));
            Assert.IsFalse(rvc.IsComponentAllowed(cv4));

            // Unfortunately we cannot test for that important edge case when the assembly version's 
            // _revision_ number (the 4th one) is higher than the component version, because we cannot
            // set a testable assembly version for the Services component here.

            //UNDONE: put assembly version into an internal property modifiable by tests
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void VersionChecker_Repository_Invalid()
        {
            var rvc = new RepositoryVersionChecker();

            // A nonexisting Services component should result in an error.
            Assert.IsTrue(rvc.IsComponentAllowed(null));
        }
    }
}
