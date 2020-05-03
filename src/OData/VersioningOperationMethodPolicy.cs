using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    internal class VersioningOperationMethodPolicy : IOperationMethodPolicy
    {
        public string Name { get; } = N.Pol.VersioningAndApproval;
        public OperationMethodVisibility GetMethodVisibility(IUser user, OperationCallingContext context)
        {
            return SavingAction.IsValidVersioningAction(context.Content?.ContentHandler, context.Operation.Name)
                ? OperationMethodVisibility.Enabled
                : OperationMethodVisibility.Invisible;
        }
    }
}
