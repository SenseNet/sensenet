using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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

        //UNDONE: XMLDOC CustomIndexField
        public bool HasCustomField { get; set; }

        /// <summary>
        /// Returns with VersionId. Shortcut of the following call: GetIntegerValue(IndexFieldName.VersionId);
        /// </summary>
        public int VersionId => GetIntegerValue(IndexFieldName.VersionId);

        /// <summary>
        /// Returns with Version. Shortcut of the following call: GetStringValue(IndexFieldName.Version);
        /// </summary>
        public string Version => GetStringValue(IndexFieldName.Version);

        /// <summary>
        /// Return with the System.String value of the existing named field.
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
        /// Return with the array of System.String value of the existing named field.
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
        /// Return with the System.Boolean value of the existing named field.
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
        /// Return with the System.Int32 value of the existing named field.
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
        /// Return with the System.Int64 value of the existing named field.
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
        /// Return with the System.Single value of the existing named field.
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
        /// Return with the System.Double value of the existing named field.
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
        /// Return with the System.DateTime value of the existing named field.
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
        /// Retuns with true if the document contains the field with the given field name.
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

        /// <summary>
        /// Returns with the deserialized IndexDocument.
        /// </summary>
        /// <param name="serializedIndexDocument"></param>
        /// <returns></returns>
        public static IndexDocument Deserialize(byte[] serializedIndexDocument)
        {
            var docStream = new MemoryStream(serializedIndexDocument);
            var formatter = new BinaryFormatter();
            var indxDoc = (IndexDocument)formatter.Deserialize(docStream);
            return indxDoc;
        }
    }
}
