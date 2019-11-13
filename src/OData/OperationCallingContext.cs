using System.Collections.Generic;
using SenseNet.ContentRepository;

namespace SenseNet.OData
{
    public class OperationCallingContext //UNDONE: Encapsulate the current HttpContext and ODataRequest
    {
        public Content Content { get; }
        public OperationInfo Operation { get; }

        public OperationCallingContext(Content content, OperationInfo info)
        {
            Content = content;
            Operation = info;
        }

        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        internal void SetParameter(string name, object parsed)
        {
            Parameters[name] = parsed;
        }
    }
}
