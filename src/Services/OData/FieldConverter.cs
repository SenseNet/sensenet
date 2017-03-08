using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.Portal.OData
{
    public abstract class FieldConverter : JsonConverter
    {
        public abstract Type TargetType { get; }
        public abstract bool CanConvert(FieldSetting fieldSetting);
    }

    public class ImageFieldConverter : FieldConverter
    {
        public override Type TargetType
        {
            get { return typeof(ImageField.ImageFieldData); }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(ImageField.ImageFieldData));
        }
        public override bool CanConvert(FieldSetting fieldSetting)
        {
            return fieldSetting.FieldDataType == typeof(ImageField.ImageFieldData);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new SnNotSupportedException();
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var imgData = value as ImageField.ImageFieldData;
            if (imgData == null)
                throw new ODataException(SNSR.GetString(SNSR.Exceptions.OData.CannotConvertToJSON_2, typeof(ImageField.ImageFieldData).FullName, value.GetType().FullName), ODataExceptionCode.CannotConvertToJSON);
            writer.WriteStartObject();
            var url = imgData.Field == null ? "#" : ((ImageField)imgData.Field).ImageUrl;
            writer.WritePropertyName("_deferred");
            writer.WriteValue(url);
            writer.WriteEnd();
        }
    }

    public class VersionFieldConverter : FieldConverter
    {
        public override Type TargetType
        {
            get { return typeof(string); }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(VersionNumber));
        }
        public override bool CanConvert(FieldSetting fieldSetting)
        {
            return fieldSetting.FieldDataType == typeof(VersionNumber);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new SnNotSupportedException();
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var versionData = value as VersionNumber;
            if (versionData == null)
                throw new ODataException(SNSR.GetString(SNSR.Exceptions.OData.CannotConvertToJSON_2, typeof(VersionNumber).FullName, value.GetType().FullName), ODataExceptionCode.CannotConvertToJSON);
            writer.WriteValue(versionData.ToString());
        }
    }
    public class NodeTypeFieldConverter : FieldConverter
    {
        public override Type TargetType
        {
            get { return typeof(string); }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(NodeType));
        }
        public override bool CanConvert(FieldSetting fieldSetting)
        {
            return fieldSetting.FieldDataType == typeof(NodeType);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new SnNotSupportedException();
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var nodeType = value as NodeType;
            if (nodeType == null)
                throw new ODataException(SNSR.GetString(SNSR.Exceptions.OData.CannotConvertToJSON_2, typeof(NodeType).FullName, value.GetType().FullName), ODataExceptionCode.CannotConvertToJSON);
            writer.WriteValue(nodeType.Name);
        }
    }

    public class KeyValue
    {
        [JsonProperty("key")]
        public string Key;
        [JsonProperty("value")]
        public string Value;
    }
    public class UrlListFieldConverter : FieldConverter
    {
        public override Type TargetType
        {
            get { return typeof(IEnumerable<KeyValue>); }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Dictionary<string, string>));
        }
        public override bool CanConvert(FieldSetting fieldSetting)
        {
            return fieldSetting.FieldDataType == typeof(IDictionary<string, string>);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new SnNotSupportedException();
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var urls = value as Dictionary<string, string>;
            if (urls == null)
                throw new ODataException(SNSR.GetString(SNSR.Exceptions.OData.CannotConvertToJSON_2, "Dictionary<string, string>", value.GetType().FullName), ODataExceptionCode.CannotConvertToJSON);

            writer.WriteStartArray();
            foreach (var item in urls)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(item.Key);
                writer.WriteValue(item.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
