using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Preview
{
    internal static class SR
    {
        internal static string F(string format, params object[] args)
        {
            return string.Format(format, args);
        }

        internal static string UnknownProvider_1 = "Unknown IPreviewImageGenerator for file extension '{0}'.";

    }
}
