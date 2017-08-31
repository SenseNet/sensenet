using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Parser;

namespace SenseNet.Search
{
    public enum IndexingMode { Default, Analyzed, AnalyzedNoNorms, No, NotAnalyzed, NotAnalyzedNoNorms }
    public enum IndexStoringMode { Default, No, Yes }
    public enum IndexTermVector { Default, No, WithOffsets, WithPositions, WithPositionsOffsets, Yes }

    public enum IndexableDataType { String, Int, Long, Float, Double }
    public enum IndexFieldType { String, Int, Long, Float, Double, DateTime }
    public enum SnTermType { String, StringArray, Bool, Int, Long, Float, Double, DateTime }

    public interface ISnField
    {
        string Name { get; }
        object GetData(bool localized = true);
    }

    public interface IFieldIndexHandler
    {
        /// <summary>For SnQuery compilers</summary>
        bool Compile(IQueryCompilerValue value);

        /// <summary>For SnLucParser</summary>
        [Obsolete("", false)]//UNDONE:!! do not use in parser
        bool TryParseAndSet(IQueryFieldValue value);
        /// <summary>For LINQ</summary>
        void ConvertToTermValue(IQueryFieldValue value);

        string GetDefaultAnalyzerName();
        IEnumerable<string> GetParsableValues(ISnField field);
        int SortingType { get; }
        IndexFieldType IndexFieldType { get; }
        IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        string GetSortFieldName(string fieldName);

        IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract);
    }
    public interface IPerFieldIndexingInfo //UNDONE: Racionalize interface names: IPerFieldIndexingInfo
    {
        string Analyzer { get; set; }
        IFieldIndexHandler IndexFieldHandler { get; set; }

        IndexingMode IndexingMode { get; set; }
        IndexStoringMode IndexStoringMode { get; set; }
        IndexTermVector TermVectorStoringMode { get; set; }

        bool IsInIndex { get; }

        Type FieldDataType { get; set; }
    }

    public enum QueryFieldLevel { NotDefined = 0, HeadOnly = 1, NoBinaryOrFullText = 2, BinaryOrFullText = 3 }

    public interface IQueryFieldValue //UNDONE:!! do not use in parser / compiler
    {
        //internal bool IsPhrase { get; }
        //internal SnLucLexer.Token Token { get; }
        //internal double? FuzzyValue { get; set; }
        string StringValue { get; }
        object InputObject { get; }

        IndexableDataType Datatype { get; }
        Int32 IntValue { get; }
        Int64 LongValue { get; }
        Single SingleValue { get; }
        Double DoubleValue { get; }

        void Set(Int32 value);
        void Set(Int64 value);
        void Set(Single value);
        void Set(Double value);
        void Set(String value);
    }


    public class QueryFieldValue : IQueryFieldValue  //UNDONE:!! do not use in parser / compiler
    {
        internal bool IsPhrase { get; private set; }
        internal CqlLexer.Token Token { get; private set; }
        internal double? FuzzyValue { get; set; }
        public string StringValue { get; private set; }
        public object InputObject { get; private set; }

        public IndexableDataType Datatype { get; private set; }
        public Int32 IntValue { get; private set; }
        public Int64 LongValue { get; private set; }
        public Single SingleValue { get; private set; }
        public Double DoubleValue { get; private set; }

        public QueryFieldValue(object value)
        {
            InputObject = value;
        }

        internal QueryFieldValue(string stringValue, CqlLexer.Token token, bool isPhrase)
        {
            Datatype = IndexableDataType.String;
            StringValue = stringValue;
            Token = token;
            IsPhrase = isPhrase;
        }

        public void Set(Int32 value)
        {
            Datatype = IndexableDataType.Int;
            IntValue = value;
        }
        public void Set(Int64 value)
        {
            Datatype = IndexableDataType.Long;
            LongValue = value;
        }
        public void Set(Single value)
        {
            Datatype = IndexableDataType.Float;
            SingleValue = value;
        }
        public void Set(Double value)
        {
            Datatype = IndexableDataType.Double;
            DoubleValue = value;
        }
        public void Set(String value)
        {
            Datatype = IndexableDataType.String;
            StringValue = value;
        }

        public override string ToString()
        {
            return String.Concat(Token, ":", StringValue, FuzzyValue == null ? "" : ":" + FuzzyValue);
        }
    }
    public class QueryCompilerValue : IQueryCompilerValue
    {
        public string StringValue { get; private set; }

        public IndexableDataType Datatype { get; private set; }
        public int IntValue { get; private set; }
        public long LongValue { get; private set; }
        public float SingleValue { get; private set; }
        public double DoubleValue { get; private set; }

        public QueryCompilerValue(string text)
        {
            Datatype = IndexableDataType.String;
            StringValue = text;
        }

        public void Set(int value)
        {
            Datatype = IndexableDataType.Int;
            IntValue = value;
        }
        public void Set(long value)
        {
            Datatype = IndexableDataType.Long;
            LongValue = value;
        }
        public void Set(float value)
        {
            Datatype = IndexableDataType.Float;
            SingleValue = value;
        }
        public void Set(double value)
        {
            Datatype = IndexableDataType.Double;
            DoubleValue = value;
        }
        public void Set(string value)
        {
            Datatype = IndexableDataType.String;
            StringValue = value;
        }

        public override string ToString()
        {
            return $"{StringValue} ({Datatype})";
        }
    }

    public interface IQueryCompilerValue
    {
        string StringValue { get; }

        IndexableDataType Datatype { get; }
        int IntValue { get; }
        long LongValue { get; }
        float SingleValue { get; }
        double DoubleValue { get; }

        void Set(int value);
        void Set(long value);
        void Set(float value);
        void Set(double value);
        void Set(string value);
    }


    public interface IQueryEngine
    {
        IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter);
        IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter);
    }
    public interface IQueryEngineSelector
    {
        IQueryEngine Select(SnQuery query, QuerySettings settings);
    }

    public interface IPermissionFilter
    {
        bool IsPermitted(int nodeId, bool isLastPublic, bool isLastDraft);
    }
    public interface IPermissionFilterFactory
    {
        IPermissionFilter Create(int userId);
    }

    public interface IQueryResult<out T>
    {
        IEnumerable<T> Hits { get; }
        int TotalCount { get; }
    }


    public class DefaultPermissionFilter : IPermissionFilter //UNDONE: Delete DefaultPermissionFilter if the final version is done.
    {
        private readonly int _userId;

        public DefaultPermissionFilter(int userId)
        {
            _userId = userId;
        }
        public bool IsPermitted(int nodeId, bool isLastPublic, bool isLastDraft)
        {
            return true; //UNDONE: fake implementation
        }
    }

    public class DefaultPermissionFilterFactory : IPermissionFilterFactory //UNDONE: Delete DefaultPermissionFilterFactory if the final version is done.
    {
        public IPermissionFilter Create(int userId)
        {
            return new DefaultPermissionFilter(userId);
        }
    }

    public class DefaultQueryEngineSelector : IQueryEngineSelector
    {
        public IQueryEngine Select(SnQuery query, QuerySettings settings)
        {
            return new DefaultQueryEngine();
        }
    }

    public class DefaultQueryEngine : IQueryEngine //UNDONE: Delete DefaultQueryEngine if the final version is done.
    {
        public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter)
        {
            throw new NotImplementedException();
        }
        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter)
        {
            var projection = query.Projection;
            throw new NotImplementedException();
        }
    }

}
