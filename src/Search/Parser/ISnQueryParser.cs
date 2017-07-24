using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser
{
    internal interface ISnQueryParser
    {
        SnQuery Parse(string queryText, QuerySettings settings);
    }
}
