using System;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines properties and methods related to a sensenet component - for example Services or Workspaces.
    /// </summary>
    public interface ISnComponent
    {
        /// <summary>
        /// Identifier of the component. Implementing classes must provide a unique value here.
        /// </summary>
        string ComponentId { get; }

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
