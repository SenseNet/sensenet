// ReSharper disable once CheckNamespace
namespace SenseNet.Search.Querying
{
    public class PermissionFilterFactory : IPermissionFilterFactory
    {
        public IPermissionFilter Create(SnQuery query, IQueryContext context)
        {
            return new PermissionFilter(query, context);
        }
    }
}