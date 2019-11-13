using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;

namespace SenseNet.OData
{
    public class ODataOperationMethod : ActionBase
    {
        public OperationCallingContext Method { get; set; }

        public override string Uri { get; } = string.Empty;

        public override object Execute(Content content, params object[] parameters)
        {
            return OperationCenter.Invoke(Method);
        }
    }
}
