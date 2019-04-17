using System;
using SenseNet.Configuration;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Data
{
    public static class SharedLockDataProviderExtensions
    {
        public static IRepositoryBuilder UseSharedLockDataProviderExtension(this IRepositoryBuilder builder, ISharedLockDataProviderExtension provider)
        {
var backup = DataStore.Enabled;
DataStore.Enabled = false;
            DataProvider.Instance.SetExtension(typeof(ISharedLockDataProviderExtension), provider); //DB:ok
DataStore.Enabled = backup;
            return builder;
        }
    }

    /// <summary>
    /// Defines methods for handling shared lock storage.
    /// </summary>
    public interface ISharedLockDataProviderExtension : IDataProviderExtension
    {
        TimeSpan SharedLockTimeout { get; }

        void DeleteAllSharedLocks();
        void CreateSharedLock(int contentId, string @lock);
        string RefreshSharedLock(int contentId, string @lock);
        string ModifySharedLock(int contentId, string @lock, string newLock);
        string GetSharedLock(int contentId);
        string DeleteSharedLock(int contentId, string @lock);
        void CleanupSharedLocks();

        /// <summary>
        /// Designed for test purposes. DO NOT USE THIS METHOD IN YOUR CODE.
        /// </summary>
        void SetSharedLockCreationDate(int nodeId, DateTime value);
        /// <summary>
        /// Designed for test purposes. DO NOT USE THIS METHOD IN YOUR CODE.
        /// </summary>
        DateTime GetSharedLockCreationDate(int nodeId);
    }
}
