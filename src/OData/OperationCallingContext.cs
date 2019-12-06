using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;

namespace SenseNet.OData
{
    public class OperationCallingContext
    {
        public Content Content { get; }
        public OperationInfo Operation { get; }
        public HttpContext HttpContext { get; set; }

        internal OperationCallingContext(Content content, OperationInfo info)
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
