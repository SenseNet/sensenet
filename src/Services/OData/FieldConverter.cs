using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.Portal.OData
{
    /// <summary>
    /// Defines a base class for field converters that can convert value 
    /// of the <see cref="Content"/>'s <see cref="Field"/> to JSON format.
    /// </summary>
    public abstract class FieldConverter : JsonConverter
    {
        /// <summary>
        /// Gets the type of the object that can be converted.
        /// </summary>
        public abstract Type TargetType { get; }
        /// <summary>
        /// Returns true whether this instance can transform the value of the <see cref="Field"/>
        /// configured by the given <see cref="FieldSetting"/>
        /// </summary>
        public abstract bool CanConvert(FieldSetting fieldSetting);
    }

    /// <summary>
    /// Supports the serialization of the <see cref="ImageField"/> to JSON format.
    /// </summary>
    public class ImageFieldConverter : FieldConverter
    {
        /// <inheritdoc />
        /// <remarks>Returns with typeof(ImageField.ImageFieldData) in this case.</remarks>
        public override Type TargetType
        {
            get { return typeof(ImageField.ImageFieldData); }
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(ImageField.ImageFieldData));
        }
        /// <inheritdoc />
        public override bool CanConvert(FieldSetting fieldSetting)
        {
            return fieldSetting.FieldDataType == typeof(ImageField.ImageFieldData);
        }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new SnNotSupportedException();
        }
        /// <inheritdoc />
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

    /// <summary>
    /// Supports the serialization of the <see cref="VersionField"/> to JSON format.
    /// </summary>
    public class VersionFieldConverter : FieldConverter
    {
        /// <inheritdoc />
        /// <remarks>Returns with typeof(string) in this case.</remarks>
        public override Type TargetType
        {
            get { return typeof(string); }
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(VersionNumber));
        }
        /// <inheritdoc />
        public override bool CanConvert(FieldSetting fieldSetting)
        {
            return fieldSetting.FieldDataType == typeof(VersionNumber);
        }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new SnNotSupportedException();
        }
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var versionData = value as VersionNumber;
            if (versionData == null)
                throw new ODataException(SNSR.GetString(SNSR.Exceptions.OData.CannotConvertToJSON_2, typeof(VersionNumber).FullName, value.GetType().FullName), ODataExceptionCode.CannotConvertToJSON);
            writer.WriteValue(versionData.ToString());
        }
    }
    /// <summary>
    /// Supports the serialization of the <see cref="NodeTypeField"/> to JSON format.
    /// </summary>
    public class NodeTypeFieldConverter : FieldConverter
    {
        /// <inheritdoc />
        /// <remarks>Returns with typeof(string) in this case.</remarks>
        public override Type TargetType
        {
            get { return typeof(string); }
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(NodeType));
        }
        /// <inheritdoc />
        public override bool CanConvert(FieldSetting fieldSetting)
        {
            return fieldSetting.FieldDataType == typeof(NodeType);
        }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new SnNotSupportedException();
        }
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var nodeType = value as NodeType;
            if (nodeType == null)
                throw new ODataException(SNSR.GetString(SNSR.Exceptions.OData.CannotConvertToJSON_2, typeof(NodeType).FullName, value.GetType().FullName), ODataExceptionCode.CannotConvertToJSON);
            writer.WriteValue(nodeType.Name);
        }
    }

    /// <summary>
    /// Represents a key-value pair for serialization to the JSON format.
    /// </summary>
    public class KeyValue
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        [JsonProperty("key")]
        public string Key;
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [JsonProperty("value")]
        public string Value;
    }
    /// <summary>
    /// Supports the serialization of the <see cref="UrlListField"/> to JSON format.
    /// </summary>
    public class UrlListFieldConverter : FieldConverter
    {
        /// <inheritdoc />
        /// <remarks>Returns with typeof(IEnumerable&lt;KeyValue&gt;) in this case.</remarks>
        public override Type TargetType
        {
            get { return typeof(IEnumerable<KeyValue>); }
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Dictionary<string, string>));
        }
        /// <inheritdoc />
        public override bool CanConvert(FieldSetting fieldSetting)
        {
            return fieldSetting.FieldDataType == typeof(IDictionary<string, string>);
        }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new SnNotSupportedException();
        }
        /// <inheritdoc />
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
