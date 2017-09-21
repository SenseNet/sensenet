using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using SenseNet.ContentRepository;
using SnCR = SenseNet.ContentRepository;
using System.Globalization;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.i18n;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search.Parser;

//UNDONE: REFACTOR after Lucene "usings" removed (Lucene and SN fields are conflicted)
namespace SenseNet.Search.Indexing
{

    public abstract class FieldIndexHandler : IFieldIndexHandler
    {
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public virtual int SortingType { get { return Lucene.Net.Search.SortField.STRING; } }
        public virtual IndexFieldType IndexFieldType { get { return IndexFieldType.String; } }

        /// <summary>
        /// For SnQuery compilers
        /// </summary>
        public abstract bool Compile(IQueryCompilerValue value);
        /// <summary>
        /// For SnLucParser
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Obsolete("", false)]//UNDONE:!! After LINQ: do not use in parser
        public abstract bool TryParseAndSet(IQueryFieldValue value);
        /// <summary>
        /// For LINQ
        /// </summary>
        /// <param name="value"></param>
        public abstract void ConvertToTermValue(IQueryFieldValue value);
        public abstract IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract);

        public abstract IEnumerable<string> GetParsableValues(ISnField snField);

        private static NumericField GetNumericField(string fieldName, PerFieldIndexingInfo indexingInfo)
        {
            // Do not reusing any fields.
            var index = EnumConverter.ToLuceneIndexingMode(indexingInfo.IndexingMode);
            var store = EnumConverter.ToLuceneIndexStoringMode(indexingInfo.IndexStoringMode);
            var lucField = new Lucene.Net.Documents.NumericField(fieldName, store, index != Lucene.Net.Documents.Field.Index.NO);
            return lucField;
        }

        protected IEnumerable<IndexField> CreateField(string name, string value)
        {
            return new[]
            {
                new IndexField(name, value,
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        }
        protected IEnumerable<IndexField> CreateField(string name, IEnumerable<string> value)
        {
            return new[]
            {
                new IndexField(name, value.ToArray(),
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        }
        protected IEnumerable<IndexField> CreateField(string name, IEnumerable<string> value, string sortTerm)
        {
            return new[]
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
        }
        protected IEnumerable<IndexField> CreateField(string name, bool value)
        {
            return new[]
            {
                new IndexField(name, value,
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        }
        protected IEnumerable<IndexField> CreateField(string name, int value)
        {
            return new[]
            {
                new IndexField(name, value,
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        }
        protected IEnumerable<IndexField> CreateField(string name, double value)
        {
            return new[]
            {
                new IndexField(name, value,
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        }
        protected IEnumerable<IndexField> CreateField(string name, DateTime value)
        {
            return new[]
            {
                new IndexField(name, value,
                    OwnerIndexingInfo.IndexingMode,
                    OwnerIndexingInfo.IndexStoringMode,
                    OwnerIndexingInfo.TermVectorStoringMode)
            };
        }

        public virtual string GetDefaultAnalyzerName() { return typeof(KeywordAnalyzer).FullName; }
        public virtual string GetSortFieldName(string fieldName) { return fieldName; }
    }

    // Not IIndexValueConverters.
    public class NotIndexedIndexFieldHandler : FieldIndexHandler
    {
        public override IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            textExtract = string.Empty;
            return new IndexField[0];
        }

        public override bool Compile(IQueryCompilerValue value)
        {
            return false;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            return false;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            value.Set(value.InputObject.ToString());
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            return null;
        }
    }
    public class BinaryIndexHandler : FieldIndexHandler
    {
        public override string GetDefaultAnalyzerName() { return typeof(StandardAnalyzer).FullName; }
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            var data = snField.GetData() as SenseNet.ContentRepository.Storage.BinaryData;
            textExtract = data == null ? string.Empty : SenseNet.Search.TextExtractor.GetExtract(data, ((SnCR.Field)snField).Content.ContentHandler);
            return CreateField(snField.Name, textExtract);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(string.Empty);
            else
                value.Set(((string)value.InputObject).ToLowerInvariant());
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            return null;
        }
    }
    public class TypeTreeIndexHandler : FieldIndexHandler
    {
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            textExtract = string.Empty;
            var nodeType = ((SnCR.Field)snField).Content.ContentHandler.NodeType;
            var types = nodeType.NodeTypePath.Split('/').Select(p => p.ToLowerInvariant());
            return CreateField(snField.Name, types);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(string.Empty);
            else
                value.Set(((string)value.InputObject).ToLowerInvariant());
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            return ((SnCR.Field)snField).Content.ContentHandler.NodeType.NodeTypePath.Split('/').Select(p => p.ToLowerInvariant());
        }
    }

    // Not implemented IIndexValueConverters.
    public class HyperLinkIndexHandler : FieldIndexHandler, IIndexValueConverter<object>, IIndexValueConverter
    {
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            var data = (SenseNet.ContentRepository.Fields.HyperLinkField.HyperlinkData)snField.GetData();
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
        public override bool Compile(IQueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(string.Empty);
            else
                value.Set(((string)value.InputObject).ToLowerInvariant());
        }
        public object GetBack(string lucFieldValue)
        {
            throw new SnNotSupportedException();
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var data = (SenseNet.ContentRepository.Fields.HyperLinkField.HyperlinkData)((SnCR.Field)snField).GetData(); //UNDONE: REFACTOR: Really disgusting solution...
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
    public class ChoiceIndexHandler : FieldIndexHandler, IIndexValueConverter<object>, IIndexValueConverter
    {
        public override string GetSortFieldName(string fieldName)
        {
            return fieldName + "_sort";
        }

        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            var data = snField.GetData() ?? string.Empty;

            var stringData = data as string;
            if (stringData != null)
            {
                textExtract = stringData.ToLowerInvariant();
                return CreateField(snField.Name, textExtract);
            }

            var listData = data as IEnumerable<string>;
            if (listData != null)
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
                    var cfs = ((SnCR.Field)snField).FieldSetting as SnCR.Fields.ChoiceFieldSetting;
                    if (cfs != null)
                    {
                        // field with ChoiceFieldSetting
                        var optionKey = cfs.Options.Where(x => x.Value == inputWord).Select(x => x.StoredText).FirstOrDefault();
                        if (optionKey != null)
                        {
                            // identified option
                            var optionTerm = "$" + inputWord.ToLowerInvariant();
                            wordList.Add(optionTerm);
                            sortList.Add(optionTerm);
                            string className;
                            string name;
                            var localized = SenseNetResourceManager.ParseResourceKey(optionKey, out className, out name);
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
                            if (inputWord.StartsWith(SnCR.Fields.ChoiceField.EXTRAVALUEPREFIX))
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
                return CreateField(snField.Name, wordList, sortTerm);
            }

            var enumerableData = data as System.Collections.IEnumerable;
            if (enumerableData != null)
            {
                var words = new List<string>();
                foreach (var item in enumerableData)
                    words.Add(Convert.ToString(item, System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant());
                var wordArray = words.ToArray();
                textExtract = string.Join(" ", wordArray);
                return CreateField(snField.Name, words);
            }

            throw new NotSupportedException(string.Concat("Cannot create index from this type: ", data.GetType().FullName,
                ". Indexable data can be string, IEnumerable<string> or IEnumerable."));
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(string.Empty);
            else
                value.Set(((string)value.InputObject).ToLowerInvariant());
        }
        public object GetBack(string lucFieldValue)
        {
            throw new SnNotSupportedException();
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var data = ((SnCR.Field)snField).GetData() ?? string.Empty;

            var stringData = data as string;
            if (stringData != null)
                return new[] { stringData.ToLowerInvariant() };

            var listData = data as IEnumerable<string>;
            if (listData != null)
                return listData.Select(s => s.ToLowerInvariant()).ToArray();

            var enumerableData = data as System.Collections.IEnumerable;
            if (enumerableData != null)
                return (from object item in enumerableData select Convert.ToString(item, System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant()).ToList();

            return new[] { string.Empty };

        }
    }
    public class PermissionChoiceIndexHandler : FieldIndexHandler, IIndexValueConverter<object>, IIndexValueConverter
    {
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            textExtract = string.Empty;

            var snFieldValue = (string[])snField.GetData();
            if (snFieldValue == null || snFieldValue.Length == 0)
                return CreateField(snField.Name, string.Empty);

            var terms = snFieldValue.Select(x => x.ToLowerInvariant()).ToArray();

            return CreateField(snField.Name, terms);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(string.Empty);
            else
                value.Set(((string)value.InputObject).ToLowerInvariant());
        }
        public object GetBack(string lucFieldValue)
        {
            throw new SnNotSupportedException();
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            // copied from TagIndexHandler...
            var data = ((SnCR.Field)snField).GetData() ?? string.Empty;

            var stringData = data as string;
            if (stringData != null)
                return new[] { stringData.ToLowerInvariant() };

            var listData = data as IEnumerable<string>;
            if (listData != null)
                return listData.Select(s => s.ToLowerInvariant()).ToArray();

            var enumerableData = data as System.Collections.IEnumerable;
            if (enumerableData != null)
                return (from object item in enumerableData select Convert.ToString(item, System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant()).ToList();

            return new[] { string.Empty };

        }
    }

    // IIndexValueConverters.
    public class LowerStringIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            var field = (SnCR.Field)snField;
            string className, name;
            if (SenseNetResourceManager.Running && field.LocalizationEnabled && field.IsLocalized && SenseNetResourceManager.ParseResourceKey(field.GetStoredValue(), out className, out name))
            {
                var strings = SenseNetResourceManager.Current.GetStrings(className, name)
                    .Select(s => s.ToLowerInvariant()).ToArray();
                textExtract = string.Join(" ", strings);
                return CreateField(field.Name, strings);
            }
            var data = field.GetData();
            var stringValue = data == null ? string.Empty : data.ToString().ToLowerInvariant();
            textExtract = stringValue;

            return CreateField(field.Name, stringValue);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(string.Empty);
            else
                value.Set(((string)value.InputObject).ToLowerInvariant());
        }
        public string GetBack(string lucFieldValue)
        {
            return lucFieldValue;
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var data = ((SnCR.Field)snField).GetData();
            return new[] { data == null ? string.Empty : data.ToString().ToLowerInvariant() };
        }
    }
    public class BooleanIndexHandler : FieldIndexHandler, IIndexValueConverter<bool>, IIndexValueConverter
    {
        public static List<string> YesList => StorageContext.Search.YesList;
        public static List<string> NoList => StorageContext.Search.NoList;

        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            textExtract = string.Empty;
            var boolValue = (bool?)snField.GetData() ?? false;
            return CreateField(snField.Name, boolValue);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            var v = value.StringValue.ToLowerInvariant();
            if (YesList.Contains(v))
            {
                value.Set(SnTerm.Yes);
                return true;
            }
            if (NoList.Contains(v))
            {
                value.Set(SnTerm.No);
                return true;
            }
            bool b;
            if (Boolean.TryParse(v, out b))
            {
                value.Set(b ? SnTerm.Yes : SnTerm.No);
                return true;
            }
            return false;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            var v = value.StringValue.ToLowerInvariant();
            if (YesList.Contains(v))
            {
                value.Set(SnTerm.Yes);
                return true;
            }
            if (NoList.Contains(v))
            {
                value.Set(SnTerm.No);
                return true;
            }
            bool b;
            if (Boolean.TryParse(v, out b))
            {
                value.Set(b ? SnTerm.Yes : SnTerm.No);
                return true;
            }
            return false;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            value.Set(((bool)value.InputObject) ? SnTerm.Yes : SnTerm.No);
        }
        public bool GetBack(string lucFieldValue)
        {
            return ConvertBack(lucFieldValue);
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }

        public static bool ConvertBack(string lucFieldValue)
        {
            return lucFieldValue == SnTerm.Yes;
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var value = ((SnCR.Field)snField).GetData();
            var boolValue = value == null ? false : (bool)value;
            return new[] { boolValue ? SnTerm.Yes : SnTerm.No };
        }
    }
    public class IntegerIndexHandler : FieldIndexHandler, IIndexValueConverter<Int32>, IIndexValueConverter
    {
        public override int SortingType { get { return Lucene.Net.Search.SortField.INT; } }
        public override IndexFieldType IndexFieldType { get { return IndexFieldType.Int; } }

        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            var value = snField.GetData();
            var intValue = 0;
            try
            {
                intValue = (int?) value ?? 0;
            }
            catch (Exception) // logged rethrown
            {
                SnTrace.Index.Write("IntegerIndexHandler ERROR: content: {0} field: {1}, value: {2}", ((SnCR.Field)snField).Content.Path, snField.Name, value);
                throw;
            }
            textExtract = intValue.ToString();
            return CreateField(snField.Name, intValue);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            Int32 intValue;
            if (!Int32.TryParse(value.StringValue, out intValue))
                return false;
            value.Set(intValue);
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            Int32 intValue;
            if (!Int32.TryParse(value.StringValue, out intValue))
                return false;
            value.Set(intValue);
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            value.Set((int)value.InputObject);
        }
        public Int32 GetBack(string lucFieldValue)
        {
            return ConvertBack(lucFieldValue);
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }

        public static Int32 ConvertBack(string lucFieldValue)
        {
            Int32 intValue;
            if (Int32.TryParse(lucFieldValue, out intValue))
                return intValue;
            return 0;
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var value = ((SnCR.Field)snField).GetData();
            var intValue = value == null ? 0 : (int)value;
            return new[] { intValue.ToString() };
        }
    }
    public class NumberIndexHandler : FieldIndexHandler, IIndexValueConverter<Decimal>, IIndexValueConverter
    {
        public override int SortingType { get { return Lucene.Net.Search.SortField.DOUBLE; } }
        public override IndexFieldType IndexFieldType { get { return IndexFieldType.Double; } }

        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            var value = snField.GetData();
            var decimalValue = (Decimal?) value ?? (Decimal)0.0;
            var doubleValue = Convert.ToDouble(decimalValue);
            textExtract = decimalValue.ToString(CultureInfo.InvariantCulture);
            return CreateField(snField.Name, doubleValue);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            Double doubleValue;
            if (!Double.TryParse(value.StringValue, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out doubleValue))
                return false;
            value.Set(doubleValue);
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            Double doubleValue;
            if (!Double.TryParse(value.StringValue, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out doubleValue))
                return false;
            value.Set(doubleValue);
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            var doubleValue = Convert.ToDouble(value.InputObject);
            value.Set(doubleValue);
        }
        public Decimal GetBack(string lucFieldValue)
        {
            return Convert.ToDecimal(lucFieldValue, System.Globalization.CultureInfo.InvariantCulture);
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var value = ((SnCR.Field)snField).GetData();
            var decimalValue = value == null ? (Decimal)0.0 : (Decimal)value;
            var doubleValue = Convert.ToDouble(decimalValue);
            return new[] { decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        }
    }
    public class DateTimeIndexHandler : FieldIndexHandler, IIndexValueConverter<DateTime>, IIndexValueConverter
    {
        public override int SortingType { get { return Lucene.Net.Search.SortField.LONG; } }
        public override IndexFieldType IndexFieldType { get { return IndexFieldType.DateTime; } }

        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            textExtract = string.Empty;
            var data = snField.GetData();
            var dateTime = (DateTime?)data ?? DateTime.MinValue;
            return CreateField(snField.Name, new DateTime(SetPrecision((SnCR.Field)snField, dateTime.Ticks)));
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            DateTime dateTimeValue;
            if (!DateTime.TryParse(value.StringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeValue))
                return false;
            value.Set(dateTimeValue.Ticks);
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            DateTime dateTimeValue;
            if (!DateTime.TryParse(value.StringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeValue))
                return false;
            value.Set(dateTimeValue.Ticks);
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            value.Set(((DateTime)value.InputObject).Ticks);
        }
        public DateTime GetBack(string lucFieldValue)
        {
            return new DateTime(Int64.Parse(lucFieldValue));
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }

        private long SetPrecision(SnCR.Field snField, long ticks)
        {
            var setting = snField.FieldSetting as SenseNet.ContentRepository.Fields.DateTimeFieldSetting;
            SenseNet.ContentRepository.Fields.DateTimePrecision? precision = null;
            if (setting != null)
                precision = setting.Precision;
            if (precision == null)
                precision = SenseNet.ContentRepository.Fields.DateTimeFieldSetting.DefaultPrecision;

            switch (precision.Value)
            {
                case SenseNet.ContentRepository.Fields.DateTimePrecision.Millisecond:
                    return ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond;
                case SenseNet.ContentRepository.Fields.DateTimePrecision.Second:
                    return ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond;
                case SenseNet.ContentRepository.Fields.DateTimePrecision.Minute:
                    return ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute;
                case SenseNet.ContentRepository.Fields.DateTimePrecision.Hour:
                    return ticks / TimeSpan.TicksPerHour * TimeSpan.TicksPerHour;
                case SenseNet.ContentRepository.Fields.DateTimePrecision.Day:
                    return ticks / TimeSpan.TicksPerDay * TimeSpan.TicksPerDay;
                default:
                    throw new SnNotSupportedException("Unknown DateTimePrecision: " + precision.Value);
            }
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var data = ((SnCR.Field)snField).GetData();
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
    public class LongTextIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        public override string GetDefaultAnalyzerName() { return typeof(StandardAnalyzer).FullName; }
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            var data = snField.GetData() as string;
            textExtract = data == null ? string.Empty : data.ToLowerInvariant();
            return CreateField(snField.Name, textExtract);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(string.Empty);
            else
                value.Set(((string)value.InputObject).ToLowerInvariant());
        }
        public string GetBack(string lucFieldValue)
        {
            throw new NotSupportedException();
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var data = ((SnCR.Field)snField).GetData() as string;
            return new[] { data == null ? string.Empty : data.ToString().ToLowerInvariant() };
        }
    }
    public class ReferenceIndexHandler : FieldIndexHandler, IIndexValueConverter<int>, IIndexValueConverter
    {
        public override int SortingType { get { return Lucene.Net.Search.SortField.STRING; } }

        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            textExtract = string.Empty;
            var data = snField.GetData();
            var node = data as Node;
            if (node != null)
                return CreateField(snField.Name, node.Id.ToString());
            var nodes = data as System.Collections.IEnumerable;
            if (nodes != null)
                return CreateField(snField.Name, nodes.Cast<Node>().Select(n => n.Id.ToString()));
            return CreateField(snField.Name, SnQuery.NullReferenceValue);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            int intValue;
            if (Int32.TryParse(value.StringValue, out intValue))
                value.Set(intValue.ToString());
            else
                value.Set(SnQuery.NullReferenceValue);
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            int intValue;
            if (Int32.TryParse(value.StringValue, out intValue))
                value.Set(intValue.ToString());
            else
                value.Set(SnQuery.NullReferenceValue);
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            if (value.InputObject == null)
            {
                value.Set(SnQuery.NullReferenceValue);
                return;
            }
            var node = value.InputObject as Node;
            if (node != null)
            {
                value.Set(node.Id.ToString());
                return;
            }
            var enumerable = value as System.Collections.IEnumerable;
            if (enumerable != null)
                throw new SnNotSupportedException("ReferenceIndexHandler.ConvertToTermValue() isn't implemented when value is IEnumerable.");
            throw new NotSupportedException(string.Format("Type {0} is not supported as value of ReferenceField",value.InputObject.GetType().ToString()));
        }
        public int GetBack(string lucFieldValue)
        {
            if (lucFieldValue == SnQuery.NullReferenceValue)
                return 0;
            Int32 singleRef;
            if (Int32.TryParse(lucFieldValue, out singleRef))
                return singleRef;
            return 0;
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var data = ((SnCR.Field)snField).GetData();
            var node = data as Node;
            if (node != null)
                return new[] { node.Id.ToString() };
            var nodes = data as System.Collections.IEnumerable;
            if (nodes != null)
                return nodes.Cast<Node>().Select(n => n.Id.ToString());
            return null;
        }
    }
    public class ExclusiveTypeIndexHandler : FieldIndexHandler, IIndexValueConverter<ContentType>, IIndexValueConverter
    {
        public override bool Compile(IQueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(string.Empty);
            else
                value.Set(((string)value.InputObject).ToLowerInvariant());
        }
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            var nodeTypeName = ((SnCR.Field)snField).Content.ContentHandler.NodeType.Name.ToLowerInvariant();
            textExtract = nodeTypeName;
            return CreateField(snField.Name, nodeTypeName);
        }
        public ContentType GetBack(string lucFieldValue)
        {
            if (string.IsNullOrEmpty(lucFieldValue))
                return null;
            return ContentType.GetByName(lucFieldValue);
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            throw new SnNotSupportedException();
        }
    }
    public class InFolderIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            var value = (string)snField.GetData() ?? string.Empty;
            textExtract = value.ToLowerInvariant();
            var parentPath = RepositoryPath.GetParentPath(textExtract) ?? "/";
            return CreateField(snField.Name, parentPath);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            if (value.StringValue.StartsWith("/root"))
                return true;
            return false;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            if (value.StringValue.StartsWith("/root"))
                return true;
            return false;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            var path = ((string)value.InputObject).ToLowerInvariant();
            if (!path.StartsWith("/root"))
                throw new ApplicationException(string.Concat("Invalid path: '", path, "'. It must be absolute: '/root' or '/root/...'."));
            value.Set(path);
        }
        public string GetBack(string lucFieldValue)
        {
            return lucFieldValue;
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var value = (string)((SnCR.Field)snField).GetData() ?? string.Empty;
            var parentPath = RepositoryPath.GetParentPath(value.ToLowerInvariant()) ?? "/";
            return new[] { parentPath.ToLowerInvariant() };
        }
    }
    public class InTreeIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            textExtract = string.Empty;
            var value = (string)snField.GetData() ?? string.Empty;
            return CreateField(snField.Name, value.ToLowerInvariant());
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            var path = ((string)value.InputObject).ToLowerInvariant();
            if (!path.StartsWith("/root"))
                throw new ApplicationException(string.Concat("Invalid path: '", path, "'. It must be absolute: '/root' or '/root/...'."));
            value.Set(path);
        }
        public string GetBack(string lucFieldValue)
        {
            throw new NotSupportedException();
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var path = (string)((SnCR.Field)snField).GetData() ?? string.Empty;
            var separator = "/";
            string[] fragments = path.ToLowerInvariant().Split(separator.ToCharArray(), StringSplitOptions.None);
            string[] pathSteps = new string[fragments.Length];
            for (int i = 0; i < fragments.Length; i++)
                pathSteps[i] = string.Join(separator, fragments, 0, i + 1);
            return pathSteps;
        }
    }
    public class TagIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        // IndexHandler for comma or semicolon separated strings (e.g. Red,Green,Blue) used in tagging fields
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            // Ensure initial textExtract for out parameter. It is used if the field value is null or empty.
            textExtract = string.Empty;
            // Get the value. A field type is indexable with this handler that provides a string value
            // but ShortText and LongText are recommended.
            var snFieldValue = (string)snField.GetData();
            // Return null if the value is not indexable. Lucene field and text extract won't be created.
            if (string.IsNullOrEmpty(snFieldValue))
                return null;
            // Convert to lowercase for case insensitive index handling
            snFieldValue = snFieldValue.ToLowerInvariant();
            // Create an array of words. Words can be separated by comma or semicolon. Whitespaces will be removed.
            var terms = snFieldValue.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim()).ToArray();
            // Concatenate the words with space separator for text extract.
            textExtract = string.Join(" ", terms);
            // Produce the lucene multiterm field with a base's tool and return with it.
            return CreateField(snField.Name, terms);
        }
        public override bool Compile(IQueryCompilerValue value)
        {
            // Set the parsed value.
            value.Set(value.StringValue.ToLowerInvariant());
            // Successful.
            return true;
        }
        public override bool TryParseAndSet(IQueryFieldValue value)
        {
            // Set the parsed value.
            value.Set(value.StringValue.ToLowerInvariant());
            // Successful.
            return true;
        }
        public override void ConvertToTermValue(IQueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(string.Empty);
            else
                value.Set(((string)value.InputObject).ToLowerInvariant());
        }
        public string GetBack(string lucFieldValue)
        {
            return lucFieldValue;
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var snFieldValue = (string)((SnCR.Field)snField).GetData();
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
        public override IEnumerable<IndexField> GetIndexFields(ISnField snField, out string textExtract)
        {
            textExtract = string.Empty;

            var content = ((SnCR.Field)snField).Content;

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
        public override IEnumerable<string> GetParsableValues(ISnField snField)
        {
            var content = ((SnCR.Field)snField).Content;
            var boolValue = false;

            // check Trash
            if (TrashBin.IsInTrash(content.ContentHandler as GenericContent))
                boolValue = true;

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

            return new[] { boolValue ? SnTerm.Yes : SnTerm.No };
        }
    }
}
