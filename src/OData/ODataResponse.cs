using System;
using System.Collections.Generic;
using System.Text;

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
        ActionsProperty,
        ActionsPropertyRaw,

        Int,
        RawData,
    }

    /// <summary>
    /// Represents an immutable response object for <see cref="ODataRequest"/>.
    /// </summary>
    public class ODataResponse
    {
        /// <summary>
        /// Key name in the HttpContext.Items
        /// </summary>
        public static readonly string Key = "SnODataResponse";

        public virtual ODataResponseType Type { get; }
        public virtual object Value { get; }

        public ODataResponse(ODataResponseType type, object value)
        {
            Type = type;
            Value = value;
        }

        /* ====================================================================== Internal factory methods */

        internal static ODataResponse CreateNoContentResponse()
        {
            return new ODataResponse(ODataResponseType.NoContent, null);
        }
        internal static ODataResponse CreateContentNotFoundResponse()
        {
            return new ODataResponse(ODataResponseType.ContentNotFound, null);
        }
        internal static ODataResponse CreateErrorResponse(ODataException exception)
        {
            return new ODataResponse(ODataResponseType.Error, exception);
        }
        internal static ODataResponse CreateServiceDocumentResponse(string[] topLevelNames)
        {
            return new ODataResponse(ODataResponseType.ServiceDocument, topLevelNames);
        }
        internal static ODataResponse CreateSingleContentResponse(ODataContent fieldData)
        {
            return new ODataResponse(ODataResponseType.SingleContent, fieldData);
        }
        internal static ODataResponse CreateChildrenCollectionResponse(IEnumerable<ODataContent> data)
        {
            return new ODataResponse(ODataResponseType.ChildrenCollection, data);
        }
        internal static ODataResponse CreateCollectionCountResponse(int count)
        {
            return new ODataResponse(ODataResponseType.Int, count);
        }
        internal static ODataResponse CreateActionsPropertyResponse(ODataActionItem[] items)
        {
            return new ODataResponse(ODataResponseType.ActionsProperty, items);
        }
        internal static ODataResponse CreateActionsPropertyRawResponse(ODataActionItem[] items)
        {
            return new ODataResponse(ODataResponseType.ActionsPropertyRaw, items);
        }
        internal static ODataResponse CreateRawResponse(object data)
        {
            return new ODataResponse(ODataResponseType.RawData, data);
        }
        internal static ODataResponse CreateMultipleContentResponse(IEnumerable<ODataContent> items, int allCount)
        {
            //UNDONE:ODATA: Use allCount parameter
            return new ODataResponse(ODataResponseType.MultipleContent, items);
        }
    }

}
