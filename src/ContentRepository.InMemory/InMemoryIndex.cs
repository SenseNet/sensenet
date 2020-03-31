using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.InMemory
{
    //TODO: Known issue: this class is not thread safe (sometimes there is an ArgumentOutOfRangeException in the InMemoryIndex.cs line 194).
    public class InMemoryIndex
    {
        /// <summary>
        /// Gets or sets the path of the local disk directory
        /// that will contain every IndexDocument for trace index modifications.
        /// </summary>
        public string IndexDocumentPath { get; set; }

        /* ========================================================================== Data */

        // FieldName => FieldValue => VersionId
        internal Dictionary<string, Dictionary<string, List<int>>> IndexData { get; private set; } = new Dictionary<string, Dictionary<string, List<int>>>();

        // VersionId, IndexFields
        internal List<Tuple<int, List<IndexField>>> StoredData { get; private set; } = new List<Tuple<int, List<IndexField>>>();

        public InMemoryIndex Clone()
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
            if(IndexDocumentPath != null)
                WriteTo(IndexDocumentPath, document);

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
        private void WriteTo(string rootPath, IndexDocument document)
        {
            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            var nodeId = document.GetIntegerValue(IndexFieldName.NodeId);
            var versionId = document.GetIntegerValue(IndexFieldName.VersionId);
            var contentType = document.GetStringValue(IndexFieldName.Type);

            var contentFileBase = $"{rootPath}\\{nodeId}-{versionId}-{contentType}";
            var contentFile = contentFileBase;
            var suffix = 0;
            while (File.Exists(contentFile + ".txt"))
                contentFile = contentFileBase + "-" + ++suffix;
            contentFile = contentFile + ".txt";

            using (var writer = new StreamWriter(contentFile))
            {
                writer.WriteLine("{");

                foreach (var field in document)
                {
                    string value;
                    var type = "";
                    switch (field.Type)
                    {
                        case IndexValueType.String:
                            value = field.StringValue == null ? "null" : $"\"{field.StringValue}\"";
                            break;
                        case IndexValueType.StringArray:
                            value = "[" + string.Join(", ", field.StringArrayValue.Select(s => $"\"{s}\"").ToArray()) + "]";
                            break;
                        case IndexValueType.DateTime:
                            value = $"\"{field.ValueAsString}\"";
                            break;
                        case IndexValueType.Long:
                        case IndexValueType.Float:
                        case IndexValueType.Double:
                            value = field.ValueAsString;
                            type = " // " + field.Type.ToString().ToLowerInvariant();
                            break;
                        case IndexValueType.Bool:
                        case IndexValueType.Int:
                        default:
                            value = field.ValueAsString;
                            break;
                    }
                    writer.WriteLine("    {0}: {1},{2}", field.Name, value, type);
                }

                writer.WriteLine("}");
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
            var indexesToDelete = new List<string>();
            foreach (var item in IndexData)
            {
                var keysToDelete = new List<string>();
                foreach (var subItem in item.Value)
                {
                    var versionIdList = subItem.Value;
                    foreach (var versionId in deletableVersionIds) //TODO: Thread safety. See the comment above.
                        versionIdList.Remove(versionId);
                    if (versionIdList.Count == 0)
                        keysToDelete.Add(subItem.Key);
                }
                foreach (var keyToDelete in keysToDelete)
                    item.Value.Remove(keyToDelete);
                if(item.Value.Count == 0)
                    indexesToDelete.Add(item.Key);
            }
            foreach (var indexToDelete in indexesToDelete)
                IndexData.Remove(indexToDelete);

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
                    .Distinct()
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

        /* ========================================================================== Activity status */

        private IndexingActivityStatus _activityStatus = new IndexingActivityStatus { LastActivityId = 0, Gaps = new int[0] };

        internal void WriteActivityStatus(IndexingActivityStatus status)
        {
            _activityStatus = new IndexingActivityStatus
            {
                LastActivityId = status.LastActivityId,
                Gaps = status.Gaps.ToArray()
            };
        }

        internal IndexingActivityStatus ReadActivityStatus()
        {
            return _activityStatus;
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
        public static string IntToString(int value)
        {
            var uValue = value >= 0
                ? Convert.ToUInt32(value) + int.MaxValue
                : Convert.ToUInt32(value + 1 + int.MaxValue);
            return uValue.ToString("0000000000") + "|" + value;
        }
        public static string LongToString(long value)
        {
            var uValue = value >= 0
                ? Convert.ToUInt64(value) + long.MaxValue
                : Convert.ToUInt64(value + 1L + long.MaxValue);
            return uValue.ToString("00000000000000000000") + "|" + value;
        }
        public static string SingleToString(float value)
        {
            //TODO: Single fields are not comparable as a string
            return value.ToString(CultureInfo.InvariantCulture);
        }
        public static string DoubleToString(double value)
        {
            //TODO: Double fields are not comparable as a string
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public void Save(string fileName)
        {
            var index = new Dictionary<string, List<string>>();
            foreach (var item in IndexData)
            {
                var list = new List<string>();
                index.Add(item.Key, list);
                foreach (var term in item.Value)
                    list.Add(term.Key + ": " + string.Join(", ",
                                 term.Value
                                 .OrderBy(x => x)
                                 .Select(x => x.ToString())
                                 .ToArray()));
            }

            var data = new {Index = index, Stored = SerializeStoredData(StoredData)};

            using (var writer = new StreamWriter(fileName, false))
                JsonSerializer.Create(SerializerSettings).Serialize(writer, data);
        }

        private class StoredItemModel
        {
            public int VersionId;
            public List<string> IndexFields;
        }

        private List<StoredItemModel> SerializeStoredData(List<Tuple<int, List<IndexField>>> data)
        {
            return data.Select(srcItem => new StoredItemModel
            {
                VersionId = srcItem.Item1,
                IndexFields = srcItem.Item2.Select(srcField => srcField.ToString(true)).ToList()
            }).ToList();
        }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Formatting = Formatting.Indented
        };

        public void Load(string path)
        {
            Load(new StreamReader(path));
        }
        public void Load(TextReader reader)
        {
            Clear();
            var deserialized = (JObject)(JsonSerializer.Create(SerializerSettings).Deserialize(new JsonTextReader(reader)));
            var index = deserialized["Index"];

            var fields = (JObject)index;
            foreach (var field in fields)
            {
                try
                {
                    var fieldName = field.Key;
                    var values = (JArray)field.Value;

                    var data = new Dictionary<string, List<int>>();

                    foreach (var termData in values.Select(v => v.ToString()).ToArray())
                    {
                        var p = termData.LastIndexOf(":", StringComparison.Ordinal);
                        var name = termData.Substring(0, p);
                        var idSrc = termData.Substring(p + 1);
                        var ids = idSrc.Split(',').Select(int.Parse).ToList();
                        data.Add(name, ids);
                    }

                    this.IndexData.Add(fieldName, data);
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            var stored = (JArray)deserialized["Stored"];
            foreach (var storedDoc in stored)
            {
                if (storedDoc["VersionId"] == null)
                {
                    // Old algorithm (backward compatibility)
                    var versionId = (int)storedDoc["Item1"];
                    var indexFields = ((JArray)storedDoc["Item2"]).Select(x => CreateIndexField((JObject)x)).ToList();
                    StoredData.Add(new Tuple<int, List<IndexField>>(versionId, indexFields));
                }
                else
                {
                    // New algorithm
                    var versionId = storedDoc["VersionId"].Value<int>();
                    var indexFields = storedDoc["IndexFields"]
                        .Select(x => x.ToString())
                        .Select(x => IndexField.Parse(x, true))
                        .ToList();
                    StoredData.Add(new Tuple<int, List<IndexField>>(versionId, indexFields));
                }
            }
        }

        private IndexField CreateIndexField(JObject x)
        {
            var name = (string)x["Name"];
            var type = (IndexValueType)(int)x["Type"];
            var mode = (IndexingMode)(int)x["Mode"];
            var store = (IndexStoringMode)(int)x["Store"];
            var termVector = (IndexTermVector)(int)x["TermVector"];
            IndexField indexField = null;
            switch (type)
            {
                case IndexValueType.String: indexField = new IndexField(name, (string)x["StringValue"], mode, store, termVector); break;
                case IndexValueType.StringArray: throw new NotSupportedException();
                case IndexValueType.Bool: indexField = new IndexField(name, (bool)x["BooleanValue"], mode, store, termVector); break;
                case IndexValueType.Int: indexField = new IndexField(name, (int)x["IntegerValue"], mode, store, termVector); break;
                case IndexValueType.Long: indexField = new IndexField(name, (long)x["LongValue"], mode, store, termVector); break;
                case IndexValueType.Float: indexField = new IndexField(name, (float)x["SingleValue"], mode, store, termVector); break;
                case IndexValueType.Double: indexField = new IndexField(name, (double)x["DoubleValue"], mode, store, termVector); break;
                case IndexValueType.DateTime: indexField = new IndexField(name, (DateTime)x["DateTimeValue"], mode, store, termVector); break;
                default: throw new ArgumentOutOfRangeException();
            }

            return indexField;
        }
    }
}
