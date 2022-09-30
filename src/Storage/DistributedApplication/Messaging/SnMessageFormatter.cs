using System;
using System.Collections;
using System.Collections.Generic;
using SenseNet.Communication.Messaging;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SenseNet.Storage.DistributedApplication.Messaging
{
    public class ClusterMessageTypes : IEnumerable<Type>
    {
        public IEnumerable<Type> Types { get; set; }
        public IEnumerator<Type> GetEnumerator() => Types.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class SnMessageFormatter : IClusterMessageFormatter
    {
        private class Envelope
        {
            public string Type { get; set; }
            public ClusterMessage Msg { get; set; }
        }

        private readonly Dictionary<string, Type> _knownMessageTypes;
        private readonly JsonSerializerSettings _serializationSettings;

        public SnMessageFormatter(ClusterMessageTypes knownMessageTypes, IEnumerable<JsonConverter> jsonConverters)
        {
            _knownMessageTypes = knownMessageTypes.ToDictionary(x => x.FullName, x => x);
            _serializationSettings = new JsonSerializerSettings
            {
                Converters = jsonConverters.ToList(),
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Formatting = Formatting.Indented
            };
        }

        public ClusterMessage Deserialize(Stream data)
        {
            using var reader = new StreamReader(data);
            var text = reader.ReadToEnd();
            var envelope = JsonConvert.DeserializeObject(text, _serializationSettings) as JObject;
            if (envelope == null)
                throw new InvalidDataException("Deserialization error.");

            var typeName = envelope["Type"]?.ToString();
            if(typeName == null)
                throw new InvalidDataException("TypeName not found.");

            if(!_knownMessageTypes.TryGetValue(typeName, out var type))
                throw new InvalidDataException("Type not found: " + typeName);

            var msg = envelope["Msg"];
            if (msg == null)
                throw new InvalidDataException("Message not found");

            var message = msg.ToObject(type, JsonSerializer.Create(_serializationSettings));
            if (message == null)
                throw new InvalidDataException($"Conversion to {typeName} is failed.");

            var result = message as ClusterMessage;
            if(result == null)
                throw new InvalidDataException("Conversion to ClusterMessage is failed.");

            return result;
        }

        public Stream Serialize(ClusterMessage message)
        {
            var envelope = new Envelope {Type = message.GetType().FullName, Msg = message};
            var text = JsonConvert.SerializeObject(envelope, _serializationSettings);
            var stream = GetStreamFromString(text);
            return stream;
        }
        private static Stream GetStreamFromString(string textData)
        {
            var stream = new MemoryStream();

            // Write to the stream only if the text is not empty, because writing an empty
            // string in UTF-8 format would result in a 3 bytes length stream.
            if (!string.IsNullOrEmpty(textData))
            {
                var writer = new StreamWriter(stream, Encoding.UTF8);
                writer.Write(textData);
                writer.Flush();

                stream.Position = 0;
            }

            return stream;
        }

    }
}
