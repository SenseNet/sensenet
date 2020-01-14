using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace SenseNet.OData
{
    internal class ODataParameterCollection : IReadOnlyDictionary<string, ODataParameterValue>
    {
        private readonly Dictionary<string, ODataParameterValue> _allParameters;

        public ODataParameterCollection(JObject body, IQueryCollection query)
        {
            var names = query?.Keys.Except(ODataRequest.WellKnownQueryStringParameterNames) ?? Array.Empty<string>();
            if (body != null)
                names = names
                    .Union(body.Properties().Select(p => p.Name))
                    .Distinct();

            var allParams = new Dictionary<string, ODataParameterValue>();
            foreach (var name in names)
            {
                if (query?.ContainsKey(name) ?? false)
                    allParams.Add(name, new ODataParameterValue(query[name]));
                else
                    // ReSharper disable once PossibleNullReferenceException
                    allParams.Add(name, new ODataParameterValue(body[name]));
            }
            _allParameters = allParams;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<string, ODataParameterValue>> GetEnumerator() => _allParameters.GetEnumerator();

        public int Count => _allParameters.Count;
        public bool ContainsKey(string key) => _allParameters.ContainsKey(key);
        public bool TryGetValue(string key, out ODataParameterValue value) => _allParameters.TryGetValue(key, out value);

        public ODataParameterValue this[string key] => _allParameters[key];

        public IEnumerable<string> Keys => _allParameters.Keys;
        public IEnumerable<ODataParameterValue> Values => _allParameters.Values;
    }
}
