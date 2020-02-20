using SenseNet.ContentRepository.Storage.Data;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Packaging.Steps;
using File = System.IO.File;

namespace SenseNet.Packaging
{
    public class Installer
    {
        public RepositoryBuilder RepositoryBuilder { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Installer"/> class.
        /// If you construct this object without a RepositoryBuilder, please
        /// make sure you start the Content Repository manually before attempting
        /// to install or import anything.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        public Installer(RepositoryBuilder repositoryBuilder = null)
        {
            RepositoryBuilder = repositoryBuilder;
        }

        /// <summary>
        /// Installer method for sensenet that uses an embedded resource as the install package.
        /// Do not use this method directly from your code. Use a higher level
        /// installer method instead.
        /// </summary>
        /// <param name="assembly">The assembly that contains the embedded package.</param>
        /// <param name="packageName">Name of the package, including folder prefixes inside the assembly.</param>
        public Installer InstallSenseNet(Assembly assembly, string packageName)
        {
            // switch off indexing so that the first repo start does not require a working index
            var origIndexingValue = RepositoryBuilder.StartIndexingEngine;
            RepositoryBuilder.StartIndexingEngine(false);

            Logger.PackageName = packageName;
            Logger.Create(LogLevel.Default);
            Logger.LogMessage("Accessing sensenet database...");

            // Make sure that the database exists and contains the schema
            // necessary for importing initial content items.
            var dbExists = DataStore.IsDatabaseReadyAsync(CancellationToken.None).GetAwaiter().GetResult();
            
            if (!dbExists)
            {
                Logger.LogMessage("Installing database...");
                var timer = Stopwatch.StartNew();

                DataStore.InstallDatabaseAsync(RepositoryBuilder.InitialData, CancellationToken.None).GetAwaiter().GetResult();

                Logger.LogMessage("Database installed.");

                // install custom security entries if provided
                if (RepositoryBuilder.InitialData?.Permissions?.Count > 0)
                {
                    using (Repository.Start(RepositoryBuilder))
                    {
                        Logger.LogMessage("Installing default security structure...");
                        
                        SecurityHandler.SecurityInstaller.InstallDefaultSecurityStructure(RepositoryBuilder.InitialData);
                    }
                }

                // prepare package: extract it to the file system
                var packageFolder = UnpackEmbeddedPackage(assembly, packageName);

                ExecutePackage(packageFolder);

                timer.Stop();

                Logger.LogMessage($"Database install finished. Elapsed time: {timer.Elapsed}");
            }
            else
            {
                // If the database already exists, we assume that it also contains
                // all the necessary content items.
                Logger.LogMessage("Database already exists.");
            }
            
            // Reset the original indexing setting so that subsequent packages use the 
            // same value as intended by the caller. 
            RepositoryBuilder.StartIndexingEngine(origIndexingValue);

            return this;
        }

        /// <summary>
        /// Installs an SnAdmin package embedded into the provided assembly.
        /// </summary>
        /// <param name="assembly">The assembly that contains the package zip.</param>
        /// <param name="packageName">Name of the embedded package resource.</param>
        /// <param name="parameters">Optional package parameters.</param>
        public Installer InstallPackage(Assembly assembly, string packageName, params PackageParameter[] parameters)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (string.IsNullOrEmpty(packageName))
                throw new ArgumentNullException(nameof(packageName));

            Logger.PackageName = packageName;
            Logger.Create(LogLevel.Default);

            // prepare package: extract it to the file system
            var packageFolder = UnpackEmbeddedPackage(assembly, packageName);

            ExecutePackage(packageFolder, parameters);

            return this;
        }
        /// <summary>
        /// Installs an SnAdmin package from the specified file system location.
        /// </summary>
        /// <param name="packagePath">Path of the package: either a zip file or a package folder.</param>
        /// <param name="parameters">Optional package parameters.</param>
        public Installer InstallPackage(string packagePath, params PackageParameter[] parameters)
        {
            if (string.IsNullOrEmpty(packagePath))
                throw new ArgumentNullException(nameof(packagePath));

            Logger.PackageName = Path.GetFileName(packagePath);
            Logger.Create(LogLevel.Default);

            // prepare package: extract it to the file system
            var packageFolder = UnpackFileSystemPackage(packagePath);

            ExecutePackage(packageFolder, parameters);

            return this;
        }

