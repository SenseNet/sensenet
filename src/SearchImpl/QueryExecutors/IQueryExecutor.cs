using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    public interface IQueryExecutor
    {
        PermissionChecker PermissionChecker { get; }
        LucQuery LucQuery { get; }

        void Initialize(LucQuery lucQuery, PermissionChecker permisionChecker);

        string QueryString { get; }

        int TotalCount { get; }
        IEnumerable<LucObject> Execute();
    }
}
