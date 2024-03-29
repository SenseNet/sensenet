﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Storage.DataModel.Usage;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository
{
    public interface IDatabaseUsageHandler
    {
        Task<DatabaseUsage> GetDatabaseUsageAsync(bool force, CancellationToken cancel);
    }
    public class DatabaseUsageHandler : IDatabaseUsageHandler
    {
        private static readonly string CacheKey = "DatabaseUsage";
        public static readonly string DatabaseUsageCachePath = "/Root/System/Cache/DatabaseUsage.cache";
        private static readonly TimeSpan DatabaseUsageCacheTime = TimeSpan.FromMinutes(5);
        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        private readonly ILogger<DatabaseUsageHandler> _logger;
        public DatabaseUsageHandler(ILogger<DatabaseUsageHandler> logger)
        {
            _logger = logger;
        }

        public async Task<DatabaseUsage> GetDatabaseUsageAsync(bool force, CancellationToken cancel)
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
                _logger.LogWarning("An error occurred during loading DatabaseUsage.cache: " + e);
                // do nothing
            }

            return (databaseUsage, cached);
        }

        private async STT.Task PutToCacheAsync(DatabaseUsage databaseUsage, File cached, CancellationToken cancel)
        {
            var resultBuilder = new StringBuilder();
            using (var writer = new StringWriter(resultBuilder))
                JsonSerializer.Create(SerializerSettings).Serialize(writer, databaseUsage);

            using (new SystemAccount())
            {
                cached ??= await CreateCacheFileAsync(cancel);

                if (cached == null)
                    return;

                var iteration = 0;
                
                try
                {
                    var serialized = resultBuilder.ToString();
                    var cachedStream = !cached.IsNew ? cached.Binary?.GetStream() : null;
                    var cachedData = cachedStream != null ? RepositoryTools.GetStreamString(cachedStream) : string.Empty;

                    // save the content only if there was a change
                    if (string.Equals(serialized, cachedData))
                        return;

                    Retrier.Retry(5, 500, typeof(NodeIsOutOfDateException), () =>
                    {
                        iteration++;
                        
                        // reload to have a fresh instance
                        if (!cached.IsNew && iteration > 1)
                            cached = Node.Load<File>(cached.Id);

                        cached.SetCachedData(CacheKey, databaseUsage);
                        cached.Binary.SetStream(RepositoryTools.GetStreamFromString(serialized));
                        cached.SaveAsync(SavingMode.KeepVersion, cancel).GetAwaiter().GetResult();

                        _logger.LogTrace($"DatabaseUsage.cache has been saved. Iteration: {iteration}");
                    });
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"An error occurred during saving DatabaseUsage.cache in iteration {iteration}.");
                    // do nothing
                }
            }
        }

        private async Task<File> CreateCacheFileAsync(CancellationToken cancel)
        {
            File file = null;
            try
            {
                var parentPath = RepositoryPath.GetParentPath(DatabaseUsageCachePath);
                var name = RepositoryPath.GetFileName(DatabaseUsageCachePath);
                var parent = RepositoryTools.CreateStructure(parentPath, "SystemFolder") ??
                             await Content.LoadAsync(parentPath, cancel).ConfigureAwait(false);

                file = new File(parent.ContentHandler) {Name = name};
            }
            catch (Exception e)
            {
                _logger.LogWarning("An error occurred during saving DatabaseUsage.cache: " + e);
            }
            return file;
        }

        private static readonly string ExclusiveBlockKey = "SenseNet.Storage.LoadDatabaseUsage";
        private DatabaseUsage _loadedDatabaseUsage;
        private async Task<DatabaseUsage> LoadDatabaseUsageAsync(CancellationToken cancel)
        {
            await ExclusiveBlock.RunAsync(ExclusiveBlockKey, Guid.NewGuid().ToString(),
                ExclusiveBlockType.WaitForReleased, new ExclusiveLockOptions(), cancel,
                async () =>
                {
                    var loader = new DatabaseUsageLoader(Providers.Instance.DataProvider);
                    var dbUsage = await loader.LoadAsync(cancel).ConfigureAwait(false);
                    _loadedDatabaseUsage = dbUsage;
                });
            return _loadedDatabaseUsage;
        }
    }
}
