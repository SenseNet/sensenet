using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.Search
{
    public enum ExecutionHint { None, ForceRelationalEngine, ForceIndexedEngine }

    public interface IContentQuery //UNDONE: namespace
    {
        string Text { get; set; }
        int TotalCount { get; }

        QuerySettings Settings { get; set; }

        void AddClause(string text);

        QueryResult Execute();
        QueryResult Execute(ExecutionHint hint);

        IEnumerable<int> ExecuteToIds();
        IEnumerable<int> ExecuteToIds(ExecutionHint hint);
    }
}
