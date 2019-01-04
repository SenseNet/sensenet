using System.Collections.Generic;
using SenseNet.Configuration;

namespace SenseNet.Diagnostics
{
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
                var ctx = System.Web.HttpContext.Current;
                properties.Add("IsHttpContext", ctx == null ? "no" : "yes");

                if (ctx != null)
                {
                    System.Web.HttpRequest req = null;
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
                            properties.Add("Url", ctx.Request.Url);
                        if (!properties.ContainsKey("Referrer"))
                            properties.Add("Referrer", ctx.Request.UrlReferrer);
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
