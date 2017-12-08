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
            var av = new Version(3, 3, 3, 3);
            var rvc = new RepositoryVersionChecker {AssemblyVersion = av};

            Assert.IsTrue(rvc.IsComponentAllowed(av));

            // it is allowed to have a component with a higher version than the assembly
            Assert.IsTrue(rvc.IsComponentAllowed(new Version(av.Major, av.Minor, av.Build, av.Revision + 1)));
            Assert.IsTrue(rvc.IsComponentAllowed(new Version(av.Major, av.Minor, av.Build + 1, av.Revision)));
            Assert.IsTrue(rvc.IsComponentAllowed(new Version(av.Major, av.Minor + 1, av.Build, av.Revision)));
            Assert.IsTrue(rvc.IsComponentAllowed(new Version(av.Major + 1, av.Minor, av.Build, av.Revision)));

            // This is the edge case where the assembly version's _revision_ number (the 4th one) 
            // is higher than the component version, which is allowed.
            Assert.IsTrue(rvc.IsComponentAllowed(new Version(av.Major, av.Minor, av.Build, av.Revision - 1)));

            Assert.IsFalse(rvc.IsComponentAllowed(new Version(av.Major, av.Minor, av.Build - 1)));
            Assert.IsFalse(rvc.IsComponentAllowed(new Version(av.Major, av.Minor - 1, av.Build)));
            Assert.IsFalse(rvc.IsComponentAllowed(new Version(av.Major - 1, av.Minor, av.Build)));
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
