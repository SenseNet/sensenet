using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class InMemoryQueryEngine : IQueryEngine
    {
        private readonly InMemoryIndex _index;

        public InMemoryQueryEngine(InMemoryIndex index)
        {
            _index = index;
        }

        public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter)
        {
            throw new NotImplementedException();
        }

        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter)
        {
            throw new NotImplementedException();
        }
    }
}
