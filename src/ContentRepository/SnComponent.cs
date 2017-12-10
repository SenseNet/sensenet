using System;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines a base class for sensenet ECM components. 
    /// </summary>
    public abstract class SnComponent : ISnComponent
    {
        /// <inheritdoc />
        public abstract string ComponentId { get; }

        /// <summary>
        /// Assembly version to check component version against. This property
        /// was created only to make this feature testable. In production this
        /// value is computed from the assembly of the derived class.
        /// </summary>
        internal Version AssemblyVersion { get; set; }

        /// <inheritdoc />
        public virtual bool IsComponentAllowed(Version componentVersion)
        {
            if (componentVersion == null)
                throw new InvalidOperationException($"{ComponentId} component is missing.");

            var assemblyVersion = AssemblyVersion ?? TypeHandler.GetVersion(GetType().Assembly);

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