        /// <summary>
        /// Imports content items from the file system to the repository.
        /// </summary>
        /// <param name="sourcePath">File system path of a content item or folder to import.</param>
        /// <param name="targetPath">Target container in the repository. Default: the Root.</param>
        public Installer Import(string sourcePath, string targetPath = null)
        {
            Logger.PackageName = "import";
            Logger.Create(LogLevel.Default);

            if (RepositoryBuilder == null)
            {
                ImportBase.Import(sourcePath, targetPath);
            }
            else using (Repository.Start(RepositoryBuilder))
            {
                ImportBase.Import(sourcePath, targetPath);
            }

            return this;
        }

        private void ExecutePackage(string packageFolder, params PackageParameter[] parameters)
        {
            Logger.LogMessage($"Executing package {Path.GetFileName(packageFolder)}...");

            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            PackageManager.Execute(packageFolder, currentDirectory, 0, parameters,
                RepositoryBuilder.Console, RepositoryBuilder);
        }

        private static string UnpackFileSystemPackage(string packagePath)
        {
            var pkgFolder = Path.GetDirectoryName(packagePath);
            var pkgName = Path.GetFileNameWithoutExtension(packagePath);
            var zipTarget = string.IsNullOrEmpty(pkgFolder) ? pkgName : Path.Combine(pkgFolder, pkgName);
            var packageZipPath = packagePath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase)
                ? packagePath
                : packagePath + ".zip";
            var isZipExist = File.Exists(packageZipPath);

            if (Directory.Exists(packagePath) && !isZipExist)
            {
                Logger.LogMessage("Package directory: " + packagePath);
                return packagePath;
            }

            Logger.LogMessage("Package directory: " + zipTarget);
            
            if (Directory.Exists(zipTarget))
            {
                if (isZipExist)
                {
                    Directory.Delete(zipTarget, true);
                    Directory.CreateDirectory(zipTarget);
                    Logger.LogMessage("Old files and directories are deleted.");
                }
            }
            else
            {
                Directory.CreateDirectory(zipTarget);
                Logger.LogMessage("Package directory created.");
            }

            if (isZipExist)
            {
                Logger.LogMessage("Extracting ...");
                Unpacker.Unpack(packageZipPath, zipTarget);
                Logger.LogMessage("Ok.");
            }

            return zipTarget;
        }
        private static string UnpackEmbeddedPackage(Assembly assembly, string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                throw new ArgumentNullException(nameof(packageName));
            
            var zipTarget = Path.GetFileNameWithoutExtension(packageName);

            // probing: try the resource name with and without the assembly name prefix
            var packageNameWithPrefix = $"{assembly.GetName().Name}.{packageName}";
            var resourceName = EmbeddedPackageExists(assembly, packageName)
                ? packageName
                : EmbeddedPackageExists(assembly, packageNameWithPrefix)
                    ? packageNameWithPrefix
                    : null;

            if (string.IsNullOrEmpty(resourceName))
                throw new PackagingException($"Package {packageName} does not exist in assembly {assembly.FullName}.");

            Logger.LogMessage("Unpacking embedded package to: " + zipTarget);

            if (Directory.Exists(zipTarget))
            {
                Directory.Delete(zipTarget, true);
                Logger.LogMessage("Old files and directories are deleted.");
            }
            else
            {
                Directory.CreateDirectory(zipTarget);
                Logger.LogMessage("Package directory created.");
            }

            Logger.LogMessage("Extracting ...");

            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                    throw new InvalidOperationException($"Package {packageName} not found.");

                Unpacker.Unpack(resourceStream, zipTarget);
            }

            Logger.LogMessage("Ok.");

            return zipTarget;
        }
        private static bool EmbeddedPackageExists(Assembly assembly, string packageName)
        {
            if (assembly == null || string.IsNullOrEmpty(packageName))
                return false;
            
            return assembly.GetManifestResourceNames().Contains(packageName);
        }
    }
}
