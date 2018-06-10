using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ComponentVersionTests : TestBase
    {
        [TestMethod]
        public void VersionCheck_Services_Correct()
        {
            // set a fake assembly version for testing purposes
            var av = new Version(3, 3, 3, 3);
            //var services = new ServicesComponent {AssemblyVersion = av};
            var servicesInfo = new SnComponentInfo {AssemblyVersion = av};

            Assert.IsTrue(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, av));

            // it is allowed to have a component with a higher version than the assembly
            Assert.IsTrue(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor, av.Build, av.Revision + 1)));
            Assert.IsTrue(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor, av.Build + 1, av.Revision)));
            Assert.IsTrue(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor + 1, av.Build, av.Revision)));
            Assert.IsTrue(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major + 1, av.Minor, av.Build, av.Revision)));

            // This is the edge case where the assembly version's _revision_ number (the 4th one) 
            // is higher than the component version, which is allowed.
            Assert.IsTrue(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor, av.Build, av.Revision - 1)));

            Assert.IsFalse(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor, av.Build - 1)));
            Assert.IsFalse(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor - 1, av.Build)));
            Assert.IsFalse(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major - 1, av.Minor, av.Build)));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void VersionCheck_Services_Invalid()
        {
            var services = new ServicesComponent();

            // A nonexisting Services component should result in an error.
            Assert.IsTrue(services.IsComponentAllowed(null));
        }
    }
}
