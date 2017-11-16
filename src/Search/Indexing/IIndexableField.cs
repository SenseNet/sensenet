using System.Collections.Generic;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Describes an indexable object.
    /// </summary>
    public interface IIndexableField
    {
        /// <summary>
        /// Name of the object. This name will be used in the index and query.
        /// </summary>
        string Name { get; }

        //UNDONE:! parameterless method is the correct in this scenario.
        object GetData(bool localized = true);

        /// <summary>
        /// Gets a value that is true if the field need to be indexed otherwise false.
        /// </summary>
        bool IsInIndex { get; }

        /// <summary>
        /// Gets a value that is true if the field is.
        /// </summary>
        bool IsBinaryField { get; }

        /// <summary>
        /// Returns with the transformed index fields and the text extract.
        /// The transformation uses the appropriate IFieldIndexHandler implementation.
        /// </summary>
        IEnumerable<IndexField> GetIndexFields(out string textExtract);
    }
}
