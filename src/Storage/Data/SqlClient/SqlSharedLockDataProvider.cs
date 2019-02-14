using System;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    public class SqlSharedLockDataProvider : ISharedLockDataProviderExtension
    {
        public DataProvider MainProvider { get; set; }
        public void DeleteAllSharedLocks()
        {
            //UNDONE: not implemented DeleteAllSharedLocks
            throw new NotImplementedException();
        }

        public void CreateSharedLock(int contentId, string @lock)
        {
            //UNDONE: not implemented CreateSharedLock
            throw new NotImplementedException();
        }

        public string RefreshSharedLock(int contentId, string @lock)
        {
            //UNDONE: not implemented RefreshSharedLock
            throw new NotImplementedException();
        }

        public string ModifySharedLock(int contentId, string @lock, string newLock)
        {
            //UNDONE: not implemented ModifySharedLock
            throw new NotImplementedException();
        }

        public string GetSharedLock(int contentId)
        {
            //UNDONE: not implemented GetSharedLock
            throw new NotImplementedException();
        }

        public string DeleteSharedLock(int contentId, string @lock)
        {
            //UNDONE: not implemented DeleteSharedLock
            throw new NotImplementedException();
        }
    }
}
