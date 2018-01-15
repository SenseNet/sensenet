using SenseNet.Search;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Tests.Implementations
{
    public class EverythingAllowedPermissionFilterFactory : IPermissionFilterFactory
    {
        public IPermissionFilter Create(SnQuery query, IQueryContext context)
        {
            return new EverythingAllowedPermissionFilter();
        }
    }
    public class EverythingAllowedPermissionFilter : IPermissionFilter
    {
        public bool IsPermitted(int nodeId, bool isLastPublic, bool isLastDraft)
        {
            return true;
        }
    }
}
