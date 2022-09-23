using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using System.Linq;
using System.Text;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Linq;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Storage;
using SenseNet.Tools;

namespace  SenseNet.ContentRepository.Schema
{
    /// <summary>
    /// Defines a class that can handle a Content type in the sensenet Content Repository.
    /// </summary>
    [ContentHandler]
    public class ContentType : Node, IFolder, IIndexableDocument, IContentType
    {
        internal static readonly string ContentDefinitionXmlNamespaceOld = "http://schemas.sensenet" + ".hu/SenseNet/ContentRepository/ContentTypeDefinition";
        /// <summary>
        /// Defines the XML namespace of the ContentTypeDefinition schema.
        /// The value is: "http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"
        /// </summary>
        public static readonly string ContentDefinitionXmlNamespace = "http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition";
        private static string ContentTypeDefinitionSchemaManifestResourceName = "SenseNet.ContentRepository.Schema.ContentTypeDefinition.xsd";
        /// <summary>
        /// Defines the extension of the file that contains a ContentType definition.
        /// </summary>
        public static readonly string ContentTypeFileNameExtension = "ContentType";

        private static readonly string[] YES_VALUES = new[] { "yes", "true", "1" };
        private static readonly string[] NO_VALUES = new[] { "no", "false", "0" };

        private string _icon;
        private string _extension;
        private string _appInfo;
        private bool? _allowIncrementalNaming;
        private bool? _isSystemType;
        private bool? _previewEnabled;
        private bool? _indexingEnabled;

        private StorageSchema StorageSchema => Providers.Instance.StorageSchema;

