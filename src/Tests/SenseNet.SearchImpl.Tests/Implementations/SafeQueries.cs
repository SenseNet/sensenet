using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class SafeQueries : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: "Name:@0"</summary>
        public static string Name => "Name:@0";
        /// <summary>Returns with the following query: "Name:@0"</summary>
        public static string OneTerm => "@0:@1";
    }
}
