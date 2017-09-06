using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class InMemoryQueryEngine : IQueryEngine
    {
        private class Hit
        {
            public int NodeId;
            public int VersionId;
            public bool IsLastPublic;
            public bool IsLastDraft;
            public string ValueForProject;
            public string[] ValuesForSort;
        }

        private readonly InMemoryIndex _index;

        public InMemoryQueryEngine(InMemoryIndex index)
        {
            _index = index;
        }

        public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter)
        {
            var interpreter = new SnQueryInterpreter(_index);
            int totalCount;
            var result = interpreter.Execute(query, filter, out totalCount);

            var nodeIds = result.Select(h => h.NodeId).ToArray();
            var queryResult = new QueryResult<int>(nodeIds, totalCount);
            return queryResult;
        }

        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter)
        {
            var interpreter = new SnQueryInterpreter(_index);
            int totalCount;
            var result = interpreter.Execute(query, filter, out totalCount);

            var projectedValues = result.Select(h => h.ValueForProject).ToArray();
            var queryResult = new QueryResult<string>(projectedValues, totalCount);
            return queryResult;
        }

        private class SnQueryInterpreter : SnQueryVisitor
        {
            private SnQuery _query;
            private readonly InMemoryIndex _index;
            private readonly Stack<List<int>> _hitStack = new Stack<List<int>>();

            public SnQueryInterpreter(InMemoryIndex index)
            {
                _index = index;
            }

            public IEnumerable<Hit> Execute(SnQuery query, IPermissionFilter filter, out int totalCount)
            {
                _query = query;

                Visit(query.QueryTree);

                var foundVersionIds = _hitStack.Pop();
                IEnumerable<Hit> permittedHits = foundVersionIds.Select(GetHitByVersionId).Where(h=>filter.IsPermitted(h.NodeId, h.IsLastPublic, h.IsLastDraft));
                var sortedHits = GetSortedResult(permittedHits).ToArray();

                totalCount = sortedHits.Length;

                var result = sortedHits.Skip(query.Skip).Take(query.Top > 0 ? query.Top : totalCount).ToArray();
                return result;
            }

            private IEnumerable<Hit> GetSortedResult(IEnumerable<Hit> hits)
            {
                throw new NotImplementedException();
            }

            private Hit GetHitByVersionId(int versionId)
            {
                throw new NotImplementedException();
            }


            public override SnQueryPredicate VisitTextPredicate(TextPredicate text)
            {
                var result = new List<int>();
                Dictionary<string, List<int>> fieldValues;
                if (_index.IndexData.TryGetValue(text.FieldName, out fieldValues))
                {
                    List<int> versionIds;
                    if (fieldValues.TryGetValue(text.Value, out versionIds))
                        result.AddRange(versionIds);
                }
                _hitStack.Push(result);
                return text;
            }

        }
    }
}
