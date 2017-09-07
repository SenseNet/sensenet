using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Parser
{
    public interface IQueryContext
    {
        QuerySettings Settings { get; }
        int UserId { get; }
        IQueryEngine QueryEngine { get; }

        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
    }
}
