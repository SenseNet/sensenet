using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    internal class SafeQueriesInternal : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: "+Id:@0"</summary>
        public static string ContentById => "+Id:@0";
    }
}
