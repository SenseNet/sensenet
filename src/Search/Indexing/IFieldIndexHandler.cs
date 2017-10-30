using System.Collections.Generic;

namespace SenseNet.Search.Indexing
{
    public interface IFieldIndexHandler
    {
        /// <summary>For SnQuery parser</summary>
        IndexValue Parse(string text);

        /// <summary>For LINQ</summary>
        IndexValue ConvertToTermValue(object value);

        IndexFieldAnalyzer GetDefaultAnalyzer();

        IEnumerable<string> GetParsableValues(IIndexableField field);
        IndexValueType IndexFieldType { get; }
        IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        string GetSortFieldName(string fieldName);

        IEnumerable<IndexField> GetIndexFields(IIndexableField field, out string textExtract);
    }
}
