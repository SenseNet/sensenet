using SenseNet.Portal.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Services.OData.Tests.Results
{
    public class ODataError : IODataResult
    {
        public ODataExceptionCode Code;
        public string ExceptionType;
        public string Message;
        public string StackTrace;
    }
}
