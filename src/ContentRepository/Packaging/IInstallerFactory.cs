using System.Reflection;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    /// <summary>
    /// Defines methods for providing information of a sensenet install package.
    /// </summary>
    public interface IInstallerFactory
    {
        Assembly GetPackageAssembly();
        string GetPackageName();
    }

    internal class InstallerFactory : IInstallerFactory
    {
        private readonly Assembly _assembly;
        private readonly string _packageName;

        public InstallerFactory(Assembly assembly, string packageName)
        {
            _assembly = assembly;
            _packageName = packageName;
        }

        public Assembly GetPackageAssembly()
        {
            return _assembly;
        }

        public string GetPackageName()
        {
            return _packageName;
        }
    }
}
