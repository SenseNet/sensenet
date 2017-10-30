using System.Collections.Generic;

namespace SenseNet.Search.Indexing
{
    public interface IIndexableField
    {
        string Name { get; }
        object GetData(bool localized = true);

        bool IsInIndex { get; }
        bool IsBinaryField { get; }
        IEnumerable<IndexField> GetIndexFields(out string textExtract);
    }
}
