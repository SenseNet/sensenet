using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    public interface IQueryContext
    {
        QuerySettings Settings { get; }
        int UserId { get; }
        IQueryEngine QueryEngine { get; }
        bool AllVersions { get; set; } //UNDONE: Move or not to QuerySettings.

        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
    }
}
