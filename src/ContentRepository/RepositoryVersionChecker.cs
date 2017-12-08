using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    internal class RepositoryVersionChecker : IVersionChecker
    {
        public string ComponentId => "SenseNet.Services";
        public bool IsComponentAllowed(Version componentVersion)
        {
            if (componentVersion == null)
                throw new InvalidOperationException("Services component is missing.");

            var assemblyVersion = TypeHandler.GetVersion(typeof(RepositoryVersionChecker).Assembly);

            // To be able to publish code hotfixes, we allow the revision number (the 4th element) 
            // to be higher in the assembly (in case every other part equals). This assumes 
            // that every repository change raises at least the build number (the 3rd one) 
            // in the component's version.
            if (componentVersion.Major == assemblyVersion.Major &&
                componentVersion.Minor == assemblyVersion.Minor &&
                componentVersion.Build == assemblyVersion.Build)
                return true;

            return componentVersion >= assemblyVersion;
        }
    }
}
