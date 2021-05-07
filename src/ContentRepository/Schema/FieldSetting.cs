using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using System.Xml;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Scripting;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Schema
{
    public enum FieldVisibility
    {
        Show, Hide, Advanced
    }
    public enum OutputMethod
    {
        Default, Raw, Text, Html
    }

    /// <summary>
    /// The <c>FieldSetting</c> class represents the contents of the Field/Configuration element within the Content Type Definition.
    /// </summary>
    /// <remarks>
    /// The <c>FieldSetting</c> is basically the validation logic for a <see cref="SenseNet.ContentRepository.Field">Field</see> object.
    /// 
    /// Looking at the big picture, a Content Type Definition defines a Content Type by defining <see cref="SenseNet.ContentRepository.Field">Field</see>s (within the Field tag of the CTD) and FieldSettings to each fields (within the Field/Configuration element in the CTD)
    /// A ContentType is therefore a collection of Fields with a FieldSetting assigned to each.
    /// 
    /// FieldSettings by default are assigned to <see cref="SenseNet.ContentRepository.Field">Field</see>s automatically (e.g. <see cref="SenseNet.ContentRepository.Fields.ShortTextFieldSetting">ShortTextFieldSetting</see> is assigned to <see cref="SenseNet.Portal.UI.Controls.ShortText">ShortText</see>).
    /// However, a custom FieldSetting can be assigned to a <see cref="SenseNet.ContentRepository.Field">Field</see> by specifying the handler attribute of the Field/Configuration element.
    /// </remarks>
    /// 
    [System.Diagnostics.DebuggerDisplay("Name={Name}, Type={ShortName}, Owner={Owner == null ? \"[null]\" : Owner.Name}  ...")]
    public abstract class FieldSetting
    {
        public const string CompulsoryName = "Compulsory";
        public const string OutputMethodName = "OutputMethod";
        public const string ReadOnlyName = "ReadOnly";
        public const string DefaultValueName = "DefaultValue";
        public const string DefaultOrderName = "DefaultOrder";
        [Obsolete("Visible property is obsolete. Please use one of the following values instead: VisibleBrowse, VisibleEdit, VisibleNew")]
        public const string VisibleName = "Visible";
        public const string VisibleBrowseName = "VisibleBrowse";
        public const string VisibleEditName = "VisibleEdit";
        public const string VisibleNewName = "VisibleNew";
        public const string ControlHintName = "ControlHint";
        public const string AddToDefaultViewName = "AddToDefaultView";
        public const string ShortNameName = "ShortName";
        public const string FieldClassNameName = "FieldClassName";
        public const string DescriptionName = "Description";
        public const string IconName = "Icon";
        public const string AppInfoName = "AppInfo";
        public const string OwnerName = "Owner";
        public const string FieldIndexName = "FieldIndex";

        // Member variables /////////////////////////////////////////////////////////////////
        protected bool _mutable = false;
        private string _displayName;
        private string _description;
        private string _icon;
        private string _appInfo;

        private bool? _configIsReadOnly;
        private bool? _required;
        private string _defaultValue;
        private OutputMethod? _outputMethod;

        private FieldVisibility? _visibleBrowse;
        private FieldVisibility? _visibleEdit;
        private FieldVisibility? _visibleNew;
        private int? _defaultOrder;
        private string _controlHint;
        private int? _fieldIndex;

        // Properties /////////////////////////////////////////////////////////////

        [JsonProperty]
        public string Type { get; }

        private string _name;
        /// <summary>
        /// Gets the name of the descripted Field. This value comes from the ContentTypeDefinition.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Name is not allowed within readonly instance.");
                _name = value;
            }
        }

        private Aspect _aspect;
        [IgnoreDataMember]
        public Aspect Aspect
        {
            get { return _aspect; }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Aspect is not allowed within readonly instance.");
                _aspect = value;
            }
        }

        private string _shortName;
        /// <summary>
        /// Gets the ShortName of the descripted Field. This value comes from the ContentTypeDefinition.
        /// </summary>
        [IgnoreDataMember]
        public string ShortName
        {
            get { return _shortName; }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting ShortName is not allowed within readonly instance.");
                _shortName = value;
            }
        }

        private string _fieldClassName;
        /// <summary>
        /// Gets the fully qualified name of the descripted Field. This value comes from the ContentTypeDefinition or derived from the ShortName.
        /// </summary>
        public string FieldClassName
        {
            get { return _fieldClassName; }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting FieldClassName is not allowed within readonly instance.");
                _fieldClassName = value;
            }
        }

        /// <summary>
        /// Gets the displayname of the descripted Field. This value comes from the ContentTypeDefinition.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (_displayName != null)
                {
                    string className, name;
                    return SenseNetResourceManager.ParseResourceKey(_displayName, out className, out name)
                        ? SenseNetResourceManager.Current.GetString(className, name)
                        : _displayName;
                }

                if (ParentFieldSetting != null)
                    return ParentFieldSetting.DisplayName;
                return null;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting DisplayName is not allowed within readonly instance.");
                _displayName = value;
            }
        }

        [IgnoreDataMember]
        public string DisplayNameStoredValue
        {
            get
            {
                if (_displayName != null)
                    return _displayName;

                return ParentFieldSetting != null
                    ? ParentFieldSetting.DisplayNameStoredValue
                    : null;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting DisplayName is not allowed within readonly instance.");
                _displayName = value;
            }
        }

        /// <summary>
        /// Gets the description of the descripted Field. This value comes from the ContentTypeDefinition.
        /// </summary>
        public string Description
        {
            get
            {
                if (_description != null)
                {
                    string className, name;
                    return SenseNetResourceManager.ParseResourceKey(_description, out className, out name)
                        ? SenseNetResourceManager.Current.GetString(className, name)
                        : _description;
                }

                return ParentFieldSetting != null
                    ? ParentFieldSetting.Description
                    : null;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Description is not allowed within readonly instance.");
                _description = value;
            }
        }

        [IgnoreDataMember]
        public string DescriptionStoredValue
        {
            get
            {
                if (_description != null)
                    return _description;

                return ParentFieldSetting != null
                    ? ParentFieldSetting.DescriptionStoredValue
                    : null;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Description is not allowed within readonly instance.");
                _description = value;
            }
        }

        /// <summary>
        /// Gets the icon name of the descripted Field. This value comes from the ContentTypeDefinition.
        /// </summary>
        public string Icon
        {
            get
            {
                if (_icon != null)
                    return _icon;
                if (ParentFieldSetting != null)
                    return ParentFieldSetting.Icon;
                return null;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Icon is not allowed within readonly instance.");
                _icon = value;
            }
        }

        private List<string> _bindings;
        /// <summary>
        /// Gets the property names of ContentHandler that are handled by the descripted Field. This value comes from the ContentTypeDefinition.
        /// </summary>
        [IgnoreDataMember]
        public List<string> Bindings
        {
            get { return _bindings; }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Bindings is not allowed within readonly instance.");
                _bindings = value;
            }
        }

        [IgnoreDataMember]
        public bool IsRerouted { get; private set; }

        private ContentType _owner;
        /// <summary>
        /// Gets the owner ContentType declares or overrides the Field in the ContentTypeDefinition.
        /// </summary>
        [IgnoreDataMember]
        public ContentType Owner
        {
            get { return _owner; }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Owner is not allowed within readonly instance.");
                _owner = value;
            }
        }

        /// <summary>
        /// Returns the FieldSetting with the same name of the parent ContentType of the owner ContentType.
        /// </summary>
        /// <remarks>
        /// ContentTypes can inherit from one another but FieldSettings do not support inheritance.
        /// Therefore the parent of a FieldSetting means the FieldSetting of the parent ContentType of the owner ContentType.
        /// Visually:
        /// ContentType parent----ParentFieldSetting (e.g. <see cref="SenseNet.Portal.UI.Controls.ShortText">ShortText</see>)
        ///     ^
        ///     |
        ///     |
        /// ContentType owner-----FieldSetting (e.g. <see cref="SenseNet.Portal.UI.Controls.ShortText">ShortText</see>)
        /// </remarks>
        [IgnoreDataMember]
        public FieldSetting ParentFieldSetting { get; internal set; }

        /// <summary>
        /// Gets the content of field's AppInfo element from ContentTypeDefinition XML.
        /// </summary>
        [IgnoreDataMember]
        public string AppInfo
        {
            get
            {
                if (_appInfo != null)
                    return _appInfo;
                if (this.ParentFieldSetting == null)
                    return null;
                return this.ParentFieldSetting.AppInfo;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting AppInfo is not allowed within readonly instance.");
                _appInfo = value;
            }
        }

        [IgnoreDataMember]
        public string FullName
        {
            get { return string.Format("{0}.{1}", Owner.Name, this.Name); }
        }

        [IgnoreDataMember]
        public string BindingName
        {
            get { return GetBindingNameFromFullName(this.FullName); }
        }

        public int? FieldIndex
        {
            get
            {
                if (_fieldIndex.HasValue)
                    return _fieldIndex;

                return ParentFieldSetting == null ? _fieldIndex : ParentFieldSetting.FieldIndex;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Index is not allowed within readonly instance.");
                if (value != null)
                {
                    _fieldIndex = value;
                }
                else
                {
                    _fieldIndex = int.MaxValue;
                }
            }
        }

        /// <summary>
        /// Gets the type of the described Field's value
        /// </summary>
        [IgnoreDataMember]
        public Type FieldDataType { get; private set; }

        internal int[] HandlerSlotIndices { get; private set; }

        internal Type[][] HandlerSlots
        {
            get { return FieldManager.GetHandlerSlots(this.ShortName); }
        }

        internal RepositoryDataType[] DataTypes
        {
            get { return FieldManager.GetDataTypes(this.ShortName); }
        }

        // Indexing control //////////////////////////////////////////////////

        [IgnoreDataMember]
        public IPerFieldIndexingInfo IndexingInfo
        {
            get
            {
                if (Aspect != null)
                    return this.Aspect.GetLocalPerFieldIndexingInfo(this.Name);
                if (!Name.Contains("."))
                    return ContentTypeManager.GetPerFieldIndexingInfo(this.Name);
                return null;
            }
        }
        protected virtual IFieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            return new LowerStringIndexHandler();
        }

        // Configured properties //////////////////////////////////////////////////

        internal bool PropertyIsReadOnly { get; set; }

        public bool ReadOnly
        {
            get
            {
                if (this.PropertyIsReadOnly)
                    return true;
                if (_configIsReadOnly != null)
                    return (bool)_configIsReadOnly;
                if (this.ParentFieldSetting == null)
                    return false;
                return this.ParentFieldSetting.ReadOnly;
            }
        }

        public bool? Compulsory
        {
            get
            {
                if (_required != null)
                    return (bool)_required;
                if (this.ParentFieldSetting == null)
                    return false;
                return this.ParentFieldSetting.Compulsory;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Compulsory is not allowed within readonly instance.");
                _required = value;
            }
        }

        public OutputMethod OutputMethod
        {
            get
            {
                if (_outputMethod != null)
                    return (OutputMethod)_outputMethod;
                if (this.ParentFieldSetting == null)
                    return OutputMethod.Default;
                return this.ParentFieldSetting.OutputMethod;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting OutputMethod is not allowed within readonly instance.");
                _outputMethod = value == OutputMethod.Default ? (OutputMethod?)null : value;
            }
        }

        public string DefaultValue
        {
            get
            {
                return _defaultValue ?? (this.ParentFieldSetting == null ? null :
                    this.ParentFieldSetting.DefaultValue);
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting FieldDataType is not allowed within readonly instance.");
                _defaultValue = value;
            }
        }

        [Obsolete("Visible property is obsolete. Please use one of the following values instead: VisibleBrowse, VisibleEdit, VisibleNew")]
        public bool Visible
        {
            get
            {
                return VisibleBrowse == FieldVisibility.Show && VisibleEdit == FieldVisibility.Show && VisibleNew == FieldVisibility.Show;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Visible is not allowed within readonly instance.");

                VisibleBrowse = value ? FieldVisibility.Show : FieldVisibility.Hide;
                VisibleEdit = value ? FieldVisibility.Show : FieldVisibility.Hide;
                VisibleNew = value ? FieldVisibility.Show : FieldVisibility.Hide;
            }
        }

        public FieldVisibility VisibleBrowse
        {
            get
            {
                if (_visibleBrowse.HasValue)
                    return _visibleBrowse.Value;

                return ParentFieldSetting == null ? FieldVisibility.Show : ParentFieldSetting.VisibleBrowse;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting VisibleBrowse is not allowed within readonly instance.");
                _visibleBrowse = value;
            }
        }

        public FieldVisibility VisibleEdit
        {
            get
            {
                if (_visibleEdit.HasValue)
                    return _visibleEdit.Value;

                return ParentFieldSetting == null ? FieldVisibility.Show : ParentFieldSetting.VisibleEdit;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting VisibleEdit is not allowed within readonly instance.");
                _visibleEdit = value;
            }
        }

        public FieldVisibility VisibleNew
        {
            get
            {
                if (_visibleNew.HasValue)
                    return _visibleNew.Value;

                return ParentFieldSetting == null ? FieldVisibility.Show : ParentFieldSetting.VisibleNew;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting VisibleNew is not allowed within readonly instance.");
                _visibleNew = value;
            }
        }

        public int DefaultOrder
        {
            get
            {
                if (_defaultOrder.HasValue)
                    return _defaultOrder.Value;

                return this.ParentFieldSetting == null ? 0 : this.ParentFieldSetting.DefaultOrder;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting DefaultOrder is not allowed within readonly instance.");
                _defaultOrder = value;
            }
        }

        public string ControlHint
        {
            get
            {
                return _controlHint ?? (ParentFieldSetting != null ? ParentFieldSetting.ControlHint : null);
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting ControlHint is not allowed within readonly instance.");
                _controlHint = value;
            }
        }

        internal Type GetHandlerSlot(int slotIndex)
        {
            if (this.HandlerSlotIndices == null)
                this.HandlerSlotIndices = new[] { 0 };

            return this.HandlerSlots[slotIndex][this.HandlerSlotIndices[slotIndex]];
        }

        // Constructors ///////////////////////////////////////////////////////////

        protected FieldSetting()
        {
            _mutable = true;

            Type = GetType().Name;
        }

        // Methods ////////////////////////////////////////////////////////////////

        public virtual void Initialize() { }

        protected virtual void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
        }
        protected virtual void ParseConfiguration(Dictionary<string, object> info)
        {
        }
        protected virtual void SetDefaults()
        {
        }

        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual FieldValidationResult ValidateData(object value, Field field)
        {
            return FieldValidationResult.Successful;
        }

        public virtual IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var defOrderFs = new IntegerFieldSetting
                                 {
                                     Name = DefaultOrderName,
                                     ShortName = "Integer",
                                     DisplayName = GetTitleString(DefaultOrderName),
                                     Description = GetDescString(DefaultOrderName),
                                     FieldClassName = typeof(IntegerField).FullName,
                                     DefaultValue = "0",
                                     VisibleBrowse = FieldVisibility.Hide,
                                     VisibleEdit = FieldVisibility.Hide,
                                     VisibleNew = FieldVisibility.Hide
                                 };

            return new Dictionary<string, FieldMetadata>
                {
                    {ShortNameName, new FieldMetadata
                        {
                            FieldName = ShortNameName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting =  new ShortTextFieldSetting
                                    {
                                        Name = ShortNameName,
                                        DisplayName = GetTitleString(ShortNameName),
                                        Description = GetDescString(ShortNameName),
                                        FieldClassName = typeof(ShortTextField).FullName,
                                        VisibleBrowse = FieldVisibility.Hide,
                                        VisibleEdit = FieldVisibility.Hide,
                                        VisibleNew = FieldVisibility.Hide
                                    }
                        }
                    }, 
                    {FieldClassNameName, new FieldMetadata
                        {
                            FieldName = FieldClassNameName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting =  new ShortTextFieldSetting
                                    {
                                        Name = FieldClassNameName,
                                        DisplayName = GetTitleString(FieldClassNameName),
                                        Description = GetDescString(FieldClassNameName),
                                        FieldClassName = typeof(ShortTextField).FullName,
                                        VisibleBrowse = FieldVisibility.Hide,
                                        VisibleEdit = FieldVisibility.Hide,
                                        VisibleNew = FieldVisibility.Hide
                                    }
                        }
                    },  
                    {OwnerName, new FieldMetadata
                        {
                            FieldName = OwnerName,
                            CanRead = true,
                            CanWrite = false,
                            FieldSetting =  new ReferenceFieldSetting
                                    {
                                        Name = OwnerName,
                                        DisplayName = GetTitleString(OwnerName),
                                        Description = GetDescString(OwnerName),
                                        FieldClassName = typeof(ReferenceField).FullName,
                                        AllowedTypes = new List<string> {"ContentType"},
                                        SelectionRoots = new List<string> { "/Root/System/Schema/ContentTypes" },
                                        VisibleBrowse = FieldVisibility.Hide,
                                        VisibleEdit = FieldVisibility.Hide,
                                        VisibleNew = FieldVisibility.Hide
                                    }
                        }
                    },  
                    {IconName, new FieldMetadata
                        {
                            FieldName = IconName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting =  new ShortTextFieldSetting
                                    {
                                        Name = IconName,
                                        DisplayName = GetTitleString(IconName),
                                        Description = GetDescString(IconName),
                                        FieldClassName = typeof(ShortTextField).FullName,
                                        VisibleBrowse = FieldVisibility.Hide,
                                        VisibleEdit = FieldVisibility.Hide,
                                        VisibleNew = FieldVisibility.Hide
                                    }
                        }
                    }, 
                    {AppInfoName, new FieldMetadata
                        {
                            FieldName = AppInfoName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting =  new ShortTextFieldSetting
                                    {
                                        Name = AppInfoName,
                                        DisplayName = GetTitleString(AppInfoName),
                                        Description = GetDescString(AppInfoName),
                                        FieldClassName = typeof(ShortTextField).FullName,
                                        VisibleBrowse = FieldVisibility.Hide,
                                        VisibleEdit = FieldVisibility.Hide,
                                        VisibleNew = FieldVisibility.Hide
                                    }
                        }
                    }, 
                    {DefaultValueName, new FieldMetadata
                        {
                            FieldName = DefaultValueName,
                            PropertyType = typeof(string),
                            FieldType = DynamicContentTools.GetSuggestedFieldType(typeof(string)),
                            DisplayName = GetTitleString(DefaultValueName),
                            Description = GetDescString(DefaultValueName),
                            CanRead = true,
                            CanWrite = true
                        }
                    }
                    , 
                    {ReadOnlyName, new FieldMetadata
                        {
                            FieldName = ReadOnlyName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting = new NullFieldSetting
                            {
                                Name = ReadOnlyName,
                                DisplayName = GetTitleString(ReadOnlyName),
                                Description = GetDescString(ReadOnlyName),
                                FieldClassName = typeof(BooleanField).FullName,
                                VisibleBrowse = FieldVisibility.Hide,
                                VisibleEdit = FieldVisibility.Hide,
                                VisibleNew = FieldVisibility.Hide
                            }
                        }
                    }
                    , 
                    {CompulsoryName, new FieldMetadata
                        {
                            FieldName = CompulsoryName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting = new NullFieldSetting
                            {
                                Name = CompulsoryName,
                                DisplayName = GetTitleString(CompulsoryName),
                                Description = GetDescString(CompulsoryName),
                                FieldClassName = typeof(BooleanField).FullName
                            }
                        }
                    }
                    , 
                    {OutputMethodName, new FieldMetadata
                        {
                            FieldName = OutputMethodName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting = new ChoiceFieldSetting
                            {
                                Name = OutputMethodName,
                                DisplayName = GetTitleString(OutputMethodName),
                                Description = GetDescString(OutputMethodName),
                                EnumTypeName = typeof(OutputMethod).FullName,
                                FieldClassName =  typeof(ChoiceField).FullName,
                                AllowMultiple = false,
                                AllowExtraValue = false,
                                DefaultValue = ((int)OutputMethod.Default).ToString(),
                                VisibleBrowse = FieldVisibility.Hide,
                                VisibleEdit = FieldVisibility.Hide,
                                VisibleNew = FieldVisibility.Hide
                            }
                        }
                    }
                    , 
                    {VisibleBrowseName, new FieldMetadata
                        {
                            FieldName = VisibleBrowseName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting = new ChoiceFieldSetting
                            {
                                Name = VisibleBrowseName,
                                DisplayName = GetTitleString(VisibleBrowseName),
                                Description = GetDescString(VisibleBrowseName),
                                EnumTypeName = typeof(FieldVisibility).FullName,
                                DisplayChoice = DisplayChoice.RadioButtons,
                                AllowMultiple = false,
                                AllowExtraValue = false,
                                DefaultValue = ((int)FieldVisibility.Show).ToString(),
                                FieldClassName = typeof(ChoiceField).FullName,
                            }
                        }
                    }
                    , 
                    {VisibleEditName, new FieldMetadata
                        {
                            FieldName = VisibleEditName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting = new ChoiceFieldSetting
                            {
                                Name = VisibleEditName,
                                DisplayName = GetTitleString(VisibleEditName),
                                Description = GetDescString(VisibleEditName),
                                EnumTypeName = typeof(FieldVisibility).FullName,
                                DisplayChoice = DisplayChoice.RadioButtons,
                                AllowMultiple = false,
                                AllowExtraValue = false,
                                DefaultValue = ((int)FieldVisibility.Show).ToString(),
                                FieldClassName = typeof(ChoiceField).FullName,
                            }
                        }
                    }
                    ,
                    {VisibleNewName, new FieldMetadata
                        {
                            FieldName = VisibleNewName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting = new ChoiceFieldSetting
                            {
                                Name = VisibleNewName,
                                DisplayName = GetTitleString(VisibleNewName),
                                Description = GetDescString(VisibleNewName),
                                EnumTypeName = typeof(FieldVisibility).FullName,
                                DisplayChoice = DisplayChoice.RadioButtons,
                                AllowMultiple = false,
                                AllowExtraValue = false,
                                DefaultValue = ((int)FieldVisibility.Show).ToString(),
                                FieldClassName = typeof(ChoiceField).FullName,
                            }
                        }
                    }
                    ,
                    {DefaultOrderName, new FieldMetadata
                        {
                            FieldName = DefaultOrderName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting = defOrderFs
                        }
                    }
                    , 
                    {AddToDefaultViewName, new FieldMetadata
                        {
                            FieldName = AddToDefaultViewName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting = new NullFieldSetting
                            {
                                        Name = AddToDefaultViewName,
                                        DisplayName = GetTitleString(AddToDefaultViewName),
                                        Description = GetDescString(AddToDefaultViewName),
                                        FieldClassName = typeof(BooleanField).FullName
                            }
                        }
                    }
                    , 
                    {FieldIndexName, new FieldMetadata
                        {
                            FieldName = FieldIndexName,
                            CanRead = true,
                            CanWrite = true,
                            FieldSetting = new IntegerFieldSetting()
                            {
                                Name = FieldIndexName,
                                DisplayName = GetTitleString(FieldIndexName),
                                Description = GetDescString(FieldIndexName),
                                FieldClassName = typeof(IntegerField).FullName,
                                VisibleBrowse = FieldVisibility.Hide,
                                VisibleEdit = FieldVisibility.Hide,
                                VisibleNew = FieldVisibility.Hide
                            }
                        }
                    }
                };
        }

        public static string GetBindingNameFromFullName(string fullName)
        {
            return fullName.Replace('.', '_').Replace('#', '_');
        }

        public static FieldSetting GetFieldSettingFromFullName(string fullName, out string fieldName)
        {
            // fullName: "GenericContent.DisplayName", "ContentList.#ListField1", "Rating"
            var names = fullName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var typeName = names.Length == 2 ? names[0] : string.Empty;
            fieldName = names.Length == 1 ? names[0] : names[1];

            if (!string.IsNullOrEmpty(typeName) && !fieldName.StartsWith("#"))
            {
                var ct = ContentType.GetByName(typeName);
                if (ct != null)
                    return ct.GetFieldSettingByName(fieldName);

                SnLog.WriteWarning($"Content type {typeName} not found when converting fullname {fullName} to a fieldsetting.");
            }

            return null;
        }

        public static FieldSetting GetRoot(FieldSetting fieldSetting)
        {
            if (fieldSetting == null)
                return null;

            while (fieldSetting.ParentFieldSetting != null)
            {
                fieldSetting = fieldSetting.ParentFieldSetting;
            }

            return fieldSetting;
        }

        public FieldSetting GetEditable()
        {
            // the idea is to create an editable copy here, to prevent 
            // the user from manipulating the real schema objects
            var fieldSetting = FieldManager.CreateFieldSetting(this.GetType().FullName);
            fieldSetting.CopyPropertiesFrom(this);
            fieldSetting.Initialize();

            return fieldSetting;
        }

        protected virtual void CopyPropertiesFrom(FieldSetting source)
        {
            Name = source.Name;
            ShortName = source.ShortName;
            FieldClassName = source.FieldClassName;
            DisplayNameStoredValue = source.DisplayNameStoredValue;
            DescriptionStoredValue = source.DescriptionStoredValue;
            Icon = source.Icon;
            Owner = source.Owner;

            Bindings = new List<string>(source.Bindings);
            HandlerSlotIndices = new List<int>(source.HandlerSlotIndices).ToArray();

            AppInfo = source.AppInfo;
            Compulsory = source.Compulsory;
            OutputMethod = source.OutputMethod;
            DefaultValue = source.DefaultValue;
            PropertyIsReadOnly = source.ReadOnly;

            VisibleBrowse = source.VisibleBrowse;
            VisibleEdit = source.VisibleEdit;
            VisibleNew = source.VisibleNew;

            FieldIndex = source.FieldIndex;
        }

        public virtual object GetProperty(string name, out bool found)
        {
            object val = null;
            found = false;

            switch (name)
            {
                case DefaultValueName:
                    val = _defaultValue;
                    found = true;
                    break;
                case ReadOnlyName:
                    val = ReadOnly;
                    found = true;
                    break;
                case CompulsoryName:
                    val = Compulsory.HasValue ? (Compulsory.Value ? 1 : 0) : 0;
                    found = true;
                    break;
                case OutputMethodName:
                    found = true;
                    if (_outputMethod.HasValue)
                        val = (int)_outputMethod.Value;
                    break;
                //TODO: remove case if the Visible property and VisibleName property removed.
                case "Visible":
                    val = VisibleBrowse == FieldVisibility.Show && VisibleEdit == FieldVisibility.Show && VisibleNew == FieldVisibility.Show;
                    found = true;
                    break;
                case VisibleBrowseName:
                    found = true;
                    if (_visibleBrowse.HasValue)
                        val = (int)_visibleBrowse.Value;
                    break;
                case VisibleEditName:
                    found = true;
                    if (_visibleEdit.HasValue)
                        val = (int)_visibleEdit.Value;
                    break;
                case VisibleNewName:
                    found = true;
                    if (_visibleNew.HasValue)
                        val = (int)_visibleNew.Value;
                    break;
                case FieldIndexName:
                    found = true;
                    if (_fieldIndex.HasValue)
                        val = (int)_fieldIndex.Value;
                    break;
            }

            return found ? val : null;
        }

        public virtual bool SetProperty(string name, object value)
        {
            var found = false;

            switch (name)
            {
                case DefaultValueName:
                    if (value != null)
                        _defaultValue = value.ToString();
                    found = true;
                    break;
                case ReadOnlyName:
                    if (value != null)
                        _configIsReadOnly = (bool)value;
                    found = true;
                    break;
                case CompulsoryName:
                    if (value != null)
                        Compulsory = (bool)value;
                    found = true;
                    break;
                case OutputMethodName:
                    if (value != null)
                    {
                        // try to convert only if it is not a string or not empty
                        var valueStr = value as string;
                        if (valueStr == null || !string.IsNullOrEmpty(valueStr))
                            OutputMethod = (OutputMethod) Convert.ToInt32(value);
                    }
                    found = true;
                    break;
                //TODO: remove case if the Visible property and VisibleName property removed.
                case "Visible":
                    if (value != null)
                    {
                        var visibility = (bool)value ? FieldVisibility.Show : FieldVisibility.Hide;
                        VisibleBrowse = visibility;
                        VisibleEdit = visibility;
                        VisibleNew = visibility;
                        found = true;
                    }
                    break;
                case VisibleBrowseName:
                    found = true;
                    if (value != null)
                        _visibleBrowse = (FieldVisibility)Convert.ToInt32(value);
                    break;
                case VisibleEditName:
                    found = true;
                    if (value != null)
                        _visibleEdit = (FieldVisibility)Convert.ToInt32(value);
                    break;
                case VisibleNewName:
                    found = true;
                    if (value != null)
                        _visibleNew = (FieldVisibility)Convert.ToInt32(value);
                    break;
                case FieldIndexName:
                    found = true;
                    if (value != null)
                    {
                        _fieldIndex = Convert.ToInt32(value);
                    }
                    else
                    {
                        _fieldIndex = int.MaxValue;
                    }
                    break;
            }

            return found;
        }

        protected void ParseEnumValue<T>(string value, ref T? member) where T : struct
        {
            if (string.IsNullOrEmpty(value))
                return;

            member = (T?)Enum.Parse(typeof(T), value);
        }

        protected T GetConfigurationValue<T>(Dictionary<string, object> info, string name, object defaultValue) where T : IConvertible
        {
            object raw;
            if (!info.TryGetValue(name, out raw))
            {
                if (defaultValue == null)
                    return default(T);
                return (T)defaultValue;
            }
            return (T)Convert.ChangeType(raw, typeof(T));
        }
        protected T? GetConfigurationNullableValue<T>(Dictionary<string, object> info, string name, T? defaultValue) where T : struct
        {
            object raw;
            if (!info.TryGetValue(name, out raw))
                return defaultValue;
            return (T)Convert.ChangeType(raw, typeof(T));
        }
        protected string GetConfigurationStringValue(Dictionary<string, object> info, string name, string defaultValue)
        {
            object raw;
            return info.TryGetValue(name, out raw) ? (string)raw : defaultValue;
        }

        // Internals //////////////////////////////////////////////////////////////

        internal static FieldSetting Create(FieldDescriptor fieldDescriptor)
        {
            // for ContentType
            return Create(fieldDescriptor, (List<string>)null, null);
        }
        internal static FieldSetting Create(FieldDescriptor fieldDescriptor, Aspect aspect)
        {
            // for Aspect
            return Create(fieldDescriptor, (List<string>)null, aspect);
        }
        internal static FieldSetting Create(FieldDescriptor fieldDescriptor, List<string> bindings, Aspect aspect)
        {
            // for ContentList if bindings is not null
            var fieldSettingTypeName = string.IsNullOrEmpty(fieldDescriptor.FieldSettingTypeName)
                                           ? FieldManager.GetDefaultFieldSettingTypeName(fieldDescriptor.FieldTypeName)
                                           : fieldDescriptor.FieldSettingTypeName;
            var product = FieldManager.CreateFieldSetting(fieldSettingTypeName);

            SetProperties(product, fieldDescriptor, bindings, aspect);

            return product;
        }
        internal static FieldSetting Create(FieldInfo fieldInfo, Aspect aspect)
        {
            var fieldSettingTypeName = (fieldInfo.Configuration != null && fieldInfo.Configuration.Handler != null)
                ? fieldInfo.Configuration.Handler
                : FieldManager.GetDefaultFieldSettingTypeName(fieldInfo.GetHandlerName());

            var product = FieldManager.CreateFieldSetting(fieldSettingTypeName);
            product.Owner = ContentType.GetByName("Aspect");
            product.Aspect = aspect;

            SetProperties(product, fieldInfo, null);

            return product;
        }

        /// <summary>
        /// Creates a FieldInfo object from this FieldSetting object.
        /// A FieldInfo object is useful for serializing and interacting with the field through OData.
        /// </summary>
        /// <returns>A FieldInfo object which represents this FieldSetting object.</returns>
        public FieldInfo ToFieldInfo()
        {
            // Create the FieldInfo object
            var fieldInfo = new FieldInfo();
            fieldInfo.AppInfo = this.AppInfo;
            fieldInfo.Bind = (this.Bindings != null && this.Bindings.Count > 0) ? this.Bindings[0] : null;
            fieldInfo.IsRerouted = this.IsRerouted;
            fieldInfo.Description = this.Description;
            fieldInfo.DisplayName = this.DisplayName;
            fieldInfo.Handler = this.FieldClassName;
            fieldInfo.Icon = this.Icon;
            fieldInfo.Name = this.Name;
            fieldInfo.Type = this.ShortName;

            // Set up the configuration
            fieldInfo.Configuration = new ConfigurationInfo();
            fieldInfo.Configuration.Compulsory = this.Compulsory;
            fieldInfo.Configuration.ControlHint = this.ControlHint;
            fieldInfo.Configuration.DefaultOrder = this.DefaultOrder;
            fieldInfo.Configuration.DefaultValue = this.DefaultValue;
            fieldInfo.Configuration.FieldIndex = this.FieldIndex;
            fieldInfo.Configuration.FieldSpecific = this.WriteConfiguration();
            fieldInfo.Configuration.Handler = this.GetType().FullName;
            fieldInfo.Configuration.OutputMethod = this.OutputMethod;
            fieldInfo.Configuration.ReadOnly = this.ReadOnly;
            fieldInfo.Configuration.VisibleBrowse = this.VisibleBrowse;
            fieldInfo.Configuration.VisibleEdit = this.VisibleEdit;
            fieldInfo.Configuration.VisibleNew = this.VisibleNew;

            // Set up indexing settings
            fieldInfo.Indexing = new IndexingInfo();
            fieldInfo.Indexing.Analyzer = this.IndexingInfo.Analyzer;
            fieldInfo.Indexing.IndexHandler = this.IndexingInfo.IndexFieldHandler.GetType().FullName;
            fieldInfo.Indexing.Mode = this.IndexingInfo.IndexingMode;
            fieldInfo.Indexing.Store = this.IndexingInfo.IndexStoringMode;
            fieldInfo.Indexing.TermVector = this.IndexingInfo.TermVectorStoringMode;

            return fieldInfo;
        }

        private void Parse(XPathNavigator nav, IXmlNamespaceResolver nsres, ContentType contentType)
        {
            Reset();
            if (nav == null)
                return;

            var iter = nav.Select(string.Concat("x:", ReadOnlyName), nsres);
            _configIsReadOnly = iter.MoveNext() ? (bool?)(ParseBoolean(iter.Current.InnerXml)) : null;

            iter = nav.Select(string.Concat("x:", CompulsoryName), nsres);
            _required = iter.MoveNext() ? (bool?)(ParseBoolean(iter.Current.InnerXml)) : null;

            iter = nav.Select(string.Concat("x:", OutputMethodName), nsres);
            _outputMethod = iter.MoveNext() ? (OutputMethod?)Enum.Parse(typeof(SenseNet.ContentRepository.Schema.OutputMethod), iter.Current.InnerXml, true) : null;

            iter = nav.Select(string.Concat("x:", DefaultValueName), nsres);
            _defaultValue = iter.MoveNext() ? iter.Current.Value : null;

            iter = nav.Select(string.Concat("x:", DefaultOrderName), nsres);
            _defaultOrder = iter.MoveNext() ? (int?)iter.Current.ValueAsInt : null;

            iter = nav.Select(string.Concat("x:", VisibleBrowseName), nsres);
            _visibleBrowse = iter.MoveNext() ? ParseVisibleValue(iter.Current.Value) : null;

            iter = nav.Select(string.Concat("x:", VisibleEditName), nsres);
            _visibleEdit = iter.MoveNext() ? ParseVisibleValue(iter.Current.Value) : null;

            iter = nav.Select(string.Concat("x:", VisibleNewName), nsres);
            _visibleNew = iter.MoveNext() ? ParseVisibleValue(iter.Current.Value) : null;

            iter = nav.Select(string.Concat("x:", ControlHintName), nsres);
            _controlHint = iter.MoveNext() ? iter.Current.Value : null;

            iter = nav.Select(string.Concat("x:", FieldIndexName), nsres);
            _fieldIndex = iter.MoveNext() ? (int?)iter.Current.ValueAsInt : null;

            ParseConfiguration(nav, nsres, contentType);
        }

        private bool? ParseBoolean(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static FieldVisibility? ParseVisibleValue(string visibleValue)
        {
            if (string.IsNullOrEmpty(visibleValue))
                return null;

            return (FieldVisibility)Enum.Parse(typeof(FieldVisibility), visibleValue);
        }

        internal void Modify(FieldDescriptor fieldDescriptor, Aspect aspect)
        {
            SetProperties(this, fieldDescriptor, null, aspect);
        }
        private static void SetProperties(FieldSetting setting, FieldDescriptor descriptor, List<string> bindings, Aspect aspect)
        {
            setting.Owner = descriptor.Owner;
            setting.Name = descriptor.FieldName;
            setting.ShortName = descriptor.FieldTypeShortName;
            setting.FieldClassName = descriptor.FieldTypeName;
            setting._displayName = descriptor.DisplayName;
            setting._description = descriptor.Description;
            setting._icon = descriptor.Icon;
            setting.Bindings = bindings ?? descriptor.Bindings;
            setting.IsRerouted = descriptor.IsRerouted;
            setting.HandlerSlotIndices = new int[descriptor.Bindings.Count];
            setting.FieldDataType = FieldManager.GetFieldDataType(descriptor.FieldTypeName);
            setting.Aspect = aspect;

            setting.Parse(descriptor.ConfigurationElement, descriptor.XmlNamespaceResolver, descriptor.Owner);

            if (descriptor.AppInfo != null)
                setting._appInfo = descriptor.AppInfo.Value;

            var indexingInfo = new PerFieldIndexingInfo();

            if (!string.IsNullOrEmpty(descriptor.IndexingMode))
            {
                IndexingMode mode;
                if (Enum.TryParse(descriptor.IndexingMode, true, out mode))
                    indexingInfo.IndexingMode = mode;
                else
                    throw new ContentRegistrationException("Invalid IndexingMode: " + descriptor.IndexingMode, descriptor.Owner.Name, descriptor.FieldName);
            }
            if (!string.IsNullOrEmpty(descriptor.IndexStoringMode))
            {
                IndexStoringMode mode;
                if (Enum.TryParse(descriptor.IndexStoringMode, true, out mode))
                    indexingInfo.IndexStoringMode = mode;
                else
                    throw new ContentRegistrationException("Invalid IndexStoringMode: " + descriptor.IndexStoringMode, descriptor.Owner.Name, descriptor.FieldName);
            }
            if (!String.IsNullOrEmpty(descriptor.IndexingTermVector))
            {
                IndexTermVector mode;
                if (Enum.TryParse(descriptor.IndexingTermVector, true, out mode))
                    indexingInfo.TermVectorStoringMode = mode;
                else
                    throw new ContentRegistrationException("Invalid IndexingTermVector: " + descriptor.IndexingTermVector, descriptor.Owner.Name, descriptor.FieldName);
            }

            indexingInfo.Analyzer = descriptor.Analyzer;
            indexingInfo.IndexFieldHandler = GetIndexFieldHandler(descriptor.IndexHandlerTypeName, setting);
            indexingInfo.IndexFieldHandler.OwnerIndexingInfo = indexingInfo;

            indexingInfo.FieldDataType = setting.FieldDataType;

            if (setting.Aspect == null)
                ContentTypeManager.SetPerFieldIndexingInfo(setting.Name, setting.Owner.Name, indexingInfo);
            else
                setting.Aspect.SetPerFieldIndexingInfo(setting.Name, indexingInfo);
        }
        private static IFieldIndexHandler GetIndexFieldHandler(string typeName, FieldSetting fieldSetting)
        {
            if (string.IsNullOrEmpty(typeName))
                return fieldSetting.CreateDefaultIndexFieldHandler();

            var type = TypeResolver.GetType(typeName);

            if (type == null)
                return fieldSetting.CreateDefaultIndexFieldHandler();

            return (FieldIndexHandler)Activator.CreateInstance(type);
        }
        private static void SetProperties(FieldSetting setting, FieldInfo descriptor, List<string> bindings)
        {
            setting.Name = descriptor.Name;
            setting.ShortName = descriptor.Type;
            setting.FieldClassName = descriptor.GetHandlerName();
            setting.DisplayName = descriptor.DisplayName;
            setting.Description = descriptor.Description;
            setting.Icon = descriptor.Icon;
            setting.Bindings = bindings ?? (descriptor.Bind == null ? null : new List<string>(new[] { descriptor.Bind }));
            setting.IsRerouted = descriptor.IsRerouted;
            setting.HandlerSlotIndices = bindings != null ? new int[bindings.Count] : descriptor.Bind == null ? new int[0] : new int[1];
            setting.FieldDataType = FieldManager.GetFieldDataType(descriptor.GetHandlerName());
            setting.AppInfo = descriptor.AppInfo;

            setting.SetConfiguration(descriptor);

            setting.SetIndexing(descriptor);
        }
        private void SetConfiguration(FieldInfo info)
        {
            Reset();
            var config = info.Configuration;
            if (config == null)
                return;

            _configIsReadOnly = config.ReadOnly;
            _required = config.Compulsory;
            _outputMethod = config.OutputMethod;
            _defaultValue = config.DefaultValue;
            _defaultOrder = config.DefaultOrder;
            _visibleBrowse = config.VisibleBrowse;
            _visibleEdit = config.VisibleEdit;
            _visibleNew = config.VisibleNew;
            _controlHint = config.ControlHint;
            _fieldIndex = config.FieldIndex;

            if (info.Configuration != null)
                if (info.Configuration.FieldSpecific != null)
                    ParseConfiguration(info.Configuration.FieldSpecific);
        }
        private void SetIndexing(FieldInfo info)
        {
            if (info.Indexing == null)
                return;

            var indexingInfo = new PerFieldIndexingInfo
            {
                IndexingMode = info.Indexing.Mode,
                IndexStoringMode = info.Indexing.Store,
                TermVectorStoringMode = info.Indexing.TermVector,
                Analyzer = info.Indexing.Analyzer,
                IndexFieldHandler = GetIndexFieldHandler(info.Indexing.IndexHandler, this),
                FieldDataType = FieldManager.GetFieldDataType(info.GetHandlerName())
            };
            indexingInfo.IndexFieldHandler.OwnerIndexingInfo = indexingInfo;

            if (this.Aspect == null)
                ContentTypeManager.SetPerFieldIndexingInfo(this.Name, this.Owner.Name, indexingInfo);
            else
                this.Aspect.SetPerFieldIndexingInfo(this.Name, indexingInfo);
        }

        [Obsolete("This method will be removed in the next release.")]
        public IEnumerable<string> GetValueForQuery(Field field)
        {
            return IndexingInfo.IndexFieldHandler.GetParsableValues(field);
        }

        internal FieldValidationResult Validate(object value, Field field)
        {
            if (((value == null) || (String.IsNullOrEmpty(value.ToString()))) && (this.Compulsory ?? false))
                return new FieldValidationResult(CompulsoryName);
            return ValidateData(value, field);
        }

        private void Reset()
        {
            SetDefaults();
            _configIsReadOnly = false;
            _required = false;
        }

        public string EvaluateDefaultValue()
        {
            return EvaluateDefaultValue(DefaultValue);
        }
        public static string EvaluateDefaultValue(string defaultValue)
        {
            if (string.IsNullOrEmpty(defaultValue))
                return defaultValue;

            // This is a workaround for enhancing the default value evaluation process with
            // the template replacer feature: the latter is not available in the Storage layer.
            var replaced = TemplateManager.Replace(typeof(DefaultValueTemplateReplacer), defaultValue);

            return Evaluator.Evaluate(replaced);
        }

        public void WriteXml(XmlWriter writer)
        {
            var isListField = this.Name[0] == '#';
            var isAspectField = this.Aspect != null;
            var elementName = isListField ? "ContentListField" : isAspectField ? "AspectField" : "Field";
            var fieldName = this.Name;
            writer.WriteStartElement(elementName);
            writer.WriteAttributeString("name", fieldName);

            WriteAttribute(writer, this._shortName, "type");

            // write handlername only if there is no shortname info
            if (string.IsNullOrEmpty(this._shortName))
                WriteAttribute(writer, this._fieldClassName, "handler");

            WriteElement(writer, this._displayName, "DisplayName");
            WriteElement(writer, this._description, "Description");
            WriteElement(writer, this._icon, "Icon");
            WriteElement(writer, this._appInfo, "AppInfo");

            WriteIndexingInfo(writer);

            if (!isListField)
            {
                WriteBinding(writer);
            }

            WriteConfigurationFrame(writer);

            writer.WriteEndElement();
            writer.Flush();
        }

        public string ToXml()
        {
            var sw = new StringWriter();
            using (var writer = XmlWriter.Create(sw, new XmlWriterSettings() { OmitXmlDeclaration = true }))
            {
                this.WriteXml(writer);
            }

            return sw.ToString();
        }

        private void WriteIndexingInfo(XmlWriter writer)
        {
            var indexingInfo = this.IndexingInfo;
            if (indexingInfo == null) 
                return;

            // <Indexing>
            writer.WriteStartElement("Indexing");

            if (indexingInfo.IndexingMode != IndexingMode.Default && indexingInfo.IndexingMode != PerFieldIndexingInfo.DefaultIndexingMode)
                WriteElement(writer, indexingInfo.IndexingMode.ToString(), "Mode");
            if (indexingInfo.IndexStoringMode != IndexStoringMode.Default && indexingInfo.IndexStoringMode != PerFieldIndexingInfo.DefaultIndexStoringMode)
                WriteElement(writer, indexingInfo.IndexStoringMode.ToString(), "Store");
            if (indexingInfo.TermVectorStoringMode != IndexTermVector.Default && indexingInfo.TermVectorStoringMode != PerFieldIndexingInfo.DefaultTermVectorStoringMode)
                WriteElement(writer, indexingInfo.TermVectorStoringMode.ToString(), "TermVector");
            if (indexingInfo.Analyzer != IndexFieldAnalyzer.Default)
                WriteElement(writer, indexingInfo.Analyzer.ToString(), "Analyzer");
            if (indexingInfo.IndexFieldHandler != null)
                WriteElement(writer, indexingInfo.IndexFieldHandler.GetType().FullName, "IndexHandler");

            writer.WriteEndElement();
            // </Indexing>
        }

        private void WriteConfigurationFrame(XmlWriter writer)
        {
            writer.WriteStartElement("Configuration");

            WriteElement(writer, this._configIsReadOnly, ReadOnlyName);
            WriteElement(writer, this._required, CompulsoryName);
            if (_outputMethod.HasValue && _outputMethod.Value != Schema.OutputMethod.Default)
                WriteElement(writer, this._outputMethod.ToString(), OutputMethodName);
            WriteElement(writer, this._defaultValue, DefaultValueName);
            WriteElement(writer, this._defaultOrder, DefaultOrderName);

            if (_visibleBrowse.HasValue)
                WriteElement(writer, _visibleBrowse.Value.ToString(), VisibleBrowseName);
            if (_visibleEdit.HasValue)
                WriteElement(writer, _visibleEdit.Value.ToString(), VisibleEditName);
            if (_visibleNew.HasValue)
                WriteElement(writer, _visibleNew.Value.ToString(), VisibleNewName);
            if (_fieldIndex.HasValue)
                WriteElement(writer, _fieldIndex.Value.ToString(), FieldIndexName);

            WriteElement(writer, this._controlHint, ControlHintName);

            this.WriteConfiguration(writer);

            writer.WriteEndElement();
        }

        protected abstract void WriteConfiguration(XmlWriter writer);

        protected virtual Dictionary<string, object> WriteConfiguration()
        {
            return new Dictionary<string, object>();
        }

        protected void WriteElement(XmlWriter writer, bool? value, string elementName)
        {
            if (value.HasValue)
                WriteElement(writer, (bool)value ? "true" : "false", elementName);
        }

        protected void WriteElement(XmlWriter writer, int? value, string elementName)
        {
            if (value.HasValue)
                WriteElement(writer, value.ToString(), elementName);
        }

        protected void WriteElement(XmlWriter writer, decimal? value, string elementName)
        {
            if (value.HasValue)
                WriteElement(writer, XmlConvert.ToString(value.Value), elementName);
        }

        protected void WriteElement(XmlWriter writer, DateTime? value, string elementName)
        {
            if (value.HasValue)
                WriteElement(writer, XmlConvert.ToString(value.Value, XmlDateTimeSerializationMode.Utc), elementName);
        }

        protected void WriteElement(XmlWriter writer, string value, string elementName)
        {
            if (value == null)
                return;

            writer.WriteStartElement(elementName);
            writer.WriteString(value);
            writer.WriteEndElement();
        }

        protected void WriteAttribute(XmlWriter writer, string value, string attributeName)
        {
            if (string.IsNullOrEmpty(value))
                return;

            writer.WriteAttributeString(attributeName, value);
        }

        protected void WriteBinding(XmlWriter writer)
        {
            if (this.Bindings == null || this.Bindings.Count == 0 ||
                (this.Bindings.Count == 1 && this.Bindings[0].CompareTo(this.Name) == 0))
                return;

            foreach (var binding in this.Bindings)
            {
                writer.WriteStartElement("Bind");
                writer.WriteAttributeString("property", binding);
                writer.WriteEndElement();
            }
        }

        protected string GetTitleString(string resName)
        {
            return GetString("FieldTitle_" + resName);
        }

        protected string GetDescString(string resName)
        {
            return GetString("FieldDesc_" + resName);
        }

        protected string GetString(string resName)
        {
            return SR.GetString("FieldEditor", resName);
        }

        [IgnoreDataMember]
        public virtual bool LocalizationEnabled { get { return this.Name == "DisplayName" || this.Name == "Description"; } }


        /// <summary>
        /// Infers a Sense/Net field setting from a given XML which is ready to use in the system, using the given name.
        /// </summary>
        /// <param name="type">XML node from which a field setting should be created</param>
        /// <param name="name">Name of the newly created field setting</param>
        /// <returns>Ready to use field setting or null</returns>
        public static FieldSetting InferFieldSettingFromXml(XmlNode fieldValueInXml, string fieldName)
        {
            if (fieldValueInXml == null || string.IsNullOrEmpty(fieldValueInXml.InnerXml))
                return InferFieldSettingFromType(typeof(string), fieldName);

            return InferFieldSettingFromString(fieldValueInXml.InnerXml, fieldName);
        }

        /// <summary>
        /// Infers a Sense/Net field setting from a given string which is ready to use in the system, using the given name.
        /// </summary>
        /// <param name="type">String from which a field setting should be created</param>
        /// <param name="name">Name of the newly created field setting</param>
        /// <returns>Ready to use field setting or null</returns>
        public static FieldSetting InferFieldSettingFromString(string fieldValueInString, string fieldName)
        {
            if (!string.IsNullOrEmpty(fieldValueInString))
            {
                int i;
                if (int.TryParse(fieldValueInString, out i))
                    return InferFieldSettingFromType(typeof(int), fieldName);

                decimal d;
                if (decimal.TryParse(fieldValueInString, out d))
                    return InferFieldSettingFromType(typeof(decimal), fieldName);

                bool b;
                if (bool.TryParse(fieldValueInString, out b))
                    return InferFieldSettingFromType(typeof(bool), fieldName);

                DateTime dt;
                if (DateTime.TryParse(fieldValueInString, out dt))
                    return InferFieldSettingFromType(typeof(DateTime), fieldName);
            }

            return InferFieldSettingFromType(typeof(string), fieldName);
        }

        /// <summary>
        /// Infers a Sense/Net field setting from a given type which is ready to use in the system, using the given name.
        /// </summary>
        /// <param name="type">.NET type from which a field setting should be created</param>
        /// <param name="name">Name of the newly created field setting</param>
        /// <returns>Ready to use field setting or null</returns>
        public static FieldSetting InferFieldSettingFromType(Type type, string name)
        {
            if (type == typeof(int) || type == typeof(byte) || type == typeof(short))
            {
                // NOTE: long is too big to fit into an IntegerFieldSetting, so for that NumberFieldSetting is used instead
                return new IntegerFieldSetting()
                {
                    Name = name,
                    ShortName = "Integer",
                    FieldClassName = typeof(IntegerField).FullName,
                };
            }
            else if (type == typeof(bool))
            {
                return new NullFieldSetting()
                {
                    Name = name,
                    ShortName = "Boolean",
                    FieldClassName = typeof(BooleanField).FullName,
                };
            }
            else if (type == typeof(double) || type == typeof(long) || type == typeof(float) || type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64))
            {
                return new NumberFieldSetting()
                {
                    Name = name,
                    ShortName = "Number",
                    FieldClassName = typeof(NumberField).FullName,
                };
            }
            else if (type == typeof(string))
            {
                return new LongTextFieldSetting()
                {
                    Name = name,
                    ShortName = "LongText",
                    FieldClassName = typeof(LongTextField).FullName,
                };
            }
            else if (type == typeof(DateTime))
            {
                return new DateTimeFieldSetting()
                {
                    Name = name,
                    ShortName = "DateTime",
                    FieldClassName = typeof(DateTimeField).FullName,
                };
            }

            // Sorry, no field setting for you today
            return null;
        }
    }
}
