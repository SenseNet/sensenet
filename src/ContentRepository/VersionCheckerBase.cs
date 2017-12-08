using System;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines a base class for component version checking. Derived classes will make sure
    /// that a component that is installed into the repository is able to work with a currently 
    /// available library version. In most cases this will mean that if the library version is
    /// higher than the component's version, the system must not start until the admin updates
    /// that component. This default behavior is implemented in this base class, derived 
    /// classes in most cases will only have to provide the component id, nothing else.
    /// </summary>
    public abstract class VersionCheckerBase
    {
        /// <summary>
        /// Identifier of the component that this version checker is implemented for.
        /// </summary>
        public abstract string ComponentId { get; }

        /// <summary>
        /// Assembly version to check component version against. This property
        /// was created only to make this feature testable. In production this
        /// value is computed from the assembly of the derived class.
        /// </summary>
        internal Version AssemblyVersion { get; set; }

        /// <summary>
        /// Checks whether this component is able to work with the available version of the assembly.
        /// </summary>
        /// <param name="componentVersion">The currently installed version of the component.</param>
        /// <returns>True if the assembly and component versions are compatible.</returns>
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
