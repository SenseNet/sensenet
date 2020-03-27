using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search.Querying;

// ReSharper disable once CheckNamespace
namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Defines a base class of the converter class family that handle the data conversions
    /// among the querying and indexing and the value of the <see cref="SenseNet.ContentRepository.Field"/>.
    /// </summary>
    public abstract class FieldIndexHandler : IFieldIndexHandler
    {
        /// <inheritdoc />
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }

        /// <inheritdoc />
        public virtual IndexValueType IndexFieldType => IndexValueType.String;

        /// <inheritdoc />
        /// <remarks>Used in SnQuery compilers.</remarks>
        public abstract IndexValue Parse(string text);

        /// <inheritdoc />
        /// <remarks>Used in LINQ.</remarks>
        public abstract IndexValue ConvertToTermValue(object value);

        /// <inheritdoc />
        public abstract IEnumerable<IndexField> GetIndexFields(IIndexableField field, out string textExtract);

        /// <inheritdoc />
        public abstract IEnumerable<string> GetParsableValues(IIndexableField snField);

        /// <summary>
        /// Creates IndexFields from a given name and a System.String value.
        /// </summary>
        protected IEnumerable<IndexField> CreateField(string name, string value) => new[]
            {
                new IndexField(name, value,
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        /// <summary>
        /// Creates IndexFields from a given name and a IEnumerable&lt;string&gt; value.
        /// </summary>
        protected IEnumerable<IndexField> CreateField(string name, IEnumerable<string> value) => new[]
            {
                new IndexField(name, value.ToArray(),
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        /// <summary>
        /// Creates IndexFields from a given name and a IEnumerable&lt;string&gt; value.
        /// Creates an additional IndexField for the value that will be used in sorting.
        /// </summary>
        protected IEnumerable<IndexField> CreateField(string name, IEnumerable<string> value, string sortTerm) => new[]
            {
                new IndexField(name, value.ToArray(),
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode),
                new IndexField(GetSortFieldName(name), sortTerm,
                    PerFieldIndexingInfo.DefaultIndexingMode,
                    PerFieldIndexingInfo.DefaultIndexStoringMode,
                    PerFieldIndexingInfo.DefaultTermVectorStoringMode)
            };
        /// <summary>
        /// Creates IndexFields from a given name and a System.Boolean value.
        /// </summary>
        protected IEnumerable<IndexField> CreateField(string name, bool value) => new[]
            {
                new IndexField(name, value,
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        /// <summary>
        /// Creates IndexFields from a given name and a System.Int32 value.
        /// </summary>
        protected IEnumerable<IndexField> CreateField(string name, int value) => new[]
            {
                new IndexField(name, value,
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        /// <summary>
        /// Creates IndexFields from a given name and a System.Double value.
        /// </summary>
        protected IEnumerable<IndexField> CreateField(string name, double value) => new[]
            {
                new IndexField(name, value,
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        /// <summary>
        /// Creates IndexFields from a given name and a DateTime value.
        /// </summary>
        protected IEnumerable<IndexField> CreateField(string name, DateTime value) => new[]
            {
                new IndexField(name, value,
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };

        /// <inheritdoc />
        public virtual IndexFieldAnalyzer GetDefaultAnalyzer() { return IndexFieldAnalyzer.Keyword; }
        /// <inheritdoc />
        public virtual string GetSortFieldName(string fieldName) { return fieldName; }
    }

    /* ============================================================= Not IIndexValueConverters */

    /// <summary>
    /// Inherited IndexFieldHandler for handling not indexed <see cref="SenseNet.ContentRepository.Field"/>s.
    /// </summary>
    public class NotIndexedIndexFieldHandler : FieldIndexHandler
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField field, out string textExtract)
        {
            textExtract = string.Empty;
            return new IndexField[0];
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return null;
        }
        /// <inheritdoc select="summary|remarks|param"/>
        public override IndexValue ConvertToTermValue(object value)
        {
            return new IndexValue(value.ToString());
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            return null;
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling <see cref="BinaryData"/> value of a <see cref="Field"/>.
    /// </summary>
    public class BinaryIndexHandler : FieldIndexHandler
    {
        /// <inheritdoc />
        public override IndexFieldAnalyzer GetDefaultAnalyzer() { return IndexFieldAnalyzer.Standard; }
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = !(snField.GetData() is BinaryData data)
                ? string.Empty
                : TextExtractor.GetExtract(data, ((Field) snField).Content.ContentHandler);

            return CreateField(snField.Name, textExtract);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return value == null ? new IndexValue(string.Empty) : new IndexValue(((string)value).ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            return null;
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling <see cref="NodeType"/> value of a <see cref="Field"/>.
    /// The NodeType is represented as a path of the type inheritance tree (e.g. "genericcontent/folder/systemfolder").
    /// </summary>
    public class TypeTreeIndexHandler : FieldIndexHandler
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;
            var nodeType = ((Field)snField).Content.ContentHandler.NodeType;
            var types = nodeType.NodeTypePath.Split('/').Select(p => p.ToLowerInvariant());
            return CreateField(snField.Name, types);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return value == null ? new IndexValue(string.Empty) : new IndexValue(((string)value).ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            return ((Field)snField).Content.ContentHandler.NodeType.NodeTypePath.Split('/').Select(p => p.ToLowerInvariant());
        }
    }

    public class ContentTypeEnumerableIndexHandler : FieldIndexHandler
    {
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }

        public override IndexValue ConvertToTermValue(object value)
        {
            return new IndexValue(((ContentType)value).Name);
        }

        public override IEnumerable<IndexField> GetIndexFields(IIndexableField field, out string textExtract)
        {
            textExtract = string.Empty;
            if(field.GetData() is IEnumerable<ContentType> types)
                return CreateField(field.Name, types.Select(x=>x.Name));
            return null;
        }

        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            if (snField.GetData() is IEnumerable<ContentType> types)
                return types.Select(x => x.Name).ToArray();
            return Enumerable.Empty<string>();
        }
    }

    /* ============================================================= Not implemented IIndexValueConverters */

    /// <summary>
    /// Inherited IndexFieldHandler for handling <see cref="HyperLinkField.HyperlinkData"/> value of a <see cref="Field"/>.
    /// </summary>
    public class HyperLinkIndexHandler : FieldIndexHandler, IIndexValueConverter<object>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            var data = (HyperLinkField.HyperlinkData)snField.GetData();
            if (data == null)
            {
                textExtract = string.Empty;
                return null;
            }
            var strings = new List<string>();
            if (data.Href != null)
                strings.Add(data.Href.ToLowerInvariant());
            if (data.Target != null)
                strings.Add(data.Target.ToLowerInvariant());
            if (data.Text != null)
                strings.Add(data.Text.ToLowerInvariant());
            if (data.Title != null)
                strings.Add(data.Title.ToLowerInvariant());
            textExtract = string.Join(" ", strings.ToArray());
            return CreateField(snField.Name, strings);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return value == null ? new IndexValue(string.Empty) : new IndexValue(((string)value).ToLowerInvariant());
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        public object GetBack(string indexFieldValue)
        {
            throw new SnNotSupportedException();
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var data = (HyperLinkField.HyperlinkData)snField.GetData();
            if (data == null)
            {
                return null;
            }
            var strings = new List<string>();
            if (data.Href != null)
                strings.Add(data.Href.ToLowerInvariant());
            if (data.Target != null)
                strings.Add(data.Target.ToLowerInvariant());
            if (data.Text != null)
                strings.Add(data.Text.ToLowerInvariant());
            if (data.Title != null)
                strings.Add(data.Title.ToLowerInvariant());
            return strings;
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling the values of a <see cref="ChoiceField"/>.
    /// </summary>
    public class ChoiceIndexHandler : FieldIndexHandler, IIndexValueConverter<object>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override string GetSortFieldName(string fieldName)
        {
            return fieldName + "_sort";
        }
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            var data = snField.GetData() ?? string.Empty;

            if (data is string stringData)
            {
                textExtract = stringData.ToLowerInvariant();
                return CreateField(snField.Name, textExtract);
            }

            if (data is IEnumerable<string> listData)
            {
                // words to choice field
                var wordList = new List<string>();
                // words to sort field
                var sortList = new List<string>();
                // words to full text field
                var localizedWords = new List<string>();
                foreach (var inputWord in listData)
                {
                    // process every word
                    if (((Field)snField).FieldSetting is ChoiceFieldSetting cfs)
                    {
                        // field with ChoiceFieldSetting
                        var optionKey = cfs.Options.Where(x => x.Value == inputWord).Select(x => x.StoredText).FirstOrDefault();
                        if (optionKey != null)
                        {
                            // identified option
                            var optionTerm = "$" + inputWord.ToLowerInvariant();
                            wordList.Add(optionTerm);
                            sortList.Add(optionTerm);

                            var localized = SenseNetResourceManager.ParseResourceKey(optionKey, out var className, out var name);
                            if (localized && className != null && name != null)
                            {
                                // localized texts: add all known mutations
                                var lw = SenseNetResourceManager.Current.GetStrings(className, name);
                                localizedWords.AddRange(lw.Select(x => x.ToLowerInvariant()));
                            }
                            else
                            {
                                // not localized: add the word
                                localizedWords.Add(optionKey.ToLowerInvariant());
                            }
                        }
                        else
                        {
                            // unidentified option: extra value
                            if (inputWord.StartsWith(ChoiceField.EXTRAVALUEPREFIX))
                            {
                                // drives ordering (additional '~' hides this information)
                                sortList.Add("~" + inputWord);
                                // add 
                                var splitted = inputWord.Split('.');
                                wordList.Add(splitted[0]);
                                localizedWords.Add(splitted[1].ToLowerInvariant());
                            }
                            else
                            {
                                // add as a lowercase word
                                wordList.Add(inputWord.ToLowerInvariant());
                                localizedWords.Add(inputWord.ToLowerInvariant());
                            }
                        }
                    }
                    else
                    {
                        // field with another field setting
                        wordList.Add(inputWord.ToLowerInvariant());
                    }
                }
                sortList.Sort();
                var sortTerm = string.Join("-", sortList);
                textExtract = string.Join(" ", localizedWords);
                wordList.AddRange(localizedWords);
                //SnTrace.Write(">>>>>>>>>>>>> {0}:'{1}' (sort by: {2})", snField.Name, wordList, sortTerm);
                return CreateField(snField.Name, wordList, sortTerm);
            }

            if (data is IEnumerable enumerableData)
            {
                var words = new List<string>();
                foreach (var item in enumerableData)
                    words.Add(Convert.ToString(item, CultureInfo.InvariantCulture).ToLowerInvariant());
                var wordArray = words.ToArray();
                textExtract = string.Join(" ", wordArray);
                return CreateField(snField.Name, words);
            }

            throw new NotSupportedException(string.Concat("Cannot create index from this type: ", data.GetType().FullName,
                ". Indexable data can be string, IEnumerable<string> or IEnumerable."));
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return value == null ? new IndexValue(string.Empty) : new IndexValue(((string)value).ToLowerInvariant());
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        public object GetBack(string indexFieldValue)
        {
            throw new SnNotSupportedException();
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var data = ((Field)snField).GetData() ?? string.Empty;

            if (data is string stringData)
                return new[] { stringData.ToLowerInvariant() };

            if (data is IEnumerable<string> listData)
                return listData.Select(s => s.ToLowerInvariant()).ToArray();

            if (data is IEnumerable enumerableData)
            {
                return (from object item in enumerableData
                    select Convert.ToString(item, CultureInfo.InvariantCulture).ToLowerInvariant()).ToList();
            }

            return new[] { string.Empty };
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling the string[] values of a <see cref="Field"/>.
    /// Designed for a special permission selector field.
    /// </summary>
    public class PermissionChoiceIndexHandler : FieldIndexHandler, IIndexValueConverter<object>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;

            var snFieldValue = (string[])snField.GetData();
            if (snFieldValue == null || snFieldValue.Length == 0)
                return CreateField(snField.Name, string.Empty);

            var terms = snFieldValue.Select(x => x.ToLowerInvariant()).ToArray();

            return CreateField(snField.Name, terms);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return value == null ? new IndexValue(string.Empty) : new IndexValue(((string)value).ToLowerInvariant());
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        public object GetBack(string indexFieldValue)
        {
            throw new SnNotSupportedException();
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            // copied from TagIndexHandler...
            var data = ((Field)snField).GetData() ?? string.Empty;

            if (data is string stringData)
                return new[] { stringData.ToLowerInvariant() };

            if (data is IEnumerable<string> listData)
                return listData.Select(s => s.ToLowerInvariant()).ToArray();

            if (data is IEnumerable enumerableData)
                return (from object item in enumerableData select Convert.ToString(item, CultureInfo.InvariantCulture).ToLowerInvariant()).ToList();

            return new[] { string.Empty };
        }
    }

    /// <summary>
    /// Experimental JSON index handler that provides all property values as index values.
    /// </summary>
    internal class GeneralJsonIndexHandler : LongTextIndexHandler
    {       
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField field, out string textExtract)
        {
            var data = field.GetData() ?? string.Empty;
            JObject jData = null;

            // These fields are technical fields, we should not let users
            // search for their values in fulltext queries.
            textExtract = string.Empty;

            if (data is string stringData)
            {
                try
                {
                    jData = JsonConvert.DeserializeObject(stringData, typeof(JObject)) as JObject;
                }
                catch (Exception ex)
                {
                    var f = field as Field;
                    SnTrace.Index.WriteError($"Error when converting JSON value. {ex.Message} " +
                        $"ContentId: {f?.Content?.Id}, Path: {f?.Content?.Path}, Field: {field.Name}");
                }                
            }
            else if (data is JObject jsonData)
            {
                jData = jsonData;
            }

            var indexFieldValues = new List<string>();
            if (jData != null)
            {
                foreach(var child in jData.Children())
                {
                    indexFieldValues.AddRange(GetIndexFieldValuesFromJToken(child, field as Field));
                }
            }

            return CreateField(field.Name, indexFieldValues);
        }

        protected virtual IEnumerable<string> GetIndexFieldValuesFromJToken(JToken token, Field field)
        {
            if (token == null)
                return Array.Empty<string>();

            static string GetIndexValue<T>(JProperty jprop)
            {
                return $"{jprop.Path.Replace('.', '#')}#{jprop.Value.Value<T>()}".ToLowerInvariant();
            }

            if (token is JProperty prop)
            {          
                switch (prop.Value.Type)
                {
                    case JTokenType.String:
                        return new string[] { GetIndexValue<string>(prop) };
                    case JTokenType.Integer:
                        return new string[] { GetIndexValue<int>(prop) };
                    case JTokenType.Boolean:
                        return new string[] { GetIndexValue<bool>(prop) };
                    case JTokenType.Object:
                        var childValues = new List<string>();
                        foreach (var child in prop.Value.Children())
                        {
                            childValues.AddRange(GetIndexFieldValuesFromJToken(child, field));
                        }
                        return childValues;
                    case JTokenType.Null:
                        return new string[] { $"{prop.Path.Replace('.', '#')}#null".ToLowerInvariant() };
                }
            }

            return Array.Empty<string>();
        }
    }

    /* ============================================================= IIndexValueConverters */

    /// <summary>
    /// Inherited IndexFieldHandler for handling short text or any similar value of a <see cref="Field"/>.
    /// </summary>
    public class LowerStringIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            var field = (Field)snField;

            if (SenseNetResourceManager.Running && field.LocalizationEnabled && field.IsLocalized && SenseNetResourceManager.ParseResourceKey(field.GetStoredValue(), out var className, out var name))
            {
                var strings = SenseNetResourceManager.Current.GetStrings(className, name)
                    .Select(s => s.ToLowerInvariant()).ToArray();
                textExtract = string.Join(" ", strings);
                return CreateField(field.Name, strings);
            }
            var data = field.GetData();
            var stringValue = data?.ToString().ToLowerInvariant() ?? string.Empty;
            textExtract = stringValue;

            return CreateField(field.Name, stringValue);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return value == null ? new IndexValue(string.Empty) : new IndexValue(((string)value).ToLowerInvariant());
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public string GetBack(string indexFieldValue)
        {
            return indexFieldValue;
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var data = ((Field)snField).GetData();
            return new[] { data?.ToString().ToLowerInvariant() ?? string.Empty };
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling <see cref="bool"/> value of a <see cref="Field"/>.
    /// </summary>
    public class BooleanIndexHandler : FieldIndexHandler, IIndexValueConverter<bool>, IIndexValueConverter
    {
        /// <summary>
        /// Contains all string value that will be converted to "true".
        /// Shortcut of the similar property of the <see cref="SearchManager" />
        /// </summary>
        public static List<string> YesList => SearchManager.YesList;
        /// <summary>
        /// Contains all string value that will be converted to "false".
        /// Shortcut of the similar property of the <see cref="SearchManager" />
        /// </summary>
        public static List<string> NoList => SearchManager.NoList;

        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;
            var boolValue = (bool?)snField.GetData() ?? false;
            return CreateField(snField.Name, boolValue);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            var v = text.ToLowerInvariant();
            if (YesList.Contains(v))
                return new IndexValue(true);
            if (NoList.Contains(v))
                return new IndexValue(false);
            if (bool.TryParse(v, out var b))
                return new IndexValue(b);
            return null;
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return new IndexValue((bool) value);
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public bool GetBack(string indexFieldValue)
        {
            return ConvertBack(indexFieldValue);
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }

        /// <summary>
        /// Converts the value of the index field to <see cref="bool"/>
        /// </summary>
        public static bool ConvertBack(string indexFieldValue)
        {
            return indexFieldValue == IndexValue.Yes;
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var value = ((Field)snField).GetData();
            var boolValue = value != null && (bool)value;
            return new[] { boolValue ? IndexValue.Yes : IndexValue.No };
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling <see cref="int"/> value of a <see cref="Field"/>.
    /// </summary>
    public class IntegerIndexHandler : FieldIndexHandler, IIndexValueConverter<int>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IndexValueType IndexFieldType => IndexValueType.Int;

        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            var value = snField.GetData();
            int intValue;
            try
            {
                intValue = (int?) value ?? 0;
            }
            catch (Exception) // logged rethrown
            {
                SnTrace.Index.Write("IntegerIndexHandler ERROR: content: {0} field: {1}, value: {2}", ((Field)snField).Content.Path, snField.Name, value);
                throw;
            }
            textExtract = intValue.ToString();
            return CreateField(snField.Name, intValue);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            if (int.TryParse(text, out var intValue))
                return new IndexValue(intValue);
            return null;
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return new IndexValue((int)value);
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public int GetBack(string indexFieldValue)
        {
            return ConvertBack(indexFieldValue);
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }

        /// <summary>
        /// Converts az index field value to <see cref="int"/>.
        /// If the value is not an integer representation, returns with 0.
        /// </summary>
        /// <param name="indexFieldValue"></param>
        /// <returns></returns>
        public static int ConvertBack(string indexFieldValue)
        {
            if (int.TryParse(indexFieldValue, out var intValue))
                return intValue;
            return 0;
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var value = ((Field)snField).GetData();
            var intValue = (int?) value ?? 0;
            return new[] { intValue.ToString() };
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling <see cref="decimal"/> value of a <see cref="Field"/>.
    /// </summary>
    public class NumberIndexHandler : FieldIndexHandler, IIndexValueConverter<decimal>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IndexValueType IndexFieldType => IndexValueType.Double;

        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            var value = snField.GetData();
            var decimalValue = (decimal?) value ?? (decimal)0.0;
            var doubleValue = Convert.ToDouble(decimalValue);
            textExtract = decimalValue.ToString(CultureInfo.InvariantCulture);
            return CreateField(snField.Name, doubleValue);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            if (double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var doubleValue))
                return new IndexValue(doubleValue);
            return null;
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            var doubleValue = Convert.ToDouble(value);
            return new IndexValue(doubleValue);
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public decimal GetBack(string indexFieldValue)
        {
            return Convert.ToDecimal(indexFieldValue, CultureInfo.InvariantCulture);
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var value = ((Field)snField).GetData();
            var decimalValue = (decimal?) value ?? (decimal)0.0;
            return new[] { decimalValue.ToString(CultureInfo.InvariantCulture) };
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling <see cref="DateTime"/> value of a <see cref="Field"/>.
    /// </summary>
    public class DateTimeIndexHandler : FieldIndexHandler, IIndexValueConverter<DateTime>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IndexValueType IndexFieldType => IndexValueType.DateTime;

        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;
            var data = snField.GetData();
            var dateTime = (DateTime?)data ?? DateTime.MinValue;
            return CreateField(snField.Name, new DateTime(SetPrecision((Field)snField, dateTime.Ticks)));
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue))
                return new IndexValue(dateTimeValue);
            return null;
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return new IndexValue(((DateTime)value));
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public DateTime GetBack(string indexFieldValue)
        {
            return new DateTime(long.Parse(indexFieldValue));
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }

        private long SetPrecision(Field snField, long ticks)
        {
            var setting = snField.FieldSetting as DateTimeFieldSetting;
            DateTimePrecision? precision = null;
            if (setting != null)
                precision = setting.Precision;
            if (precision == null)
                precision = DateTimeFieldSetting.DefaultPrecision;

            switch (precision.Value)
            {
                case DateTimePrecision.Millisecond:
                    return ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond;
                case DateTimePrecision.Second:
                    return ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond;
                case DateTimePrecision.Minute:
                    return ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute;
                case DateTimePrecision.Hour:
                    return ticks / TimeSpan.TicksPerHour * TimeSpan.TicksPerHour;
                case DateTimePrecision.Day:
                    return ticks / TimeSpan.TicksPerDay * TimeSpan.TicksPerDay;
                default:
                    throw new SnNotSupportedException("Unknown DateTimePrecision: " + precision.Value);
            }
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var data = ((Field)snField).GetData();
            try
            {
                var dateData = Convert.ToDateTime(data);
                if (dateData != DateTime.MinValue)
                    return new[] {"'" + dateData.ToString("yyyy.MM.dd HH:mm:ss") + "'"};
            }
            catch (Exception ex)
            {
                SnLog.WriteInformation(ex);
            }
            return new[] { string.Empty };
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling long text value of a <see cref="Field"/>.
    /// </summary>
    public class LongTextIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IndexFieldAnalyzer GetDefaultAnalyzer() { return IndexFieldAnalyzer.Standard; }
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            var data = snField.GetData() as string;
            textExtract = data?.ToLowerInvariant() ?? string.Empty;
            return CreateField(snField.Name, textExtract);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return value == null ? new IndexValue(string.Empty) : new IndexValue(((string)value).ToLowerInvariant());
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public string GetBack(string indexFieldValue)
        {
            throw new NotSupportedException();
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var data = ((Field)snField).GetData() as string;
            return new[] { data?.ToLowerInvariant() ?? string.Empty };
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling the value of a <see cref="ReferenceField"/>.
    /// The index value is the Id set of the referenced nodes.
    /// </summary>
    public class ReferenceIndexHandler : FieldIndexHandler, IIndexValueConverter<int>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;
            var data = snField.GetData();
            if (data is Node node)
                return CreateField(snField.Name, node.Id);
            if (data is IEnumerable nodes)
                return nodes.Cast<Node>().Select(n => CreateField(snField.Name, n.Id).First());

            return CreateField(snField.Name, SnQuery.NullReferenceValue);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            if (int.TryParse(text, out var intValue))
                return new IndexValue(intValue);
            return new IndexValue(SnQuery.NullReferenceValue);
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            if (value == null)
                return new IndexValue(SnQuery.NullReferenceValue);

            if (value is Node node)
                return new IndexValue(node.Id);

            if (value is IEnumerable)
                throw new SnNotSupportedException("ReferenceIndexHandler.ConvertToTermValue() isn't implemented when value is IEnumerable.");

            throw new NotSupportedException($"Type {value.GetType()} is not supported as value of ReferenceField");
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public int GetBack(string indexFieldValue)
        {
            if (indexFieldValue == SnQuery.NullReferenceValue)
                return 0;
            if (int.TryParse(indexFieldValue, out var singleRef))
                return singleRef;
            return 0;
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var data = ((Field)snField).GetData();
            if (data is Node node)
                return new[] { node.Id.ToString() };
            if (data is IEnumerable nodes)
                return nodes.Cast<Node>().Select(n => n.Id.ToString());
            return null;
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling <see cref="NodeType"/> value of a <see cref="Field"/>.
    /// Designed for "Type" field of the index especially.
    /// </summary>
    public class ExclusiveTypeIndexHandler : FieldIndexHandler, IIndexValueConverter<ContentType>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return value == null ? new IndexValue(string.Empty) : new IndexValue(((string)value).ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            var nodeTypeName = ((Field)snField).Content.ContentHandler.NodeType.Name.ToLowerInvariant();
            textExtract = nodeTypeName;
            return CreateField(snField.Name, nodeTypeName);
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public ContentType GetBack(string indexFieldValue)
        {
            if (string.IsNullOrEmpty(indexFieldValue))
                return null;
            return ContentType.GetByName(indexFieldValue);
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            throw new SnNotSupportedException();
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling the value of a <see cref="Field"/>.
    /// Designed for "InFolder" field of the index especially.
    /// </summary>
    public class InFolderIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            var value = (string)snField.GetData() ?? string.Empty;
            textExtract = value.ToLowerInvariant();
            var parentPath = RepositoryPath.GetParentPath(textExtract) ?? "/";
            return CreateField(snField.Name, parentPath);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            var stringValue = text.ToLowerInvariant();
            if (stringValue.StartsWith("/root"))
                return new IndexValue(stringValue);
            return null;
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            var path = ((string)value).ToLowerInvariant();
            if (!path.StartsWith("/root"))
                throw new ApplicationException(string.Concat("Invalid path: '", path, "'. It must be absolute: '/root' or '/root/...'."));
            return new IndexValue(path);
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public string GetBack(string indexFieldValue)
        {
            return indexFieldValue;
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var value = (string)((Field)snField).GetData() ?? string.Empty;
            var parentPath = RepositoryPath.GetParentPath(value.ToLowerInvariant()) ?? "/";
            return new[] { parentPath.ToLowerInvariant() };
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling the value of a <see cref="Field"/>.
    /// Designed for "InTree" field of the index especially.
    /// </summary>
    public class InTreeIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;
            var value = (string)snField.GetData() ?? string.Empty;
            return CreateField(snField.Name, value.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            var path = ((string)value).ToLowerInvariant();
            if (!path.StartsWith("/root"))
                throw new ApplicationException(string.Concat("Invalid path: '", path, "'. It must be absolute: '/root' or '/root/...'."));
            return new IndexValue(path);
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public string GetBack(string indexFieldValue)
        {
            throw new NotSupportedException();
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var path = (string)((Field)snField).GetData() ?? string.Empty;
            var separator = "/";
            string[] fragments = path.ToLowerInvariant().Split(separator.ToCharArray(), StringSplitOptions.None);
            string[] pathSteps = new string[fragments.Length];
            for (int i = 0; i < fragments.Length; i++)
                pathSteps[i] = string.Join(separator, fragments, 0, i + 1);
            return pathSteps;
        }
    }
    /// <summary>
    /// Inherited IndexFieldHandler for handling comma or semicolon separated string value of a <see cref="Field"/>.
    /// (e.g. Red,Green,Blue). Used in tagging fields
    /// </summary>
    public class TagIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            // Ensure initial textExtract for out parameter. It is used if the field value is null or empty.
            textExtract = string.Empty;
            // Get the value. A field type is indexable with this handler that provides a string value
            // but ShortText and LongText are recommended.
            var snFieldValue = (string)snField.GetData();
            // Return null if the value is not indexable. Index field and text extract won't be created.
            if (string.IsNullOrEmpty(snFieldValue))
                return null;
            // Convert to lowercase for case insensitive index handling
            snFieldValue = snFieldValue.ToLowerInvariant();
            // Create an array of words. Words can be separated by comma or semicolon. Whitespaces will be removed.
            var terms = snFieldValue.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim()).ToArray();
            // Concatenate the words with space separator for text extract.
            textExtract = string.Join(" ", terms);
            // Produce a multiterm field with a base's tool and return with it.
            return CreateField(snField.Name, terms);
        }
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            return value == null ? new IndexValue(string.Empty) : new IndexValue(((string)value).ToLowerInvariant());
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter&lt;T&gt;.GetBack(string)" />
        public string GetBack(string indexFieldValue)
        {
            return indexFieldValue;
        }
        /// <inheritdoc cref="SenseNet.ContentRepository.Search.Indexing.IIndexValueConverter.GetBack(string)" />
        object IIndexValueConverter.GetBack(string indexFieldValue)
        {
            return GetBack(indexFieldValue);
        }
        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var snFieldValue = (string)((Field)snField).GetData();
            if (string.IsNullOrEmpty(snFieldValue))
                return null;
            snFieldValue = snFieldValue.ToLowerInvariant();
            var terms = snFieldValue.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(w => w.Trim()).ToArray();
            return terms;
        }
    }

    // Not finalized feature. After finalizing make public.
    internal class SystemContentIndexHandler : BooleanIndexHandler
    {
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;

            var content = ((Field)snField).Content;

            // Do not index documents sent to the trash as system content, because when
            // restored (moved back to the original location) they will not be re-indexed
            // and would remain system content. Only the container TrashBags are system content.
            bool boolValue = content.ContentHandler is TrashBag;

            // check SystemFile
            if (!boolValue)
            {
                if (content.ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("SystemFile"))
                    boolValue = true;
            }

            // check SystemFolder
            if (!boolValue)
            {
                var parent = content.ContentHandler;

                using (new SystemAccount())
                {
                    while (parent != null)
                    {
                        if (parent.NodeType.IsInstaceOfOrDerivedFrom("SystemFolder"))
                        {
                            boolValue = true;
                            break;
                        }

                        parent = parent.Parent;
                    }
                }
            }

            return CreateField(snField.Name, boolValue);
        }
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            var content = ((Field)snField).Content;

            // check Trash
            var boolValue = TrashBin.IsInTrash(content.ContentHandler as GenericContent);

            // check SystemFile
            if (!boolValue)
            {
                if (content.ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("SystemFile"))
                    boolValue = true;
            }

            // check SystemFolder
            if (!boolValue)
            {
                var parent = content.ContentHandler;

                using (new SystemAccount())
                {
                    while (parent != null)
                    {
                        if (parent.NodeType.IsInstaceOfOrDerivedFrom("SystemFolder"))
                        {
                            boolValue = true;
                            break;
                        }

                        parent = parent.Parent;
                    }
                }
            }

            return new[] { boolValue ? IndexValue.Yes : IndexValue.No };
        }
    }
}
