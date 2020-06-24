using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SenseNet.ContentRepository;

namespace SenseNet.OData
{
    /// <summary>
    /// Context object for custom OData operations. It is available when implementing
    /// an <see cref="IOperationMethodPolicy"/>. The Content and the Operation
    /// properties are always filled, other values only if they are available.
    /// </summary>
    public class OperationCallingContext
    {
        public Content Content { get; }
        public OperationInfo Operation { get; }
        public HttpContext HttpContext { get; set; }
        public IConfiguration ApplicationConfiguration { get; set; }

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
