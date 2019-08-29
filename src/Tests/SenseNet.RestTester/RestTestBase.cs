using System;
using System.Linq;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SenseNet.RestTester
{
    internal class RestTestBase
    {
        public static ServerContext[] Servers { get; private set; }

        public static void InitAllTests(string[] urls)
        {
            Console.Write("---- Initialize tests ...");

            Servers = urls.Select(u => new ServerContext
            {
                Url = u,
                Username = "admin",
                Password = "admin"
            }).ToArray();
            ClientContext.Initialize(Servers);
            ClientContext.Current.ChunkSizeInBytes = 419430;

            CleanupRootFolder();
            Console.WriteLine(" ok.");
        }

        public static void CleanupAllTests()
        {
            Console.Write("---- Cleanup after all tests ...");
            CleanupRootFolder();
            Console.WriteLine(" ok.");
        }

        private static void CleanupRootFolder()
        {
            var contents = Content.QueryForAdminAsync("+InFolder:/Root +(Name:CreateContent* Name:Test*)")
                .GetAwaiter().GetResult();
            Task.WaitAll(contents.Select(c => c.DeleteAsync()).ToArray());
        }
    }
}
