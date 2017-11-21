using System.Collections.Generic;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing
{
    //UNDONE:!!!! XMLDOC ContentRepository
    public interface IIndexableDocument
    {
        //UNDONE:!!!! XMLDOC ContentRepository
        IEnumerable<IIndexableField> GetIndexableFields();
    }
}
