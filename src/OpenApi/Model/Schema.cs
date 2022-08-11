using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public abstract class Schema
    {
        [JsonProperty("$ref")]                 public string Ref { get; set; }

        [JsonProperty("title")]                public string Title { get; set; }
        [JsonProperty("multipleOf")]           public double? MultipleOf { get; set; }
        [JsonProperty("maximum")]              public double? Maximum { get; set; }
        [JsonProperty("exclusiveMaximum")]     public double? ExclusiveMaximum { get; set; }
        [JsonProperty("minimum")]              public double? Minimum { get; set; }
        [JsonProperty("exclusiveMinimum")]     public double? ExclusiveMinimum { get; set; }
        [JsonProperty("maxLength")]            public int? MaxLength { get; set; }
        [JsonProperty("minLength")]            public int? MinLength { get; set; }
        [JsonProperty("pattern")]              public string Pattern { get; set; }
        [JsonProperty("maxItems")]             public int? MaxItems { get; set; }
        [JsonProperty("minItems")]             public int? MinItems { get; set; }
        [JsonProperty("uniqueItems")]          public bool? UniqueItems { get; set; }
        [JsonProperty("maxProperties")]        public int? MaxProperties { get; set; }
        [JsonProperty("minProperties")]        public int? MinProperties { get; set; }
        [JsonProperty("required")]             public bool? Required { get; set; }
        [JsonProperty("enum")]                 public string[] Enum { get; set; }
        [JsonProperty("x-enumNames")]          public string[] EnumNames { get; set; }

        [JsonProperty("type")]                 public string Type { get; set; }
        [JsonProperty("allOf")]                public Schema[] AllOf { get; set; }
        [JsonProperty("oneOf")]                public Schema[] OneOf { get; set; }
        [JsonProperty("anyOf")]                public Schema[] AnyOf { get; set; }
        [JsonProperty("not")]                  public Schema[] Not { get; set; }
        [JsonProperty("items")]                public Schema Items { get; set; }
        [JsonProperty("properties")]           public IDictionary<string, Schema> Properties { get; set; }
        [JsonProperty("description")]          public string Description { get; set; }
        [JsonProperty("format")]               public string Format { get; set; }
        [JsonProperty("default")]              public object Default { get; set; }

        [JsonProperty("nullable")]             public bool? Nullable { get; set; }
        [JsonProperty("discriminator")]        public Discriminator Discriminator { get; set; }
        [JsonProperty("readOnly")]             public bool? ReadOnly { get; set; }
        [JsonProperty("writeOnly")]            public bool? WriteOnly { get; set; }
        [JsonProperty("xml")]                  public Xml Xml { get; set; }
        [JsonProperty("externalDocs")]         public ExternalDocumentation ExternalDocs { get; set; }
        [JsonProperty("example")]              public string Example { get; set; } // original specification: "any"
        [JsonProperty("deprecated")]           public bool? Deprecated { get; set; }
    }

    public class ObjectSchema : Schema
    {
        [JsonProperty("additionalProperties")] public bool? AdditionalProperties { get; set; }
    }
    public class DictionarySchema : Schema
    {
        [JsonProperty("additionalProperties")] public Schema AdditionalProperties { get; set; }
    }
}