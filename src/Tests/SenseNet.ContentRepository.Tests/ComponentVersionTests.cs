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
            var servicesInfo = new SnComponentInfo
            {
                AssemblyVersion = av,
                SupportedVersion = av
            };

            Assert.IsTrue(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, av));
            
            // it is NOT allowed to have an assembly with an older version than the installed component
            Assert.IsFalse(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor, av.Build + 1, av.Revision)));
            Assert.IsFalse(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor + 1, av.Build, av.Revision)));
            Assert.IsFalse(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major + 1, av.Minor, av.Build, av.Revision)));

            // This is the edge case where the assembly version's _revision_ number (the 4th one) 
            // is different than the component version, which is allowed.
            Assert.IsTrue(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor, av.Build, av.Revision - 1)));
            Assert.IsTrue(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor, av.Build, av.Revision + 1)));

            // This should be false, because the supported version is set to the current version that means
            // the component can work only with the same version as the component version in the assembly.
            Assert.IsFalse(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor, av.Build - 1)));
            Assert.IsFalse(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major, av.Minor - 1, av.Build)));
            Assert.IsFalse(RepositoryVersionInfo.IsComponentAllowed(servicesInfo, new Version(av.Major - 1, av.Minor, av.Build)));
        }

        [TestMethod]
        public void VersionCheck_Services_Invalid()
        {
            var services = new ServicesComponent();

            // The Services component does not override the base algorithm that always returns true.
            Assert.IsTrue(services.IsComponentAllowed(null));
        }
    }
}
