using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;

namespace SenseNet.Search.Lucene29
{
    public class LucObject
    {
        private readonly Dictionary<string, string> _data = new Dictionary<string, string>();
        
        public virtual int NodeId => ValueToInt(IndexFieldName.NodeId);
        public virtual int VersionId => ValueToInt(IndexFieldName.VersionId);
        public virtual int OwnerId => ValueToInt(IndexFieldName.OwnerId);
        public virtual int CreatedById => ValueToInt(IndexFieldName.CreatedById);
        public virtual int ModifiedById => ValueToInt(IndexFieldName.ModifiedById);
        public virtual string Name => ValueToString(IndexFieldName.Name);
        public virtual string Path => ValueToString(IndexFieldName.Path);
        public virtual bool IsInherited => ValueToBool(IndexFieldName.IsInherited);
        public virtual bool IsMajor => ValueToBool(IndexFieldName.IsMajor);
        public virtual bool IsPublic => ValueToBool(IndexFieldName.IsPublic);
        public virtual bool IsLastPublic => ValueToBool(IndexFieldName.IsLastPublic);
        public virtual bool IsLastDraft => ValueToBool(IndexFieldName.IsLastDraft);
        public virtual long NodeTimestamp => ValueToLong(IndexFieldName.NodeTimestamp);
        public virtual long VersionTimestamp => ValueToLong(IndexFieldName.VersionTimestamp);

        public LucObject() { }
        public LucObject(Document doc)
        {
            foreach (Field field in doc.GetFields())
                this[field.Name()] = doc.Get(field.Name());
        }
        
        public virtual IEnumerable<string> Names => _data.Keys.ToList().AsReadOnly();

        public virtual string this[string name]
        {
            get { return _data[name]; }
            set
            {
                if (_data.ContainsKey(name))
                    _data[name] = value;
                else
                    _data.Add(name, value);
            }
        }
        public virtual string this[string name, bool throwOnError]
        {
            get
            {
                if(throwOnError)
                    return this[name];

                return _data.TryGetValue(name, out var value) ? value : string.Empty;
            }
        }

        private string ValueToString(string name)
        {
            return _data[name];
        }
        private int ValueToInt(string name)
        {
            return Convert.ToInt32(_data[name]);
        }
        private long ValueToLong(string name)
        {
            return Convert.ToInt64(_data[name]);
        }
        private bool ValueToBool(string name)
        {
            return _data[name] == IndexValue.Yes;
        }
    }
}
