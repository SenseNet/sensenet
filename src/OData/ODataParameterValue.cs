using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.OData;
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

                if (expectedType == typeof(ODataArray<string>)) return new ODataArray<string>((IEnumerable<string>)array.Select(x => x.ToObject<string>()).ToArray());
                if (expectedType == typeof(ODataArray<int>)) return new ODataArray<int>(array.Select(x => x.ToObject<int>()).ToArray());
                if (expectedType == typeof(ODataArray<long>)) return new ODataArray<long>(array.Select(x => x.ToObject<long>()).ToArray());
                if (expectedType == typeof(ODataArray<bool>)) return new ODataArray<bool>(array.Select(x => x.ToObject<bool>()).ToArray());
                if (expectedType == typeof(ODataArray<float>)) return new ODataArray<float>(array.Select(x => x.ToObject<float>()).ToArray());
                if (expectedType == typeof(ODataArray<double>)) return new ODataArray<double>(array.Select(x => x.ToObject<double>()).ToArray());
                if (expectedType == typeof(ODataArray<decimal>)) return new ODataArray<decimal>(array.Select(x => x.ToObject<decimal>()).ToArray());

                if (typeof(ODataArray).IsAssignableFrom(expectedType))
                {
                    realType = expectedType;
                    var ctorParam = array.Select(x => x.ToObject<object>()).ToArray();
                    return ODataTools.CreateODataArray(expectedType, ctorParam);
                }

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
}
