using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Indexing
{
    internal class SnTermJsonConverter : JsonConverter<SnTerm>
    {
        public override void WriteJson(JsonWriter writer, SnTerm value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            writer.WriteValue(value.Name);
            writer.WritePropertyName("Type");
            writer.WriteValue(value.Type.ToString());
            writer.WritePropertyName("Value");
            switch (value.Type)
            {
                case IndexValueType.String:
                    writer.WriteValue(value.StringValue);
                    break;
                case IndexValueType.Bool:
                    writer.WriteValue(value.BooleanValue);
                    break;
                case IndexValueType.Int:
                    writer.WriteValue(value.IntegerValue);
                    break;
                case IndexValueType.Long:
                    writer.WriteValue(value.LongValue);
                    break;
                case IndexValueType.Float:
                    writer.WriteValue(value.SingleValue);
                    break;
                case IndexValueType.Double:
                    writer.WriteValue(value.DoubleValue);
                    break;
                case IndexValueType.DateTime:
                    writer.WriteValue(value.DateTimeValue);
                    break;
                case IndexValueType.StringArray:
                    writer.WriteStartArray();
                    writer.WriteRaw("\"" + string.Join("\",\"", value.StringArrayValue) + "\"");
                    writer.WriteEndArray();
                    break;
                case IndexValueType.IntArray:
                    writer.WriteStartArray();
                    writer.WriteRaw(string.Join(",", value.IntegerArrayValue.Select(x => x.ToString())));
                    writer.WriteEndArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            writer.WriteEndObject();
        }

        public override SnTerm ReadJson(JsonReader reader, Type objectType, SnTerm existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            string name = null;
            IndexValueType type = IndexValueType.String;
            object value = null;
            var stringValues = new List<string>();
            var intValues = new List<int>();
            string currentProperty = null;

            void SetProperty(object pvalue)
            {
                switch (currentProperty)
                {
                    case "Name": name = (string)pvalue; return;
                    case "Type": type = ParseEnum<IndexValueType>(pvalue); return;
                    case "Value":
                        if (type == IndexValueType.IntArray)
                            intValues.Add(Convert.ToInt32(pvalue));
                        else if (type == IndexValueType.StringArray)
                            stringValues.Add((string)pvalue);
                        else
                            value = pvalue;
                        return;
                }
            }

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    default:
                        throw new ArgumentOutOfRangeException();

                    case JsonToken.None:
                    case JsonToken.Comment:
                    case JsonToken.Raw:
                    case JsonToken.StartConstructor:
                    case JsonToken.EndConstructor:
                    case JsonToken.StartArray:
                    case JsonToken.EndArray:
                        break;

                    case JsonToken.StartObject: break;
                    case JsonToken.PropertyName: currentProperty = (string)reader.Value; break;
                    case JsonToken.Integer: SetProperty(reader.Value); break;
                    case JsonToken.Float: SetProperty(reader.Value); break;
                    case JsonToken.String: SetProperty(reader.Value); break;
                    case JsonToken.Boolean: SetProperty(reader.Value); break;
                    case JsonToken.Null: SetProperty(reader.Value); break;
                    case JsonToken.Undefined: SetProperty(reader.Value); break;
                    case JsonToken.Date: SetProperty(reader.Value); break;
                    case JsonToken.Bytes: SetProperty(reader.Value); break;
                    case JsonToken.EndObject:
                        return CreateSnTerm(name, type, value, stringValues, intValues);
                }
            }
            throw new NotImplementedException();
        }

        public TEnum ParseEnum<TEnum>(object valueOrName) where TEnum : System.Enum
        {
            if (valueOrName is string stringValue)
                return (TEnum)Enum.Parse(typeof(TEnum), stringValue);
            return (TEnum)Enum.ToObject(typeof(TEnum), valueOrName);
        }

        private SnTerm CreateSnTerm(string name, IndexValueType type, object value, List<string> strings, List<int> integers)
        {
            switch (type)
            {
                case IndexValueType.String:
                    return new SnTerm(name, (string)value);
                case IndexValueType.StringArray:
                    return new SnTerm(name, strings.ToArray());
                case IndexValueType.Bool:
                    return new SnTerm(name, Convert.ToBoolean(value));
                case IndexValueType.Int:
                    return new SnTerm(name, Convert.ToInt32(value));
                case IndexValueType.IntArray:
                    return new SnTerm(name, integers.ToArray());
                case IndexValueType.Long:
                    return new SnTerm(name, Convert.ToInt64(value));
                case IndexValueType.Float:
                    return new SnTerm(name, Convert.ToSingle(value));
                case IndexValueType.Double:
                    return new SnTerm(name, Convert.ToDouble(value));
                case IndexValueType.DateTime:
                    return new SnTerm(name, Convert.ToDateTime(value));
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
