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

    /// <summary>
    /// Defines an IPermissionFilter factory.
    /// </summary>
    public interface IPermissionFilterFactory
    {
        /// <summary>
        /// Returns with any implementation instance of the IPermissionFilterFactory.
        /// Parameters are help to decide any creation options.
        /// Called in every query execution.
        /// </summary>
        IPermissionFilter Create(SnQuery query, IQueryContext context);
    }
}
