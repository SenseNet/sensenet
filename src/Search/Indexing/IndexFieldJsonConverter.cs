using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SenseNet.Search.Indexing
{
    internal class IndexFieldJsonConverter : JsonConverter<IndexField>
    {
        public override void WriteJson(JsonWriter writer, IndexField value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            writer.WriteValue(value.Name);
            writer.WritePropertyName("Type");
            writer.WriteValue(value.Type.ToString());
            if (value.Mode != IndexingMode.Default)
            {
                writer.WritePropertyName("Mode");
                writer.WriteValue(value.Mode.ToString());
            }
            if (value.Store != IndexStoringMode.Default)
            {
                writer.WritePropertyName("Store");
                writer.WriteValue(value.Store.ToString());
            }
            if (value.TermVector != IndexTermVector.Default)
            {
                writer.WritePropertyName("TermVector");
                writer.WriteValue(value.TermVector.ToString());
            }
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
                    writer.WriteRaw(string.Join(",", value.IntegerArrayValue.Select(x=>x.ToString())));
                    writer.WriteEndArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            writer.WriteEndObject();
        }

        public override IndexField ReadJson(JsonReader reader, Type objectType, IndexField existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            string name = null;
            IndexValueType type = IndexValueType.String;
            object value = null;
            var stringValues = new List<string>();
            var intValues = new List<int>();
            IndexingMode mode = IndexingMode.Default;
            IndexStoringMode store = IndexStoringMode.Default;
            IndexTermVector termVector = IndexTermVector.Default;
            string currentProperty = null;

            void SetProperty(object pvalue)
            {
                switch (currentProperty)
                {
                    case "Name": name = (string)pvalue; return;
                    case "Type": type = ParseEnum<IndexValueType>(pvalue); return;
                    case "Mode": mode = ParseEnum<IndexingMode>(pvalue); return;
                    case "Store": store = ParseEnum<IndexStoringMode>(pvalue); return;
                    case "TermVector": termVector = ParseEnum<IndexTermVector>(pvalue); return;
                    case "Value":
                        if(type == IndexValueType.IntArray)
                            intValues.Add(Convert.ToInt32(pvalue));
                        else if (type == IndexValueType.StringArray)
                            stringValues.Add((string) pvalue);
                        else
                            value = pvalue;
                        return;
                }
            }

            //Debug.WriteLine($">>{reader.TokenType}:{reader.ValueType?.Name ?? ""}:{reader.Value}");
            while (reader.Read())
            {
                //Debug.WriteLine($"{reader.TokenType}:{reader.ValueType?.Name ?? ""}:{reader.Value}");
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
                        return CreateIndexField(name, type, value, stringValues, intValues, mode, store, termVector);
                }
            }
            throw new NotImplementedException();
        }

        public TEnum ParseEnum<TEnum>(object valueOrName) where TEnum : System.Enum
        {
            if (valueOrName is string stringValue)
                return (TEnum)Enum.Parse(typeof(TEnum), stringValue);
            return (TEnum) Enum.ToObject(typeof(TEnum), valueOrName);
        }

        private IndexField CreateIndexField(string name, IndexValueType type, object value, List<string> strings, List<int> integers,
            IndexingMode mode, IndexStoringMode store, IndexTermVector termVector)
        {
            switch (type)
            {
                case IndexValueType.String:
                    return new IndexField(name, (string) value, mode, store, termVector);
                case IndexValueType.StringArray:
                    return new IndexField(name, strings.ToArray(), mode, store, termVector);
                case IndexValueType.Bool:
                    return new IndexField(name, Convert.ToBoolean(value), mode, store, termVector);
                case IndexValueType.Int:
                    return new IndexField(name, Convert.ToInt32(value), mode, store, termVector);
                case IndexValueType.IntArray:
                    return new IndexField(name, integers.ToArray(), mode, store, termVector);
                case IndexValueType.Long:
                    return new IndexField(name, Convert.ToInt64(value), mode, store, termVector);
                case IndexValueType.Float:
                    return new IndexField(name, Convert.ToSingle(value), mode, store, termVector);
                case IndexValueType.Double:
                    return new IndexField(name, Convert.ToDouble(value), mode, store, termVector);
                case IndexValueType.DateTime:
                    return new IndexField(name, Convert.ToDateTime(value), mode, store, termVector);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
