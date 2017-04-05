using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Packaging
{
    internal interface IPackageStorageProviderFactory
    {
        IPackageStorageProvider CreateProvider();
    }

    internal class BuiltinPackageStorageProviderFactory : IPackageStorageProviderFactory
    {
        public IPackageStorageProvider CreateProvider()
        {
            return DataProvider.Current;
        }
    }
}
