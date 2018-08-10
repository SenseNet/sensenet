using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Services.OData.Tests.Results
{
    public class ODataEntity : IODataResult
    {
        private Dictionary<string, object> _data;
        public ODataEntity(Dictionary<string, object> data)
        {
            _data = data;
        }

        public int Id
        {
            get
            {
                if (!_data.ContainsKey("Id"))
                    return 0;
                return ((JValue)_data["Id"]).Value<int>();
            }
        }
        public string Name
        {
            get
            {
                if (!_data.ContainsKey("Name"))
                    return null;
                return ((JValue)_data["Name"]).Value<string>();
            }
        }
        public string Path
        {
            get
            {
                if (!_data.ContainsKey("Path"))
                    return null;
                return ((JValue)_data["Path"]).Value<string>();
            }
        }
        public ContentType ContentType
        {
            get
            {
                string typeName = null;
                // ((JValue)((JObject)entity.AllProperties["__metadata"])["type"]).Value
                if (_data.TryGetValue("__metadata", out var meta))
                    typeName = (string) ((JValue) ((JObject) meta)["type"]).Value;
                else if (_data.TryGetValue("Type", out var typeValue))
                    typeName = (string)typeValue;

                if (string.IsNullOrEmpty(typeName))
                    return null;

                return ContentType.GetByName(typeName);
            }
        }

        private ODataEntity _createdBy;
        public ODataEntity CreatedBy
        {
            get
            {
                if (_createdBy == null)
                    _createdBy = GetEntity("CreatedBy");
                return _createdBy;
            }
        }

        private ODataEntity _manager;
        public ODataEntity Manager
        {
            get
            {
                if (_manager == null)
                    _manager = GetEntity("Manager");
                return _manager;
            }
        }

        private ODataEntity[] _children;

        public ODataEntity[] Children
        {
            get
            {
                if (_children == null)
                {
                    if (_data.TryGetValue("Children", out var childrenValue))
                    {
                        if (childrenValue is JArray childArray)
                        {
                            _children = childArray.Where(co => co is JObject).Select(c => Create((JObject) c)).ToArray();
                        }
                    }
                }

                return _children;
            }
        }

        public int Index
        {
            get
            {
                if (!_data.ContainsKey("Index"))
                    return 0;
                return ((JValue)_data["Index"]).Value<int>();
            }
        }

        public Dictionary<string, object> AllProperties { get { return _data; } }
        public bool IsDeferred { get { return AllProperties.Count == 1 && AllProperties.Keys.First() == "__deferred"; } }
        public bool IsExpanded { get { return AllProperties.Count > 1; } }
        public bool AllPropertiesSelected { get { return AllProperties.Count > 20; } }

        private ODataEntity GetEntity(string name)
        {
            if (!_data.ContainsKey(name))
                return null;
            var obj = _data[name];
            var jobj = obj as JObject;
            if (jobj != null)
                return jobj == null ? (ODataEntity)null : ODataEntity.Create(jobj);

            var jvalue = obj as JValue;
            if (jvalue.Type == JTokenType.Null)
                return null;

            throw new SnNotSupportedException();
        }

        public static ODataEntity Create(JObject obj)
        {
            var props = new Dictionary<string, object>();
            obj.Properties().Select(y => { props.Add(y.Name, y.Value.Value<object>()); return true; }).ToArray();
            return new ODataEntity(props);
        }

    }
}
