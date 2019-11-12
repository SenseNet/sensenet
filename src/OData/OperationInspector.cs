using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    public class OperationInspector
    {
        public static OperationInspector Instance { get; set; } = new OperationInspector();

        public virtual bool CheckBeforeInvoke(IUser user, OperationCallingContext context)
        {
            return false;
        }

        public virtual bool CheckByRoles(IUser user, string[] roles)
        {
            //UNDONE: call appropriate method of sensenet.
            return false;
        }

        public virtual bool CheckByPermissions(Content content, IUser user, string[] permissions)
        {
            //UNDONE: call appropriate method of sensenet.
            return false;
        }
    }
}
