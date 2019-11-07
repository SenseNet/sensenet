using SenseNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ODataTests.Responses
{
    public class ODataErrorResponse : IODataResponse
    {
        public ODataExceptionCode Code;
        public string ExceptionType;
        public string Message;
        public string StackTrace;
    }
}
