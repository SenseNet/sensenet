using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.ContentRepository.Schema
{
    //public enum IndexingMode { Analyzed, AnalyzedNoNorms, No, NotAnalyzed, NotAnalyzedNoNorms }
    //public enum IndexStoringMode { No, Yes }
    //public enum IndexTermVector { No, WithOffsets, WithPositions, WithPositionsOffsets, Yes }

    /// <summary>
    /// Carries field indexing information in a FieldInfo
    /// </summary>
    public class IndexingInfo
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public IndexingMode Mode { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public IndexStoringMode Store { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public IndexTermVector TermVector { get; set; }
        /// <summary>
        /// Fully qualified type name of the associated Analyzer
        /// </summary>
        public string Analyzer { get; set; }
        /// <summary>
        /// Fully qualified type name of the associated FieldIndexHandler
        /// </summary>
        public string IndexHandler { get; set; }
    }
    /// <summary>
    /// Carries field configuration information in a FieldInfo
    /// </summary>
    public class ConfigurationInfo
    {
        /// <summary>
        /// Gets or sets the fully qualified type name of the FieldSetting descendant if it is different from the default of the corresponding Field's type.
        /// </summary>
        public string Handler { get; set; }
        /// <summary>
        /// Gets or sets the writeability of the Field.
        /// If the value is false, the Field will be read only regardles of wether the underlying property is writeable or not.
        /// True value is indifferent information because read only property cannot make writeable.
        /// Null value means the Field is read/write expect the underlying property is read only.
        /// Default: null
        /// </summary>
        public bool? ReadOnly { get; set; }
        /// <summary>
        /// Gets or sets whether the Field is nullable or not. True if the value cannot be null. Default: null.
        /// </summary>
        public bool? Compulsory { get; set; }
        /// <summary>
        /// Gets or sets the the handling method of the output in some web sceario: depending on the OutputMethod the field value will be escaped or sanitized etc. 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OutputMethod OutputMethod { get; set; }
        /// <summary>
        /// Gets or sets the default value of the Field.
        /// </summary>
        public string DefaultValue { get; set; }
        public int? DefaultOrder { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public FieldVisibility? VisibleBrowse { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public FieldVisibility? VisibleEdit { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public FieldVisibility? VisibleNew { get; set; }
        public string ControlHint { get; set; }
        public int? FieldIndex { get; set; }
        /// <summary>
        /// Gets or sets other field specific information. The value must be JSON serializable.
        /// </summary>
        public Dictionary<string, object> FieldSpecific { get; set; }
    }
    /// <summary>
    /// Field descriptor for rest API
    /// </summary>
    public class FieldInfo
    {
        private string _type;
        private string _handler;
        private string _handlerName;

        /// <summary>
        /// Name of the Field
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Short type name of the Field (e.g.: ShortText, Number, etc.)
        /// </summary>
        public string Type
        {
            get { return _type; }
            set { _type = value; _handlerName = null; }
        }
        /// <summary>
        /// Fully qualified type name of the Field. If it is not null, the Type property is ignored.
        /// </summary>
        public string Handler
        {
            get { return _handler; }
            set { _handler = value; _handlerName = null; }
        }
        /// <summary>
        /// Human readable name of the Field.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Detailed information of the Field.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Icon of the Field.
        /// </summary>
        public string Icon { get; set; }
        /// <summary>
        /// Property name if the Field name is different from underlying property name of the content handler class, or desired storing name.
        /// </summary>
        public string Bind { get; set; }

        public bool IsRerouted { get; set; }
        /// <summary>
        /// Carries indexing information
        /// </summary>
        public IndexingInfo Indexing { get; set; }
        /// <summary>
        /// Complex configuration of the Field
        /// </summary>
        public ConfigurationInfo Configuration { get; set; }
        /// <summary>
        /// Contains any unprocessed information.
        /// </summary>
        public string AppInfo { get; set; }

        /// <summary>
        /// Returns the fully qualified name of the Field. This method uses the Handler property or Type property if Handler is null
        /// </summary>
        /// <returns></returns>
        public string GetHandlerName()
        {
            if (_handlerName == null)
            {
                if (Handler != null)
                    _handlerName = Handler;
                else
                    FieldManager.FieldShortNamesFullNames.TryGetValue(this.Type, out _handlerName);
            }
            return _handlerName;
        }
    }
}
