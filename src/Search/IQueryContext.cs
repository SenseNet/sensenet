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
        IMetaQueryEngine MetaQueryEngine { get; }

        bool AllVersions { get; set; } //UNDONE:!!!!! tusmester API: TEST: AllVersions: Move to QuerySettings.

        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
    }
}
