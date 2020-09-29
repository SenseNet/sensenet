using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines methods for packaging database operations.
    /// </summary>
    public interface IPackagingDataProviderExtension : IDataProviderExtension
    {
        /// <summary>
        /// Loads all installed components. The descriptions come from installers
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of <see cref="ComponentInfo"/> objects.</returns>
        Task<IEnumerable<ComponentInfo>> LoadInstalledComponentsAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Loads all components that not successfully installed.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of <see cref="ComponentInfo"/> objects.</returns>
        Task<IEnumerable<ComponentInfo>> LoadIncompleteComponentsAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Loads all installed packages.
        /// </summary>
        /// <remarks>Every package execution will appear in this list, even faulty ones. Subsequent versions
        /// of the same component are also represented by separate items in this list, so this is
        /// a package install history.
        /// To get a list of installed components, use the <see cref="LoadInstalledComponentsAsync"/> method.</remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of <see cref="Package"/> objects.</returns>
        Task<IEnumerable<Package>> LoadInstalledPackagesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Saves a package to the database after execution.
        /// </summary>
        /// <param name="package">The package to save.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task SavePackageAsync(Package package, CancellationToken cancellationToken);
        /// <summary>
        /// Updates package information in the database.
        /// </summary>
        /// <param name="package">The package to update.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task UpdatePackageAsync(Package package, CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether a package exists in the database for the provided component, package type and version.
        /// </summary>
        /// <param name="componentId">Component id.</param>
        /// <param name="packageType">Package type.</param>
        /// <param name="version">Package version.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps true if the package exists.</returns>
        Task<bool> IsPackageExistAsync(string componentId, PackageType packageType, Version version, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes a package from the database by its id.
        /// </summary>
        /// <param name="package">The package to delete.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeletePackageAsync(Package package, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes all packages from the database.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteAllPackagesAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Loads the manifest xml of a package.
        /// After the operation the package object provided as a parameter
        /// should contain the loaded manifest xml.
        /// </summary>
        /// <param name="package">The package to load the manifest for.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task LoadManifestAsync(Package package, CancellationToken cancellationToken);
    }
}
