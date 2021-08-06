using System.Reflection;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    /// <summary>
    /// Defines methods for providing information of a sensenet install package.
    /// </summary>
    public interface IInstallPackageDescriptor
    {
        Assembly GetPackageAssembly();
        string GetPackageName();
    }

    internal class InstallPackageDescriptor : IInstallPackageDescriptor
    {
        private readonly Assembly _assembly;
        private readonly string _packageName;

        public InstallPackageDescriptor(Assembly assembly, string packageName)
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
