using System.Collections.Generic;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines an interface that describes an indexable object.
    /// The implementation can provide a collection of the indexable fields.
    /// </summary>
    public interface IIndexableDocument
    {
        /// <summary>
        /// Returns with a collection of the indexable fields of the object.
        /// </summary>
        /// 
        IEnumerable<IIndexableField> GetIndexableFields();
    }
}
