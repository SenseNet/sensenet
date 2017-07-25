using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Tests.Implementations
{
    internal class TestIndexFieldHandler_string : IFieldIndexHandler
    {
        public bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }

        public void ConvertToTermValue(IQueryFieldValue value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.String;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IIndexFieldInfo> GetIndexFieldInfos(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
    internal class TestIndexFieldHandler_int : IFieldIndexHandler
    {
        public bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(int.Parse(value.StringValue));
            return true;
        }

        public void ConvertToTermValue(IQueryFieldValue value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.String;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IIndexFieldInfo> GetIndexFieldInfos(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
}
