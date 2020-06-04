using System;
using System.IO;
using System.Threading;
using System.Xml;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ODataTest.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = CreateWebHostBuilder(args);
            var host = builder.Build();

            SnTrace.EnableAll();

            using (InMemoryExtensions.StartInMemoryRepository(repositoryBuilder =>
            {
                repositoryBuilder
                    .UseAccessProvider(new DesktopAccessProvider())
                    .UseLogger(new SnFileSystemEventLogger())
                    .UseTracer(new SnFileSystemTracer());
            }))
            {
                SnTrace.EnableAll();
                InitializeDatabase();
                host.Run();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

        private static void InitializeDatabase()
        {
            var path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\..\nuget\snadmin\install-services-core\manifest.xml"));
            string manifestXml;
            using (var reader = new StreamReader(path))
                manifestXml = reader.ReadToEnd();
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            var root = xml.DocumentElement;

            var package = new Package
            {
                ComponentId = root.SelectSingleNode("Id").InnerText,
                Description = root.SelectSingleNode("Description").InnerText,
                PackageType = Enum.Parse<PackageType>(root.Attributes["type"].Value),
                ReleaseDate = DateTime.Parse(root.SelectSingleNode("ReleaseDate").InnerText),
                ComponentVersion = Version.Parse(root.SelectSingleNode("Version").InnerText),
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = ExecutionResult.Successful,
                Manifest = manifestXml
            };
            var provider = DataStore.GetDataProviderExtension<IPackagingDataProviderExtension>();
            provider.SavePackageAsync(package, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
