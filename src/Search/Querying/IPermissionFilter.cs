namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Defines a permission checker method for authorize the query hit candidates.
    /// </summary>
    public interface IPermissionFilter
    {
        /// <summary>
        /// Authorizes a query hit candidate.
        /// </summary>
        bool IsPermitted(int nodeId, bool isLastPublic, bool isLastDraft);
    }
    public interface IPermissionFilterFactory
    {
        IPermissionFilter Create(SnQuery query, IQueryContext context);
    }
}
