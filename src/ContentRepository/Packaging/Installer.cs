using SenseNet.ContentRepository.Storage.Data;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Packaging
{
    public static class Installer
    {
        /// <summary>
        /// Installer method for sensenet that uses an embedded resource as the install package.
        /// Do not use this method directly from your code. Use a higher level
        /// installer method instead.
        /// </summary>
        /// <param name="assembly">The assembly that contains the embedded package.</param>
        /// <param name="packageName">Name of the package, including folder prefixes inside the assembly.</param>
        /// <param name="repositoryBuilder">Repository builder for starting the repo during the install process.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static async Task InstallSenseNetAsync(Assembly assembly, string packageName, 
            RepositoryBuilder repositoryBuilder, CancellationToken cancellationToken)
        {
            // switch off indexing so that the first repo start does not require a working index
            var origIndexingValue = repositoryBuilder.StartIndexingEngine;
            repositoryBuilder.StartIndexingEngine(false);

            await LogLineAsync(repositoryBuilder, "Accessing sensenet database...").ConfigureAwait(false);

            // Make sure that the database exists and contains the schema
            // necessary for importing initial content items.
            var dbExists = await DataStore.IsDatabaseReadyAsync(cancellationToken).ConfigureAwait(false);
            
            if (!dbExists)
            {
                await LogLineAsync(repositoryBuilder, "Installing database...").ConfigureAwait(false);
                var timer = Stopwatch.StartNew();

                await DataStore.InstallDatabaseAsync(repositoryBuilder.InitialData, cancellationToken)
                    .ConfigureAwait(false);

                await LogLineAsync(repositoryBuilder, "Database installed.").ConfigureAwait(false);

                // install custom security entries if provided
                if (repositoryBuilder.InitialData?.Permissions?.Count > 0)
                {
                    using (Repository.Start(repositoryBuilder))
                    {
                        await LogLineAsync(repositoryBuilder, "Installing default security structure.").ConfigureAwait(false);
                        
                        SecurityHandler.SecurityInstaller.InstallDefaultSecurityStructure(repositoryBuilder.InitialData);
                    }
                }

                // execute the install package
                await InstallPackageAsync(assembly, packageName, repositoryBuilder, cancellationToken)
                    .ConfigureAwait(false);

                timer.Stop();

                await LogLineAsync(repositoryBuilder, $"Database install finished. Elapsed time: {timer.Elapsed}")
                    .ConfigureAwait(false);
            }
            else
            {
                // If the database already exists, we assume that it also contains
                // all the necessary content items.
                await LogLineAsync(repositoryBuilder, "Database already exists.").ConfigureAwait(false);
            }
            
            // Reset the original indexing setting so that subsequent packages use the 
            // same value as intended by the caller. 
            repositoryBuilder.StartIndexingEngine(origIndexingValue);
        }

        public static async Task InstallPackageAsync(Assembly assembly, string packageName,
            RepositoryBuilder repositoryBuilder, CancellationToken cancellationToken)
        {
            // prepare package: extract it to the file system
            var packageFolder = UnpackEmbeddedPackage(assembly, packageName, repositoryBuilder.Console);

            Logger.PackageName = packageName;
            Logger.Create(LogLevel.Default);

            await LogLineAsync(repositoryBuilder, $"Executing package {packageName}...").ConfigureAwait(false);

            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            PackageManager.Execute(packageFolder, currentDirectory, 0, null, 
                repositoryBuilder.Console, repositoryBuilder);
        }
        
        private static string UnpackEmbeddedPackage(Assembly assembly, string packageName, TextWriter console)
        {
            if (string.IsNullOrEmpty(packageName))
                throw new ArgumentNullException(nameof(packageName));
            
            var zipTarget = Path.GetFileNameWithoutExtension(packageName);

            console?.WriteLine("Package directory: " + zipTarget);

            if (Directory.Exists(zipTarget))
            {
                Directory.Delete(zipTarget, true);
                    console?.WriteLine("Old files and directories are deleted.");
            }
            else
            {
                Directory.CreateDirectory(zipTarget);
                console?.WriteLine("Package directory created.");
            }

            console?.WriteLine("Extracting ...");

            using (var resourceStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{packageName}"))
            {
                if (resourceStream == null)
                    throw new InvalidOperationException($"Package {packageName} not found.");

                Unpacker.Unpack(resourceStream, zipTarget);
            }

            console?.WriteLine("Ok.");

            return zipTarget;
        }

        private static async Task LogLineAsync(RepositoryBuilder builder, string text)
        {
            if (builder.Console != null)
                await builder.Console.WriteLineAsync(text).ConfigureAwait(false);
        }
    }
}
