
 // ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Implementations are able to validate the executability of the Save, Move and Delete content operations.
    /// Warning! When implementing this intarface do not modify the properties of the provided objects.
    /// </summary>
    public interface INodeOperationValidator
    {
        /// <summary>
        /// Checks the executability of the saving operation of the target Node based on the changed data.
        /// Returns true if the operation is allowed.
        /// Returns false and provides an error message in the out parameter if the operation is not allowed.
        /// A disallowed operation causes an InvalidOperationException with the provided message.
        /// Warning! Do not modify the properties of the provided objects.
        /// </summary>
        bool CheckSaving(Node node, ChangedData[] changeData, out string errorMessage);

        /// <summary>
        /// Checks the executability of the move operation of the source and target Node instances.
        /// Returns true if the operation is allowed.
        /// Returns false and provides an error message in the out parameter if the operation is not allowed.
        /// A disallowed operation causes an InvalidOperationException with the provided message.
        /// Warning! Do not modify the properties of the provided objects.
        /// </summary>
        bool CheckMoving(Node source, Node target, out string errorMessage);

        /// <summary>
        /// Checks the executability of the delete operation of the target Node instance.
        /// Returns true if the operation is allowed.
        /// Returns false and provides an error message in the out parameter if the operation is not allowed.
        /// A disallowed operation causes an InvalidOperationException with the provided message.
        /// Warning! Do not modify the properties of the provided objects.
        /// </summary>
        bool CheckDeletion(Node node, out string errorMessage);
    }
}
