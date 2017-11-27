using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Portal.OData
{
    /// <summary>
    /// Represents the qualification of an error in the OData response.
    /// </summary>
    public enum ODataExceptionCode
    {
        /// <summary>Means: error is not qualified.</summary>
        NotSpecified,
        /// <summary>General request error.</summary>
        RequestError,
        /// <summary>Content id is in wrong format.</summary>
        InvalidId,
        /// <summary>The value is invalid in the $top parameter.</summary>
        InvalidTopParameter,
        /// <summary>The value is negative in the $top parameter.</summary>
        NegativeTopParameter,
        /// <summary>The value is invalid in the $skip parameter.</summary>
        InvalidSkipParameter,
        /// <summary>The value is negative in the $skip parameter.</summary>
        NegativeSkipParameter,
        /// <summary>The value is invalid in the $inlinecount parameter.</summary>
        InvalidInlineCountParameter,
        /// <summary>The value is invalid in the $format parameter.</summary>
        InvalidFormatParameter,
        /// <summary>The value is invalid in the $orderby parameter.</summary>
        InvalidOrderByParameter,
        /// <summary>The direction is invalid in the $orderby parameter.</summary>
        InvalidOrderByDirectionParameter,
        /// <summary>The value is invalid in the $expand parameter.</summary>
        InvalidExpandParameter,
        /// <summary>The value is invalid in the $select parameter.</summary>
        InvalidSelectParameter,
        /// <summary>Means: cannot create the content because it already exists.</summary>
        ContentAlreadyExists,
        /// <summary>The requested resource was not found. The equivalent HTTP status code: 404.</summary>
        ResourceNotFound,
        /// <summary>The value cannot serialize to JSON format.</summary>
        CannotConvertToJSON,
        /// <summary>An exception occurs when an action is not an OData operation or an OData action is invoked with HTTP GET.</summary>
        IllegalInvoke,
        /// <summary>The current user has not enough permission for accessing the requested resource. The equivalent HTTP status code: 403.</summary>
        Forbidden,
        /// <summary>User is not authorized. The equivalent HTTP status code: 401.</summary>
        Unauthorized
    }

    /// <summary>
    /// Represents a general error in OData.
    /// </summary>
    [Serializable]
    public class ODataException : Exception
    {
        /// <summary>
        /// Gets the <see cref="ODataExceptionCode"/> of the error. 
        /// </summary>
        public ODataExceptionCode ODataExceptionCode { get; private set; }
        /// <summary>
        /// Gets the string representation of the ODataExceptionCode property.
        /// The value can be extension of the <see cref="ODataExceptionCode"/> enumeration.
        /// </summary>
        public string ErrorCode { get; internal set; }
        /// <summary>
        /// Gets the string representation of the HTTP Status Cose (e.g. "403 Forbidden").
        /// </summary>
        public string HttpStatus { get; private set; }
        /// <summary>
        /// Gets the HTTP Status Code (e.g. 403).
        /// </summary>
        public int HttpStatusCode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ODataException with a general message and the given error code.
        /// </summary>
        /// <param name="code">The <see cref="ODataExceptionCode"/> of the exception.</param>
        public ODataException(ODataExceptionCode code) : base(String.Format("An exception occured during the processing an OData request. Code: {0} ({1})", Convert.ToInt32(code), code.ToString())) { Initialize(code); }
        /// <summary>
        /// Initializes a new instance of the ODataException with the given parameters.
        /// </summary>
        /// <param name="message">The message of the error.</param>
        /// <param name="code">The <see cref="ODataExceptionCode"/> of the exception.</param>
        public ODataException(string message, ODataExceptionCode code) : base(message) { Initialize(code); }
        /// <summary>
        /// Initializes a new instance of the ODataException with the given parameters.
        /// </summary>
        /// <param name="message">The message of the error.</param>
        /// <param name="code">The <see cref="ODataExceptionCode"/> of the exception.</param>
        /// <param name="inner">The wrapped exception.</param>
        public ODataException(string message, ODataExceptionCode code, Exception inner) : base(message, inner) { Initialize(code); }
        /// <summary>
        /// Initializes a new instance of the ODataException with the given parameters.
        /// </summary>
        /// <param name="code">The <see cref="ODataExceptionCode"/> of the exception.</param>
        /// <param name="inner">The wrapped exception.</param>
        public ODataException(ODataExceptionCode code, Exception inner) : base(GetRelevantMessage(inner), inner) { Initialize(code); }
        /// <inheritdoc />
        protected ODataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        private const string HTTPERROR200 = "200 Ok";
        private const string HTTPERROR400 = "400 Bad Request";
        private const string HTTPERROR401 = "401 Unauthorized";
        private const string HTTPERROR403 = "403 Forbidden";
        private const string HTTPERROR404 = "404 Not Found";
        private const string HTTPERROR500 = "500 Internal Server Error";
        private void Initialize(ODataExceptionCode exceptionCode)
        {
            ODataExceptionCode = exceptionCode;
            switch (exceptionCode)
            {
                case ODataExceptionCode.NotSpecified:                     HttpStatusCode = 500; HttpStatus = HTTPERROR500; break;
                case ODataExceptionCode.RequestError:                     HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.InvalidTopParameter:              HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.NegativeTopParameter:             HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.InvalidSkipParameter:             HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.NegativeSkipParameter:            HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.InvalidInlineCountParameter:      HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.InvalidFormatParameter:           HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.InvalidOrderByParameter:          HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.InvalidOrderByDirectionParameter: HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.InvalidExpandParameter:           HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.InvalidSelectParameter:           HttpStatusCode = 400; HttpStatus = HTTPERROR400; break;
                case ODataExceptionCode.ContentAlreadyExists:             HttpStatusCode = 403; HttpStatus = HTTPERROR403; break;
                case ODataExceptionCode.ResourceNotFound:                 HttpStatusCode = 404; HttpStatus = HTTPERROR404; break;
                case ODataExceptionCode.CannotConvertToJSON:              HttpStatusCode = 500; HttpStatus = HTTPERROR500; break;
                case ODataExceptionCode.Forbidden:                        HttpStatusCode = 403; HttpStatus = HTTPERROR403; break;
                case ODataExceptionCode.Unauthorized:                     HttpStatusCode = 401; HttpStatus = HTTPERROR401; break;
                default:                                                  HttpStatusCode = 500; HttpStatus = HTTPERROR500; break;
            }
        }
        private static string GetRelevantMessage(Exception e)
        {
            var ee = e;
            while (ee != null)
            {
                if (ee.Message != "Exception has been thrown by the target of an invocation.")
                    break;
                ee = ee.InnerException;
            }
            return ee == null ? e.Message : ee.Message;
        }
    }
}
