using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class InMemoryIndex
    {
        // FieldName => FieldValue => VersionId
        private Dictionary<string, Dictionary<string, List<int>>> _indexData = new Dictionary<string, Dictionary<string, List<int>>>();

        // VersionId, IndexFields
        private List<Tuple<int, List<IndexField>>> _storedData = new List<Tuple<int, List<IndexField>>>();

        public void AddDocument(IndexDocument document)
        {
            var versionId = document.GetIntegerValue(IndexFieldName.VersionId);

            var storedFields = document.Where(f => f.Store == IndexStoringMode.Yes).ToList();
            if (storedFields.Count > 0)
                _storedData.Add(new Tuple<int, List<IndexField>>(versionId, storedFields));

            foreach (var field in document)
            {
                var fieldName = field.Name;

                Dictionary<string, List<int>> existingFieldData;
                if (!_indexData.TryGetValue(fieldName, out existingFieldData))
                {
                    existingFieldData = new Dictionary<string, List<int>>();
                    _indexData.Add(fieldName, existingFieldData);
                }

                var fieldValues = GetValues(field);

                foreach (var fieldValue in fieldValues)
                {
                    List<int> versionIds;
                    if (!existingFieldData.TryGetValue(fieldValue, out versionIds))
                    {
                        versionIds = new List<int>();
                        existingFieldData.Add(fieldValue, versionIds);
                    }

                    versionIds.Add(versionId);
                }
            }
        }

        public void Delete(SnTerm term)
        {
            var fieldName = term.Name;

            // get category by term name
            Dictionary<string, List<int>> existingFieldData;
            if (!_indexData.TryGetValue(fieldName, out existingFieldData))
                return;

            var deletableVersionIds = new List<int>();
            var fieldValues = GetValues(term);
            foreach (var fieldValue in fieldValues)
            {
                // get version id set by term value
                List<int> versionIds;
                if (!existingFieldData.TryGetValue(fieldValue, out versionIds))
                    continue;
                deletableVersionIds.AddRange(versionIds);
            }

            // delete all version ids in any depth
            foreach (var item in _indexData)
            {
                foreach (var subItem in item.Value)
                {
                    var versionIdList = subItem.Value;
                    foreach (var versionId in deletableVersionIds)
                        versionIdList.Remove(versionId);
                }
            }

            // delete stored data by all version ids
            foreach (var deletableStoredData in _storedData.Where(s => deletableVersionIds.Contains(s.Item1)).ToArray())
                _storedData.Remove(deletableStoredData);
        }

        public void Update(SnTerm term, IndexDocument document)
        {
            Delete(term);
            AddDocument(document);
        }

        public IEnumerable<Tuple<int, List<IndexField>>> GetStoredFieldsByTerm(SnTerm term)
        {
            var fieldName = term.Name;

            var fieldValues = GetValues(term);
            if (fieldValues.Count == 0)
                return null;
            if (fieldValues.Count > 1)
                throw new NotImplementedException();

            var fieldValue = fieldValues[0];

            // get category by term name
            Dictionary<string, List<int>> existingFieldData;
            if (!_indexData.TryGetValue(fieldName, out existingFieldData))
                return null;

            // get version id set by term value
            List<int> versionIds;
            if (!existingFieldData.TryGetValue(fieldValue, out versionIds))
                return null;

            // return with all stored data by version ids without distinct
            var result = _storedData.Where(d => versionIds.Contains(d.Item1)).ToArray();
            if (result.Length == 0)
                result = null;

            return result;
        }

        private List<string> GetValues(SnTerm field)
        {
            var fieldValues = new List<string>();

            if (field.Name == IndexFieldName.AllText) //UNDONE: Need to use analyzer
            {
                var words = field.StringValue.Split("\t\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                fieldValues.AddRange(words);
            }
            else
            {
                switch (field.Type)
                {
                    case SnTermType.String: fieldValues.Add(field.StringValue); break;
                    case SnTermType.StringArray: fieldValues.AddRange(field.StringArrayValue); break;
                    case SnTermType.Bool: fieldValues.Add(field.BooleanValue.ToString(CultureInfo.InvariantCulture)); break;
                    case SnTermType.Int: fieldValues.Add(field.IntegerValue.ToString(CultureInfo.InvariantCulture)); break;
                    case SnTermType.Long: fieldValues.Add(field.LongValue.ToString(CultureInfo.InvariantCulture)); break;
                    case SnTermType.Float: fieldValues.Add(field.StringValue.ToString(CultureInfo.InvariantCulture)); break;
                    case SnTermType.Double: fieldValues.Add(field.DoubleValue.ToString(CultureInfo.InvariantCulture)); break;
                    case SnTermType.DateTime: fieldValues.Add(field.DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss.ffff")); break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return fieldValues;
        }

    }
}
