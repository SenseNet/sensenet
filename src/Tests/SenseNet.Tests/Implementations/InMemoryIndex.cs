using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Tests.Implementations
{
    public class InMemoryIndex
    {
        private static InMemoryIndex _prototype;
        public static InMemoryIndex Create()
        {
            return _prototype == null ? new InMemoryIndex() : _prototype.Clone();
        }

        public static void SetPrototype(InMemoryIndex prototype)
        {
            _prototype = prototype;
        }

        private InMemoryIndex() { }

        /* ========================================================================== Data */

        // FieldName => FieldValue => VersionId
        internal Dictionary<string, Dictionary<string, List<int>>> IndexData { get; private set; } = new Dictionary<string, Dictionary<string, List<int>>>();

        // VersionId, IndexFields
        internal List<Tuple<int, List<IndexField>>> StoredData { get; private set; } = new List<Tuple<int, List<IndexField>>>();

        private InMemoryIndex Clone()
        {
            using(var op = SnTrace.Index.StartOperation("Clone index."))
            {
                var index = new InMemoryIndex
                {
                    IndexData = IndexData.ToDictionary(
                        x => x.Key,
                        x => x.Value.ToDictionary(
                            y => y.Key,
                            y => y.Value.ToList())),
                    StoredData = StoredData
                        .Select(x => new Tuple<int, List<IndexField>>(x.Item1, x.Item2.Select(Clone).ToList()))
                        .ToList()
                };

                op.Successful = true;
                return index;
            }
        }

        private IndexField Clone(IndexField field)
        {
            switch (field.Type)
            {
                case IndexValueType.String:
                    return new IndexField(field.Name, field.StringValue, field.Mode, field.Store, field.TermVector);
                case IndexValueType.StringArray:
                    return new IndexField(field.Name, field.StringArrayValue.ToArray(), field.Mode, field.Store, field.TermVector);
                case IndexValueType.Bool:
                    return new IndexField(field.Name, field.BooleanValue, field.Mode, field.Store, field.TermVector);
                case IndexValueType.Int:
                    return new IndexField(field.Name, field.IntegerValue, field.Mode, field.Store, field.TermVector);
                case IndexValueType.Long:
                    return new IndexField(field.Name, field.LongValue, field.Mode, field.Store, field.TermVector);
                case IndexValueType.Float:
                    return new IndexField(field.Name, field.SingleValue, field.Mode, field.Store, field.TermVector);
                case IndexValueType.Double:
                    return new IndexField(field.Name, field.DoubleValue, field.Mode, field.Store, field.TermVector);
                case IndexValueType.DateTime:
                    return new IndexField(field.Name, field.DateTimeValue, field.Mode, field.Store, field.TermVector);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

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
                var words = field.StringValue
                    .Split(" \t\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.ToLowerInvariant());
                fieldValues.AddRange(words);
            }
            else
            {
                if (field.Type == IndexValueType.StringArray)
                    fieldValues.AddRange(field.StringArrayValue.Select(s=>s.ToLowerInvariant()));
                else
                    fieldValues.Add(IndexValueToString(field));
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

        internal static string IndexValueToString(IndexValue value)
        {
            if (value == null)
                return null;

            switch (value.Type)
            {
                case IndexValueType.String: return value.StringValue.ToLowerInvariant();
                case IndexValueType.StringArray: throw new NotImplementedException(); //TODO: Test and implement or rewrite to NotSupportedException
                case IndexValueType.Bool: return value.BooleanValue ? IndexValue.Yes : IndexValue.No;
                case IndexValueType.Int: return IntToString(value.IntegerValue);
                case IndexValueType.Long: return LongToString(value.LongValue);
                case IndexValueType.Float: return SingleToString(value.SingleValue);
                case IndexValueType.Double: return DoubleToString(value.DoubleValue);
                case IndexValueType.DateTime: return value.DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        internal static string IntToString(int value)
        {
            var uValue = value >= 0
                ? Convert.ToUInt32(value) + int.MaxValue
                : Convert.ToUInt32(value + 1 + int.MaxValue);
            return uValue.ToString("0000000000") + "|" + value;
        }
        internal static string LongToString(long value)
        {
            var uValue = value >= 0
                ? Convert.ToUInt64(value) + long.MaxValue
                : Convert.ToUInt64(value + 1L + long.MaxValue);
            return uValue.ToString("00000000000000000000") + "|" + value;
        }
        internal static string SingleToString(float value)
        {
            //TODO: Single fields are not comparable as a string
            return value.ToString(CultureInfo.InvariantCulture);
        }
        internal static string DoubleToString(double value)
        {
            //TODO: Double fields are not comparable as a string
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
