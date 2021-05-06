using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.OData;
// ReSharper disable IdentifierTypo

namespace SenseNet.ODataTests.Responses
{
    public class ODataOperationResponse
    {
        public string Title { get; set; }
        public string Name { get; set; }
        public string OpId { get; set; }
        public string Target { get; set; }
        public bool Forbidden { get; set; }
        public OperationParameter[] Parameters { get; set; }
    }

    public class OperationParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
    }

    public class ODataEntityResponse : IODataResponse
    {
        // ((JObject)data["__metadata"])["actions"]
        // ((JObject)data["__metadata"])["functions"]
        // ((JArray)((JObject)data["__metadata"])["functions"]).Count


        private readonly Dictionary<string, object> _data;
        public ODataEntityResponse(Dictionary<string, object> data)
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
                    //typeName = (string)typeValue;
                    typeName = typeValue.ToString();

                if (string.IsNullOrEmpty(typeName))
                    return null;

                return ContentType.GetByName(typeName);
            }
        }

        private ODataEntityResponse _createdBy;
        public ODataEntityResponse CreatedBy
        {
            get
            {
                if (_createdBy == null)
                    _createdBy = GetEntity("CreatedBy");
                return _createdBy;
            }
        }

        private ODataEntityResponse _modifiedBy;
        public ODataEntityResponse ModifiedBy
        {
            get
            {
                if (_modifiedBy == null)
                    _modifiedBy = GetEntity("ModifiedBy");
                return _modifiedBy;
            }
        }

        private ODataEntityResponse _owner;
        public ODataEntityResponse Owner
        {
            get
            {
                if (_owner == null)
                    _owner = GetEntity("Owner");
                return _owner;
            }
        }

        private ODataEntityResponse _manager;
        public ODataEntityResponse Manager
        {
            get
            {
                if (_manager == null)
                    _manager = GetEntity("Manager");
                return _manager;
            }
        }

        private ODataEntityResponse[] _children;

        public ODataEntityResponse[] Children
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

        private ODataOperationResponse[] _metadataActions;
        public ODataOperationResponse[] MetadataActions => _metadataActions ?? (_metadataActions = GetOperations(_data, true));

        private ODataOperationResponse[] _metadataFunctions;
        public ODataOperationResponse[] MetadataFunctions => _metadataFunctions ?? (_metadataFunctions = GetOperations(_data, false));

        private ODataActionItem[] _actions;
        public ODataActionItem[] Actions => _actions ?? (_actions = GetActionField(_data));

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

        private ODataEntityResponse GetEntity(string name)
        {
            if (!_data.ContainsKey(name))
                return null;
            var obj = _data[name];

            if (obj is JObject jobj)
                return Create(jobj);
            
            if (obj is JValue jvalue && jvalue.Type == JTokenType.Null)
                return null;

            throw new SnNotSupportedException();
        }

        public static ODataEntityResponse Create(JObject obj)
        {
            var props = new Dictionary<string, object>();
            var _ = obj.Properties().Select(y => { props.Add(y.Name, y.Value.Value<object>()); return true; }).ToArray();
            return new ODataEntityResponse(props);
        }

        private ODataOperationResponse[] GetOperations(Dictionary<string, object> data, bool actions)
        {
            if (!data.TryGetValue("__metadata", out var metadata))
                return Array.Empty<ODataOperationResponse>();

            if (!((JObject)metadata).TryGetValue(actions ? "actions" : "functions", out var operations))
                return Array.Empty<ODataOperationResponse>();

            var result = new List<ODataOperationResponse>();
            foreach (var operation in operations)
            {
                var item = new ODataOperationResponse
                {
                    Title = operation["title"].Value<string>(),
                    Name = operation["name"].Value<string>(),
                    OpId = operation["opId"].Value<string>(),
                    Target = operation["target"].Value<string>(),
                    Forbidden = operation["forbidden"].Value<bool>(),
                    Parameters = operation["parameters"].Select(p => new OperationParameter
                    {
                        Name = p["name"].Value<string>(),
                        Type = p["type"].Value<string>(),
                        Required = p["required"].Value<bool>(),
                    }).ToArray()
                };
                result.Add(item);
            }

            return result.ToArray();
        }
        private ODataActionItem[] GetActionField(Dictionary<string, object> data)
        {
            if (!data.TryGetValue("Actions", out var actionData))
                return Array.Empty<ODataActionItem>();

            if(!(actionData is JArray operations))
                return Array.Empty<ODataActionItem>();

            var result = new List<ODataActionItem>();
            foreach (var operation in operations)
            {
                var item = new ODataActionItem
                {
                    Name = operation["Name"].Value<string>(),
                    OpId = operation["OpId"].Value<string>(),
                    DisplayName = operation["DisplayName"].Value<string>(),
                    Icon = operation["Icon"].Value<string>(),
                    Index = operation["Index"].Value<int>(),
                    Scenario = operation["Scenario"].Value<string>(),
                    Forbidden = operation["Forbidden"].Value<bool>(),
                    Url = operation["Url"].Value<string>(),
                    IsODataAction = operation["IsODataAction"].Value<bool>(),
                    ActionParameters = operation["ActionParameters"].Select(p => p.ToString()).ToArray()
                };
                result.Add(item);
            }

            return result.ToArray();
        }

    }
}
