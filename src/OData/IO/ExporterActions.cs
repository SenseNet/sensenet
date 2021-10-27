using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.Search;

namespace SenseNet.OData.IO
{
    public static class ExporterActions
    {
        /// <summary>
        /// Returns the count of all contents in the requested subtree.
        /// </summary>
        /// <param name="content">The root of the requested subtree.</param>
        /// <param name="oDataRequest">The current <see cref="ODataRequest"/> instance.</param>
        /// <returns>Count of contents.</returns>
        [ODataFunction]
        [AllowedRoles(N.R.Everyone)]
        public static int GetContentCountInTree(Content content, ODataRequest oDataRequest)
        {
            var query = oDataRequest.HasContentQuery ? $"+({oDataRequest.ContentQueryText}) " : string.Empty;
            query += $"+InTree:'{content.Path}' .AUTOFILTERS:OFF .COUNTONLY";
            var result = ContentQuery.Query(query);
            return result.Count;
        }
    }
}
