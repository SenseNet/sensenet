using SenseNet.ContentRepository.Storage.Data;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Packaging;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.InstallData
{
    //UNDONE: move the whole install api to the Packaging layer
    public class Installer
    {
        public static async Task InstallSenseNetAsync(Action<RepositoryBuilder> buildRepository,
            CancellationToken cancellationToken)
        {
            const string installPackageName = "install-services-core.zip";

            var builder = new RepositoryBuilder();

            // switch off indexing so that the first repo start does not require a working index
            builder.StartIndexingEngine(false);

            buildRepository(builder);

            await builder.LogLineAsync("Accessing sensenet database...").ConfigureAwait(false);

            var dbResult = await DataStore.EnsureInitialDatabase(builder.InitialData, cancellationToken)
                .ConfigureAwait(false);

            // If the database was installed just now, add initial repository items:
            // - default security entries
            // - default install package

            if (dbResult == DatabaseStateResult.Installed)
            { 
                await builder.LogLineAsync("Database installed.").ConfigureAwait(false);

                if (builder.InitialData?.Permissions?.Count > 0)
                {
                    using (Repository.Start(builder))
                    {
                        await builder.LogLineAsync("Installing default security structure.").ConfigureAwait(false);
                        
                        SecurityHandler.SecurityInstaller.InstallDefaultSecurityStructure(builder.InitialData);
                    }
                }

                // prepare install package: save it to the file system and extract
                var packageFolder = await UnpackEmbeddedPackageAsync(installPackageName, builder.Console)
                    .ConfigureAwait(false);

                Logger.Create(LogLevel.Default);

                await builder.LogLineAsync("Executing install package...").ConfigureAwait(false);

                var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                PackageManager.Execute(packageFolder, currentDirectory, 0, null, builder.Console, builder);
            }
            else
            {
                await builder.LogLineAsync("Database already exists.").ConfigureAwait(false);
            }

            // after-install log ----------------------------------------------------------------------------------

            using (Repository.Start(builder))
            {
                await LogComponentsAsync(builder.Console).ConfigureAwait(false);
            }
        }

        private static async Task<string> UnpackEmbeddedPackageAsync(string packageName, TextWriter console)
        {
            if (console != null)
                await console.WriteLineAsync($"Writing package {packageName} to file system...").ConfigureAwait(false);

            using (var resourceStream = typeof(Installer).Assembly.GetManifestResourceStream($"SenseNet.Services.InstallData.{packageName}"))
            {
                if (resourceStream == null)
                    throw new InvalidOperationException($"Package {packageName} not found.");

                using (var fileStream = System.IO.File.OpenWrite(packageName))
                {
                    await resourceStream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }

            var packageFolder = Unpack(packageName, console);

            return packageFolder;
        }

        private static async Task LogComponentsAsync(TextWriter console)
        {
            if (console == null)
                return;

            await console.WriteLineAsync("Installed components:").ConfigureAwait(false);
            await console.WriteLineAsync("-----------------------------------------------------------------")
                .ConfigureAwait(false);

            foreach (var component in RepositoryVersionInfo.Instance.Components)
            {
                await console.WriteLineAsync($"{component.ComponentId.PadRight(50)}\t{component.Version}")
                    .ConfigureAwait(false);
            }
        }

        private static string Unpack(string package, TextWriter console)
        {
            //var pkgFolder = Path.GetDirectoryName(package);
            //var zipTarget = Path.Combine(pkgFolder, Path.GetFileNameWithoutExtension(package));
            var zipTarget = Path.GetFileNameWithoutExtension(package);
            var packageZipPath = package.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase)
                ? package
                : package + ".zip";
            var isZipExist = System.IO.File.Exists(packageZipPath);

            if (Directory.Exists(package) && !isZipExist)
            {
                console?.WriteLine("Package directory: " + package);
                return package;
            }

            console?.WriteLine("Package directory: " + zipTarget);

            if (Directory.Exists(zipTarget))
            {
                if (isZipExist)
                {
                    Directory.Delete(zipTarget, true);
                    console?.WriteLine("Old files and directories are deleted.");
                }
            }
            else
            {
                Directory.CreateDirectory(zipTarget);
                console?.WriteLine("Package directory created.");
            }

            if (isZipExist)
            {
                console?.WriteLine("Extracting ...");
                Unpacker.Unpack(packageZipPath, zipTarget);
                console?.WriteLine("Ok.");
            }

            return zipTarget;
        }
    }
}
