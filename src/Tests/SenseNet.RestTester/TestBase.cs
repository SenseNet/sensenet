using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client;

namespace SenseNet.RestTester
{
    internal class TestBase
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
            var contents = Content.QueryForAdminAsync("Name:CreateContent*").Result
                .Union(Content.QueryForAdminAsync("Name:Test*").Result)
                .ToArray();
            Task.WaitAll(contents.Select(c => c.DeleteAsync()).ToArray());
        }
    }
}
