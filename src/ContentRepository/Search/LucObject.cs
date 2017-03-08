using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search.Indexing;

namespace SenseNet.Search
{
    public class LucObject
    {
        public static class FieldName
        {
            public static readonly string NodeId                = "Id";
            public static readonly string VersionId             = "VersionId";
            public static readonly string Name                  = "Name";
            public static readonly string DisplayName           = "DisplayName";
            public static readonly string Path                  = "Path";
            public static readonly string Depth                 = "Depth";
            public static readonly string InTree                = "InTree";
            public static readonly string InFolder              = "InFolder";
            public static readonly string ParentId              = "ParentId";
            public static readonly string IsMajor               = "IsMajor";
            public static readonly string IsPublic              = "IsPublic";
            public static readonly string IsLastPublic          = "IsLastPublic";
            public static readonly string IsLastDraft           = "IsLastDraft";
            public static readonly string Type                  = "Type";
            public static readonly string TypeIs                = "TypeIs";
            public static readonly string NodeTypeId            = "NodeTypeId";

            public static readonly string ContentListId         = "ContentListId";
            public static readonly string ContentListTypeId     = "ContentListTypeId";
            public static readonly string Version               = "Version";
            public static readonly string VersionStatus         = "VersionStatus";

            public static readonly string IsDeleted             = "IsDeleted";
            public static readonly string IsInherited           = "IsInherited";
            public static readonly string Index                 = "Index";
            public static readonly string Locked                = "Locked";
            public static readonly string LockedById            = "LockedById";
            public static readonly string ETag                  = "ETag";
            public static readonly string LockType              = "LockType";
            public static readonly string LockTimeout           = "LockTimeout";
            public static readonly string LockDate              = "LockDate";
            public static readonly string LockToken             = "LockToken";
            public static readonly string LastLockUpdate        = "LastLockUpdate";
            public static readonly string MajorNumber           = "MajorNumber";
            public static readonly string MinorNumber           = "MinorNumber";
            public static readonly string CreationDate          = "CreationDate";
            public static readonly string CreatedById           = "CreatedById";
            public static readonly string ModificationDate      = "ModificationDate";
            public static readonly string ModifiedById          = "ModifiedById";
            public static readonly string IsSystem              = "IsSystemContent";
            public static readonly string OwnerId               = "OwnerId";
            public static readonly string SavingState           = "SavingState";

            public static readonly string NodeTimestamp         = "NodeTimestamp";
            public static readonly string VersionTimestamp      = "VersionTimestamp";

            public static readonly string IsFaulted             = "IsFaulted";
            public static readonly string FaultedFieldName      = "FaultedFieldName";

            public static readonly string AllText               = "_Text";
        }

        private Dictionary<string, string> data = new Dictionary<string, string>();
        
        public virtual int NodeId { get { return ValueToInt(LucObject.FieldName.NodeId); } }
        public virtual int VersionId { get { return ValueToInt(LucObject.FieldName.VersionId); } }
        public virtual int OwnerId { get { return ValueToInt(LucObject.FieldName.OwnerId); } }
        public virtual int CreatedById { get { return ValueToInt(LucObject.FieldName.CreatedById); } }
        public virtual int ModifiedById { get { return ValueToInt(LucObject.FieldName.ModifiedById); } }
        public virtual string Name { get { return ValueToString(LucObject.FieldName.Name); } }
        public virtual string Path { get { return ValueToString(LucObject.FieldName.Path); } }
        public virtual bool IsInherited { get { return ValueToBool(LucObject.FieldName.IsInherited); } }
        public virtual bool IsMajor { get { return ValueToBool(LucObject.FieldName.IsMajor); } }
        public virtual bool IsPublic { get { return ValueToBool(LucObject.FieldName.IsPublic); } }
        public virtual bool IsLastPublic { get { return ValueToBool(LucObject.FieldName.IsLastPublic); } }
        public virtual bool IsLastDraft { get { return ValueToBool(LucObject.FieldName.IsLastDraft); } }
        public virtual long NodeTimestamp { get { return ValueToLong(LucObject.FieldName.NodeTimestamp); } }
        public virtual long VersionTimestamp { get { return ValueToLong(LucObject.FieldName.VersionTimestamp); } }

        public LucObject() { }
        public LucObject(Document doc)
        {
            foreach (Field field in doc.GetFields())
                this[field.Name()] = doc.Get(field.Name());
        }
        public T Get<T>(string fieldName)
        {
            var info = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(fieldName);
            var converter = info.IndexFieldHandler as IIndexValueConverter<T>;
            if (converter == null)
                return default(T);
            var value = converter.GetBack(data[fieldName]);
            return value;
        }
        public object Get(string fieldName)
        {
            var info = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(fieldName);
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
            return data[name] == BooleanIndexHandler.YES;
        }
    }
}
