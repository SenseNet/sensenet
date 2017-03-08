using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;

namespace SenseNet.Portal.Virtualization
{
    public class GenericApi
    {
        protected static void AssertPermission(string placeholderPath)
        {
            var permissionContent = Node.LoadNode(placeholderPath);
            if (permissionContent == null || !permissionContent.Security.HasPermission(PermissionType.RunApplication))
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
