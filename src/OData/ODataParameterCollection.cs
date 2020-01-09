using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Services.Core.Operations;

namespace SenseNet.OData
{
    internal class ODataParameterValue
    {
        private readonly JToken _jToken;
        private readonly StringValues _stringValues;
        private JToken _unifiedToken;

        public ODataParameterValue(JToken jToken)
        {
            _jToken = jToken;
        }
        public ODataParameterValue(StringValues stringValues)
        {
            _stringValues = stringValues;
        }

        public JTokenType Type => _jToken?.Type ?? (_stringValues.Count == 1 ? JTokenType.String : JTokenType.Array);

        public T Value<T>()
        {
            return GetUnifiedToken().Value<T>();
        }

        public object ToObject(Type expectedType, JsonSerializer valueDeserializer)
        {
            var token = GetUnifiedToken();

            if (expectedType == typeof(ContentOperations.SetPermissionsRequest))
                return _jToken?.Parent.Parent.ToObject<ContentOperations.SetPermissionsRequest>();

            return token.ToObject(expectedType, valueDeserializer);
        }

        private JToken GetUnifiedToken()
        {
            return _unifiedToken ?? (_unifiedToken = _jToken ?? CreateJToken(_stringValues));
        }
        private JToken CreateJToken(StringValues values)
        {
            switch (values.Count)
            {
                case 0:
                    return JToken.Parse("''");
                case 1:
                    var value = values.First().Replace("'", @"\'");
                    return JToken.Parse($"'{value}'");
                default:
                    return JToken.Parse($"['{string.Join("','", values.ToArray())}']");
            }
        }

        internal T ToObject<T>()
        {
            // The only exceptional case.
            return _jToken == null ? default : _jToken.Parent.Parent.ToObject<T>();
        }

        public object ToArray(Type expectedType, out Type realType)
        {
            var array = (JArray)GetUnifiedToken();
            realType = expectedType;
            try
            {
                if (expectedType == typeof(string[])) return array.Select(x => x.ToObject<string>()).ToArray();
                if (expectedType == typeof(int[])) return array.Select(x => x.ToObject<int>()).ToArray();
                if (expectedType == typeof(long[])) return array.Select(x => x.ToObject<long>()).ToArray();
                if (expectedType == typeof(bool[])) return array.Select(x => x.ToObject<bool>()).ToArray();
                if (expectedType == typeof(float[])) return array.Select(x => x.ToObject<float>()).ToArray();
                if (expectedType == typeof(double[])) return array.Select(x => x.ToObject<double>()).ToArray();
                if (expectedType == typeof(decimal[])) return array.Select(x => x.ToObject<decimal>()).ToArray();

                if (expectedType == typeof(List<string>)) return array.Select(x => x.ToObject<string>()).ToList();
                if (expectedType == typeof(List<int>)) return array.Select(x => x.ToObject<int>()).ToList();
                if (expectedType == typeof(List<long>)) return array.Select(x => x.ToObject<long>()).ToList();
                if (expectedType == typeof(List<bool>)) return array.Select(x => x.ToObject<bool>()).ToList();
                if (expectedType == typeof(List<float>)) return array.Select(x => x.ToObject<float>()).ToList();
                if (expectedType == typeof(List<double>)) return array.Select(x => x.ToObject<double>()).ToList();
                if (expectedType == typeof(List<decimal>)) return array.Select(x => x.ToObject<decimal>()).ToList();

                if (expectedType == typeof(IEnumerable<string>)) return array.Select(x => x.ToObject<string>()).ToArray();
                if (expectedType == typeof(IEnumerable<int>)) return array.Select(x => x.ToObject<int>()).ToArray();
                if (expectedType == typeof(IEnumerable<long>)) return array.Select(x => x.ToObject<long>()).ToArray();
                if (expectedType == typeof(IEnumerable<bool>)) return array.Select(x => x.ToObject<bool>()).ToArray();
                if (expectedType == typeof(IEnumerable<float>)) return array.Select(x => x.ToObject<float>()).ToArray();
                if (expectedType == typeof(IEnumerable<double>)) return array.Select(x => x.ToObject<double>()).ToArray();
                if (expectedType == typeof(IEnumerable<decimal>)) return array.Select(x => x.ToObject<decimal>()).ToArray();

                realType = typeof(object[]);
                return array.Select(x => x.ToObject<object>()).ToArray();
            }
            catch
            {
                // ignored
            }

            realType = typeof(object);
            return array;
        }
    }

    internal class ODataParameterCollection : IReadOnlyDictionary<string, ODataParameterValue>

    {
        private readonly Dictionary<string, ODataParameterValue> _allParameters;

        public ODataParameterCollection(JObject body, IQueryCollection query)
        {
            //UNDONE:?? Use blacklist or not?
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
