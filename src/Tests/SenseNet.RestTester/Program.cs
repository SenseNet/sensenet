using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SenseNet.RestTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new ServerContext
            {
                Url = "http://localhost:3340/",
                Username = "admin",
                Password = "admin"
            };
            ClientContext.Initialize(new[] { server,  });
            ClientContext.Current.ChunkSizeInBytes = 419430;

            var root = GetRootAsync().Result;
            var topLevelCollection = GetChildrenAsync(root, server).Result;
            var topLevelCollectionNames = topLevelCollection.Select(c => c.Name).ToArray();

            var actual = string.Join(", ", topLevelCollectionNames);
            var expected = "(apps), IMS, Localization, System, Trash";
            AssertAreEqual(expected, actual);
        }

        private static void AssertAreEqual(string expected, string actual)
        {
            if (expected == actual)
                return;
            throw new Exception($"Assert are equal failed: Expected:<{expected}>. Actual:<{actual}>");
        }

        private static async Task<Content> GetRootAsync()
        {
            return await Content.LoadAsync("/Root");
        }
        private static async Task<IEnumerable<Content>> GetChildrenAsync(Content content, ServerContext server)
        {
            return await Content.LoadCollectionAsync(content.Path, server);
        }
    }
}
