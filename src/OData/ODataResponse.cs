using System.Collections.Generic;
using SenseNet.OData.Responses;

namespace SenseNet.OData
{
    public enum ODataResponseType
    {
        NoContent,
        ContentNotFound,
        Error,
        ServiceDocument,
        SingleContent,
        ChildrenCollection,
        MultipleContent,
        CollectionCount,
        ActionsProperty,
        ActionsPropertyRaw,
        OperationCustomResult,
        RawData,
    }

    /// <summary>
    /// Represents an immutable response object for <see cref="ODataRequest"/>.
    /// </summary>
    public abstract class ODataResponse
    {
        /// <summary>
        /// Key name in the HttpContext.Items
        /// </summary>
        public static readonly string Key = "SnODataResponse";

        public abstract ODataResponseType Type { get; }
        public abstract object GetValue();

        /* ====================================================================== Internal factory methods */

        internal static ODataNoContentResponse CreateNoContentResponse()
        {
            return new ODataNoContentResponse();
        }
        internal static ODataContentNotFoundResponse CreateContentNotFoundResponse()
        {
            return new ODataContentNotFoundResponse();
        }
        internal static ODataErrorResponse CreateErrorResponse(ODataException exception)
        {
            return new ODataErrorResponse(exception);
        }
        internal static ODataServiceDocumentResponse CreateServiceDocumentResponse(string[] topLevelNames)
        {
            return new ODataServiceDocumentResponse(topLevelNames);
        }
        internal static ODataSingleContentResponse CreateSingleContentResponse(ODataContent fieldData)
        {
            return new ODataSingleContentResponse(fieldData);
        }
        internal static ODataChildrenCollectionResponse CreateChildrenCollectionResponse(IEnumerable<ODataContent> data)
        {
            return new ODataChildrenCollectionResponse(data);
        }
        internal static ODataCollectionCountResponse CreateCollectionCountResponse(int count)
        {
            return new ODataCollectionCountResponse(count);
        }
        internal static ODataActionsPropertyResponse CreateActionsPropertyResponse(ODataActionItem[] items)
        {
            return new ODataActionsPropertyResponse(items);
        }
        internal static ODataActionsPropertyRawResponse CreateActionsPropertyRawResponse(ODataActionItem[] items)
        {
            return new ODataActionsPropertyRawResponse(items);
        }
        internal static ODataRawResponse CreateRawResponse(object data)
        {
            return new ODataRawResponse(data);
        }
        internal static ODataMultipleContentResponse CreateMultipleContentResponse(IEnumerable<ODataContent> items, int allCount)
        {
            return new ODataMultipleContentResponse(items, allCount);
        }

        /// <summary>
        /// Creates an <see cref="ODataResponse"/> from a general object.
        /// </summary>
        /// <param name="result">The object that will be written.</param>
        /// <param name="allCount">A nullable int that contains the count of items in the result object if it is an enumerable and
        /// if the request specifies the total count of the collection ("$inlinecount=allpages"), otherwise the value is null.</param>
        /// <returns></returns>
        internal static ODataOperationCustomResultResponse CreateOperationCustomResultResponse(object result, int? allCount)
        {
            return new ODataOperationCustomResultResponse(result, allCount);
        }
    }

}
