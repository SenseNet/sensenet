using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using System.Xml.XPath;
using System.IO;
using System.Xml;
using SenseNet.ContentRepository.Storage.Events;
using System.Reflection;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Provides an additional field set for an individual <see cref="Content"/> in XML 
    /// format similar to a ContentType definition.
    /// </summary>
    [ContentHandler]
    public class Aspect : GenericContent
    {
        /// <summary>
        /// Defines the XML namespace of the AspectDefinition schema.
        /// The value is: "http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition"
        /// </summary>
        public static readonly string AspectDefinitionXmlNamespace = "http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition";
        private static string AspectDefinitionSchemaManifestResourceName = "SenseNet.ContentRepository.Schema.AspectDefinition.xsd";
        /// <summary>
        /// Defines an empty AspectDefinition.
        /// </summary>
        public static readonly string DefaultAspectDefinition = String.Concat("<AspectDefinition xmlns='", AspectDefinitionXmlNamespace, "'><Fields /></AspectDefinition>");

        private string _displayName;
        private string _description;
        private string _icon;

        /// <summary>
        /// Protected member that lets inheriting types access the raw value of the FieldSettings property.
        /// </summary>
        protected List<FieldSetting> _fieldSettings = new List<FieldSetting>();
        private Dictionary<string, PerFieldIndexingInfo> _indexingInfo = new Dictionary<string, PerFieldIndexingInfo>();

        // ================================================================================= Properties

        /// <summary>
        /// Defines a constant value for the name of the AspectDefinition property.
        /// </summary>
        public const string ASPECTDEFINITION = "AspectDefinition";
        /// <summary>
        /// Gets or sets the XML source of the AspectDefinition. Persisted as <see cref="RepositoryDataType.Text"/>.
        /// </summary>
        [RepositoryProperty(ASPECTDEFINITION, RepositoryDataType.Text)]
        public string AspectDefinition
        {
            get { return base.GetProperty<string>(ASPECTDEFINITION); }
            set
            {
                var doc = GetValidDocument(value);
                this[ASPECTDEFINITION] = value;
                Build(doc, true);
            }
        }

        /// <summary>
        /// Gets the list of <see cref="FieldSetting"/>s defined by the AspectDefinition xml.
        /// </summary>
        public List<FieldSetting> FieldSettings
        {
            get { return _fieldSettings; }
        }

        // ================================================================================= Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="Aspect"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public Aspect(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Aspect"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public Aspect(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Aspect"/> class in the loading procedure.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected Aspect(NodeToken nt) : base(nt) { }

        private void Build()
        {
            // only Loading calls
            string def = this.AspectDefinition;
            if (String.IsNullOrEmpty(def))
                return;
            Build(new XPathDocument(new StringReader(def)), false);
        }
        private void Build(IXPathNavigable definitionXml, bool modify)
        {
            XPathNavigator nav = definitionXml.CreateNavigator();
            XmlNamespaceManager nsres = new XmlNamespaceManager(nav.NameTable);
            XPathNavigator root = nav.SelectSingleNode("/*[1]", nsres);
            nsres.AddNamespace("x", root.NamespaceURI);

            Dictionary<string, FieldDescriptor> fieldDescriptorList = ParseRootElement(root, nsres);
            _fieldSettings = ManageAspectType(fieldDescriptorList);

            SetFieldSlots();

            base.SetCachedData(FIELDSETTINGSKEY, _fieldSettings);
            base.SetCachedData(INDEXINGINFOKEY, _indexingInfo);
        }

        private Dictionary<string, FieldDescriptor> ParseRootElement(XPathNavigator rootElement, IXmlNamespaceResolver nsres)
        {
            Dictionary<string, FieldDescriptor> result = null;
            foreach (XPathNavigator subElement in rootElement.SelectChildren(XPathNodeType.Element))
            {
                switch (subElement.LocalName)
                {
                    case "DisplayName":
                        _displayName = subElement.Value;
                        break;
                    case "Description":
                        _description = subElement.Value;
                        break;
                    case "Icon":
                        _icon = subElement.Value;
                        break;
                    case "Fields":
                        result = ParseFieldElements(subElement, nsres);
                        break;
                    case "Actions":
                        SnLog.WriteWarning("Ignoring obsolete Actions element in List definition: " + this.Name);
                        break;
                    default:
                        throw new NotSupportedException("Unknown element in AspectDefinition: " + subElement.LocalName);
                }
            }
            return result;
        }
        private Dictionary<string, FieldDescriptor> ParseFieldElements(XPathNavigator fieldsElement, IXmlNamespaceResolver nsres)
        {
            Dictionary<string, FieldDescriptor> fieldDescriptorList = new Dictionary<string, FieldDescriptor>();
            ContentType listType = ContentType.GetByName("Aspect");
            foreach (XPathNavigator fieldElement in fieldsElement.SelectChildren(XPathNodeType.Element))
            {
                FieldDescriptor fieldDescriptor = FieldDescriptor.Parse(fieldElement, nsres, listType);
                fieldDescriptorList.Add(fieldDescriptor.FieldName, fieldDescriptor);
            }
            return fieldDescriptorList;
        }
        private List<FieldSetting> ManageAspectType(Dictionary<string, FieldDescriptor> fieldInfoList)
        {
            var fieldSettings = new List<FieldSetting>();
            foreach (string name in fieldInfoList.Keys)
                fieldSettings.Add(FieldSetting.Create(fieldInfoList[name], this));
            return fieldSettings;
        }
        private FieldSetting CreateNewFieldType(FieldDescriptor fieldInfo)
        {
            return FieldSetting.Create(fieldInfo, new List<string>(), this);
        }
        /// <summary>
        /// Set field slot indexes and readonly flags for aspect fields.
        /// </summary>
        private void SetFieldSlots() 
        {
            // Field slot indices and readonly.
            foreach (FieldSetting fieldSetting in this.FieldSettings)
            {
                if (fieldSetting.DataTypes.Length == 0)
                    continue;
                Type[][] slots = fieldSetting.HandlerSlots;
                for (int i = 0; i < fieldSetting.Bindings.Count; i++)
                {
                    string propName = fieldSetting.Bindings[i];
                    Type propertyType = null;
                    bool readOnly = false;

                    // generic property
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

        // ================================================================================= Node

        /// <summary>
        /// Declares a constant for the aspect field separator. The value is: ".".
        /// </summary>
        public const string ASPECTFIELDSEPARATOR = ".";
        private static readonly char[] AspectFieldSeparatorSplitter = ASPECTFIELDSEPARATOR.ToCharArray();

        /// <summary>
        /// Returns the <see cref="FieldSettings"/> specified by the given fieldName.
        /// Return value is null if the <see cref="FieldSettings"/> does not exist.
        /// The fieldName structure: {AspectName}.{FieldSettingName}
        /// </summary>
        /// <param name="fieldName">Full name of the field: {AspectName}.{FieldSettingName}.</param>
        public static FieldSetting GetFieldSettingByFieldName(string fieldName)
        {
            string realFieldName;
            var aspect = LoadAspectByFieldName(fieldName, out realFieldName);
            if (aspect == null)
                return null;
            var fieldSetting = aspect.FieldSettings.Where(f => f.Name == realFieldName).FirstOrDefault();
            return fieldSetting;
        }
        /// <summary>
        /// Returns an existing <see cref="Aspect"/> by the given full name. If the <see cref="Aspect"/> 
        /// does not exist, returns null.
        /// The Aspect name will be removed from the full name and the remainder will be
        /// returned as the real field name output argument.
        /// </summary>
        /// <param name="fieldName">Full name of the field: {AspectName}.{FieldSettingName}</param>
        /// <param name="realFieldName">Output value of the FieldSettingName part of the given fieldName.</param>
        public static Aspect LoadAspectByFieldName(string fieldName, out string realFieldName)
        {
            realFieldName = null;
            var sa = fieldName.Split(AspectFieldSeparatorSplitter, StringSplitOptions.RemoveEmptyEntries);
            if (sa.Length != 2)
                return null;
            realFieldName = sa[1];
            return LoadAspectByName(sa[0]);
        }
        /// <summary>
        /// Returns an existing <see cref="Aspect"/> by the given name.
        /// If it is not found, returns null.
        /// </summary>
        /// <param name="name">Name of the <see cref="Aspect"/>.</param>
        public static Aspect LoadAspectByName(string name)
        {
            // IMPORTANT:
            // We can't use external query here because this method is called during indexing!
            // It would mean trying to query the index while writing to it, which would result in a deadlock.
            // For this reason, this method uses its own cache for finding aspects.

            // Try to find the aspect in cache
            var cacheKey = "SN_AspectCacheByName_" + name;
            var aspect = DistributedApplication.Cache.Get(cacheKey) as Aspect;

            if (aspect == null)
            {
                // Find aspect via node query.
                // DO NOT replace this call with either Linq or Content Query for the reasons detailed above!
                var result = NodeQuery.QueryNodesByTypeAndName(ActiveSchema.NodeTypes[typeof(Aspect).Name], false, name);
                aspect = result.Nodes.FirstOrDefault() as Aspect;

                // If not found, return null
                if (aspect == null)
                    return null;

                // Store in cache
                var dependency = CacheDependencyFactory.CreateNodeDependency(aspect);
                DistributedApplication.Cache.Insert(cacheKey, aspect, dependency);
            }

            return aspect;
        }
        /// <summary>
        /// Returns an existing <see cref="Aspect"/> by the given parameter.
        /// If it is not found, returns null.
        /// </summary>
        /// <param name="pathOrName">Path or name of the <see cref="Aspect"/>.</param>
        public static Aspect LoadAspectByPathOrName(string pathOrName)
        {
            return pathOrName.Contains('/') ? Node.LoadNode(pathOrName) as Aspect : Aspect.LoadAspectByName(pathOrName);
        }
        /// <summary>
        /// Returns true if the <see cref="Aspect"/> exists.
        /// </summary>
        /// <param name="name">Name of the <see cref="Aspect"/>.</param>
        public static bool AspectExists(string name)
        {
            if (SearchManager.ContentQueryIsAllowed)
                return ContentQuery.Query(SafeQueries.AspectExists, null, name).Count > 0;
            return LoadAspectByName(name) != null;
        }

        /// <summary>
        /// Persists the modifications of this Content.
        /// Do not use this method directly from your code.
        /// If the AspectDefinition is invalid, <see cref="InvalidContentException"/> will be thrown.
        /// Also throws an <see cref="InvalidContentException"/> if the Path of the instance is not under the Aspects container.
        /// </summary>
        public override void Save(SavingMode mode)
        {
            Validate();
            base.Save(mode);
        }
        private void Validate()
        {
            if (String.IsNullOrEmpty(this.AspectDefinition))
                this.AspectDefinition = DefaultAspectDefinition;

            var parentPath = this.ParentPath;
            if (String.Compare(parentPath, Repository.AspectsFolderPath, true) != 0)
                if (!parentPath.StartsWith(Repository.AspectsFolderPath + "/"))
                    throw new InvalidContentException(String.Concat("Aspect can only be under the '", Repository.AspectsFolderPath, "' folder."));

            try
            {
                Build();
            }
            catch (Exception e)
            {
                throw new InvalidContentException("Invalid Aspect. " + e.Message, e);
            }
        }

        private static object _saveSync = new object();
        /// <summary>
        /// Persist this Content's changes by the given settings.
        /// Do not use this method directly from your code.
        /// </summary>
        /// <param name="settings"><see cref="NodeSaveSettings"/> that contains the persistence algorithm.</param>
        public override void Save(NodeSaveSettings settings)
        {
            if (this.IsNew)
                this.Parent.Security.Assert(PermissionType.ManageListsAndWorkspaces);
            else
                this.Security.Assert(PermissionType.ManageListsAndWorkspaces);

            if (this.Id > 0)
            {
                base.Save(settings);
                return;
            }

            Aspect existingAspect = null;
            lock (_saveSync)
            {
                if ((existingAspect = LoadAspectByName(this.Name)) == null)
                {
                    base.Save(settings);
                    return;
                }
            }
            throw new InvalidOperationException(String.Concat("Cannot create new Aspect because another Aspect exists with same name: ", existingAspect.Path));
        }

        /// <summary>
        /// Deletes this <see cref="Aspect"/> permanently.
        /// The logged-in user need to have ManageListsAndWorkspaces permission,
        /// otherwise <see cref="SenseNetSecurityException"/> will be thrown.
        /// </summary>
        public override void ForceDelete()
        {
            Security.Assert(PermissionType.ManageListsAndWorkspaces);
            base.ForceDelete();
        }

        // ================================================================================= Generic Property handling

        /// <inheritdoc/>
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case ASPECTDEFINITION:
                    return this.AspectDefinition;
                default:
                    return base.GetProperty(name);
            }
        }
        /// <inheritdoc/>
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case ASPECTDEFINITION:
                    this.AspectDefinition = (string)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        // ================================================================================= Xml validation

        private IXPathNavigable GetValidDocument(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                xml = DefaultAspectDefinition;

            var doc = new XPathDocument(new StringReader(xml));
            CheckValidation(doc);
            return doc;
        }
        private static void CheckValidation(IXPathNavigable xml)
        {
            var schema = XmlValidator.LoadFromManifestResource(Assembly.GetExecutingAssembly(), AspectDefinitionSchemaManifestResourceName);
            if (!schema.Validate(xml))
            {
                if (schema.Errors.Count == 0)
                    throw new ContentRegistrationException(SR.Exceptions.Registration.Msg_InvalidAspectDefinitionXml);
                else
                    throw new ContentRegistrationException(String.Concat(
                        SR.Exceptions.Registration.Msg_InvalidAspectDefinitionXml, ": ", schema.Errors[0].Exception.Message),
                        schema.Errors[0].Exception);
            }
        }

        // ================================================================================= Tools

        /// <summary>
        /// Tool method that returns <see cref="FieldSettings"/> by the specified name from the given list.
        /// </summary>
        /// <param name="fieldName">Name of the desired <see cref="FieldSettings"/>.</param>
        /// <param name="fieldSettings">The list that will be enumerated.</param>
        protected static FieldSetting GetFieldTypeByName(string fieldName, List<FieldSetting> fieldSettings)
        {
            int i = GetFieldTypeIndexByName(fieldName, fieldSettings);
            return i < 0 ? null : fieldSettings[i];
        }
        /// <summary>
        /// Tool method that returns the index of the <see cref="FieldSettings"/> by the specified name in the given list.
        /// </summary>
        /// <param name="fieldName">Index of the desired <see cref="FieldSettings"/> in the given list.</param>
        /// <param name="fieldSettings">The list that will be enumerated.</param>
        protected static int GetFieldTypeIndexByName(string fieldName, List<FieldSetting> fieldSettings)
        {
            for (int i = 0; i < fieldSettings.Count; i++)
                if (fieldSettings[i].Name == fieldName)
                    return i;
            return -1;
        }

        // ================================================================================= Field operations

        /// <inheritdoc />
        public override List<FieldSetting> GetAvailableFields(bool rootFields)
        {
            var availableFields = base.GetAvailableFields(rootFields);

            foreach (var fieldSetting in this.FieldSettings)
            {
                var fsRoot = FieldSetting.GetRoot(fieldSetting);

                if (!availableFields.Contains(fsRoot))
                    availableFields.Add(fsRoot);
            }

            return availableFields;
        }

        /// <summary>
        /// Adds one or more new fields to this instance or replaces existing ones and saves the modifications.
        /// </summary>
        /// <param name="fieldInfos">Array of <see cref="Schema.FieldInfo"/>s that will be added.</param>
        public void AddFields(params SenseNet.ContentRepository.Schema.FieldInfo[] fieldInfos)
        {
            foreach(var fieldInfo in fieldInfos)
                AddFieldInternal(FieldSetting.Create(fieldInfo, this));
            Build();
            Save();
        }
        /// <summary>
        /// Removes the specified fields of his instance and saves the modifications.
        /// </summary>
        /// <param name="fieldNames">Array of the field names that will be removed.</param>
        public void RemoveFields(params string[] fieldNames)
        {
            foreach(var fieldName in fieldNames)
            for (int i = 0; i < fieldNames.Length; i++)
                DeleteFieldInternal(fieldName);
            Build();
            Save();
        }
        /// <summary>
        /// Empties the field collection of this instance and saves it.
        /// </summary>
        public void RemoveAllfields()
        {
            AspectDefinition = DefaultAspectDefinition;
            Build();
            Save();
        }
        private void AddFieldInternal(FieldSetting fieldSetting)
        {
            if (string.IsNullOrEmpty(this.AspectDefinition))
                this.AspectDefinition = DefaultAspectDefinition;

            fieldSetting.Aspect = this;

            DeleteFieldInternal(fieldSetting.Name);

            var doc = new XmlDocument();
            doc.LoadXml(this.AspectDefinition);
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("x", AspectDefinitionXmlNamespace);

            var fields = doc.DocumentElement.SelectSingleNode("/x:AspectDefinition/x:Fields", nsmgr);

            using (var writer = fields.CreateNavigator().AppendChild())
            {
                fieldSetting.WriteXml(writer);
            }

            this.AspectDefinition = doc.OuterXml;
        }
        private void DeleteFieldInternal(string fieldName)
        {
            var p = fieldName.LastIndexOf(ASPECTFIELDSEPARATOR);
            if (p >= 0)
                fieldName = fieldName.Substring(p + 1);

            XmlDocument doc;
            var node = FindFieldXmlNode(fieldName, out doc);

            if (node == null)
                return;

            node.ParentNode.RemoveChild(node);

            this.AspectDefinition = doc.OuterXml;
        }
        private XmlNode FindFieldXmlNode(string fieldName, out XmlDocument doc)
        {
            doc = new XmlDocument();
            doc.LoadXml(this.AspectDefinition);

            if (string.IsNullOrEmpty(fieldName))
                return null;

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("x", AspectDefinitionXmlNamespace);

            var xTemplate = string.Format("/x:AspectDefinition/x:Fields/x:AspectField[@name='{0}']", fieldName);

            return string.IsNullOrEmpty(fieldName) ? null :
                doc.DocumentElement.SelectSingleNode(xTemplate, nsmgr);
        }

        /*================================================================================= Cached data */

        private const string FIELDSETTINGSKEY = "FieldSettings";
        private const string INDEXINGINFOKEY = "IndexingInfo";
        /// <summary>
        /// Overrides the base class behavior. Builds internal structures.
        /// Do not use this method directly from your code.
        /// </summary>
        protected override void OnLoaded(object sender, NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            _fieldSettings = (List<FieldSetting>)base.GetCachedData(FIELDSETTINGSKEY);
            _indexingInfo = (Dictionary<string, PerFieldIndexingInfo>)base.GetCachedData(INDEXINGINFOKEY);
            if (_indexingInfo == null)
                _indexingInfo = new Dictionary<string, PerFieldIndexingInfo>();

            if (_fieldSettings != null)
                return;

            Build();
        }

        /// <summary>
        /// Returns <see cref="PerFieldIndexingInfo"/> of the requested <see cref="FieldSettings"/>.
        /// Return value is null if the <see cref="Aspect"/> or <see cref="FieldSettings"/> does not exist.
        /// The fieldName structure: {AspectName}.{FieldSettingName}
        /// </summary>
        /// <param name="fieldName">Full name of the field: {AspectName}.{FieldSettingName}.</param>
        public static PerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            string realFieldName;
            var aspect = LoadAspectByFieldName(fieldName, out realFieldName);
            if (aspect == null)
                return null;
            return aspect.GetLocalPerFieldIndexingInfo(realFieldName);
        }
        /// <summary>
        /// Returns <see cref="PerFieldIndexingInfo"/> of the requested <see cref="FieldSettings"/>.
        /// Return value is null if the <see cref="FieldSettings"/> does not exist.
        /// </summary>
        /// <param name="fieldName">Name of the field without aspect specifier.</param>
        public PerFieldIndexingInfo GetLocalPerFieldIndexingInfo(string fieldName)
        {
            PerFieldIndexingInfo info = null;
            _indexingInfo.TryGetValue(fieldName, out info);
            return info;
        }
        internal void SetPerFieldIndexingInfo(string fieldName, PerFieldIndexingInfo indexingInfo)
        {
            _indexingInfo[fieldName] = indexingInfo;
        }
    }
}
