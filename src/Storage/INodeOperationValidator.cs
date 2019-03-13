
 // ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Implementations are able to validate the executability of the Save, Move and Delete operations.
    /// Warning! If implement this intarface, do not modify any property of the given objects.
    /// </summary>
    public interface  INodeOperationValidator
    {
        /// <summary>
        /// Checks the saving operation executability by the target Node instance and the currently changed data.
        /// Returns true if the operation is allowed.
        /// Returns false and provides an error message in the out parameter if the operation is disabled.
        /// Disallowed operation causes an InvalidOperationException with the provided message.
        /// Warning! Do not modify any property of the given objects.
        /// </summary>
        bool CheckSaving(Node node, ChangedData[] changeData, out string errorMessage);

        /// <summary>
        /// Checks the Move operation executability by the source and target Node instances.
        /// Returns true if the operation is allowed.
        /// Returns false and provides an error message in the out parameter if the operation is disabled.
        /// Disallowed operation causes an InvalidOperationException with the provided message.
        /// Warning! Do not modify any property of the given object.
        /// </summary>
        bool CheckMoving(Node source, Node target, out string errorMessage);

        /// <summary>
        /// Checks the Delete operation executability by the target Node instance.
        /// Returns true if the operation is allowed.
        /// Returns false and provides an error message in the out parameter if the operation is disabled.
        /// Disallowed operation causes an InvalidOperationException with the provided message.
        /// Warning! Do not modify any property of the given object.
        /// </summary>
        bool CheckDeletion(Node node, out string errorMessage);
    }
}