        // ====================================================================== Node interface: Properties

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentType"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public ContentType(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentType"/> class.
        /// Do not use this constructor directly from your code.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public ContentType(Node parent, string nodeTypeName) : base(parent, nodeTypeName)
        {
            Initialize();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentType"/> class in the loading procedure.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected ContentType(NodeToken nt) : base(nt)
        {
            Initialize();
            Build();
        }

        private void Initialize()
        {
            this.IsSystem = true;

            this.FieldSettings = new List<FieldSetting>();
            this.ChildTypes = new List<ContentType>();
        }

        /// <summary>
        /// Gets or sets the <see cref="BinaryData"/> of the ContentTypeDefinition.
        /// Persisted as <see cref="RepositoryDataType.Binary"/>.
        /// </summary>
        [RepositoryProperty("Binary", RepositoryDataType.Binary)]
        public BinaryData Binary
        {
            get { return this.GetBinary("Binary"); }
            set
            {
                var doc = GetValidDocument(StringFromStream(value.GetStream()), value.FileName);
                this.SetBinary("Binary", value);
                Build(doc);
            }
        }

        // ====================================================================== Properties

        /// <summary>
        /// Gets whether this class is <see cref="ContentType"/>.
        /// The value is always true in this case.
        /// </summary>
        public override bool IsContentType { get { return true; } }

        /// <summary>
        /// Gets the fully qualified type name of the content handler class.
        /// This value comes from the 'handler' attribute of the ContentTypeDefinition xml root element.
        /// </summary>
        public string HandlerName { get; private set; }
        /// <summary>
        /// Gets the name of the parent ContentType in the inheritance tree. The name of the root type's parent is null.
        /// This value comes from the 'parentType' attribute of the ContentTypeDefinition xml root element.
        /// </summary>
        public string ParentTypeName { get; private set; }
        /// <summary>
        /// Gets the parent ContentType in the inheritance tree. Parent of root type is null.
        /// </summary>
        public ContentType ParentType { get; private set; }
        /// <summary>
        /// Gets the list of child <see cref="ContentType"/>s.
        /// </summary>
        public List<ContentType> ChildTypes { get; private set; }

        private bool _isUnknownHandler;
        private bool IsUnknownHandler
        {
            get => _isUnknownHandler || (this.ParentType?.IsUnknownHandler ?? false);
            set => _isUnknownHandler = value;
        }

        private bool _hasUnknownField;
        private bool HasUnknownField
        {
            get => _hasUnknownField || (this.ParentType?.HasUnknownField ?? false);
            set => _hasUnknownField = value;
        }

        internal bool IsInvalid => IsUnknownHandler || HasUnknownField;

        /// <summary>
        /// Gets the description of the ContentType. This value comes from the ContentTypeDefinition.
        /// </summary>
        public string Description { get; private set; }
        /// <summary>
        /// Gets the icon name of the ContentType. This value comes from the ContentTypeDefinition.
        /// If that is left empty, the parent value is returned.
        /// </summary>
        public string Icon {
            get
            {
                if (_icon != null)
                    return _icon;
                if (ParentType == null)
                    return null;
                return ParentType.Icon;
            }
        }
        /// <summary>
        /// Gets the required name extension for the ContentType. This value comes from the ContentTypeDefinition.
        /// If that is left empty, the parent value is returned.
        /// </summary>
        public string Extension
        {
            get
            {
                if (_extension != null)
                    return _extension;
                if (ParentType == null)
                    return null;
                return ParentType.Extension;
            }
        }
        /// <summary>
        /// Gets a value that specifies whether this ContentType allows or disallows the incremental
        /// name suffix generation during content creation if another content exists with same name.
        /// This value comes from the ContentTypeDefinition.
        /// If that is left empty, the parent value is returned.
        /// </summary>
        public new bool AllowIncrementalNaming
        {
            get
            {
                if (_allowIncrementalNaming != null)
                    return _allowIncrementalNaming.Value;
                if (ParentType == null)
                    return false;
                return ParentType.AllowIncrementalNaming;
            }
        }

        /// <summary>
        /// Gets a value that specifies whether this ContentType is system type or not.
        /// This value comes from the ContentTypeDefinition's &lt;System> element and it is not inherited from the parent.
        /// </summary>
        public bool IsSystemType => _isSystemType.HasValue && _isSystemType.Value;

        /// <summary>
        /// Gets a value that specifies whether this ContentType allows or disallows preview image generation.
        /// This value comes from the ContentTypeDefinition and it is not inherited from the parent.
        /// </summary>
        public bool Preview
        {
            get
            {
                return _previewEnabled.HasValue && _previewEnabled.Value;
            }
        }

        /// <summary>
        /// Gets a value that specifies whether this ContentType allows or disallows 
        /// indexing of Content items of this type.
        /// If that is left empty, the parent value is returned.
        /// </summary>
        public bool IndexingEnabled
        {
            get
            {
                if (_indexingEnabled != null)
                    return _indexingEnabled.Value;
                if (ParentType == null)
                    return true;
                return ParentType.IndexingEnabled;
            }
        }

        /// <summary>
        /// Gets a value that defines whether this Content can contain child Content items.
        /// Returns true in this case.
        /// </summary>
        public bool IsFolder { get { return true; } }

        internal static readonly string[] EmptyAllowedChildTypeNames = new string[0];
        internal static readonly ContentType[] EmptyAllowedChildTypes = new ContentType[0];
        /// <summary>
        /// Gets a collection of the allowed child <see cref="ContentType"/> names.
        /// This list is applied to Contents instantiated from this ContentType.
        /// This value comes from the ContentTypeDefinition.
        /// </summary>
        public IEnumerable<string> AllowedChildTypeNames { get; private set; }
        /// Gets a collection of the allowed child <see cref="ContentType"/>s.
        /// This list is applied to Contents instantiated from this ContentType.
        /// This value comes from the ContentTypeDefinition.
        public IEnumerable<ContentType> AllowedChildTypes { get; private set; }

        /// <summary>
        /// Gets the content of the AppInfo element under the ContentType element of the ContentTypeDefinition XML.
        /// If that is left empty, the parent value is returned.
        /// </summary>
        public string AppInfo
        {
            get
            {
                if (_appInfo != null)
                    return _appInfo;
                if (ParentType == null)
                    return null;
                return ParentType.AppInfo;
            }
        }

        /// <summary>
        /// Gets a value that specifies whether the lifespan control is enabled for instances of this class or not.
        /// Returns false in this case.
        /// </summary>
        public bool EnableLifespan
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the list of <see cref="FieldSettings"/> instances that represent the Fields element in the ContentTypeDefinition.
        /// </summary>
        public List<FieldSetting> FieldSettings { get; private set; }
        internal int[] FieldBits { get; set; }

        /// <summary>
        /// Gets the collection of the virtual <see cref="Node"/>s that represent the <see cref="FieldSetting"/>s of this instance.
        /// </summary>
        public IEnumerable<Node> AllFieldSettingContents
        {
            get
            {
                var fsList = new List<FieldSetting>();
                var typeList = new List<ContentType>() {this};
                var parent = this.Parent as ContentType;
                while (parent != null)
                {
                    typeList.Add(parent);

                    parent = parent.Parent as ContentType;
                }

                for (var i = typeList.Count - 1; i >= 0; i--)
                {
                    foreach (var newFs in typeList[i].FieldSettings.Where(fs => fsList.Count(oldFs => oldFs.Name == fs.Name) == 0))
                    {
                        fsList.Add(newFs);
                    }
                }

                return from fs in fsList
                       where StorageSchema.NodeTypes[fs.GetType().Name] != null
                       select new FieldSettingContent(fs.GetEditable(), this) as Node;
            }
        }

        // ====================================================================== Construction

        internal static ContentType LoadAndInitialize(string contentTypePath)
        {
            ContentType contentType = Node.LoadNode(contentTypePath) as ContentType;
            contentType.Build();
            return contentType;
        }
        private void Build()
        {
            Build(new XPathDocument(new StringReader(StringFromStream(this.Binary.GetStream()))));
        }
        private void Build(IXPathNavigable definitionXml)
        {
            XPathNavigator nav = definitionXml.CreateNavigator();
            XmlNamespaceManager nsres = new XmlNamespaceManager(nav.NameTable);
            XPathNavigator root = nav.SelectSingleNode("/*[1]", nsres);
            nsres.AddNamespace("x", root.NamespaceURI);

            // Preparing: remove unused Fields
            List<string> unusedFieldNames = new List<string>();
            foreach (FieldSetting fieldSetting in this.FieldSettings)
                if (fieldSetting.Owner == this)
                    unusedFieldNames.Add(fieldSetting.Name);
            foreach (XPathNavigator fieldNav in nav.Select("/x:ContentType/x:Fields/x:Field", nsres))
                unusedFieldNames.Remove(fieldNav.GetAttribute("name", ""));
            foreach (string name in unusedFieldNames)
                RemoveUnusedField(name);

            ParseContentTypeElement(root, nsres);

            SetFieldSlots();
        }
        private void RemoveUnusedField(string name)
        {
            int index = GetFieldSettingIndexByName(name);

            FieldSetting parentFieldSetting = this.ParentType == null ? null : this.ParentType.GetFieldSettingByName(name);
            if (parentFieldSetting != null)
            {
                foreach (ContentType ct in this.ChildTypes)
                    ct.ChangeInheritedField(parentFieldSetting);
                this.FieldSettings[index] = parentFieldSetting;
            }
            else
            {
                foreach (ContentType ct in this.ChildTypes)
                    ct.RemoveUnusedInheritedField(name);
                this.FieldSettings.RemoveAt(index);
            }
        }
        private void RemoveUnusedInheritedField(string name)
        {
            int index = GetFieldSettingIndexByName(name);

            if (index == -1)
                return;

            FieldSetting fieldSetting = this.FieldSettings[index];

            // break recursion if owner of field is this contenttype
            if (fieldSetting.Owner == this)
            {
                fieldSetting.ParentFieldSetting = null;
                return;
            }

            // with postorder recursion
            foreach (ContentType child in this.ChildTypes)
                child.RemoveUnusedInheritedField(name);

            fieldSetting.ParentFieldSetting = null;
            this.FieldSettings.RemoveAt(index);
        }
        private void ChangeInheritedField(FieldSetting parent)
        {
            // Change the fieldSetting parent if its owner is this contentType
            int index = GetFieldSettingIndexByName(parent.Name);
            if (index == -1)
                return;

            FieldSetting fieldSetting = this.FieldSettings[index];

            // break recursion if owner of field is this contenttype
            if (fieldSetting.Owner == this)
            {
                fieldSetting.ParentFieldSetting = parent;
                return;
            }

            // with postorder recursion
            foreach (ContentType child in this.ChildTypes)
                child.ChangeInheritedField(parent);
            this.FieldSettings[index] = parent;
        }

        private void ParseContentTypeElement(XPathNavigator contentTypeElement, IXmlNamespaceResolver nsres)
        {
            var thisName = this.Name;
            var name = contentTypeElement.GetAttribute("name", String.Empty);
            if (thisName != null && thisName != name)
                throw new ContentRegistrationException(String.Concat("ContentTypeName cannot be modified. Original name: ", thisName, ", new name: ", name));

            this.HandlerName = contentTypeElement.GetAttribute("handler", String.Empty);
            this.ParentTypeName = contentTypeElement.GetAttribute("parentType", String.Empty);

            this.IsUnknownHandler = TypeResolver.GetType(this.HandlerName, false) == null;
            if (this.IsUnknownHandler)
            {
                SnLog.WriteWarning($"Unknown content handler {this.HandlerName} for content type {this.Name}.");
            }

            if (this.ParentTypeName.Length == 0)
                this.ParentTypeName = null;

            foreach (XPathNavigator subElement in contentTypeElement.SelectChildren(XPathNodeType.Element))
            {
                switch (subElement.LocalName)
                {
                    case "DisplayName":
                        this.DisplayName = subElement.Value;
                        break;
                    case "Description":
                        this.Description = subElement.Value;
                        break;
                    case "Icon":
                        this._icon = subElement.Value;
                        break;
                    case "Extension":
                        this._extension = subElement.Value;
                        break;
                    case "AllowIncrementalNaming":
                        this._allowIncrementalNaming = subElement.Value == "true";
                        break;
                    case "AppInfo":
                        this._appInfo = subElement.Value;
                        break;
                    case "AllowedChildTypes":
                        ParseAllowedChildTypes(subElement, nsres);
                        break;
                    case "SystemType":
                        if (!string.IsNullOrEmpty(subElement.Value) && YES_VALUES.Contains(subElement.Value.Trim().ToLower()))
                            this._isSystemType = true;
                        break;
                    case "Preview":
                        if (!string.IsNullOrEmpty(subElement.Value) && YES_VALUES.Contains(subElement.Value.Trim().ToLower()))
                            this._previewEnabled = true;
                        break;
                    case "AllowIndexing":
                        var v = subElement.Value.Trim().ToLower();
                        if (YES_VALUES.Contains(v))
                            this._indexingEnabled = true;
                        else if (NO_VALUES.Contains(v))
                            this._indexingEnabled = false;
                        break;
                    case "Fields":
                        ParseFieldElements(subElement, nsres);
                        break;
                    default:
                        throw new NotSupportedException(String.Concat("Unknown element in ContentType: ", subElement.LocalName));
                }
            }
        }

        /// <summary>
        /// Defines a char[] constant that contains all characters that may separate items
        /// in any ContenTypeDefnition's XmlElement or XmlAttribute.
        /// </summary>
        public static readonly char[] XmlListSeparators = " ,;\t\r\n".ToCharArray();
        private void ParseAllowedChildTypes(XPathNavigator allowedChildTypesElement, IXmlNamespaceResolver nsres)
        {
            AllowedChildTypeNames = allowedChildTypesElement.InnerXml.Split(XmlListSeparators, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
        }
        internal void FinalizeAllowedChildTypes(Dictionary<string, ContentType> contentTypes, List<string> allFieldNames)
        {
            if (AllowedChildTypeNames == null)
            {
                AllowedChildTypeNames = EmptyAllowedChildTypeNames;
                AllowedChildTypes = EmptyAllowedChildTypes;
            }
            else
            {
                AllowedChildTypeNames = new System.Collections.ObjectModel.ReadOnlyCollection<string>(AllowedChildTypeNames.Where(x => contentTypes.ContainsKey(x)).ToList());
                AllowedChildTypes = new System.Collections.ObjectModel.ReadOnlyCollection<ContentType>(AllowedChildTypeNames.Select(y => contentTypes[y]).ToList());
            }
            CalculateFieldBits(allFieldNames);
        }

        private static readonly int BitsOfInt = (sizeof(int) * 8);
        private void CalculateFieldBits(List<string> allFieldNames)
        {
            var bits = new List<int>(new int[allFieldNames.Count / BitsOfInt + 1]);
            foreach (var fs in this.FieldSettings)
                SetBit(bits, allFieldNames.IndexOf(fs.Name));
            this.FieldBits = bits.ToArray();
        }
        private void SetBit(List<int> bits, int fieldIndex)
        {
            var i = fieldIndex / BitsOfInt;
            var m = fieldIndex % BitsOfInt;
            while (bits.Count < i - 1)
                bits.Add(0);
            bits[i] |= 1 << m;
        }


        private void ParseFieldElements(XPathNavigator fieldsElement, IXmlNamespaceResolver nsres)
        {
            foreach (XPathNavigator fieldElement in fieldsElement.SelectChildren(XPathNodeType.Element))
            {
                FieldDescriptor fieldDescriptor = null;

                try
                {
                    fieldDescriptor = FieldDescriptor.Parse(fieldElement, nsres, this);
                }
                catch (ContentRegistrationException ex)
                {

                    this.HasUnknownField = true;

                    // throw registration exceptions only during creation.
                    if (this.Id < 1)
                        throw;

                    // continue building the content type when loading it without breaking the whole system
                    SnLog.WriteWarning(
                        $"Error during registration of field {ex.FieldName} in content type {this.Name}.",
                        properties: new Dictionary<string, object>
                        {
                            {"FieldXml", fieldElement.OuterXml}
                        });

                    continue;
                }

                int fieldIndex = GetFieldSettingIndexByName(fieldDescriptor.FieldName);
                FieldSetting fieldSetting = fieldIndex < 0 ? null : this.FieldSettings[fieldIndex];
                if (fieldSetting == null)
                {
                    // if there is not...: create and inherit
                    fieldSetting = FieldSetting.Create(fieldDescriptor);
                    this.FieldSettings.Add(fieldSetting);
                    // inherit new fieldType
                    foreach (ContentType child in this.ChildTypes)
                        child.InheritField(fieldSetting);
                }
                else
                {
                    // if there is ...
                    if (fieldSetting.Owner == this)
                    {
                        string fieldSettingTypeName = fieldDescriptor.FieldSettingTypeName;
                        if (String.IsNullOrEmpty(fieldSettingTypeName))
                            fieldSettingTypeName = FieldManager.GetDefaultFieldSettingTypeName(fieldDescriptor.FieldTypeName);
                        if (fieldSettingTypeName == fieldSetting.GetType().FullName)
                            fieldSetting.Modify(fieldDescriptor, null);
                        else
                            ChangeFieldSetting(fieldSetting, fieldDescriptor);

                    }
                    else
                    {
                        // if inherited: break inheritance
                        // create new
                        FieldSetting newFieldSetting = FieldSetting.Create(fieldDescriptor);
                        newFieldSetting.ParentFieldSetting = fieldSetting;
                        this.FieldSettings[fieldIndex] = newFieldSetting;
                        // inherit
                        foreach (ContentType child in this.ChildTypes)
                            child.InheritField(newFieldSetting);
                    }
                }
            }
        }
        private void InheritField(FieldSetting parent)
        {
            int fieldIndex = GetFieldSettingIndexByName(parent.Name);
            if (fieldIndex < 0)
            {
                this.FieldSettings.Add(parent);
            }
            else
            {
                var fs = this.FieldSettings[fieldIndex];
                if (fs.Owner == this)
                    fs.ParentFieldSetting = parent;
                else
                    this.FieldSettings[fieldIndex] = parent;
            }
        }
        private void ChangeFieldSetting(FieldSetting oldSetting, FieldDescriptor fieldDescriptor)
        {
            FieldSetting newSetting = FieldSetting.Create(fieldDescriptor);

            int index = this.GetFieldSettingIndexByName(newSetting.Name);
            this.FieldSettings[index] = newSetting;

            newSetting.ParentFieldSetting = oldSetting.ParentFieldSetting;

            ChangeParentFieldSettingRecursive(oldSetting, newSetting);
        }
        private void ChangeParentFieldSettingRecursive(FieldSetting oldSetting, FieldSetting newSetting)
        {
            foreach (ContentType childType in this.ChildTypes)
            {
                childType.ChangeParentFieldSettingRecursive(oldSetting, newSetting);
                foreach(FieldSetting childSetting in childType.FieldSettings)
                    if (childSetting.ParentFieldSetting == oldSetting)
                        childSetting.ParentFieldSetting = newSetting;
            }
        }


        internal void SetParentContentType(ContentType parentContentType)
        {
            RemoveInheritedFields();
            if (this.ParentType != null)
                this.ParentType.ChildTypes.Remove(this);

            if (parentContentType != null)
            {
                parentContentType.ChildTypes.Add(this);
                InheritFields(parentContentType);
            }
            this.ParentType = parentContentType;
        }
        private void RemoveInheritedFields()
        {
            for (int i = this.FieldSettings.Count - 1; i >= 0; i--)
                if (this.FieldSettings[i].Owner != this)
                    this.FieldSettings.Remove(this.FieldSettings[i]);
            foreach (ContentType childType in this.ChildTypes)
                childType.RemoveInheritedFields();
        }
        private void InheritFields(ContentType parentContentType)
        {
            Dictionary<string, FieldSetting> existingFields = new Dictionary<string, FieldSetting>();
            foreach (FieldSetting fieldSetting in this.FieldSettings)
                existingFields.Add(fieldSetting.Name, fieldSetting);
            foreach (FieldSetting parentFieldSetting in parentContentType.FieldSettings)
            {
                if (existingFields.ContainsKey(parentFieldSetting.Name))
                    existingFields[parentFieldSetting.Name].ParentFieldSetting = parentFieldSetting;
                else
                    this.FieldSettings.Add(parentFieldSetting);
            }
            foreach (ContentType childType in this.ChildTypes)
                childType.InheritFields(this);
        }

        private void SetFieldSlots()
        {
            try
            {
                var handlerType = TypeResolver.GetType(this.HandlerName, false);
                if (handlerType == null)
                {
                    SnLog.WriteWarning($"Unknown content handler: {HandlerName}.");
                    return;
                }

                SetFieldSlots(handlerType); 
            }
            catch (TypeNotFoundException e)
            {
                throw new ContentRegistrationException($"An error occurred during installing '{this.HandlerName}' ContentType: {e.Message}", e);
            }
        }
        private void SetFieldSlots(Type handlerType)
        {
            // Field slot indices and readonly.
            if (handlerType == null)
            {
                throw new ContentRegistrationException(String.Concat("Unknown ContentHandler: '", this.HandlerName, "'. ContentType: ", this.Name));
            }
            foreach (FieldSetting fieldSetting in this.FieldSettings)
            {
                if (fieldSetting.DataTypes.Length == 0)
                    continue;
                Type[][] slots = fieldSetting.HandlerSlots;
                for (int i = 0; i < fieldSetting.Bindings.Count; i++)
                {
                    string propName = fieldSetting.Bindings[i];
                    PropertyInfo propInfo = null;
                    propInfo = handlerType.GetProperty(propName);
                    Type propertyType = null;
                    bool readOnly = false;
                    if (propInfo != null)
                    {
                        // code property
                        propertyType = propInfo.PropertyType;
                        readOnly = readOnly || !ContentTypeManager.PropertyHasPublicSetter(propInfo);
                    }
                    else
                    {
                        // generic property
                        if (handlerType != typeof(GenericContent)
                            && !handlerType.IsSubclassOf(typeof(GenericContent)))
                        {
                            throw new ContentRegistrationException(String.Concat("Unknown property: ", propName), this.Name, fieldSetting.Name);
                        }
                        RepositoryDataType dataType = fieldSetting.DataTypes[i];
                        switch (dataType)
                        {
                            case RepositoryDataType.String:
                            case RepositoryDataType.Text:
                                propertyType = typeof(string);
                                break;
                            case RepositoryDataType.Int:
                                propertyType = typeof(Int32);
                                break;
                            case RepositoryDataType.Currency:
                                propertyType = typeof(decimal);
                                break;
                            case RepositoryDataType.DateTime:
                                propertyType = typeof(DateTime);
                                break;
                            case RepositoryDataType.Binary:
                                propertyType = typeof(BinaryData);
                                break;
                            case RepositoryDataType.Reference:
                                propertyType = typeof(NodeList<Node>);
                                break;
                            default:
                                throw new ContentRegistrationException(String.Concat("Unknown datatype: ", dataType), this.Name, fieldSetting.Name);
                        }
                    }
                    for (int j = 0; j < slots[i].Length; j++)
                    {
                        if (slots[i][j].IsAssignableFrom(propertyType))
                        {
                            fieldSetting.HandlerSlotIndices[i] = j;
                            fieldSetting.PropertyIsReadOnly = readOnly;
                            break;
                        }
                    }
                }
                fieldSetting.Initialize();
            }
        }

        // ====================================================================== Node interface: Methods

        internal void Save(bool withInstall)
        {
            if (withInstall)
                SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            else
                base.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Persist this ContentType's changes.
        /// The name of the instance and the contained ContentTypeDefinition's name must be the same.
        /// otherwise <see cref="ContentRegistrationException"/> will be thrown.
        /// </summary>
        [Obsolete("Use async version instead.", true)]
        public override void Save()
        {
            SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Asynchronously persist this ContentType's changes.
        /// The name of the instance and the contained ContentTypeDefinition's name must be the same.
        /// otherwise <see cref="ContentRegistrationException"/> will be thrown.
        /// </summary>
        public override async System.Threading.Tasks.Task SaveAsync(CancellationToken cancel)
        {
            if (!this.Path.StartsWith(Repository.ContentTypesFolderPath))
            {
                await base.SaveAsync(cancel).ConfigureAwait(false);
                return;
            }

            if (!Object.ReferenceEquals(this, ContentTypeManager.Instance.GetContentTypeByName(this.Name)))
            {
                string src = this.ToXml();
                ContentTypeManager.LoadOrCreateNew(src);
            }
            else
            {
                // Name check
                Stream stream = this.Binary.GetStream();
                long pos = stream.Position;
                stream.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(stream);
                string xml = await reader.ReadToEndAsync();
                stream.Seek(pos, SeekOrigin.Begin);
                StringReader sr = new StringReader(xml);
                XPathDocument xpathDoc = new XPathDocument(sr);
                XPathNavigator nav = xpathDoc.CreateNavigator().SelectSingleNode("/*[1]/@name");
                string name = nav.Value;
                if (name != this.Name)
                    throw new ContentRegistrationException(SR.Exceptions.Registration.Msg_InconsistentContentTypeName, this.Name);
            }
            ContentTypeManager.ApplyChanges(this, false);
            await base.SaveAsync(cancel).ConfigureAwait(false);
            ContentTypeManager.Reset();
        }

        /// <summary>
        /// Persist this Content's changes by the given settings.
        /// Do not use this method directly from your code.
        /// </summary>
        /// <param name="settings"><see cref="NodeSaveSettings"/> that contains algorithm of the persistence.</param>
        [Obsolete("Use async version instead.", true)]
        public override void Save(NodeSaveSettings settings)
        {
            SaveAsync(settings, CancellationToken.None).GetAwaiter().GetResult();
        }
        public override async System.Threading.Tasks.Task SaveAsync(NodeSaveSettings settings, CancellationToken cancel)
        {
            this.IsSystem = true;
            await base.SaveAsync(settings, cancel).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes this <see cref="ContentType"/> and the whole subtree physically.
        /// The operation is forbidden if an instance exists of any of these types.
        /// In this case an <see cref="ApplicationException"/> will be thrown.
        /// </summary>
        [Obsolete("Use async version instead", false)]
        public override void Delete()
        {
            DeleteAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Asynchronously deletes this <see cref="ContentType"/> and the whole subtree physically.
        /// The operation is forbidden if an instance exists of any of these types.
        /// In this case an <see cref="ApplicationException"/> will be thrown.
        /// </summary>
        public override async System.Threading.Tasks.Task DeleteAsync(CancellationToken cancel)
        {
            if (this.Path.StartsWith(Repository.ContentTypesFolderPath))
            {
                ContentType contentTypeToDelete = ContentTypeManager.Instance.GetContentTypeByName(this.Name);
                if (contentTypeToDelete != null)
                {
                    // We must prevent removing the content type from the schema database,
                    // because this operation is not reversible if the delete operation below fails.
                    if (!IsDeletable(contentTypeToDelete, out var message))
                        throw new ApplicationException($"Cannot delete ContentType '{Name}' because {message}.");
                    ContentTypeManager.Instance.RemoveContentType(contentTypeToDelete.Name);
                }
            }

            DisableObserver(typeof(SharingNodeObserver));
            DisableObserver(typeof(GroupMembershipObserver));
            await base.DeleteAsync(cancel);
            ContentTypeManager.Reset(); // necessary (Delete)
        }
        private static bool IsDeletable(ContentType contentType, out string message)
        {
            message = null;

            // Returns false if there is a Node which is inherited from passed ContentType or its descendant.
            var nodeType = Providers.Instance.StorageSchema.NodeTypes[contentType.Name];
            if (nodeType == null)
                return true;

            if (Providers.Instance.ContentProtector.GetProtectedPaths().Contains(contentType.Path, StringComparer.OrdinalIgnoreCase))
            {
                message = "it is protected";
                return false;
            }

            if (NodeQuery.InstanceCount(nodeType, false) > 0)
            {
                message = "one or more Content items use this type or any descendant type";
                return false;
            }

            return true;
        }
        /// <summary>
        /// Returns true if this <see cref="ContentType"/> is a descendant of the given ContentType.
        /// </summary>
        /// <param name="contentTypeName">Name of the ancestor <see cref="ContentType"/>.</param>
        public bool IsInstaceOfOrDerivedFrom(string contentTypeName)
        {
            ContentType currentContentType = this;
            while (currentContentType != null)
            {
                if(currentContentType.Name == contentTypeName)
                    return true;
                currentContentType = currentContentType.ParentType;
            }
            return false;
        }
        
        // ---------------------------------------------------------------------- Xml validation

        private IXPathNavigable GetValidDocument(string xml, string name)
        {
            if (Configuration.RepositoryEnvironment.BackwardCompatibilityXmlNamespaces)
                xml = xml.Replace(ContentDefinitionXmlNamespaceOld, ContentDefinitionXmlNamespace);

            var doc = new XPathDocument(new StringReader(xml));
            CheckValidation(doc, name);
            return doc;
        }
        private static void CheckValidation(IXPathNavigable xml, string name)
        {
            var schema = XmlValidator.LoadFromManifestResource(Assembly.GetExecutingAssembly(), ContentTypeDefinitionSchemaManifestResourceName);
            if (!schema.Validate(xml))
            {
                if (schema.Errors.Count == 0)
                    throw new ContentRegistrationException(SR.Exceptions.Registration.Msg_InvalidContentTypeDefinitionXml, name);
                else
                    throw new ContentRegistrationException(String.Concat(
                        SR.Exceptions.Registration.Msg_InvalidContentTypeDefinitionXml, ": ", schema.Errors[0].Exception.Message),
                        schema.Errors[0].Exception, name);
            }
            CheckFieldNames(xml);
        }
        private static void CheckFieldNames(IXPathNavigable xml)
        {
            XPathNavigator rootNav = xml.CreateNavigator();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(rootNav.NameTable);
            nsmgr.AddNamespace("x", ContentDefinitionXmlNamespace);

            XPathNodeIterator x = rootNav.Select("/x:ContentType", nsmgr);
            x.MoveNext();
            XPathNavigator rootNode = x.Current;
            string ctdName = rootNode.GetAttribute("name", "");

            List<string> allBindings = new List<string>();
            foreach (XPathNavigator fieldNode in rootNav.Select("//x:Field", nsmgr))
            {
                string name = fieldNode.GetAttribute("name", "");
                List<string> bindings = new List<string>();
                foreach (XPathNavigator bindNode in fieldNode.Select("x:Bind", nsmgr))
                {
                    string property = bindNode.GetAttribute("property", "");
                    bindings.Add(property);
                }
                if (bindings.Count == 0)
                    bindings.Add(name);
                allBindings.AddRange(bindings);
            }
            Dictionary<string, string> names = new Dictionary<string, string>();
            foreach (string name in allBindings)
            {
                if (names.ContainsKey(name.ToLower()))
                {
                    if (names[name.ToLower()] != name)
                        throw new RegistrationException(String.Concat(
                            SR.Exceptions.Registration.Msg_InvalidContentTypeDefinitionXml, ": '", ctdName, "'. ",
                            "Two Field or Binding names are case insensitive equal, but case sensitive they are not equal: '", names[name.ToLower()], "' != '", name, "'"));
                }
                else
                {
                    names.Add(name.ToLower(), name);
                }
            }
            foreach (PropertyType propType in Providers.Instance.StorageSchema.PropertyTypes)
            {
                string newName;
                if (names.TryGetValue(propType.Name.ToLower(), out newName))
                    if (propType.Name != newName)
                        throw new RegistrationException(String.Concat(
                            SR.Exceptions.Registration.Msg_InvalidContentTypeDefinitionXml, ": '", ctdName, "'. ",
                            "A Field or Binding name and an existing RepositoryProperty name are case insensitive equal, but case sensitive they are not equal: '", propType.Name,
                            "', Field or Binding name: '", newName, "'"));
            }
        }

        internal static string StringFromStream(Stream stream)
        {
            // buffer to copy
            byte[] buf = new byte[stream.Length];

            // source init
            long startPos = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);

            // copy
            int data = 0;
            long targetPos = 0;
            while ((data = stream.ReadByte()) != -1)
                buf[targetPos++] = (byte)data;


            // reset source
            stream.Seek(startPos, SeekOrigin.Begin);

            // read copied stream
            MemoryStream str = new MemoryStream(buf);
            StreamReader reader = new StreamReader(str);
            string textData = reader.ReadToEnd();
            reader.Close();

            return textData;
        }

        /// <summary>
        /// Returns the <see cref="FieldSetting"/> by the given field name.
        /// </summary>
        /// <param name="fieldName">Inherited or owned Field name.</param>
        /// <returns>Null or <see cref="FieldSetting"/>.</returns>
        public FieldSetting GetFieldSettingByName(string fieldName)
        {
            int i = GetFieldSettingIndexByName(fieldName);
            return i < 0 ? null : this.FieldSettings[i];
        }
        private int GetFieldSettingIndexByName(string fieldName)
        {
            for (int i = 0; i < this.FieldSettings.Count; i++)
                if (this.FieldSettings[i].Name == fieldName)
                    return i;
            return -1;
        }

        // ==================================================== IFolder 

        /// <summary>
        /// Gets the collection of the child <see cref="Node"/>s.
        /// </summary>
        public IEnumerable<Node> Children
        {
            get { return base.GetChildren(); }
        }
        /// <summary>
        /// Gets the count of the Children property value.
        /// </summary>
        public int ChildCount
        {
            get { return base.GetChildCount(); }
        }

        /// <inheritdoc />
        public virtual QueryResult GetChildren(QuerySettings settings)
        {
            return GetChildren(string.Empty, settings);
        }

        /// <inheritdoc />
        public virtual QueryResult GetChildren(string text, QuerySettings settings)
        {
            return GetChildren(text, settings, false);
        }

        /// <summary>
        /// Returns this Content's children as a query result.
        /// </summary>
        /// <param name="text">An additional filter clause.</param>
        /// <param name="settings">A <see cref="QuerySettings"/> that extends the base query.</param>
        /// <param name="getAllChildren">Defines whether all items of the subtree (true)
        ///  or only the direct children (false) should be returned.</param>
        /// <returns>The <see cref="QueryResult"/> instance.</returns>
        public virtual QueryResult GetChildren(string text, QuerySettings settings, bool getAllChildren)
        {
            if (Providers.Instance.SearchManager.ContentQueryIsAllowed)
            {
                var query = ContentQuery.CreateQuery(getAllChildren ? SafeQueries.InTree : SafeQueries.InFolder, settings, this.Path);
                if (!string.IsNullOrEmpty(text))
                    query.AddClause(text);
                return query.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                var nqr = NodeQuery.QueryChildren(this.Path);
                return new QueryResult(nqr.Identifiers, nqr.Count);
            }
        }

        // ---------------------------------------------------- Linq

        /// <summary>
        /// Protected member that lets inherited classes access the raw value of the ChildrenDefinition property.
        /// </summary>
        protected ChildrenDefinition _childrenDefinition;
        /// <summary>
        /// Gets or defines the child collection of this Content.
        /// </summary>
        public virtual ChildrenDefinition ChildrenDefinition
        {
            get
            {
                if (_childrenDefinition == null)
                    _childrenDefinition = ChildrenDefinition.Default;
                return _childrenDefinition;
            }
            set { _childrenDefinition = value; }
        }

        internal ISnQueryable<Content> GetQueryableChildren()
        {
            return new ContentSet<Content>(this.ChildrenDefinition.Clone(), this.Path);
        }

        // ==================================================== Tools

        /// <summary>
        /// Returns the <see cref="ContentType"/> by the given name. 
        /// </summary>
        public static ContentType GetByName(string contentTypeName)
        {
            return ContentTypeManager.Instance.GetContentTypeByName(contentTypeName);
        }

        /// <summary>
        /// Returns an array of <see cref="ContentType"/>s that does not have a parent <see cref="ContentType"/>.
        /// </summary>
        public static ContentType[] GetRootTypes()
        {
            return ContentTypeManager.Instance.GetRootTypes();
        }
        /// <summary>
        /// Returns an array of <see cref="ContentType"/> names that does not have a parent <see cref="ContentType"/>.
        /// </summary>
        public static string[] GetRootTypeNames()
        {
            return ContentTypeManager.Instance.GetRootTypeNames();
        }
        /// <summary>
        /// Returns an array of all <see cref="ContentType"/>s in the system.
        /// </summary>
        public static ContentType[] GetContentTypes()
        {
            return ContentTypeManager.Instance.GetContentTypes();
        }
        /// <summary>
        /// Returns an array of all <see cref="ContentType"/> names.
        /// </summary>
        public static string[] GetContentTypeNames()
        {
            return ContentTypeManager.Instance.GetContentTypeNames();
        }

        /// <summary>
        /// Returns the ContentTypeDefinition XML.
        /// </summary>
        public string ToXml()
        {
            Stream stream = this.Binary.GetStream();
            long pos = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(stream);
            string xml = reader.ReadToEnd();
            stream.Seek(pos, SeekOrigin.Begin);
            reader.Close();
            return xml;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Provides information about <see cref="ContentType"/> 
        /// inheritance hierarchy for debugger users only.
        /// </summary>
        internal static string TraceContentSchema()
        {
            return ContentTypeManager.Instance.TraceContentSchema();
        }

        /// <summary>
        /// Gets indexing information of the <see cref="Field"/> identified by the given name.
        /// </summary>
        /// <returns>An existing <see cref="IPerFieldIndexingInfo"/> instance or null.</returns>
        public static IPerFieldIndexingInfo GetPerfieldIndexingInfo(string fieldName)
        {
            return ContentTypeManager.GetPerFieldIndexingInfo(fieldName);
        }

        // ==================================================== IIndexable Members

        /// <summary>
        /// Returns the fields that can be found in the index of instances of this <see cref="ContentType"/>.
        /// This list does not contain the fields defined in the ContentTypeDefinition xml, but
        /// the fields of this Content itself.
        /// </summary>
        public virtual IEnumerable<IIndexableField> GetIndexableFields()
        {
            return Content.Create(this).Fields.Values.Where(f => f.IsInIndex).Cast<IIndexableField>();
        }

        // ======================================================================================================= Runtime ContentType

        internal static ContentType Create(Type type, string ctd)
        {
            var contentType = new ContentType(ContentType.GetByName("GenericContent"));

            var reader = new StringReader(ctd);
            var xml = new XPathDocument(reader);
            var nametable = new NameTable();
            var nav = xml.CreateNavigator();
            var nsres = new XmlNamespaceManager(nav.NameTable);
            nsres.AddNamespace("x", ContentDefinitionXmlNamespace);
            var contentTypeElement = nav.Select("/x:ContentType", nsres);
            contentTypeElement.MoveNext();
            var contentTypeName = contentTypeElement.Current.GetAttribute("name", "");
            contentType.Name = contentTypeName;
            var fieldElements = nav.Select("/x:ContentType/x:Fields/x:Field", nsres);
            foreach (XPathNavigator fieldElement in fieldElements)
            {
                var fieldDescriptor = FieldDescriptor.Parse(fieldElement, nsres, contentType);
                var fieldSetting = FieldSetting.Create(fieldDescriptor);
                contentType.FieldSettings.Add(fieldSetting);
            }
            contentType.SetFieldSlots(type);
            return contentType;
        }
        internal static ContentType Create(ISupportsDynamicFields handler, ContentType baseContentType)
        {
            var ctd = DynamicContentTools.GenerateCtd(handler, baseContentType);
            return Create(handler, baseContentType, ctd);
        }
        private static ContentType Create(ISupportsDynamicFields handler, ContentType baseContentType, string ctd)
        {
            var contentType = new ContentType(baseContentType);
            contentType.DisplayName = baseContentType.DisplayName;
            contentType.Description = baseContentType.Description;

            contentType.ParentType = baseContentType;

            var reader = new StringReader(ctd);
            var xml = new XPathDocument(reader);
            var nametable = new NameTable();
            var nav = xml.CreateNavigator();
            var nsres = new XmlNamespaceManager(nav.NameTable);
            nsres.AddNamespace("x", ContentDefinitionXmlNamespace);
            var contentTypeElement = nav.Select("/x:ContentType", nsres);
            contentTypeElement.MoveNext();
            var contentTypeName = contentTypeElement.Current.GetAttribute("name", "");
            contentType.Name = contentTypeName;
            var fieldElements = nav.Select("/x:ContentType/x:Fields/x:Field", nsres);
            var newFieldNames = new List<string>();
            foreach (XPathNavigator fieldElement in fieldElements)
            {
                var fieldDescriptor = FieldDescriptor.Parse(fieldElement, nsres, contentType);
                var fieldSetting = FieldSetting.Create(fieldDescriptor);
                contentType.SetFieldSlots(fieldSetting, handler);
                contentType.FieldSettings.Add(fieldSetting);
                newFieldNames.Add(fieldSetting.Name);
            }

            var baseFields = new List<FieldSetting>();
            foreach(var baseField in baseContentType.FieldSettings)
                if(!newFieldNames.Contains(baseField.Name))
                    baseFields.Add(baseField);
            contentType.FieldSettings.InsertRange(0, baseFields);
            contentType.FinalizeAllowedChildTypes(ContentTypeManager.Instance.ContentTypes, ContentTypeManager.Instance.AllFieldNames);

            return contentType;
        }
        private void SetFieldSlots(FieldSetting fieldSetting, ISupportsDynamicFields handler)
        {
            if (fieldSetting.DataTypes.Length == 0)
                return;
            var handlerType = handler.GetType();
            Type[][] slots = fieldSetting.HandlerSlots;
            var dynamicFieldMetadata = handler.GetDynamicFieldMetadata();
            for (int i = 0; i < fieldSetting.Bindings.Count; i++)
            {
                string propName = fieldSetting.Bindings[i];
                PropertyInfo propInfo = null;
                propInfo = handlerType.GetProperty(propName);
                Type propertyType = null;
                bool readOnly = false;
                if (propInfo != null)
                {
                    // code property
                    propertyType = propInfo.PropertyType;
                    readOnly = readOnly || !ContentTypeManager.PropertyHasPublicSetter(propInfo);
                }
                else
                {
                    // generic property
                    propertyType = dynamicFieldMetadata[propName].PropertyType;
                    readOnly = !dynamicFieldMetadata[propName].CanWrite;
                }
                for (int j = 0; j < slots[i].Length; j++)
                {
                    if (slots[i][j].IsAssignableFrom(propertyType))
                    {
                        fieldSetting.HandlerSlotIndices[i] = j;
                        fieldSetting.PropertyIsReadOnly = readOnly;
                        break;
                    }
                }
            }
            fieldSetting.Initialize();
        }

        // =======================================================================================================

        /// <summary>
        /// Defines an event that occurs when the ContentType system is restarted.
        /// </summary>
        public static event EventHandler TypeSystemRestarted;
        internal static void OnTypeSystemRestarted()
        {
            if (TypeSystemRestarted != null)
                TypeSystemRestarted(null, EventArgs.Empty);
        }

        // =======================================================================================================

        /// <summary>
        /// Provides information about indexing of all the fields that are configured in ContentTypeDefinitions.
        /// </summary>
        /// <returns>String containing a tab separated table of the indexing configurations.</returns>
        public static string TracePerFieldIndexingInfo()
        {
            var items = (from ct in ContentType.GetContentTypes()
                                from ft in ct.FieldSettings
                                select new { Name = ft.Name, ShortName = ft.ShortName}).Distinct().ToArray();
            var fsettinginfo = new Dictionary<string, string>();
            foreach (var item in items)
                if (!fsettinginfo.ContainsKey(item.Name))
                    fsettinginfo.Add(item.Name, item.ShortName);

            var sb = new StringBuilder();
            sb.AppendLine("Field\tFieldType\tMode\tStore\tTermVector\tAnalyzer\tIndexFieldHandler");
            sb.Append("[default]\t\t");
            sb.Append(PerFieldIndexingInfo.DefaultIndexingMode).Append("\t");
            sb.Append(PerFieldIndexingInfo.DefaultIndexStoringMode).Append("\t");
            sb.Append(PerFieldIndexingInfo.DefaultTermVectorStoringMode).Append("\t");
            sb.AppendLine("[null]");
            foreach (var item in ContentTypeManager.GetPerFieldIndexingInfo())
            {
                sb.Append(item.Key).Append("\t");
                if(fsettinginfo.ContainsKey(item.Key))
                    sb.Append(fsettinginfo[item.Key]).Append("\t");
                else
                    sb.Append("\t").Append("\t");
                sb.Append(item.Value.IndexingMode).Append("\t");
                sb.Append(item.Value.IndexStoringMode).Append("\t");
                sb.Append(item.Value.TermVectorStoringMode).Append("\t");
                sb.Append(item.Value.Analyzer).Append("\t");
                sb.AppendLine(item.Value.IndexFieldHandler?.GetType().FullName?.Replace("SenseNet.Search.", string.Empty) ?? "[null]");
            }
            return sb.ToString();
        }
    }
}
