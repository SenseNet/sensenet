using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Tests.Implementations
{
    public class InMemoryIndex
    {
        /* ========================================================================== Data */

        // FieldName => FieldValue => VersionId
        internal Dictionary<string, Dictionary<string, List<int>>> IndexData { get; } = new Dictionary<string, Dictionary<string, List<int>>>();

        // VersionId, IndexFields
        internal List<Tuple<int, List<IndexField>>> StoredData { get; } = new List<Tuple<int, List<IndexField>>>();

        /* ========================================================================== Operations */

        public void AddDocument(IndexDocument document)
        {
            var versionId = document.GetIntegerValue(IndexFieldName.VersionId);

            var storedFields = document.Where(f => f.Store == IndexStoringMode.Yes).ToList();
            if (storedFields.Count > 0)
                StoredData.Add(new Tuple<int, List<IndexField>>(versionId, storedFields));

            foreach (var field in document)
            {
                var fieldName = field.Name;

                if (!IndexData.TryGetValue(fieldName, out var existingFieldData))
                {
                    existingFieldData = new Dictionary<string, List<int>>();
                    IndexData.Add(fieldName, existingFieldData);
                }

                var fieldValues = GetValues(field);

                foreach (var fieldValue in fieldValues)
                {
                    if (!existingFieldData.TryGetValue(fieldValue, out var versionIds))
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
            if (!IndexData.TryGetValue(fieldName, out var existingFieldData))
                return;

            var deletableVersionIds = new List<int>();
            var fieldValues = GetValues(term);
            foreach (var fieldValue in fieldValues)
            {
                // get version id set by term value
                if (!existingFieldData.TryGetValue(fieldValue, out var versionIds))
                    continue;
                deletableVersionIds.AddRange(versionIds);
            }

            // delete all version ids in any depth
            foreach (var item in IndexData)
            {
                foreach (var subItem in item.Value)
                {
                    var versionIdList = subItem.Value;
                    foreach (var versionId in deletableVersionIds)
                        versionIdList.Remove(versionId);
                }
            }

            // delete stored data by all version ids
            foreach (var deletableStoredData in StoredData.Where(s => deletableVersionIds.Contains(s.Item1)).ToArray())
                StoredData.Remove(deletableStoredData);
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
            if (!IndexData.TryGetValue(fieldName, out var existingFieldData))
                return null;

            // get version id set by term value
            if (!existingFieldData.TryGetValue(fieldValue, out var versionIds))
                return null;

            // return with all stored data by version ids without distinct
            var result = StoredData.Where(d => versionIds.Contains(d.Item1)).ToArray();
            if (result.Length == 0)
                result = null;

            return result;
        }

        private List<string> GetValues(SnTerm field)
        {
            var fieldValues = new List<string>();

            if (field.Name == IndexFieldName.AllText) //TODO: it would be better to use an analyzer
            {
                var words = field.StringValue.Split("\t\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                fieldValues.AddRange(words);
            }
            else
            {
                switch (field.Type)
                {
                    case IndexValueType.String: fieldValues.Add(field.StringValue); break;
                    case IndexValueType.StringArray: fieldValues.AddRange(field.StringArrayValue); break;
                    case IndexValueType.Bool: fieldValues.Add(field.BooleanValue.ToString(CultureInfo.InvariantCulture)); break;
                    case IndexValueType.Int: fieldValues.Add(field.IntegerValue.ToString(CultureInfo.InvariantCulture)); break;
                    case IndexValueType.Long: fieldValues.Add(field.LongValue.ToString(CultureInfo.InvariantCulture)); break;
                    case IndexValueType.Float: fieldValues.Add(field.StringValue.ToString(CultureInfo.InvariantCulture)); break;
                    case IndexValueType.Double: fieldValues.Add(field.DoubleValue.ToString(CultureInfo.InvariantCulture)); break;
                    case IndexValueType.DateTime: fieldValues.Add(field.DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss.ffff")); break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return fieldValues;
        }

        public int GetTermCount(string fieldName)
        {
            return IndexData.TryGetValue(fieldName, out var fieldValues) ? fieldValues.Count : 0;
        }

        public void Clear()
        {
            IndexData.Clear();
            StoredData.Clear();
        }

        /* ========================================================================== Activity staus */

        private IndexingActivityStatus _activityStatux = new IndexingActivityStatus { LastActivityId = 0, Gaps = new int[0] };

        internal void WriteActivityStatus(IndexingActivityStatus status)
        {
            _activityStatux = new IndexingActivityStatus
            {
                LastActivityId = status.LastActivityId,
                Gaps = status.Gaps.ToArray()
            };
        }

        internal IndexingActivityStatus ReadActivityStatus()
        {
            return _activityStatux;
        }

    }
}
