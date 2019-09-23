using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
    /// Represents a response object for <see cref="ODataRequest"/>.
    /// </summary>
    public abstract class ODataResponse
    {
        public abstract ODataResponseType Type { get; }
        public abstract object Value { get; }

        //UNDONE:ODATA: Make "Write" abstract and implement in every response type.
        public async Task WriteAsync(HttpContext httpContext, ODataFormatter formatter)
        {
            var value = Value;
            if (value != null)
            {
                if (value is IEnumerable<ODataContent> enumerable)
                {
                    var sb = new StringBuilder();
                    await httpContext.Response.WriteAsync($"{Type}\n------------------\n");
                    foreach (var item in enumerable)
                        sb.AppendLine(item.Name);
                    await httpContext.Response.WriteAsync($"{sb}");
                    return;
                }
            }

            var stringValue = value?.ToString() ?? "{null}";
            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync($"{Type}: {stringValue}");
        }

        /* ====================================================================== Internal factory methods */

        internal static ODataServiceDocumentResponse CreateServiceDocumentResponse(string[] topLevelNames)
        {
            return new ODataServiceDocumentResponse(topLevelNames);
        }
        internal static ODataResponse CreateMetadataResponse(string entityPath)
        {
            return new ODataMetadataResponse(entityPath == "/" ? null : entityPath);
        }
        internal static ODataSingleContentResponse CreateSingleContentResponse(ODataContent fieldData)
        {
            return new ODataSingleContentResponse(fieldData);
        }
        internal static ODataChildrenCollectionResponse CreateChildrenCollectionResponse(IEnumerable<ODataContent> data, int allCount)
        {
            return new ODataChildrenCollectionResponse(data, allCount);
        }
        internal static ODataMultipleContentResponse CreateMultipleContentResponse(IEnumerable<ODataContent> items, int allCount)
        {
            return new ODataMultipleContentResponse(items, allCount);
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
