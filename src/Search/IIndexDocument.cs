using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    public class DocumentUpdate
    {
        public SnTerm UpdateTerm;
        public IndexDocument Document;
    }

    [Serializable]
    [DebuggerDisplay("{Name}:{ValueAsString}:{Type}")]
    public class SnTerm
    {
        public const string Yes = "yes";
        public const string No = "no";

        public SnTerm(string name, string value)   { Name = name; Type = SnTermType.String;      StringValue = value;      ValueAsString = value;}
        public SnTerm(string name, string[] value) { Name = name; Type = SnTermType.StringArray; StringArrayValue = value; ValueAsString = string.Join(",", value); }
        public SnTerm(string name, bool value)     { Name = name; Type = SnTermType.Bool;        BooleanValue = value;     ValueAsString = value ? Yes : No; }
        public SnTerm(string name, int value)      { Name = name; Type = SnTermType.Int;         IntegerValue = value;     ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public SnTerm(string name, long value)     { Name = name; Type = SnTermType.Long;        LongValue = value;        ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public SnTerm(string name, float value)    { Name = name; Type = SnTermType.Float;       SingleValue = value;      ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public SnTerm(string name, double value)   { Name = name; Type = SnTermType.Double;      DoubleValue = value;      ValueAsString = value.ToString(CultureInfo.InvariantCulture); }
        public SnTerm(string name, DateTime value) { Name = name; Type = SnTermType.DateTime;    DateTimeValue = value;    ValueAsString = value.ToString("yyyy-MM-dd HH:mm:ss.ffff"); }

        public string Name { get; }
        public SnTermType Type { get; }

        public virtual string StringValue { get; }
        public virtual string[] StringArrayValue { get; }
        public virtual bool BooleanValue { get; }
        public virtual int IntegerValue { get; }
        public virtual long LongValue { get; }
        public virtual float SingleValue { get; }
        public virtual double DoubleValue { get; }
        public virtual DateTime DateTimeValue { get; }

        public string ValueAsString { get; }
    }

    [Serializable]
    [DebuggerDisplay("{Name}:{ValueAsString}:{Type}, Mode:{Mode}, Store:{Store}, TermVector:{TermVector}")]
    public class IndexField : SnTerm
    {
        public IndexingMode Mode { get; }
        public IndexStoringMode Store { get; }
        public IndexTermVector TermVector { get; }

        public IndexField(string name, string value,   IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, string[] value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, bool value,     IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, int value,      IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, long value,     IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, float value,    IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, double value,   IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, DateTime value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
    }

    [Serializable]
    public class NotIndexedIndexDocument : IndexDocument { }

    [Serializable]
    public class IndexDocument : IEnumerable<IndexField>
    {
        [NonSerialized]
        public static readonly IndexDocument NotIndexedDocument = new NotIndexedIndexDocument();

        [NonSerialized]
        public static List<string> PostponedFields = new List<string>(new string[] {
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
