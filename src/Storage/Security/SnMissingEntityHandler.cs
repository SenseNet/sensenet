using SenseNet.ContentRepository.Storage;
using SenseNet.Security;

namespace SenseNet.ContentRepository.Storage.Security
{
    public class SnMissingEntityHandler : IMissingEntityHandler
    {
        public bool GetMissingEntity(int entityId, out int parentId, out int ownerId)
        {
            var nodeHead = NodeHead.Get(entityId);
            if (nodeHead == null)
            {
                parentId = 0;
                ownerId = 0;
                return false;
            }
            parentId = nodeHead.ParentId;
            ownerId = nodeHead.OwnerId;
            return true;
        }
    }
}
