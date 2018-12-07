using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage
{
    public class SharedLock
    {
        public static void RemoveAllLocks()
        {
            DataProvider.Current.DeleteAllSharedLocks();
        }

        public static void Lock(int contentId, string @lock)
        {
            DataProvider.Current.CreateSharedLock(contentId, @lock);
        }
        public static string RefreshLock(int contentId, string @lock)
        {
            return DataProvider.Current.RefreshSharedLock(contentId, @lock);
        }
        public static string ModifyLock(int contentId, string @lock, string newLock)
        {
            return DataProvider.Current.ModifySharedLock(contentId, @lock, newLock);
        }
        public static string GetLock(int contentId)
        {
            return DataProvider.Current.GetSharedLock(contentId);
        }
        public static string Unlock(int contentId, string @lock)
        {
            return DataProvider.Current.DeleteSharedLock(contentId, @lock);
        }
    }
}
