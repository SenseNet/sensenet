using System;
using System.Collections.Generic;

namespace SenseNet.Diagnostics
{
    public static class Utility
    {
        [Obsolete("Use the SnLog API for logging events.", true)]
        public static IDictionary<string, object> CollectAutoProperties(IDictionary<string, object> properties)
        {
            throw new NotSupportedException("Use the SnLog API for logging events.");
        }
    }
}
