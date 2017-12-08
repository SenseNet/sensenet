using System;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines an interface for component version checking. Implementing classes will make sure
    /// that a component that is installed into the repository is able to work with a currently 
    /// available library version. In most cases this will mean that if the library version is
    /// higher than the component's version, the system must not start until the admin updates
    /// that component.
    /// </summary>
    public interface IVersionChecker
    {
        string ComponentId { get; }
        bool IsComponentAllowed(Version componentVersion);
    }
}
