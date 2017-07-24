using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser
{
    internal class DefaultQueryParserFactory : IQueryParserFactory
    {
        public ISnQueryParser Create()
        {
            return new SnQueryParser();
        }
    }
}
