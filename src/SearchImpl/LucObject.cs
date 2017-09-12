using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.Search
{
    public class LucObject
    {
        private Dictionary<string, string> data = new Dictionary<string, string>();
        
        public virtual int NodeId { get { return ValueToInt(IndexFieldName.NodeId); } }
        public virtual int VersionId { get { return ValueToInt(IndexFieldName.VersionId); } }
        public virtual int OwnerId { get { return ValueToInt(IndexFieldName.OwnerId); } }
        public virtual int CreatedById { get { return ValueToInt(IndexFieldName.CreatedById); } }
        public virtual int ModifiedById { get { return ValueToInt(IndexFieldName.ModifiedById); } }
        public virtual string Name { get { return ValueToString(IndexFieldName.Name); } }
        public virtual string Path { get { return ValueToString(IndexFieldName.Path); } }
        public virtual bool IsInherited { get { return ValueToBool(IndexFieldName.IsInherited); } }
        public virtual bool IsMajor { get { return ValueToBool(IndexFieldName.IsMajor); } }
        public virtual bool IsPublic { get { return ValueToBool(IndexFieldName.IsPublic); } }
        public virtual bool IsLastPublic { get { return ValueToBool(IndexFieldName.IsLastPublic); } }
        public virtual bool IsLastDraft { get { return ValueToBool(IndexFieldName.IsLastDraft); } }
        public virtual long NodeTimestamp { get { return ValueToLong(IndexFieldName.NodeTimestamp); } }
        public virtual long VersionTimestamp { get { return ValueToLong(IndexFieldName.VersionTimestamp); } }

        public LucObject() { }
        public LucObject(Document doc)
        {
            foreach (Field field in doc.GetFields())
                this[field.Name()] = doc.Get(field.Name());
        }
        public T Get<T>(string fieldName)
        {
            var info = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(fieldName);
            var converter = info.IndexFieldHandler as IIndexValueConverter<T>;
            if (converter == null)
                return default(T);
            var value = converter.GetBack(data[fieldName]);
            return value;
        }
        public object Get(string fieldName)
        {
            var info = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(fieldName);
            var converter = info.IndexFieldHandler as IIndexValueConverter;
            if (converter == null)
                return null;
            var value = converter.GetBack(data[fieldName]);
            return value;
        }

        public virtual IEnumerable<string> Names { get { return data.Keys.ToList().AsReadOnly(); } }
        public virtual string this[string name]
        {
            get { return data[name]; }
            set
            {
                if (data.ContainsKey(name))
                    data[name] = value;
                else
                    data.Add(name, value);
            }
        }
        public virtual string this[string name, bool throwOnError]
        {
            get
            {
                if(throwOnError)
                    return this[name];
                string value;
                if (data.TryGetValue(name, out value))
                    return value;
                return String.Empty;
            }
        }

        private string ValueToString(string name)
        {
            return data[name];
        }
        private int ValueToInt(string name)
        {
            return Convert.ToInt32(data[name]);
        }
        private long ValueToLong(string name)
        {
            return Convert.ToInt64(data[name]);
        }
        private bool ValueToBool(string name)
        {
            return data[name] == SnTerm.Yes;
        }
    }
}
