using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.Fields;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Json;
using SenseNet.Diagnostics;

namespace SenseNet.Portal.Handlers
{
    /// <summary>
    /// Content type which can store dynamically added fields in JSON.
    /// This class also serves as an example for using the JsonDynamicFieldHelper class. 
    /// </summary>
    [ContentHandler]
    public class DynamicJsonContent : File, ISupportsDynamicFields, ISupportsAddingFieldsOnTheFly
    {
        private bool _dynamicFieldsChanged = false;
        private IDictionary<string, FieldMetadata> _dynamicFieldMetadata = null;
        private JObject _jObject = null;

        /*================================================================================= Required construction */

        public DynamicJsonContent(Node parent) : this(parent, "DynamicJsonContent") { }

        public DynamicJsonContent(Node parent, string nodeTypeName)
            : base(parent, nodeTypeName)
        {
            BuildFieldMetadata();
        }

        protected DynamicJsonContent(NodeToken nt)
            : base(nt)
        {
            BuildFieldMetadata();
        }

        /*================================================================================= Required generic property handling */

        public override object GetProperty(string name)
        {
            if (this.HasProperty(name))
                return base.GetProperty(name);
            else
                return GetDynamicProperty(name);
        }

        public override void SetProperty(string name, object value)
        {
            if (this.HasProperty(name))
                base.SetProperty(name, value);
            else
                SetDynamicProperty(name, value);
        }

        /*================================================================================= Custom methods */

        protected void BuildFieldMetadata()
        {
            using (var stream = this.Binary.GetStream())
            {
                _jObject = null;

                if (stream != null && stream.Length > 0)
                {
                    try
                    {
                        using (var streamReader = new System.IO.StreamReader(stream))
                        using (var jsonReader = new JsonTextReader(streamReader))
                        {
                            var token = JObject.ReadFrom(jsonReader);
                            if (!(token is JObject))
                                throw new Exception("Binary content of this settings file should be a JSON object.");

                            _jObject = (JObject)token;
                        }
                    }
                    catch (Exception exc)
                    {
                        SnLog.WriteException(exc);
                    }
                }

                if (_jObject == null)
                    _jObject = new JObject();
            }

            _dynamicFieldMetadata = JsonDynamicFieldHelper.BuildDynamicFieldMetadata(_jObject);
        }

        private object GetDynamicProperty(string name)
        {
            if (_dynamicFieldMetadata == null)
                BuildFieldMetadata();

            bool found;
            return JsonDynamicFieldHelper.GetProperty(_jObject, name, out found);
        }

        private void SetDynamicProperty(string name, object value)
        {
            if (_dynamicFieldMetadata == null)
                BuildFieldMetadata();

            JsonDynamicFieldHelper.SetProperty(_jObject, name, value);
            _dynamicFieldsChanged = true;
        }

        /*================================================================================= ISupportsDynamicFields implementation */

        IDictionary<string, FieldMetadata> ISupportsDynamicFields.GetDynamicFieldMetadata()
        {
            if (_dynamicFieldMetadata == null)
                BuildFieldMetadata();

            return _dynamicFieldMetadata;
        }

        bool ISupportsDynamicFields.IsNewContent
        {
            get { return this.IsNew; }
        }

        void ISupportsDynamicFields.ResetDynamicFields()
        {
            _dynamicFieldMetadata = null;
        }

        bool ISupportsAddingFieldsOnTheFly.AddFields(IEnumerable<FieldMetadata> fields)
        {
            foreach (var field in fields)
            {
                _dynamicFieldMetadata.Add(field.FieldName, field);
                this.SetDynamicProperty(field.FieldName, field.FieldSetting.DefaultValue);
                _dynamicFieldsChanged = true;
            }

            return true;
        }

        public override void Save(NodeSaveSettings settings)
        {
            if (_dynamicFieldsChanged)
            {
                JsonDynamicFieldHelper.SaveToStream(_jObject, stream =>
                {
                    this.Binary.SetStream(stream);
                    base.Save(settings);
                    _dynamicFieldsChanged = false;
                });
            }
            else
            {
                base.Save(settings);
            }
        }
    }
}
