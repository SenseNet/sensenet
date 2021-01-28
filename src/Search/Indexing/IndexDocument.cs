using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Represents an atomic data structure to updating a document in the index.
    /// </summary>
    public class DocumentUpdate
    {
        /// <summary>
        /// A term that identifies the document in the index
        /// </summary>
        public SnTerm UpdateTerm;
        /// <summary>
        /// The new document that will overwrite the existing one.
        /// </summary>
        public IndexDocument Document;
    }

    /// <summary>
    /// Represents a class for the index document that will be not included in the index.
    /// </summary>
    [Serializable]
    public class NotIndexedIndexDocument : IndexDocument { }

    /// <summary>
    /// Represents a collection of index fields.
    /// </summary>
    [Serializable]
    public class IndexDocument : IEnumerable<IndexField>
    {
        /// <summary>
        /// Represents an index document that will be not included in the index.
        /// </summary>
        [NonSerialized]
        public static readonly IndexDocument NotIndexedDocument = new NotIndexedIndexDocument();

        /// <summary>
        /// Contains all field names that are indexed but not stored in the precompiled index document.
        /// </summary>
        [NonSerialized] public static List<string> PostponedFields = new List<string>(new[]
        {
            IndexFieldName.Name, IndexFieldName.Path, IndexFieldName.InTree, IndexFieldName.InFolder,
            IndexFieldName.Depth, IndexFieldName.ParentId, IndexFieldName.IsSystem
        });

        /// <summary>
        /// Contains all field names that are not indexed.
        /// </summary>
        [NonSerialized]
        public static List<string> ForbiddenFields = new List<string>(new[] { "Password", "PasswordHash" });

        private readonly Dictionary<string, IndexField> _fields = new Dictionary<string, IndexField>();

        /// <summary>
        /// Returns with VersionId. Shortcut of the following call: GetIntegerValue(IndexFieldName.VersionId);
        /// </summary>
        public int VersionId => GetIntegerValue(IndexFieldName.VersionId);

        /// <summary>
        /// Returns with Version. Shortcut of the following call: GetStringValue(IndexFieldName.Version);
        /// </summary>
        public string Version => GetStringValue(IndexFieldName.Version);

        /// <summary>
        /// Returns with the System.String value of the existing named field.
        /// If the field does not exist in the document, returns with null.
        /// If the IndexValueType of the existing field is not String or StringArray, an ApplicationException will be thrown.
        /// If the IndexValueType of the existing field is StringArray, returns with the first value of the array.
        /// </summary>
        public string GetStringValue(string fieldName)
        {
            if (!_fields.TryGetValue(fieldName, out var field))
                return default(string);

            if (field.Type == IndexValueType.String)
                return field.StringValue;

            if (field.Type == IndexValueType.StringArray)
                return field.StringArrayValue.FirstOrDefault();

            throw TypeError(fieldName, field.Type);
        }
        /// <summary>
        /// Returns with the array of System.String value of the existing named field.
        /// If the field does not exist in the document, returns with null.
        /// If the IndexValueType of the existing field is not String or StringArray, an ApplicationException will be thrown.
        /// If the IndexValueType of the existing field is String, returns with an one element array.
        /// </summary>
        public string[] GetStringArrayValue(string fieldName)
        {
            if (!_fields.TryGetValue(fieldName, out var field))
                return default(string[]);

            if (field.Type == IndexValueType.String)
                return new[] {field.StringValue};

            if (field.Type == IndexValueType.StringArray)
                return field.StringArrayValue;

            throw TypeError(fieldName, field.Type);
        }
        /// <summary>
        /// Returns with the System.Boolean value of the existing named field.
        /// If the field does not exist in the document, returns with false.
        /// If the IndexValueType of the existing field is not Bool, an ApplicationException will be thrown.
        /// </summary>
        public bool GetBooleanValue(string fieldName)
        {
            if (!_fields.TryGetValue(fieldName, out var field))
                return default(bool);

            if (field.Type == IndexValueType.Bool)
                return field.BooleanValue;

            throw TypeError(fieldName, field.Type);
        }
        /// <summary>
        /// Returns with the System.Int32 value of the existing named field.
        /// If the field does not exist in the document, returns with 0.
        /// If the IndexValueType of the existing field is not Int, an ApplicationException will be thrown.
        /// </summary>
        public int GetIntegerValue(string fieldName)
        {
            if (!_fields.TryGetValue(fieldName, out var field))
                return default(int);

            if (field.Type == IndexValueType.Int)
                return field.IntegerValue;

            throw TypeError(fieldName, field.Type);
        }
        /// <summary>
        /// Returns with the System.Int64 value of the existing named field.
        /// If the field does not exist in the document, returns with 0l.
        /// If the IndexValueType of the existing field is not Long, an ApplicationException will be thrown.
        /// </summary>
        public long GetLongValue(string fieldName)
        {
            if (!_fields.TryGetValue(fieldName, out var field))
                return default(long);

            if (field.Type == IndexValueType.Long)
                return field.LongValue;

            throw TypeError(fieldName, field.Type);
        }
        /// <summary>
        /// Returns with the System.Single value of the existing named field.
        /// If the field does not exist in the document, returns with 0f.
        /// If the IndexValueType of the existing field is not Float, an ApplicationException will be thrown.
        /// </summary>
        public float GetSingleValue(string fieldName)
        {
            if (!_fields.TryGetValue(fieldName, out var field))
                return default(float);

            if (field.Type == IndexValueType.Float)
                return field.SingleValue;

            throw TypeError(fieldName, field.Type);
        }
        /// <summary>
        /// Returns with the System.Double value of the existing named field.
        /// If the field does not exist in the document, returns with 0d.
        /// If the IndexValueType of the existing field is not Double, an ApplicationException will be thrown.
        /// </summary>
        public double GetDoubleValue(string fieldName)
        {
            if (!_fields.TryGetValue(fieldName, out var field))
                return default(double);

            if (field.Type == IndexValueType.Double)
                return field.DoubleValue;

            throw TypeError(fieldName, field.Type);
        }
        /// <summary>
        /// Returns with the System.DateTime value of the existing named field.
        /// If the field does not exist in the document, returns with DateTime.MinValue.
        /// If the IndexValueType of the existing field is not DateTime, an ApplicationException will be thrown.
        /// </summary>
        public DateTime GetDateTimeValue(string fieldName)
        {
            if (!_fields.TryGetValue(fieldName, out var field))
                return default(DateTime);

            if (field.Type == IndexValueType.DateTime)
                return field.DateTimeValue;

            throw TypeError(fieldName, field.Type);
        }

        /// <summary>
        /// Adds or change the existing field in the document.
        /// </summary>
        /// <param name="field"></param>
        public void Add(IndexField field)
        {
            if (!ForbiddenFields.Contains(field.Name))
                _fields[field.Name] = field;
        }

        /// <summary>
        /// Removes a field by name if it exists.
        /// </summary>
        /// <param name="fieldName"></param>
        public void Remove(string fieldName)
        {
            if (_fields.ContainsKey(fieldName))
                _fields.Remove(fieldName);
        }

        /// <summary>
        /// Returns with true if the document contains the field with the given field name.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool HasField(string fieldName)
        {
            return _fields.ContainsKey(fieldName);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        /// <inheritdoc />
        public IEnumerator<IndexField> GetEnumerator()
        {
            return _fields.Values.GetEnumerator();
        }

        private static Exception TypeError(string fieldName, IndexValueType fieldType)
        {
            return new ApplicationException($"Cannot return with string value because Indexfield '{fieldName}' is {fieldType}");
        }

        /* =========================================================================================== */

        private static readonly JsonSerializerSettings FormattedSerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> {new IndexFieldJsonConverter()},
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Formatting = Formatting.Indented
        };
        private static readonly JsonSerializerSettings OneLineSerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new IndexFieldJsonConverter() },
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Formatting = Formatting.None
        };

        public static IndexDocument Deserialize(string serializedIndexDocument)
        {
            try
            {
                var deserialized = JsonSerializer.Create(FormattedSerializerSettings).Deserialize(
                    new JsonTextReader(new StringReader(serializedIndexDocument)));
                var result = new IndexDocument();
                foreach (JObject field in (JArray)deserialized)
                {
                    string name = null;
                    var type = IndexValueType.String;
                    var mode = IndexingMode.Default;
                    var store = IndexStoringMode.Default;
                    var termVector = IndexTermVector.Default;
                    object value = null;

                    foreach (var item in field)
                    {
                        switch (item.Key)
                        {
                            case "Name": name = ((JValue)item.Value).ToString(); break;
                            case "Type": type = (IndexValueType)Enum.Parse(typeof(IndexValueType), ((JValue)item.Value).ToString(), true); break;
                            case "Mode": mode = (IndexingMode)Enum.Parse(typeof(IndexingMode), ((JValue)item.Value).ToString(), true); break;
                            case "Store": store = (IndexStoringMode)Enum.Parse(typeof(IndexStoringMode), ((JValue)item.Value).ToString(), true); break;
                            case "TermVector": termVector = (IndexTermVector)Enum.Parse(typeof(IndexTermVector), ((JValue)item.Value).ToString(), true); break;
                            case "Value":
                                if (item.Value is JValue jvalue)
                                    value = jvalue;
                                else if (item.Value is JArray jarray)
                                    value = jarray.Select(x => x.ToString()).ToArray();
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }

                    IndexField indexField;
                    switch (type)
                    {
                        case IndexValueType.String:
                            indexField = new IndexField(name, value?.ToString(), mode, store, termVector);
                            break;
                        case IndexValueType.StringArray:
                            indexField = new IndexField(name, (string[])value, mode, store, termVector);
                            break;
                        case IndexValueType.Bool:
                            indexField = new IndexField(name, Convert.ToBoolean(value), mode, store, termVector);
                            break;
                        case IndexValueType.Int:
                            indexField = new IndexField(name, Convert.ToInt32(value), mode, store, termVector);
                            break;
                        case IndexValueType.Long:
                            indexField = new IndexField(name, Convert.ToInt64(value), mode, store, termVector);
                            break;
                        case IndexValueType.Float:
                            indexField = new IndexField(name, Convert.ToSingle(value), mode, store, termVector);
                            break;
                        case IndexValueType.Double:
                            indexField = new IndexField(name, Convert.ToDouble(value), mode, store, termVector);
                            break;
                        case IndexValueType.DateTime:
                            indexField = new IndexField(name, DateTime.Parse(value.ToString()), mode, store, termVector);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    result.Add(indexField);
                }
                return result;
            }
            catch (Exception e)
            {
                throw new SerializationException("Cannot deserialize the index document: " + e.Message, e);
            }
        }

        public string Serialize(bool oneLine = false)
        {
            using (var writer = new StringWriter())
            {
                var settings = oneLine ? OneLineSerializerSettings : FormattedSerializerSettings;
                JsonSerializer.Create(settings).Serialize(writer, this);
                var serializedDoc = writer.GetStringBuilder().ToString();
                return serializedDoc;
            }
        }
    }
}
