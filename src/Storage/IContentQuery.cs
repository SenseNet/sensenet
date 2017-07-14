using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.Search
{
    public interface IContentQuery
    {
        string Text { get; set; }
        int TotalCount { get; }

        QuerySettings Settings { get; set; }

        void AddClause(string text);

        void AddClause(Expression expression);

        QueryResult Execute();
        QueryResult Execute(ExecutionHint hint);

        IEnumerable<int> ExecuteToIds();
        IEnumerable<int> ExecuteToIds(ExecutionHint hint);
    }
}
