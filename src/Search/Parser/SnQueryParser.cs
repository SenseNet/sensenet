using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser
{
    internal class SnQueryParser : ISnQueryParser
    {
        public SnQuery Parse(string queryText, QuerySettings settings) //UNDONE: SnQueryParser.Parse is not implemented
        {
            return new SnQuery {Querytext = queryText};
        }
    }
}
