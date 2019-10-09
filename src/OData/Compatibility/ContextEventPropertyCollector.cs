using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using SenseNet.Configuration;

namespace SenseNet.Diagnostics
{
    //UNDONE:ODATA: ?IsHttpContext? SenseNet.Diagnostics.ContextEventPropertyCollector
    public class ContextEventPropertyCollector : EventPropertyCollector
    {
        protected override void CollectProperties(IDictionary<string, object> props)
        {
            base.CollectProperties(props);

            CollectContextProperties(props);
        }

        private static void CollectContextProperties(IDictionary<string, object> properties)
        {
            if (!properties.ContainsKey("WorkingMode"))
                properties.Add("WorkingMode", RepositoryEnvironment.WorkingMode.RawValue);

            if (!properties.ContainsKey("IsHttpContext"))
            {
                var ctx = (HttpContext)null; //System.Web.HttpContext.Current;
                properties.Add("IsHttpContext", ctx == null ? "no" : "yes");

                if (ctx != null)
                {
                    HttpRequest req = null;
                    try
                    {
                        req = ctx.Request;
                    }
                    catch
                    {
                        // ignored
                    }
                    if (req != null)
                    {
                        if (!properties.ContainsKey("Url"))
                            properties.Add("Url", ctx.Request.GetDisplayUrl());
                        if (!properties.ContainsKey("Referrer"))
                            properties.Add("Referrer", ctx.Request.Headers["Referer"].ToString());
                    }
                    else
                    {
                        if (!properties.ContainsKey("Url"))
                            properties.Add("Url", "// not available //");
                    }
                }
            }
        }
    }
}
