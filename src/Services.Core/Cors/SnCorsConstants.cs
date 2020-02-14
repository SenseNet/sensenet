using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.Services.Core.Cors
{
    internal class SnCorsConstants
    {
        internal static readonly string[] AccessControlAllowMethodsDefault =
        {
            "GET", "POST", "PATCH", "DELETE", "MERGE", "PUT"
        };
        internal static readonly string[] AccessControlAllowHeadersDefault =
        {
            "X-Authentication-Type",
            "X-Refresh-Data", "X-Access-Data",
            "X-Requested-With", "Authorization", "Content-Type",
            "Content-Range", "Content-Disposition"
        };
    }
}
