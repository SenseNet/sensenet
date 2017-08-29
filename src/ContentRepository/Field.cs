using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Xml;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using System.Collections.Generic;
using SenseNet.Search.Indexing;
using System.Diagnostics;
using System.Globalization;
using SenseNet.ContentRepository.i18n;
using SenseNet.Diagnostics;
using System.Linq;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// <c>Field</c> represent an atomic data of a <see cref="SenseNet.ContentRepository.Content">Content</see>
    /// </summary>
    /// <remarks>
    /// 
    /// A <c>Field</c> represents an atomic data type from the perspective of the system.
    /// It usually handles <c>Fields</c> usually handles a single <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> property.
    /// It is also capable of handling of multiple <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> properties (see the <see cref="SenseNet.Portal.UI.Controls.WhoAndWhen">WhoAndWhen</see> Field for an example).
    /// It may also consist of values computed from the handled <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> propery(s) or even represent data independent of these properties.
    /// 
    /// In the communication bus this atomic data type is resembled as a transfer object:
    /// ContentHandler<--properties--> Field <--transfer object--> FieldControl (Content, ContenView)
    /// The Field constructs a transfer object from ContentHandler properties and passes it up to the <see cref="SenseNet.Portal.UI.Controls.FieldControl">FieldControl</see>.
    /// At the same time this tranfer object is disassembled to ContentHandler properties when passing its data down to the ContentHandler.
    /// 
	/// The <c>Field</c> class is responsible for invoking data validation (data validation itself is done by the  <see cref="SenseNet.ContentRepository.Schema.FieldSetting">FieldSetting</see> property).
    /// 
    /// The class <c>Field</c> can be resembled as bridge between <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>properties and <see cref="SenseNet.Portal.UI.ContentView">ContentView</see> (or any other ways of data output/input).
    /// 
    /// The <c>Field</c> class is also one of the most important system extension points (among with <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>).
    /// By defining a new <c>Field</c> class a system-wide usable atomic data type is defined.
    /// This kind of extension is usually done when having a need of a more complex or different type than ones already defined in the system.
    /// </remarks>
	public abstract partial class Field : IIndexableField, ISnField //UNDONE: Racionalize these interfaces
    {
        private object __value;
        private bool _changed;
        private bool _isValidated;

        /// <summary>
        /// Gets a backreference to the  <see cref="SenseNet.ContentRepository.Content">Content</see> referencing the Field
        /// </summary>
		public Content Content { get; private set; }
        /// <summary>
		/// Gets approriate <see cref="SenseNet.ContentRepository.Schema.FieldSetting">FieldSetting</see> for the Field which is configured in the Field/Cofiguration block in the CTD
        /// </summary>
        /// <remarks>
        /// By default that <see cref="SenseNet.ContentRepository.Schema.FieldSetting">FieldSetting</see> is assigned to the Field which is defined in the [DefaultFieldSetting] attribte on the overriden Field class.
        /// This default can be overriden in the CTD by defining a handler attribute in the CTD (Field/Configuration@handler)
        /// </remarks>
		public FieldSetting FieldSetting { get; private set; }
        /// <summary>
        /// Gets wether the Field belongs an Aspect or not.
        /// </summary>
        public bool IsAspectField { get; private set; }
        /// <summary>
        /// Gets the name of the field. Is determined by the Content Type Definiton.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Gets the title of the field. Is determined by the Content Type Definiton.
        /// </summary>
        public string DisplayName
        {
            get { return this.FieldSetting.DisplayName ?? this.Name; }
        }
        /// <summary>
        /// Gets the description of the field. Is determined by the Content Type Definiton.
        /// </summary>
        public string Description
        {
            get { return this.FieldSetting.Description; }
        }
        /// <summary>
        /// Gets the icon of the field. Is determined by the Content Type Definiton.
        /// </summary>
        public string Icon
        {
            get { return this.FieldSetting.Icon; }
        }
        /// <summary>
        /// Gets the stored value of the field.
        /// </summary>
        /// <remarks>
        /// OriginalValue is not necessarily equal to Value.
        /// E.g. if the value stored in the database is 12, the user changes this value by editing it to 13 in the view, the _value will be 13, while the OriginalValue will still be 12 (until saving).
        /// </remarks>
		public object OriginalValue
        {
            get { return ReadProperties(); }
        }
        /// <summary>
        /// Gets wether the Field is valid.
        /// </summary>
        /// <remarks>
        /// The Field is invalid if an error has occured (and therefore the error message has been set)
        /// </remarks>
		public bool IsValid
        {
            get { return this.ValidationResult == FieldValidationResult.Successful; }
        }
        /// <summary>
        /// Gets a validation error message or null
        /// </summary>
        public FieldValidationResult ValidationResult { get; private set; }
        /// <summary>
        /// Returns true if the value of Field had beed changed
        /// </summary>
        public bool IsChanged
        {
            get { return _changed; }
        }

        /// <summary>
        /// Gets wether the Field is read only. This value is determined by the Content Type Definition.
        /// </summary>
		public virtual bool ReadOnly
        {
            get { return this.FieldSetting.ReadOnly || IsLinked; }
        }
        internal bool IsLinked { get; set; }
        private object Value
        {
            get
            {
                // If this is an aspect field, it should not try to read property values here.
                if (__value == null && !_changed && (!this.IsAspectField || (this.FieldSetting.Bindings.Any() && this.FieldSetting.Bindings[0] != this.FieldSetting.Name)))
                    __value = ReadProperties();

                return __value;
            }
            set
            {
                __value = value;
            }
        }

        // ========================================================================= Construction

        protected Field() { }

        internal static Field Create(Content content, FieldSetting fieldSetting)
        {
            return Create(content, fieldSetting, null);
        }
        internal static Field Create(Content content, FieldSetting fieldSetting, Aspect aspect)
        {
            string fieldHandlerName = fieldSetting.FieldClassName;
            if (fieldHandlerName == null)
            {
                string fieldTypeName = fieldSetting.ShortName;
                if (String.IsNullOrEmpty(fieldTypeName))
                    throw new NotSupportedException(String.Concat(SR.Exceptions.Registration.Msg_FieldTypeNotSpecified, ". FieldName: ", fieldSetting.Name));
                fieldHandlerName = FieldManager.GetFieldHandlerName(fieldTypeName);
            }

            Field field = FieldManager.CreateField(fieldHandlerName);
            field.IsAspectField = aspect != null;
            field.Name = aspect == null ? fieldSetting.Name : String.Concat(aspect.Name, Aspect.ASPECTFIELDSEPARATOR, fieldSetting.Name);
            field.Content = content;
            field.FieldSetting = fieldSetting;

            return field;
        }

        // ========================================================================= Property handling

        protected virtual object ReadProperties()
        {
            object[] handlerValues = new object[this.FieldSetting.Bindings.Count];
            for (int i = 0; i < this.FieldSetting.Bindings.Count; i++)
                if (!this.IsAspectField || (this.FieldSetting.Bindings.Any() && this.FieldSetting.Bindings[0] != this.FieldSetting.Name))
                    handlerValues[i] = ReadProperty(this.FieldSetting.Bindings[i]);
            return ConvertTo(handlerValues);
        }
        protected virtual object ReadProperty(string propertyName)
        {
            Node contentHandler = this.Content.ContentHandler;

            var dynamicHandler = contentHandler as ISupportsDynamicFields;
            if (dynamicHandler != null)
                return dynamicHandler.GetProperty(propertyName);

            var genericHandler = contentHandler as GenericContent;
            if (genericHandler != null)
                return genericHandler.GetProperty(propertyName);

            var runtimeNode = contentHandler as Content.RuntimeContentHandler;
            if (runtimeNode != null)
                return runtimeNode.GetProperty(propertyName);

            // with reflection:
            Type type = contentHandler.GetType();
            PropertyInfo prop = type.GetProperty(propertyName);
            if (prop == null) // it can be null when property is ContentListProperty?
                throw new InvalidOperationException(String.Concat("Property not found: ", propertyName, " (ContentType: ", this.Content.ContentType.Name, ")."));
            MethodInfo getter = prop.GetGetMethod();
            if (getter == null) // it can be null when property is ContentListProperty?
                throw new InvalidOperationException(String.Concat("Property is not readable: ", propertyName, " (ContentType: ", this.Content.ContentType.Name, ")."));
            return getter.Invoke(contentHandler, null);
        }
        protected virtual void WriteProperties(object value)
        {
            object[] convertedValues = ConvertFrom(value);
            if (!this.ReadOnly)
                for (int i = 0; i < this.FieldSetting.Bindings.Count; i++)
                    if (!this.IsAspectField || (this.FieldSetting.Bindings.Any() && this.FieldSetting.Bindings[0] != this.FieldSetting.Name))
                        WriteProperty(this.FieldSetting.Bindings[i], convertedValues[i]);
        }
        protected virtual void WriteProperty(string propertyName, object value)
        {
            Node contentHandler = this.Content.ContentHandler;

            var dynamicHandler = contentHandler as ISupportsDynamicFields;
            if (dynamicHandler != null)
            {
                dynamicHandler.SetProperty(propertyName, value);
                return;
            }
            var genericHandler = contentHandler as GenericContent;
            if (genericHandler != null)
            {
                genericHandler.SetProperty(propertyName, value);
                return;
            }
            var runtimeNode = contentHandler as Content.RuntimeContentHandler;
            if (runtimeNode != null)
            {
                runtimeNode.SetProperty(propertyName, value);
                return;
            }

            // with reflection:
            Type type = contentHandler.GetType();
            PropertyInfo prop = type.GetProperty(propertyName);
            if (prop == null) // it can be null when property is ContentListProperty?
                throw new InvalidOperationException(String.Concat("Property not found: ", propertyName, " (ContentType: ", this.Content.ContentType.Name, ")."));
            MethodInfo setter = prop.GetSetMethod();
            if (setter == null) // it can be null when property is ContentListProperty?
                throw new InvalidOperationException(String.Concat("Property is not writeable: ", propertyName, " (ContentType: ", this.Content.ContentType.Name, ")."));
            setter.Invoke(contentHandler, new object[] { value });
        }

        // ========================================================================= Data handling

        /// <summary>
        /// Returns object data which is a transfer object.
        /// </summary>
        public virtual object GetData(bool localized = true)
        {
            if (!LocalizationEnabled || !localized || !SenseNetResourceManager.Running)
                return Value;
            var stringData = Value as string;
            if (stringData == null)
                return Value;

            string className, name;
            if (SenseNetResourceManager.ParseResourceKey(stringData, out className, out name))
                return SenseNetResourceManager.Current.GetString(className, name);

            return Value;
        }

        /// <summary>
        /// Sets object data which is a transfer object.
        /// </summary>
		public virtual void SetData(object value)
        {
            if (ReadOnly)
                return;
            _changed = true;
            Value = value;
            this.Content.FieldChanged();
            _isValidated = false;
        }

        internal bool Validate()
        {
            if (ReadOnly)
            {
                this.ValidationResult = FieldValidationResult.Successful;
                return true;
            }
            if (_isValidated)
                return this.IsValid;
            return DoValidate();
        }
        internal void Save(bool validOnly)
        {
            // Rewrites ContentHandler properties if Field is changed and values are valid
            if (!_changed)
            {
                this.ValidationResult = FieldValidationResult.Successful;
                return;
            }

            Validate();

            if (!validOnly || this.IsValid)
            {
                WriteProperties(Value);
                _changed = false;
                if (!this.IsAspectField)
                    __value = null;
            }
        }
        private bool _isDefaultValueHandled;
        public void SetDefaultValue()
        {
            if (_isDefaultValueHandled)
                return;

            _isDefaultValueHandled = true;

            if (this.FieldSetting.IsRerouted)
                return;

            if (FieldSetting.DefaultValue != null)
            {
                Parse(FieldSetting.EvaluateDefaultValue());
                this.Save(false);
            }
        }
        private bool DoValidate()
        {
            this.ValidationResult = null;
            this.ValidationResult = this.FieldSetting.Validate(Value, this);
            _isValidated = true;
            return this.ValidationResult == FieldValidationResult.Successful;
        }
        protected internal virtual void OnSaveCompleted()
        {
        }

        // ------------------------------------------------------------------------- Globalization

        public bool LocalizationEnabled
        {
            get { return this.FieldSetting.LocalizationEnabled; }
        }
        public bool IsLocalized
        {
            get
            {
                if (!LocalizationEnabled)
                    return false;
                var sdata = GetStoredValue();
                if (string.IsNullOrEmpty(sdata))
                    return false;
                return sdata[0] == SenseNetResourceManager.ResourceKeyPrefix;
            }
        }

        public string GetStoredValue()
        {
            if (Value == null)
                return null;
            return Value.ToString();
        }
        public string GetLocalizedValue(CultureInfo cultureInfo = null)
        {
            if (!LocalizationEnabled || !SenseNetResourceManager.Running)
                return GetStoredValue();
            string className, name;
            var stringData = this.GetStoredValue();
            if (SenseNetResourceManager.ParseResourceKey(stringData, out className, out name))
            {
                if (cultureInfo == null)
                    return SenseNetResourceManager.Current.GetString(className, name);
                return SenseNetResourceManager.Current.GetString(className, name, cultureInfo);
            }
            return stringData;
        }

        // ------------------------------------------------------------------------- Format value

        public virtual string GetFormattedValue()
        {
            var data = GetData();

            return data == null ? string.Empty : data.ToString();
        }

        // ========================================================================= IIndexableField Members

        public bool IsInIndex
        {
            get
            {
                if (this.FieldSetting.IndexingInfo == null)
                    return false;
                return this.FieldSetting.IndexingInfo.IsInIndex;
            }
        }

        public bool IsBinaryField => this is BinaryField;

        public string GetIndexFieldInfoErrorLog(string message, FieldSetting fieldSetting, PerFieldIndexingInfo indexingInfo)
        {
            return message +
                " (Field name: " + this.Name +
                ", Field type: " + this.GetType().Name +
                ", Content path: " + this.Content.Path +
                ", ContentType name: " + this.Content.ContentType.Name +
                (fieldSetting == null ? string.Empty : ", FieldSetting type: " + fieldSetting.GetType().Name) +
                (indexingInfo == null ? string.Empty : ", IndexingInfo IsInIndex: " + indexingInfo.IsInIndex.ToString()) +
                ")";
        }

        public virtual IEnumerable<IndexField> GetIndexFields(out string textExtract)
        {
            var fieldSetting = this.FieldSetting;
            if (fieldSetting == null)
                throw new InvalidOperationException(GetIndexFieldInfoErrorLog("FieldSetting cannot be null.", null, null));

            var indexingInfo = fieldSetting.IndexingInfo;
            if (indexingInfo == null)
                throw new InvalidOperationException(GetIndexFieldInfoErrorLog("IndexingInfo cannot be null.", fieldSetting, null));

            var indexFieldHandler = indexingInfo.IndexFieldHandler;
            if (indexFieldHandler == null)
                throw new InvalidOperationException(GetIndexFieldInfoErrorLog("IndexFieldHandler cannot be null.", fieldSetting, indexingInfo));

            return indexFieldHandler.GetIndexFields(this, out textExtract);
        }

        // ========================================================================= Conversions

        /// <summary>
        /// Creates a transfer object from <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> properties
        /// </summary>
        /// <param name="handlerValues"></param>
        /// <returns></returns>
        protected virtual object ConvertTo(object[] handlerValues)
        {
            return handlerValues[0];
        }

        /// <summary>
        /// Creates <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> properties from the transfer object.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		protected virtual object[] ConvertFrom(object value)
        {
            return new object[] { value };
        }

        /// <summary>
        /// Gets the type of the slot.
        /// </summary>
        /// <remarks>
        /// A slot type may represent numeros CLR types in compile time. In runtime, however a single type from the previously defined enumeration is assigned to this slot. This function returns this type in runtime.
        /// </remarks>
        /// <param name="slotIndex">Index of the slot of which the type is needed</param>
		protected Type GetHandlerSlot(int slotIndex)
        {
            return this.FieldSetting.GetHandlerSlot(slotIndex);
        }

        internal string GetInnerXml()
        {
            return GetXmlData();
        }
        protected virtual string GetXmlData()
        {
            return null;
        }

        /*========================================================================= Import - Export */

        /// <summary>
        /// For old-way-import
        /// </summary>
        /// <param name="fieldNode"></param>
        /// <param name="context"></param>
        internal void Import(XmlNode fieldNode, ImportContext context)
        {
            ImportData(fieldNode, context);
        }
        /// <summary>
        /// For old-way-import
        /// </summary>
        /// <param name="fieldNode"></param>
        /// <param name="context"></param>
		protected abstract void ImportData(XmlNode fieldNode, ImportContext context);

        /// <summary>
        /// For Powershell provider
        /// </summary>
        /// <param name="fieldNode"></param>
        internal void Import(XmlNode fieldNode)
        {
            ImportData(fieldNode);
        }
        /// <summary>
        /// For Powershell provider
        /// </summary>
        /// <param name="fieldNode"></param>
        protected virtual void ImportData(XmlNode fieldNode)
        {
            ImportData(fieldNode, null);
        }



        internal void Export(XmlWriter writer, ExportContext context)
        {
            if (ReadOnly)
                return;
            if (GetData() == null)
                return;

            if (!HasExportData)
                return;

            FieldSubType subType;
            var exportName = GetExportName(this.Name, out subType);

            writer.WriteStartElement(exportName);
            if (subType != FieldSubType.General)
                writer.WriteAttributeString(FIELDSUBTYPEATTRIBUTENAME, subType.ToString());

            ExportData(writer, context);

            writer.WriteEndElement();
        }
        internal void Export2(XmlWriter writer, ExportContext context)
        {
            if (ReadOnly)
                return;
            if (GetData() == null)
                return;

            if (!HasExportData)
                return;

            FieldSubType subType;
            var exportName = GetExportName(this.Name, out subType);

            writer.WriteStartElement(exportName);
            if (subType != FieldSubType.General)
                writer.WriteAttributeString(FIELDSUBTYPEATTRIBUTENAME, subType.ToString());

            ExportData2(writer, context);

            writer.WriteEndElement();
        }

        protected virtual bool HasExportData
        {
            get { return false; }
        }
        public bool IsExportable { get { return HasExportData; } }
        protected virtual void ExportData(XmlWriter writer, ExportContext context)
        {
            throw ExportNotSupportedException();
        }
        protected virtual void ExportData2(XmlWriter writer, ExportContext context)
        {
            ExportData(writer, context);
        }

        protected Exception InvalidImportDataException(string message)
        {
            return new TransferException(true, message, this.Content.ContentHandler.Path, this.Content.ContentHandler.NodeType.Name, this.Name);
        }
        protected Exception InvalidImportDataException(string message, Exception innerException)
        {
            return new TransferException(true, message, this.Content.ContentHandler.Path, this.Content.ContentHandler.NodeType.Name, this.Name, innerException);
        }

        [Obsolete("Use ExportNotSupportedException instead", true)]
        protected Exception ExportNotImplementedException()
        {
            return ExportNotSupportedException();
        }
        protected Exception ExportNotSupportedException()
        {
            return new SnNotSupportedException(String.Concat(
                "Export is not supported. Content: ", this.Content.Path,
                ", Field: ", this.Name,
                ", FieldType: ", this.FieldSetting.ShortName));
        }
        [Obsolete("Use ExportNotSupportedException(object) instead", true)]
        protected Exception ExportNotImplementedException(object notSupportedValue)
        {
            return ExportNotSupportedException(notSupportedValue);
        }
        protected Exception ExportNotSupportedException(object notSupportedValue)
        {
            throw new SnNotSupportedException(String.Concat(
                "Export is not supported. Content: ", this.Content.Path,
                ", Field: ", this.Name,
                ", FieldType: ", this.FieldSetting.ShortName,
                ", field value type: ", notSupportedValue.GetType().FullName));
        }

        public string GetValidationMessage()
        {
            if (ValidationResult == null)
                return "Field has not validated yet.";

            var sb = new StringBuilder();
            sb.Append(ValidationResult.Category);
            var parameterNames = ValidationResult.GetParameterNames();
            if (parameterNames.Length > 0)
            {
                sb.Append("(");
                for (int i = 0; i < parameterNames.Length; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(parameterNames[i]);
                    sb.Append(": ");
                    sb.Append(ValidationResult.GetParameter(parameterNames[i]));
                }
                sb.Append(")");
            }
            return sb.ToString();
        }

        public const string FIELDSUBTYPEATTRIBUTENAME = "subType";
        internal static string GetExportName(string fieldName, out FieldSubType subType)
        {
            if (fieldName[0] == '#')
            {
                subType = FieldSubType.ContentList;
                return fieldName.Substring(1);
            }
            subType = FieldSubType.General;
            return fieldName;
        }
        internal static string ParseImportName(string importName, FieldSubType subType)
        {
            switch (subType)
            {
                case FieldSubType.General:
                    return importName;
                case FieldSubType.ContentList:
                    return importName.Insert(0, "#");
                default:
                    throw new SnNotSupportedException("FieldSubType '" + subType + "' is not supported.");
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            if (Value == null)
                return;

            FieldSubType subType;
            var exportName = GetExportName(this.Name, out subType);

            writer.WriteStartElement(exportName);
            if (subType != FieldSubType.General)
                writer.WriteAttributeString(FIELDSUBTYPEATTRIBUTENAME, subType.ToString());

            WriteXmlData(writer);

            writer.WriteEndElement();
        }
        protected virtual void WriteXmlData(XmlWriter writer)
        {
            writer.WriteString(Convert.ToString(GetData(), CultureInfo.InvariantCulture));
        }

        public bool Parse(string value)
        {
            try
            {
                if (!ParseValue(value))
                {
                    this.ValidationResult = new FieldValidationResult("FieldParse");
                    this.ValidationResult.AddParameter("InputValue", value);
                    return false;
                }
                else
                {
                    Validate();
                    return IsValid;
                }
            }
            catch (Exception e) // rethrow
            {
                throw FieldParsingException.GetException(this, e);
            }
        }
        protected virtual bool ParseValue(string value)
        {
            throw new NotSupportedException(String.Concat(
                "Parse is not supported on a field. Content: ", this.Content.Path,
                ", ContentType: ", this.Content.ContentType.Name,
                ", Field: ", this.Name,
                ", FieldType: ", this.FieldSetting.ShortName));
        }
        public virtual bool HasValue()
        {
            return OriginalValue != null;
        }
    }


    public enum FieldSubType
    {
        General, ContentList
    }
}
