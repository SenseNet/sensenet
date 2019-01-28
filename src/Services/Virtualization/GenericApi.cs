using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;

namespace SenseNet.Portal.Virtualization
{
    public class GenericApi
    {
        protected static void AssertPermission(string placeholderPath)
        {
            var permissionContent = NodeHead.Get(placeholderPath);
            if (permissionContent == null || !SecurityHandler.HasPermission(permissionContent.Id, PermissionType.RunApplication))
            {
                throw new SenseNetSecurityException("Access denied for " + placeholderPath);
            }
        }

        protected static void SetCurrentWorkspace(string workspacePath)
        {
            PortalContext.ContextWorkspaceResolver = p => Node.LoadNode(workspacePath) as Workspace;
        }
    }
}
