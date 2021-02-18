using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Storage.DataModel.Usage;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository
{
    public class DatabaseUsageHandler
    {
        private static readonly string CacheKey = "1";
        private static readonly string DatabaseUsageCachePath = "/Root/System/Cache/DatabaseUsage.cache";
        private static readonly TimeSpan DatabaseUsageCacheTime = TimeSpan.FromMinutes(5);
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        private ILogger<SnILogger> _logger;
        public DatabaseUsageHandler(ILogger<SnILogger> logger)
        {
            _logger = logger;
        }

        //UNDONE:<?usage: Test: "force" true/false"
        public async Task<DatabaseUsage> GetDatabaseUsage(bool force, CancellationToken cancel)
        {
            var (dbUsage, cached) = await GetFromCache(cancel).ConfigureAwait(false);
            if (dbUsage != null && !force)
                return dbUsage;

            dbUsage = await LoadDatabaseUsageAsync(cancel).ConfigureAwait(false);

            await PutToCacheAsync(dbUsage, cached, cancel).ConfigureAwait(false);

            return dbUsage;
        }

        private async Task<(DatabaseUsage DbUsage, File Cached)> GetFromCache(CancellationToken cancel)
        {
            File cached = null;
            DatabaseUsage databaseUsage = null;

            try
            {
                using (new SystemAccount())
                {
                    cached = await Node.LoadAsync<File>(DatabaseUsageCachePath, cancel)
                        .ConfigureAwait(false);
                }

                if (cached != null)
                {
                    if (DateTime.UtcNow - cached.ModificationDate <= DatabaseUsageCacheTime)
                    {
                        databaseUsage = (DatabaseUsage) cached.GetCachedData(CacheKey);
                        if (databaseUsage == null)
                        {
                            var src = RepositoryTools.GetStreamString(cached.Binary.GetStream());
                            databaseUsage = JsonConvert.DeserializeObject<DatabaseUsage>(src);
                            cached.SetCachedData(CacheKey, databaseUsage);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning("An error occured during loading DatabaseUsage.cache: " + e);
                // do nothing
            }

            return (databaseUsage, cached);
        }

        private async STT.Task PutToCacheAsync(DatabaseUsage databaseUsage, File cached, CancellationToken cancel)
        {
            var resultBuilder = new StringBuilder();
            using (var writer = new StringWriter(resultBuilder))
                JsonSerializer.Create(SerializerSettings).Serialize(writer, databaseUsage);

            if (cached == null)
                cached = await CreateCacheFileAsync(cancel);

            cached.SetCachedData(CacheKey, databaseUsage);
            var serialized = resultBuilder.ToString();

            try
            {
                cached.Binary.SetStream(RepositoryTools.GetStreamFromString(serialized));
                using (new SystemAccount())
                    cached.Save();
            }
            catch(Exception e)
            {
                _logger.LogWarning("An error occured during saving DatabaseUsage.cache: " + e);
                // do nothing
            }
        }

        private async Task<File> CreateCacheFileAsync(CancellationToken cancel)
        {
            var parentPath = RepositoryPath.GetParentPath(DatabaseUsageCachePath);
            var name = RepositoryPath.GetFileName(DatabaseUsageCachePath);
            var parent = await EnsureFolderAsync(parentPath, cancel).ConfigureAwait(false);
            var file = new File(parent) { Name = name };
            return file;
        }
        private async Task<Node> EnsureFolderAsync(string path, CancellationToken cancel)
        {
            var parentPath = RepositoryPath.GetParentPath(path);
            var name = RepositoryPath.GetFileName(path);
            var parent = await Node.LoadNodeAsync(parentPath, cancel).ConfigureAwait(false);
            if (parent == null)
                parent = await EnsureFolderAsync(parentPath, cancel).ConfigureAwait(false);
            var folder = new SystemFolder(parent) { Name = name };
            folder.Save();
            return folder;
        }

        private static readonly string ExclusiveBlockKey = "SenseNet.Storage.LoadDatabaseUsage";
        private async Task<DatabaseUsage> LoadDatabaseUsageAsync(CancellationToken cancel)
        {
            await ExclusiveBlock.RunAsync(ExclusiveBlockKey, Guid.NewGuid().ToString(),
                ExclusiveBlockType.WaitForReleased, new ExclusiveLockOptions(), cancel,
                LoadDatabaseUsageAsync);
            return _loadedDatabaseUsage;
        }

        private DatabaseUsage _loadedDatabaseUsage;
        private async STT.Task LoadDatabaseUsageAsync()
        {
            var dbUsage = new DatabaseUsage(Providers.Instance.DataProvider);
            await dbUsage.BuildAsync().ConfigureAwait(false);
            _loadedDatabaseUsage = dbUsage;
        }
    }
}
