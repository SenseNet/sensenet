using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using System;
using System.Linq;

namespace SenseNet.Search.Parser
{
    //UNDONE: SQL: SqlQueryFieldInfo in the SnQuery to SQL query compilation.
    internal abstract class SqlQueryFieldInfo
    {
        public string SqlName;
        private string _parameterName;
        public string ParameterName
        {
            get
            {
                if (_parameterName == null)
                    return SqlName;
                return _parameterName;
            }
            set { _parameterName = value; }
        }
        public bool NeedApos;
        public virtual string SqlOperator { get; set; }
        public abstract object GetParameterValue(string termValue);
        public abstract string GetSqlTextValue(string termValue);
    }

    internal class QueryFieldInfo_String : SqlQueryFieldInfo
    {
        public override object GetParameterValue(string termValue)
        {
            return termValue;
        }
        public override string GetSqlTextValue(string termValue)
        {
            return termValue;
        }
    }
    internal class QueryFieldInfo_Int : SqlQueryFieldInfo
    {
        public override object GetParameterValue(string termValue)
        {
            return termValue;
        }
        public override string GetSqlTextValue(string termValue)
        {
            return GetParameterValue(termValue).ToString();
        }
    }
    internal class QueryFieldInfo_NullableInt : SqlQueryFieldInfo
    {
        public override object GetParameterValue(string termValue)
        {
            if (termValue == "0")
                return null;
            return termValue;
        }
        public override string GetSqlTextValue(string termValue)
        {
            var value = GetParameterValue(termValue);
            return value == null ? " IS NULL" : value.ToString();
        }
    }
    internal class QueryFieldInfo_Locked : SqlQueryFieldInfo
    {
        public override object GetParameterValue(string termValue)
        {
            return null;
        }
        public override string GetSqlTextValue(string termValue)
        {
            return StorageContext.Search.YesList.Contains(termValue.ToLowerInvariant()) ? " IS NOT NULL" : " IS NULL";
        }
    }

    internal class QueryFieldInfo_Date : SqlQueryFieldInfo
    {
        public override object GetParameterValue(string termValue)
        {
            var ticks = long.Parse(termValue);
            var dateTime = new DateTime(ticks);
            return dateTime;
        }
        public override string GetSqlTextValue(string termValue)
        {
            var dateTime = (DateTime)GetParameterValue(termValue);
            var isoDate = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            return isoDate;
        }
    }
    internal class QueryFieldInfo_NodeType : SqlQueryFieldInfo
    {
        internal static NodeType GetNodeType(string termValue)
        {
            var nodeType = SenseNet.ContentRepository.Storage.ActiveSchema.NodeTypes.Where(n => n.Name.ToLower() == termValue).FirstOrDefault();
            if (nodeType == null)
                throw new ApplicationException("Type is not found: " + termValue);
            return nodeType;
        }
        public override object GetParameterValue(string termValue)
        {
            return GetNodeType(termValue).Id;
        }
        public override string GetSqlTextValue(string termValue)
        {
            return GetParameterValue(termValue).ToString();
        }
    }
    internal class QueryFieldInfo_NodeTypeRecursive : SqlQueryFieldInfo
    {
        public override object GetParameterValue(string termValue)
        {
            return GetSqlTextValue(termValue);
        }
        public override string GetSqlTextValue(string termValue)
        {
            var nodeType = QueryFieldInfo_NodeType.GetNodeType(termValue);
            return String.Join(", ", nodeType.GetAllTypes().OrderBy(t => t.Id).Select(t => t.Id.ToString()).ToArray());
        }
    }
    internal class QueryFieldInfo_Bool : SqlQueryFieldInfo
    {
        public override object GetParameterValue(string termValue)
        {
            return StorageContext.Search.YesList.Contains(termValue.ToLowerInvariant()) ? "1" : "0";
        }
        public override string GetSqlTextValue(string termValue)
        {
            return GetParameterValue(termValue).ToString();
        }
    }
    internal class QueryFieldInfo_PathToId : SqlQueryFieldInfo
    {
        public override object GetParameterValue(string termValue)
        {
            var head = NodeHead.Get(termValue);
            return head == null ? 0 : head.Id;
        }
        public override string GetSqlTextValue(string termValue)
        {
            return GetParameterValue(termValue).ToString();
        }
    }
}
