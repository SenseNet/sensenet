using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Web;

namespace SenseNet.Services.Tests
{
    public static class HttpHelper
    {
        public static void AddHeader(this HttpRequest request, string key, string value)
        {
            var headers = request.Headers;
            Type hdr = headers.GetType();
            PropertyInfo ro = hdr.GetProperty("IsReadOnly",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
            // Remove the ReadOnly property
            ro.SetValue(headers, false, null);
            // Invoke the protected InvalidateCachedArrays method 
            hdr.InvokeMember("InvalidateCachedArrays",
                BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                null, headers, null);
            // Now invoke the protected "BaseAdd" method of the base class to add the
            // headers you need. The header content needs to be an ArrayList or the
            // the web application will choke on it.
            hdr.InvokeMember("BaseAdd",
                BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                null, headers,
                new object[] { key, new ArrayList { value }});
            // repeat BaseAdd invocation for any other headers to be added
            // Then set the collection back to ReadOnly
            ro.SetValue(headers, true, null);
        }

        public static void SetContext(this HttpApplication application, HttpContext context)
        {
            var contextProp = typeof(HttpApplication).GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic);
            contextProp.SetValue(application, context);

        }

    }
}