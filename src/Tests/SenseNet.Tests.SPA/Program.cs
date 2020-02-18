using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Tests.SPA
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
                    .UseAccessProvider(new UserAccessProvider())
                    .UseLogger(new SnFileSystemEventLogger())
                    .UseTracer(new SnFileSystemTracer());
            }))
            {
                using (new SystemAccount())
                {
                    // FOR TESTING PURPOSES: create a default user with a well-known password
                    var parentPath = "/Root/IMS/BuiltIn/Temp";
                    var parent = RepositoryTools.CreateStructure(parentPath, "OrganizationalUnit");

                    var user = new User(parent.ContentHandler)
                    {
                        Name = "edvin-example.com",
                        LoginName = "edvin@example.com",
                        PasswordHash =
                        "AQAAAAEAACcQAAAAEKEsynr6baKE5rYqS4Rn6pjqckl+NG4W9UQqqGh4g23zlJQpQvnaZnzx44+z78FVsg==",
                        Email = "edvin@example.com"
                    };
                    user.Save();

                    // create a doclib that contains a file
                    parent = RepositoryTools.CreateStructure("/Root/MyContent", "SystemFolder");
                    var docLib = RepositoryTools.CreateStructure("/Root/MyContent/MyFiles", "DocumentLibrary");

                    var file = new File(docLib.ContentHandler) {Name = "testfile.txt"};
                    file.Binary.SetStream(RepositoryTools.GetStreamFromString($"temp text data {DateTime.UtcNow}"));
                    file.Save();
                }

                SnTrace.EnableAll();
                host.Run();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
