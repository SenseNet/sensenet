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
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class Aspect : GenericContent
    {
        public static readonly string AspectDefinitionXmlNamespace = "http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition";
        private static string AspectDefinitionSchemaManifestResourceName = "SenseNet.ContentRepository.Schema.AspectDefinition.xsd";
        public static readonly string DefaultAspectDefinition = String.Concat("<AspectDefinition xmlns='", AspectDefinitionXmlNamespace, "'><Fields /></AspectDefinition>");

        private string _displayName;
        private string _description;
        private string _icon;
        protected List<FieldSetting> _fieldSettings = new List<FieldSetting>();
        private Dictionary<string, PerFieldIndexingInfo> _indexingInfo = new Dictionary<string, PerFieldIndexingInfo>();

        // ================================================================================= Properties

        public const string ASPECTDEFINITION = "AspectDefinition";
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

        public List<FieldSetting> FieldSettings
        {
            get { return _fieldSettings; }
        }

        // ================================================================================= Construction

        public Aspect(Node parent) : this(parent, null) { }
        public Aspect(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
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
        protected virtual void SetFieldSlots()
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

        public const string ASPECTFIELDSEPARATOR = ".";
        private static readonly char[] AspectFieldSeparatorSplitter = ASPECTFIELDSEPARATOR.ToCharArray();

        public static FieldSetting GetFieldSettingByFieldName(string fieldName)
        {
            string realFieldName;
            var aspect = LoadAspectByFieldName(fieldName, out realFieldName);
            if (aspect == null)
                return null;
            var fieldSetting = aspect.FieldSettings.Where(f => f.Name == realFieldName).FirstOrDefault();
            return fieldSetting;
        }
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
        /// Loads an aspect by its name.
        /// </summary>
        public static Aspect LoadAspectByName(string name)
        {
            // IMPORTANT:
            // We can't use Lucene query here because this method is called during indexing!
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
        public static Aspect LoadAspectByPathOrName(string pathOrName)
        {
            return pathOrName.Contains('/') ? Node.LoadNode(pathOrName) as Aspect : Aspect.LoadAspectByName(pathOrName);
        }
        public static bool AspectExists(string name)
        {
            if (StorageContext.Search.ContentQueryIsAllowed)
                return ContentQuery_NEW.Query(SafeQueries.AspectExists, null, name).Count > 0;
            return LoadAspectByName(name) != null;
        }

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

        public override void ForceDelete()
        {
            Security.Assert(PermissionType.ManageListsAndWorkspaces);
            base.ForceDelete();
        }

        // ================================================================================= Generic Property handling

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

        protected static FieldSetting GetFieldTypeByName(string fieldName, List<FieldSetting> fieldSettings)
        {
            int i = GetFieldTypeIndexByName(fieldName, fieldSettings);
            return i < 0 ? null : fieldSettings[i];
        }
        protected static int GetFieldTypeIndexByName(string fieldName, List<FieldSetting> fieldSettings)
        {
            for (int i = 0; i < fieldSettings.Count; i++)
                if (fieldSettings[i].Name == fieldName)
                    return i;
            return -1;
        }

        // ================================================================================= Field operations

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

        public void AddFields(params SenseNet.ContentRepository.Schema.FieldInfo[] fieldInfos)
        {
            foreach(var fieldInfo in fieldInfos)
                AddFieldInternal(FieldSetting.Create(fieldInfo, this));
            Build();
            Save();
        }
        public void RemoveFields(params string[] fieldNames)
        {
            foreach(var fieldName in fieldNames)
            for (int i = 0; i < fieldNames.Length; i++)
                DeleteFieldInternal(fieldName);
            Build();
            Save();
        }
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

        public static PerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            string realFieldName;
            var aspect = LoadAspectByFieldName(fieldName, out realFieldName);
            if (aspect == null)
                return null;
            return aspect.GetLocalPerFieldIndexingInfo(realFieldName);
        }
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
