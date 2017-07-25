using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser
{
    public abstract class SnQueryNode
    {
        public double? Boost { get; set; }
    }
}
