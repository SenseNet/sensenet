// ReSharper disable once CheckNamespace
namespace SenseNet.Search.Querying
{
    internal class PermissionFilterFactory : IPermissionFilterFactory
    {
        public IPermissionFilter Create(SnQuery query, IQueryContext context)
        {
            return new PermissionFilter(query, context);
        }
    }
}