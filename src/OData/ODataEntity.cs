using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SenseNet.OData
{
    public class ODataEntity : IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        [JsonIgnore]
        public int Id => _properties.TryGetValue(nameof(Id), out var value) ? (int)value : 0;
        [JsonIgnore]
        public string Name => _properties.TryGetValue(nameof(Name), out var value) ? (string)value : null;
        [JsonIgnore]
        public string Path => _properties.TryGetValue(nameof(Path), out var value) ? (string)value : null;
        [JsonIgnore]
        public string ContentType => _properties.TryGetValue("Type", out var value) ? (string) value : null;
        [JsonIgnore]
        internal ODataEntity[] Children => _properties.TryGetValue("Children", out var value) ? ((IEnumerable<ODataEntity>)value).ToArray() : null;

        #region IDictionary<string, object> implementation
        public int Count => _properties.Count;
        public bool IsReadOnly => false;
        public object this[string key]
        {
            get => _properties[key];
            set => _properties[key] = value;
        }
        public ICollection<string> Keys => _properties.Keys;
        public ICollection<object> Values => _properties.Values;

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public void Add(KeyValuePair<string, object> item)
        {
            _properties.Add(item.Key, item.Value);
        }
        public void Clear()
        {
            _properties.Clear();
        }
        public bool Contains(KeyValuePair<string, object> item)
        {
            return ((IDictionary<string, object>) _properties).Contains(item);
        }
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((IDictionary<string, object>)_properties).CopyTo(array, arrayIndex);
        }
        public bool Remove(KeyValuePair<string, object> item)
        {
            return ((IDictionary<string, object>)_properties).Remove(item);
        }
        public void Add(string key, object value)
        {
            _properties.Add(key, value);
        }
        public bool ContainsKey(string key)
        {
            return _properties.ContainsKey(key);
        }
        public bool Remove(string key)
        {
            return _properties.Remove(key);
        }
        public bool TryGetValue(string key, out object value)
        {
            return _properties.TryGetValue(key, out value);
        }

        #endregion
    }
}
