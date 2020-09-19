using System;
using SenseNet.Packaging;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines a base class for sensenet components. 
    /// </summary>
    public abstract class SnComponent : ISnComponent
    {
        /// <inheritdoc />
        public abstract string ComponentId { get; }

        /// <inheritdoc />
        public virtual Version SupportedVersion => null;

        /// <inheritdoc />
        public virtual bool IsComponentAllowed(Version componentVersion)
        {
            return true;
        }

        public ISnPatch[] Patches { get; } = new ISnPatch[0];
    }
}
