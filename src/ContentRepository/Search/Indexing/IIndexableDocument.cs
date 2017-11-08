using System.Collections.Generic;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing
{
    public interface IIndexableDocument
    {
        IEnumerable<IIndexableField> GetIndexableFields();
    }
}
