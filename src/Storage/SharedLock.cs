using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage
{
    public class SharedLock
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(20d);

        public static void AssociateLock(int contentId, string @lock, TimeSpan? timeout = null)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: AssociateLock
        }
        public static string RefreshLock(int contentId, string @lock)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: RefreshLock
        }
        public static string ModifyLock(int contentId, string @lock, string newLock)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: RefreshLock
        }
        public static string GetLock(int contentId)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: GetLock
        }
        public static string Unlock(int contentId, string @lock)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: Unlock
        }
    }
}
