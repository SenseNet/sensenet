using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;

namespace SenseNet.Packaging.Tests
{
    internal class SafeQueries : ISafeQueryHolder
    {
        public static string LockedContent { get; } = "+Locked:true";
    }
}
