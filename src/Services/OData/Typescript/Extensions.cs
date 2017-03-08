using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Portal.OData.Typescript
{
    internal static class Extensions
    {
        public static string ToCamelCase(this string s)
        {
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}
