using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using Newtonsoft.Json.Linq;
using System.Collections;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Json
{
    /// <summary>
    /// Helps creating dynamic field implementations from JSON binaries.
    /// </summary>
    public static class JsonDynamicFieldHelper
    {
        public sealed class SaneNumberConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(decimal);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new SnNotSupportedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var dec = (decimal)value;
                if (Math.Floor(dec) == dec)
                    writer.WriteRawValue(dec.ToString("#"));
                else
                    writer.WriteRawValue(dec.ToString());
            }
        }

        /// <summary>
        /// Separator between names of properties for embedded JSON objects.
        /// </summary>
        public const char FieldNameSeparator = '.';

        /// <summary>
        /// Builds dynamic field metadata from a JObject which can be used in ISupportsDynamicFields implementations.
        /// The aim of this class is to reduce the amount of boilerplate code needed for such use cases.
        /// NOTE: only those properties will be made into field metadata which have a direct counterpart in Sense/Net.
        /// </summary>
        public static IDictionary<string, FieldMetadata> BuildDynamicFieldMetadata(JObject jObject)
        {
            var fieldMetadata = new Dictionary<string, FieldMetadata>();
            AddFieldMetadataFromJToken(null, jObject, fieldMetadata);
            return fieldMetadata;
        }

        /// <summary>
        /// Examines a given JObject and recursively creates field metadata from it.
        /// </summary>
        private static void AddFieldMetadataFromJToken(string name, JToken token, Dictionary<string, FieldMetadata> meta)
        {
            if (token is JValue)
            {
                var val = ((JValue)token).Value;
                if (val == null)
                    return;

                var fieldSetting = FieldSetting.InferFieldSettingFromType(val.GetType(), name);
                if (fieldSetting == null)
                    return;

                meta.Add(name, new FieldMetadata()
                {
                    CanRead = true,
                    CanWrite = true,
                    FieldName = name,
                    DisplayName = name,
                    FieldSetting = fieldSetting,
                });
            }
            else if (token is JArray)
            {
                // No fitting field type in Sense/Net
            }
            else if (token is JObject)
            {
                // Object properties will look like this:
                // Settings JSON example:
                //     { aaa: "bbb", myproperty: { mysubproperty: 42 } }
                // C# code example:
                //     content["myproperty.mysubproperty"] = value;

                foreach (var jsonProperty in ((JObject)token).Properties())
                {
                    var n = name == null ? jsonProperty.Name : name + FieldNameSeparator + jsonProperty.Name;
                    AddFieldMetadataFromJToken(n, token[jsonProperty.Name], meta);
                }
            }
        }

        /// <summary>
        /// Gets a property from a JObject.
        /// </summary>
        public static object GetProperty(JObject jObject, string name, out bool found, Type typeWanted = null)
        {
            if (jObject == null)
                throw new ArgumentNullException("jObject");
            if (name == null)
                throw new ArgumentNullException("name");

            var token = GetTokenFromPropertyName(name, jObject);
            found = token != null;

            if (token == null)
                return null;
            if (!(token is JValue))
                throw new SnNotSupportedException("Can't get value from the specified token.");

            return ((JValue)token).Value;
        }

        /// <summary>
        /// Sets a property in a JObject.
        /// </summary>
        public static void SetProperty(JObject jObject, string name, object value)
        {
            if (jObject == null)
                throw new ArgumentNullException("jObject");
            if (name == null)
                throw new ArgumentNullException("name");

            var token = GetTokenFromPropertyName(name, jObject, true);
            if (token == null)
                return;
            if (value == null)
            {
                if (token.Parent is JProperty)
                    token.Parent.Remove();
                else
                    token.Remove();
                return;
            }

            if (!(token is JValue))
                throw new InvalidOperationException("Can't replace an already existing JObject with a JValue.");

            ((JValue)token).Value = value;
        }

        /// <summary>
        /// Gets the token from a JObject which corresponds to a given property name.
        /// </summary>
        private static JToken GetTokenFromPropertyName(string nameWithSeparators, JObject obj, bool create = false)
        {
            var names = nameWithSeparators.Split(FieldNameSeparator);
            JToken token = obj;
            JToken parentToken = null;

            if (names.Length == 0)
                throw new InvalidOperationException();

            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                parentToken = token;
                token = token[name];

                if (token == null)
                {
                    if (!create)
                        return null;

                    if (i == names.Length - 1)
                        token = parentToken[name] = new JValue(0);
                    else
                        token = parentToken[name] = new JObject();
                }
            }

            return token;
        }

        public static void SaveToStream(JObject jObject, Action<System.IO.Stream> beforeDispose)
        {
            if (beforeDispose == null)
                throw new ArgumentNullException("beforeDispose");

            using (var stream = new System.IO.MemoryStream())
            using (var streamWriter = new System.IO.StreamWriter(stream))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                jsonWriter.Formatting = Newtonsoft.Json.Formatting.Indented;
                jObject.WriteTo(jsonWriter, new JsonDynamicFieldHelper.SaneNumberConverter());
                jsonWriter.Flush();
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                beforeDispose(stream);
            }
        }
    }
}
