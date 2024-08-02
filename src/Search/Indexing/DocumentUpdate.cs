using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Search.Querying;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace SenseNet.Search.Indexing;

/// <summary>
/// Represents an atomic data structure to updating a document in the index.
/// </summary>
public class DocumentUpdate
{
    /// <summary>
    /// A term that identifies the document in the index
    /// </summary>
    public SnTerm UpdateTerm;
    /// <summary>
    /// The new document that will overwrite the existing one.
    /// </summary>
    public IndexDocument Document;


    public string Serialize()
    {
        var term = JsonConvert.SerializeObject(this.UpdateTerm, SnTerm.SerializerSettings);
        var doc = JsonConvert.SerializeObject(this.Document, IndexField.FormattedSerializerSettings);

        return @$"{{
  ""UpdateTerm"": {term},
  ""Document"": {doc}
}}";
    }

    public static DocumentUpdate Deserialize(string serializedIndexDocument)
    {
        try
        {
            var deserialized = JsonSerializer.Create(IndexField.FormattedSerializerSettings).Deserialize(
                new JsonTextReader(new StringReader(serializedIndexDocument)));

            var jObj = (JObject) deserialized;
            var termJson = (JObject)jObj["UpdateTerm"];
            var documentJson = (JArray)jObj["Document"];

            var term = SnTerm.Deserialize(termJson);
            var document = IndexDocument.Deserialize(documentJson);

            return new DocumentUpdate
            {
                UpdateTerm = term,
                Document = document
            };
        }
        catch (Exception e)
        {
            throw new SerializationException("Cannot deserialize the DocumentUpdate: " + e.Message, e);
        }
    }
}