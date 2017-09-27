using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;

namespace SenseNet.Search.Tests.Implementations
{
    public class TestIndexFieldHandlerString : IFieldIndexHandler
    {
        public IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }

        public IndexValue ConvertToTermValue(object value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
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

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
    public class TestIndexFieldHandlerInt : IFieldIndexHandler
    {
        public IndexValue Parse(string text)
        {
            int converted;
            if (int.TryParse(text, out converted))
                return new IndexValue(converted);
            return null;
        }

        public IndexValue ConvertToTermValue(object value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.Int;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
    public class TestIndexFieldHandlerLong : IFieldIndexHandler
    {
        public IndexValue Parse(string text)
        {
            long converted;
            if (long.TryParse(text, out converted))
                return new IndexValue(converted);
            return null;
        }

        public IndexValue ConvertToTermValue(object value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.Long;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
    public class TestIndexFieldHandlerSingle : IFieldIndexHandler
    {
        public IndexValue Parse(string text)
        {
            float converted;
            if (float.TryParse(text, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out converted))
                return new IndexValue(converted);
            return null;
        }

        public IndexValue ConvertToTermValue(object value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.Float;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
    public class TestIndexFieldHandlerDouble : IFieldIndexHandler
    {
        public IndexValue Parse(string text)
        {
            double converted;
            if (double.TryParse(text, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out converted))
                return new IndexValue(converted);
            return null;
        }

        public IndexValue ConvertToTermValue(object value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.Double;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }

    public class TestIndexFieldHandlerBool : IFieldIndexHandler
    {
        public static readonly List<string> YesList = new List<string>(new[] { "1", "true", "y", IndexValue.Yes });
        public static readonly List<string> NoList = new List<string>(new[] { "0", "false", "n", IndexValue.No });

        public IndexValue Parse(string text)
        {
            var v = text.ToLowerInvariant();
            if (YesList.Contains(v))
                return new IndexValue(true);
            if (NoList.Contains(v))
                return new IndexValue(false);
            bool b;
            if (bool.TryParse(v, out b))
                return new IndexValue(b);
            return null;
        }

        public IndexValue ConvertToTermValue(object value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.Double;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
    public class TestIndexFieldHandlerDateTime : IFieldIndexHandler
    {
        public IndexValue Parse(string text)
        {
            DateTime dateTimeValue;
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeValue))
                return new IndexValue(dateTimeValue);
            return null;
        }

        public IndexValue ConvertToTermValue(object value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.Double;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
}
