using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;

namespace SenseNet.OData.Operations
{
    public class MethodBasedOperation : ActionBase
    {
        public override string Uri => "##";

        public MethodInfo Method { get; }

        public override ActionParameter[] ActionParameters { get; }
        public override bool IsHtmlOperation => false;
        public override bool IsODataOperation => true;
        public override bool CausesStateChange { get; }


        public MethodBasedOperation(MethodInfo method, bool causesStateChange, ActionParameter[] parameters, string description = null)
        {
            Method = method;
            CausesStateChange = causesStateChange;
            ActionParameters = parameters;
            if (description != null)
                Description = description;
        }

        public override object Execute(Content content, params object[] parameters)
        {
            var allParams = new object[parameters.Length + 1];
            allParams[0] = content;
            parameters.CopyTo(allParams, 1);
            var result = Method.Invoke(null, allParams);
            return result;
        }
    }
}
