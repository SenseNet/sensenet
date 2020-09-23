using System.Diagnostics;
using SenseNet.ContentRepository.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    /// <summary>
    /// Represents an installer action that will be executed only if the current component
    /// does not exist in the database.
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public class ComponentInstaller : SnPatchBase
    {
        /// <summary>
        /// Gets the type of the patch. In this case PackageType.Install
        /// </summary>
        public override PackageType Type => PackageType.Install;

        public override string ToString()
        {
            return $"{ComponentId}: {Version}";
        }
    }
}
