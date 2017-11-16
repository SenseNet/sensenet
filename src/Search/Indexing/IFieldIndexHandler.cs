using System;
using System.Collections.Generic;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Defines API elements for customization of the IIndexableField -> IndexValue transformation.
    /// This interface is designed to transformations of every data types that are used in querying and indexing.
    /// It is highly recommended to implement a class for each data type.
    /// </summary>
    public interface IFieldIndexHandler
    {
        /// <summary>
        /// Parse a string value and return with an atomic IndexValue instance.
        /// </summary>
        IndexValue Parse(string text);

        /// <summary>
        /// Parse an object value and return with an atomic IndexValue instance.
        /// </summary>
        IndexValue ConvertToTermValue(object value);

        /// <summary>
        /// Returns the default analyzer choice of the data type.
        /// </summary>
        IndexFieldAnalyzer GetDefaultAnalyzer();

        /// <summary>
        /// Returns with field value's string representation that can be inserted into a CQL as a valid term value.
        /// </summary>
        [Obsolete("This method will be removed in the next release.")]
        IEnumerable<string> GetParsableValues(IIndexableField field);

        /// <summary>
        /// IndexValueType of the converted values.
        /// </summary>
        IndexValueType IndexFieldType { get; }

        /// <summary>
        /// Gets or sets the indexing metadata descriptor object for the implementation.
        /// This is only a storage slot. The implementation just holds the given object.
        /// </summary>
        IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }

        /// <summary>
        /// Returns with the sort field name if it is converted. For example if the field is converted
        /// to multiple index values, and the name of the value for sorting is renamed (prefixed or suffixed),
        /// this method gives the real name of the sort field that is used in the compiled query.
        /// Used in the query execution.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        string GetSortFieldName(string fieldName);

        /// <summary>
        /// Converts a field to one or more index values.
        /// </summary>
        /// <param name="field">Source of the transformation.</param>
        /// <param name="textExtract">All words of the input field value concatenated to one string.</param>
        /// <returns>Collection of the converted values.</returns>
        IEnumerable<IndexField> GetIndexFields(IIndexableField field, out string textExtract);
    }
}
