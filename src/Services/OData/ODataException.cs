using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Portal.OData
{
    public enum ODataExceptionCode
    {
        NotSpecified,
        /// <summary>General request error.</summary>
        RequestError,
        InvalidId,
        InvalidTopParameter,
        NegativeTopParameter,
        InvalidSkipParameter,
        NegativeSkipParameter,
        InvalidInlineCountParameter,
        InvalidFormatParameter,
        InvalidOrderByParameter,
        InvalidOrderByDirectionParameter,
        InvalidExpandParameter,
        InvalidSelectParameter,
        ContentAlreadyExists,
        ResourceNotFound,
        CannotConvertToJSON,
        /// <summary>An exception occurs when an action is not an OData operation or an OData action is invoked with HTTP GET.</summary>
        IllegalInvoke,
        Forbidden,
        Unauthorized
    }

    [Serializable]
    public class ODataException : Exception
    {
        public ODataExceptionCode ODataExceptionCode { get; private set; }
        public string ErrorCode { get; internal set; }
        public string HttpStatus { get; private set; }
        public int HttpStatusCode { get; private set; }

        public ODataException(ODataExceptionCode code) : base(String.Format("An exception occured during the processing an OData request. Code: {0} ({1})", Convert.ToInt32(code), code.ToString())) { Initialize(code); }
        public ODataException(string message, ODataExceptionCode code) : base(message) { Initialize(code); }
        public ODataException(string message, ODataExceptionCode code, Exception inner) : base(message, inner) { Initialize(code); }
        public ODataException(ODataExceptionCode code, Exception inner) : base(GetRelevantMessage(inner), inner) { Initialize(code); }
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
