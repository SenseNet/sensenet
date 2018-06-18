using System;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines properties and methods related to a sensenet component - for example Services or Workspaces.
    /// </summary>
    public interface ISnComponent
    {
        /// <summary>
        /// Gets the identifier of the component. Implementing classes must provide a unique value here.
        /// </summary>
        string ComponentId { get; }

        /// <summary>
        /// Gets the last allowed component version that is compatible with this component instance.
        /// If the component version found in the database is smaller than the version defined here,
        /// the version check algorithm will not allow the component to run and will throw an exception.
        /// This value cannot be greater than the version of the containing assembly otherwise
        /// an exception will be thrown.
        /// Null value means: supports only its own version.
        /// </summary>
        Version SupportedVersion { get; }

        /// <summary>
        /// Checks whether this component is able to work with the available version of the assembly.
        /// Implementing classes will make sure that a component that is installed into the repository 
        /// is able to work with a currently available library version. In most cases this will mean 
        /// that if the library version is higher than the component's version, the system must not 
        /// start until the admin updates that component. This default behavior is implemented 
        /// in the <see cref="SnComponent"/> base class, derived classes in most cases will 
        /// only have to provide the component id, nothing else.
        /// </summary>
        /// <param name="componentVersion">The currently installed version of the component.</param>
        /// <returns>True if the assembly and component versions are compatible.</returns>
        bool IsComponentAllowed(Version componentVersion);
    }
}
