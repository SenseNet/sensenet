using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace SenseNet.Search
{
    public class DocumentUpdate
    {
        public SnTerm UpdateTerm;
        public IndexDocument Document;
    }

    [Serializable]
    public class NotIndexedIndexDocument : IndexDocument { }

    [Serializable]
    public class IndexDocument : IEnumerable<IndexField>
    {
        [NonSerialized]
        public static readonly IndexDocument NotIndexedDocument = new NotIndexedIndexDocument();

        [NonSerialized]
        public static List<string> PostponedFields = new List<string>(new [] {
            IndexFieldName.Name, IndexFieldName.Path, IndexFieldName.InTree, IndexFieldName.InFolder, IndexFieldName.Depth, IndexFieldName.ParentId,
            IndexFieldName.IsSystem
        });

        [NonSerialized]
        public static List<string> ForbiddenFields = new List<string>(new[] { "Password", "PasswordHash" });

        private readonly Dictionary<string, IndexField> _fields = new Dictionary<string, IndexField>();

        public bool HasCustomField { get; set; }

        /// <summary>
        /// Returns with VersionId. Shortcut of the following call: GetIntegerValue(IndexFieldName.VersionId);
        /// </summary>
        public int VersionId => GetIntegerValue(IndexFieldName.VersionId);

        /// <summary>
        /// Returns with Version. Shortcut of the following call: GetStringValue(IndexFieldName.Version);
        /// </summary>
        public string Version => GetStringValue(IndexFieldName.Version);

        public string GetStringValue(string fieldName)
        {
            IndexField field;
            if (!_fields.TryGetValue(fieldName, out field))
                return default(string);

            if (field.Type == SnTermType.String)
                return field.StringValue;

            if (field.Type == SnTermType.StringArray)
                return field.StringArrayValue.FirstOrDefault();

            throw TypeError(fieldName, field.Type);
        }
        public string[] GetStringArrayValue(string fieldName)
        {
            IndexField field;
            if (!_fields.TryGetValue(fieldName, out field))
                return default(string[]);

            if (field.Type == SnTermType.String)
                return new[] {field.StringValue};

            if (field.Type == SnTermType.StringArray)
                return field.StringArrayValue;

            throw TypeError(fieldName, field.Type);
        }
        public bool GetBooleanValue(string fieldName)
        {
            IndexField field;
            if (!_fields.TryGetValue(fieldName, out field))
                return default(bool);

            if (field.Type == SnTermType.Bool)
                return field.BooleanValue;

            throw TypeError(fieldName, field.Type);
        }
        public int GetIntegerValue(string fieldName)
        {
            IndexField field;
            if (!_fields.TryGetValue(fieldName, out field))
                return default(int);

            if (field.Type == SnTermType.Int)
                return field.IntegerValue;

            throw TypeError(fieldName, field.Type);
        }
        public long GetLongValue(string fieldName)
        {
            IndexField field;
            if (!_fields.TryGetValue(fieldName, out field))
                return default(long);

            if (field.Type == SnTermType.Long)
                return field.LongValue;

            throw TypeError(fieldName, field.Type);
        }
        public float GetSingleValue(string fieldName)
        {
            IndexField field;
            if (!_fields.TryGetValue(fieldName, out field))
                return default(float);

            if (field.Type == SnTermType.Float)
                return field.SingleValue;

            throw TypeError(fieldName, field.Type);
        }
        public double GetDoubleValue(string fieldName)
        {
            IndexField field;
            if (!_fields.TryGetValue(fieldName, out field))
                return default(double);

            if (field.Type == SnTermType.Double)
                return field.DoubleValue;

            throw TypeError(fieldName, field.Type);
        }
        public DateTime GetDateTimeValue(string fieldName)
        {
            IndexField field;
            if (!_fields.TryGetValue(fieldName, out field))
                return default(DateTime);

            if (field.Type == SnTermType.DateTime)
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

        public bool HasField(string fieldName)
        {
            return _fields.ContainsKey(fieldName);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public IEnumerator<IndexField> GetEnumerator()
        {
            return _fields.Values.GetEnumerator();
        }

        private Exception TypeError(string fieldName, SnTermType fieldType)
        {
            return new ApplicationException($"Cannot return with string value because Indexfield '{fieldName}' is {fieldType}");
        }

        /* =========================================================================================== */

        public static IndexDocument Deserialize(byte[] serializedIndexDocument)
        {
            var docStream = new MemoryStream(serializedIndexDocument);
            var formatter = new BinaryFormatter();
            var indxDoc = (IndexDocument)formatter.Deserialize(docStream);
            return indxDoc;
        }
    }
}
