using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services.Wopi
{
    internal class WopiNodeOperationValidator : INodeOperationValidator
    {
        public bool CheckSaving(Node node, ChangedData[] changeData, out string errorMessage)
        {
            errorMessage = null;

            // Do not check newly created node
            if (changeData == null)
                return true;

            // Changing metadata is always allowed except rename
            var isFileContentChanged = changeData.Any(x => x.Name == "Binary");
            var isNameChanged = changeData.Any(x => x.Name == "Name");
            if (!isFileContentChanged && !isNameChanged)
                return true;

            // Changing file content is always allowed for Wopi
            var expectedSharedLock = node.GetCachedData(WopiService.ExpectedSharedLock);
            var isWopiPutFile = !string.IsNullOrEmpty(expectedSharedLock as string);
            if (isWopiPutFile)
                return true;

            // Everything is allowed if the node is not locked
            var existingLock = SharedLock.GetLock(node.Id);
            if (existingLock == null)
                return true;

            throw new LockedNodeException(node.Lock, "The content is already open elsewhere.");
        }
        public bool CheckMoving(Node source, Node target, out string errorMessage)
        {
            errorMessage = null;

            // Everything is allowed if the file is not locked
            var existingLock = SharedLock.GetLock(source.Id);
            if (existingLock == null)
                return true;

            throw new LockedNodeException(source.Lock, "The content is already open elsewhere.");
        }
        public bool CheckDeletion(Node node, out string errorMessage)
        {
            errorMessage = null;

            // Everything is allowed if the file is not locked
            var existingLock = SharedLock.GetLock(node.Id);
            if (existingLock == null)
                return true;

            throw new LockedNodeException(node.Lock, "The content is already open elsewhere.");
        }
    }
}
