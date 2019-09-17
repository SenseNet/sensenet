﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.OData
{
    public enum ODataResponseType
    {
        NoContent,
        ContentNotFound,
        ServiceDocument,
        SingleContent,
        Error,
        Int
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
        public static ODataResponse CreateSingleContentResponse(Dictionary<string, object> fieldData)
        {
            return new ODataResponse(ODataResponseType.Error, fieldData);
        }
    }

}
