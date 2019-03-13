using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Implementations are able to validate the executability of the Save, Move and Delete operations.
    /// Warning! If implement this intarface, do not modify any property of the given objects.
    /// </summary>
    public interface  INodeOperationValidator
    {
        bool CheckSaving(Node node, ChangedData[] changeData, out string errorMessage);
        bool CheckMoving(Node source, Node target, out string errorMessage);
        bool CheckDeletion(Node node, out string errorMessage);
    }
}
