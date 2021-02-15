using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Storage.DataModel.Usage;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.Services.Core.Operations
{
    public static class DiagnosticOperations
    {
        private static readonly string DatabaseUsageCachePath = "/Root/System/DatabaseUsage";
        private static readonly TimeSpan DatabaseUsageCacheTime = TimeSpan.FromMinutes(5);
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<string> GetDatabaseUsage(Content content, HttpContext httpContext)
        {
            File cached = null;
            try
            {
                cached = await Node.LoadAsync<File>(DatabaseUsageCachePath, httpContext.RequestAborted)
                    .ConfigureAwait(false);
                if (cached != null)
                {
                    if (DateTime.UtcNow - cached.ModificationDate <= DatabaseUsageCacheTime)
                        return RepositoryTools.GetStreamString(cached.Binary.GetStream());
                }
            }
            catch
            {
                // do nothing
            }


            var profile = new DatabaseUsage(Providers.Instance.DataProvider);
            await profile.BuildProfileAsync(httpContext.RequestAborted);

            var resultBuilder = new StringBuilder();
            using (var writer = new StringWriter(resultBuilder))
                JsonSerializer.Create(SerializerSettings).Serialize(writer, profile);

            if (cached == null)
            {
                var parentPath = RepositoryPath.GetParentPath(DatabaseUsageCachePath);
                var name = RepositoryPath.GetFileName(DatabaseUsageCachePath);
                var parent = await Node.LoadNodeAsync(parentPath, httpContext.RequestAborted).ConfigureAwait(false);
                cached = new File(parent) {Name = name};
            }

            var result = resultBuilder.ToString();
            cached.Binary.SetStream(RepositoryTools.GetStreamFromString(result));
            cached.Save();

            return result;
        }
    }
}
