using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Mail;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using System.Xml.XPath;
using System.IO;
using System.Xml;
using System.Reflection;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Data;
using System.Linq;
using SenseNet.ContentRepository.Storage.Search.Internal;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Search;
using System.Diagnostics;
using System.Globalization;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Fields;
using SenseNet.Search;
using SenseNet.Tools;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class ContentList : Folder, IContentList, ISupportsVirtualChildren
    {
        private class SlotTable
        {
            private Dictionary<DataType, List<int>> _slotTable;
            private Dictionary<DataType, int> _currentSlots;

            public SlotTable(Dictionary<string, List<string>> bindings)
            {
                _slotTable = new Dictionary<DataType, List<int>>();
                _currentSlots = new Dictionary<DataType, int>();
                foreach (DataType dataType in Enum.GetValues(typeof(DataType)))
                {
                    _slotTable.Add(dataType, new List<int>());
                    _currentSlots.Add(dataType, -1);
                }
                foreach (string key in bindings.Keys)
                {
                    foreach (string binding in bindings[key])
                    {
                        DataType dataType;
                        int ordinalNumber;
                        ContentList.DecodeBinding(binding, out dataType, out ordinalNumber);
                        _slotTable[dataType].Add(ordinalNumber);
                    }
                }
            }

            public int ReserveSlot(DataType dataType)
            {
                List<int> slots = _slotTable[dataType];
                int currentSlot = _currentSlots[dataType];
                while (slots.Contains(++currentSlot)) ;
                slots.Add(currentSlot);
                _currentSlots[dataType] = currentSlot;
                return currentSlot;
            }
        }

        private static readonly string ContentListDefinitionXmlNamespaceOld = "http://schemas.sensenet" + ".hu/SenseNet/ContentRepository/Lis" + "terTypeDefinition";
        public static readonly string ContentListDefinitionXmlNamespace = "http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition";
        private static string ContentListDefinitionSchemaManifestResourceName = "SenseNet.ContentRepository.Schema.ContentListDefinition.xsd";
        public static readonly string ContentListFileNameExtension = "ContentListDefinition";
        private static string DefaultContentListDefinition
        {
            get
            {
                return String.Concat("<ContentListDefinition xmlns='", ContentListDefinitionXmlNamespace, "'><Fields /></ContentListDefinition>");
            }
        }

        private string _displayName;
        private string _description;
        private string _icon;
        private List<FieldSetting> _fieldSettings;
        private ContentListType _contentListType;
        private string[] __listFieldNames;

        // ================================================================================= Properties

        [RepositoryProperty("ContentListBindings", RepositoryDataType.Text)]
        public Dictionary<string, List<string>> ContentListBindings
        {
            get { return ParseBindingsXml(base.GetProperty<string>("ContentListBindings")); }
            set { this["ContentListBindings"] = CreateBindingsXml(value); }
        }
        [RepositoryProperty("ContentListDefinition", RepositoryDataType.Text)]
        public string ContentListDefinition
        {
            get { return base.GetProperty<string>("ContentListDefinition"); }
            set
            {
                var doc = GetValidDocument(value);
                this["ContentListDefinition"] = value;
                Build(doc, this.ContentListBindings, true);
            }
        }

        public string[] ListFieldNames
        {
            get
            {
                if (__listFieldNames == null)
                    __listFieldNames = this.ContentListBindings.Keys.ToArray();
                return __listFieldNames;
            }
        }

        [RepositoryProperty("DefaultView")]
        public string DefaultView
        {
            get { return GetProperty<string>("DefaultView"); }
            set { this["DefaultView"] = value; }
        }

        public List<FieldSetting> FieldSettings
        {
            get { return _fieldSettings; }
        }

        public IEnumerable<Node> FieldSettingContents
        {
            get
            {
                return from fs in this.FieldSettings
                       where ActiveSchema.NodeTypes[fs.GetType().Name] != null
                       select new FieldSettingContent(fs.GetEditable(), this) as Node;
            }
        }

        public IEnumerable<Node> AvailableContentTypeFieldSettingContents
        {
            get
            {
                var availableFields = new List<FieldSetting>();
                var fsContents = new List<Node>();

                GetAvailableContentTypeFields(availableFields);

                foreach (var fs in availableFields)
                {
                    try
                    {
                        // nodetype check is needed here because newly created
                        // field types doesn't necessary have a ctd
                        if ((fs.VisibleBrowse != FieldVisibility.Hide ||
                             fs.VisibleEdit != FieldVisibility.Hide ||
                             fs.VisibleNew != FieldVisibility.Hide) && ActiveSchema.NodeTypes[fs.GetType().Name] != null)
                            fsContents.Add(new FieldSettingContent(fs.GetEditable(), this));
                    }
                    catch (RegistrationException ex)
                    {
                        // ctd doesn't exist for this field type
                        SnLog.WriteException(ex);
                    }
                }

                return fsContents;
            }
        }

        public const string AVAILABLEVIEWS = "AvailableViews";
        [RepositoryProperty(AVAILABLEVIEWS, RepositoryDataType.Reference)]
        public IEnumerable<Node> AvailableViews
        {
            get
            {
                return this.GetReferences(AVAILABLEVIEWS);

            }
            set
            {
                this.SetReferences(AVAILABLEVIEWS, value);
            }
        }

        // ================================================================================= Construction

        public ContentList(Node parent) : this(parent, null) { }
        public ContentList(Node parent, string nodeTypeName) : base(parent, nodeTypeName)
        {
            Initialize();
        }
        protected ContentList(NodeToken nt) : base(nt)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            base.Initialize();
            _fieldSettings = new List<FieldSetting>();
        }
        private void Build()
        {
            // only Loading calls
            string def = this.ContentListDefinition;
            if (String.IsNullOrEmpty(def))
                return;
            Build(new XPathDocument(new StringReader(def)), this.ContentListBindings, false);
        }
        private void Build(IXPathNavigable definitionXml, Dictionary<string, List<string>> bindings, bool modify)
        {
            XPathNavigator nav = definitionXml.CreateNavigator();
            XmlNamespaceManager nsres = new XmlNamespaceManager(nav.NameTable);
            XPathNavigator root = nav.SelectSingleNode("/*[1]", nsres);
            nsres.AddNamespace("x", root.NamespaceURI);
            List<FieldSetting> fieldSettings;

            Dictionary<string, FieldDescriptor> fieldDescriptorList = ParseContentTypeElement(root, nsres);
            _contentListType = ManageContentListType(fieldDescriptorList, bindings, modify, out fieldSettings);

            _fieldSettings = fieldSettings;
            SetFieldSlots();
        }

        private Dictionary<string, FieldDescriptor> ParseContentTypeElement(XPathNavigator contentTypeElement, IXmlNamespaceResolver nsres)
        {
            Dictionary<string, FieldDescriptor> result = null;
            foreach (XPathNavigator subElement in contentTypeElement.SelectChildren(XPathNodeType.Element))
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
                        throw new NotSupportedException(String.Concat("Unknown element in ContentListDefinition: ", subElement.LocalName));
                }
            }
            return result;
        }
        private Dictionary<string, FieldDescriptor> ParseFieldElements(XPathNavigator fieldsElement, IXmlNamespaceResolver nsres)
        {
            Dictionary<string, FieldDescriptor> fieldDescriptorList = new Dictionary<string, FieldDescriptor>();
            ContentType listType = ContentType.GetByName("ContentList");
            foreach (XPathNavigator fieldElement in fieldsElement.SelectChildren(XPathNodeType.Element))
            {
                FieldDescriptor fieldDescriptor = FieldDescriptor.Parse(fieldElement, nsres, listType);
                fieldDescriptorList.Add(fieldDescriptor.FieldName, fieldDescriptor);
            }
            return fieldDescriptorList;
        }
        private ContentListType ManageContentListType(Dictionary<string, FieldDescriptor> fieldInfoList, Dictionary<string, List<string>> oldBindings, bool modify, out List<FieldSetting> fieldSettings)
        {
            var attempts = 0;
            while (true)
            {
                try
                {
                    return ManageContentListTypeOneAttempt(fieldInfoList, oldBindings, modify, out fieldSettings);
                }
                catch (Exception e)
                {
                    if (!e.Message.Contains("Storage schema is out of date") || attempts++ >= 42)
                        throw;
                }
                var timer = Stopwatch.StartNew();
                ActiveSchema.Reload();
                ContentTypeManager.Reload();
                timer.Stop();
                var d = timer.Elapsed;
                var timeString = $"{d.Minutes}:{d.Seconds}.{d.Milliseconds}";
                SnLog.WriteInformation(
                    $"Type system is reloaded because it was out of date during managing a list type. Attempt: {attempts}, reloading time: {timeString}",
                    EventId.RepositoryRuntime);
            }
        }
        private ContentListType ManageContentListTypeOneAttempt(Dictionary<string, FieldDescriptor> fieldInfoList, Dictionary<string, List<string>> oldBindings, bool modify, out List<FieldSetting> fieldSettings)
        {
            fieldSettings = new List<FieldSetting>();
            if (!modify)
            {
                // Load
                foreach (string name in fieldInfoList.Keys)
                    fieldSettings.Add(FieldSetting.Create(fieldInfoList[name], oldBindings[name], null));
                return this.ContentListType;
            }

            SchemaEditor editor = new SchemaEditor();
            editor.Load();
            bool hasChanges = false;
            var listType = this.ContentListType;
            Dictionary<string, List<string>> newBindings = new Dictionary<string, List<string>>();
            SlotTable slotTable = new SlotTable(oldBindings);
            if (listType == null)
            {
                // new
                listType = editor.CreateContentListType(Guid.NewGuid().ToString());
                foreach (string name in fieldInfoList.Keys)
                    fieldSettings.Add(CreateNewFieldType(fieldInfoList[name], newBindings, listType, slotTable, editor));
                hasChanges = true;
            }
            else
            {
                // merge
                listType = editor.ContentListTypes[listType.Name];
                hasChanges |= RemoveUnusedFields(fieldInfoList, oldBindings, listType, editor);
                foreach (string name in fieldInfoList.Keys)
                {
                    FieldSetting origField = GetFieldTypeByName(name, _fieldSettings);
                    if (origField == null)
                    {
                        fieldSettings.Add(CreateNewFieldType(fieldInfoList[name], newBindings, listType, slotTable, editor));
                        hasChanges = true;
                    }
                    else
                    {
                        List<string> bindList = new List<string>(origField.Bindings.ToArray());
                        fieldSettings.Add(FieldSetting.Create(fieldInfoList[name], bindList, null));
                        newBindings.Add(name, bindList);
                    }
                }
            }
            if (hasChanges)
                editor.Register();
            this.ContentListBindings = newBindings;
            return ActiveSchema.ContentListTypes[listType.Name];
        }
        private FieldSetting CreateNewFieldType(FieldDescriptor fieldInfo, Dictionary<string, List<string>> newBindings, ContentListType listType, SlotTable slotTable, SchemaEditor editor)
        {
            List<string> bindList = new List<string>();
            foreach (RepositoryDataType slotType in FieldManager.GetDataTypes(fieldInfo.FieldTypeShortName))
            {
                if (slotType == RepositoryDataType.NotDefined)
                    continue;
                int slotNumber = slotTable.ReserveSlot((DataType)slotType);
                string binding = EncodeBinding(slotType, slotNumber);
                bindList.Add(binding);

                PropertyType pt = editor.PropertyTypes[binding];
                if (pt == null)
                    pt = editor.CreateContentListPropertyType((DataType)slotType, slotNumber);
                editor.AddPropertyTypeToPropertySet(pt, listType);
            }
            newBindings.Add(fieldInfo.FieldName, bindList);

            return FieldSetting.Create(fieldInfo, bindList, null);
        }
        private bool RemoveUnusedFields(Dictionary<string, FieldDescriptor> fieldInfoList, Dictionary<string, List<string>> oldBindings, ContentListType listType, SchemaEditor editor)
        {
            bool hasChanges = false;
            for (int i = _fieldSettings.Count - 1; i >= 0; i--)
            {
                FieldSetting oldType = _fieldSettings[i];
                bool needtoDelete = !fieldInfoList.ContainsKey(oldType.Name);
                if (!needtoDelete)
                {
                    FieldDescriptor newType = fieldInfoList[oldType.Name];
                    if (oldType.DataTypes.Length != newType.DataTypes.Length)
                    {
                        needtoDelete = true;
                    }
                    else
                    {
                        for (int j = 0; j < oldType.DataTypes.Length; j++)
                        {
                            if (oldType.DataTypes[j] != newType.DataTypes[j])
                            {
                                needtoDelete = true;
                                break;
                            }
                        }
                    }
                }
                if (needtoDelete)
                {
                    hasChanges = true;
                    foreach (string binding in oldType.Bindings)
                    {
                        PropertyType oldPropertyType = editor.PropertyTypes[binding];
                        editor.RemovePropertyTypeFromPropertySet(oldPropertyType, listType);
                    }
                    _fieldSettings.RemoveAt(i);
                    oldBindings.Remove(oldType.Name);
                }
            }
            return hasChanges;
        }
        private void SetFieldSlots()
        {
            //  Field slot indices and readonly.
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

        /*================================================================================= Node, IContentList */

        public override void Save(SavingMode mode)
        {
            if (String.IsNullOrEmpty(this.ContentListDefinition))
                this.ContentListDefinition = DefaultContentListDefinition;

            base.Save(mode);
        }

        public override void Save(NodeSaveSettings settings)
        {
            if (this.IsNew)
                SecurityHandler.Assert(this.ParentId, PermissionType.ManageListsAndWorkspaces);
            else
                this.Security.Assert(PermissionType.ManageListsAndWorkspaces);

            AssertEmail();

            var newEmail = this["ListEmail"] as string;
            var listEmailChanged = (IsNew && !string.IsNullOrEmpty(newEmail)) || (!IsNew && IsPropertyChanged("ListEmail"));
            if (listEmailChanged)
                SetAllowedChildTypesForEmails();

            base.Save(settings);

            if (listEmailChanged)
            {
                using (new SystemAccount())
                {
                    // remove current mail processor workflow
                    RemoveWorkflow();

                    // start new workflow + subscription if email is given
                    if (!string.IsNullOrEmpty(newEmail))
                        StartSubscription();
                }
            }
        }

        public override void ForceDelete()
        {
            Security.Assert(PermissionType.ManageListsAndWorkspaces);
            base.ForceDelete();
        }

        public ContentListType GetContentListType()
        {
            return _contentListType;
        }

        // ================================================================================= Generic Property handling

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "ContentListDefinition":
                    return this.ContentListDefinition;
                case "FieldSettingContents":
                    return this.FieldSettingContents;
                case "AvailableContentTypeFields":
                    return this.AvailableContentTypeFieldSettingContents;
                case "DefaultView":
                    return this.DefaultView;
                case AVAILABLEVIEWS:
                    return this.AvailableViews;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "ContentListDefinition":
                    this.ContentListDefinition = (string)value;
                    break;
                case "FieldSettingContents":
                case "AvailableContentTypeFields":
                    break;
                case "DefaultView":
                    this.DefaultView = (string)value;
                    break;
                case AVAILABLEVIEWS:
                    this.AvailableViews = (IEnumerable<Node>)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        // ================================================================================= Copy

        protected override void CopyDynamicProperties(Node target)
        {
            var content = (GenericContent)target;
            foreach (var propType in this.PropertyTypes)
                if (propType.Name != "ContentListBindings" && !EXCLUDED_COPY_PROPERTIES.Contains(propType.Name))
                    if (!propType.IsContentListProperty || target.PropertyTypes[propType.Name] != null)
                        content.SetProperty(propType.Name, this.GetProperty(propType.Name));
        }

        // ================================================================================= Xml validation

        private IXPathNavigable GetValidDocument(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                xml = DefaultContentListDefinition;

            if (RepositoryEnvironment.BackwardCompatibilityXmlNamespaces)
                xml = xml.Replace(ContentListDefinitionXmlNamespaceOld, ContentListDefinitionXmlNamespace);

            var doc = new XPathDocument(new StringReader(xml));
            CheckValidation(doc);
            return doc;
        }
        private static void CheckValidation(IXPathNavigable xml)
        {
            var schema = XmlValidator.LoadFromManifestResource(Assembly.GetExecutingAssembly(), ContentListDefinitionSchemaManifestResourceName);
            if (!schema.Validate(xml))
            {
                if (schema.Errors.Count == 0)
                    throw new ContentRegistrationException(SR.Exceptions.Registration.Msg_InvalidContentListDefinitionXml);
                else
                    throw new ContentRegistrationException(String.Concat(
                        SR.Exceptions.Registration.Msg_InvalidContentListDefinitionXml, ": ", schema.Errors[0].Exception.Message),
                        schema.Errors[0].Exception);
            }
        }

        // ================================================================================= Tools

        private static FieldSetting GetFieldTypeByName(string fieldName, List<FieldSetting> fieldSettings)
        {
            int i = GetFieldTypeIndexByName(fieldName, fieldSettings);
            return i < 0 ? null : fieldSettings[i];
        }
        private static int GetFieldTypeIndexByName(string fieldName, List<FieldSetting> fieldSettings)
        {
            for (int i = 0; i < fieldSettings.Count; i++)
                if (fieldSettings[i].Name == fieldName)
                    return i;
            return -1;
        }

        private string CreateBindingsXml(Dictionary<string, List<string>> bindingList)
        {
			// <?xml version="1.0" encoding="utf-8"?>
			// <Bindings>
			// 	<Bind field="[fieldName]">[propName] [propName]</Bind>
			// 	<Bind field="[fieldName]">[propName] [propName]</Bind>
			// </Bindings>
            StringBuilder sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Bindings>");
            foreach (string name in bindingList.Keys)
            {
                List<string> propList = bindingList[name];
                sb.Append("\t<Bind field=\"").Append(name).Append("\">");
                for (int i = 0; i < propList.Count; i++)
                    sb.Append(i > 0 ? " " : "").Append(propList[i]);
                sb.Append("</Bind>\r\n");
            }
            sb.Append("</Bindings>");
            return sb.ToString();
        }
        private Dictionary<string, List<string>> ParseBindingsXml(string bindings)
        {
            Dictionary<string, List<string>> bindingList = new Dictionary<string, List<string>>();
            if (String.IsNullOrEmpty(bindings))
                return bindingList;
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(bindings);
            foreach (XmlNode node in xml.SelectNodes("/Bindings/Bind"))
                bindingList.Add(node.Attributes["field"].Value, new List<string>(node.InnerText.Trim().Split(' ')));
            return bindingList;
        }

        internal static string EncodeBinding(RepositoryDataType slotType, int slotNumber)
        {
            return String.Concat("#", slotType, "_", slotNumber);
        }
        internal static void DecodeBinding(string binding, out DataType dataType, out int ordinalNumber)
        {
            int p = binding.IndexOf('_');
            dataType = (DataType)Enum.Parse(typeof(DataType), binding.Substring(1, p - 1));
            ordinalNumber = int.Parse(binding.Substring(p + 1));
        }

        public string GetPropertySingleId(string propertyName)
        {
            if (ContentListBindings[propertyName] != null)
            {
                return ContentListBindings[propertyName][0];
            }
            return string.Empty;
        }

        // Returns the node itself cast to ContentList if it's a ContentList, or the
        // ContentList containing the node.
        public static ContentList GetContentListForNode(Node n)
        {
            ContentList result = n as ContentList;

            if (result == null && n != null)
                result = n.LoadContentList() as ContentList;

            return result;
        }

        // For cases where we know we have no ContentListId on the node
        // Eg. Folders or configuration systemfolder contents
        public static ContentList GetContentListByParentWalk(Node child)
        {
            return Node.GetAncestorOfType<ContentList>(child);
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

        public void AddField(FieldSetting fieldSetting)
        {
            if (fieldSetting == null)
                throw new ArgumentNullException("fieldSetting");

            if (FieldExists(fieldSetting))
                throw new ArgumentException("Existing list field: " + fieldSetting.Name);

            AddFieldInternal(fieldSetting);
        }

        private void AddFieldInternal(FieldSetting fieldSetting)
        {
            if (string.IsNullOrEmpty(this.ContentListDefinition))
                this.ContentListDefinition = DefaultContentListDefinition;

            var doc = new XmlDocument();
            doc.LoadXml(this.ContentListDefinition);
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("x", ContentListDefinitionXmlNamespace);

            var fields = doc.DocumentElement.SelectSingleNode("/x:ContentListDefinition/x:Fields", nsmgr);

            using (var writer = fields.CreateNavigator().AppendChild())
            {
                fieldSetting.WriteXml(writer);
            }

            this.ContentListDefinition = doc.OuterXml;
            this.Save();
        }

        public void AddOrUpdateField(FieldSetting fieldSetting)
        {
            if (fieldSetting == null)
                throw new ArgumentNullException("fieldSetting");

            if (FieldExists(fieldSetting))
                UpdateFieldInternal(fieldSetting);
            else
                AddFieldInternal(fieldSetting);
        }

        public void UpdateField(FieldSetting fieldSetting)
        {
            if (fieldSetting == null)
                throw new ArgumentNullException("fieldSetting");

            if (!FieldExists(fieldSetting))
                throw new ArgumentException("List field does not exist: " + fieldSetting.Name);

            foreach (var fs in this.FieldSettings)
            {
                if (fs.Name.CompareTo(fieldSetting.Name) != 0)
                    continue;

                if (fs.ShortName.CompareTo(fieldSetting.ShortName) != 0)
                    throw new ArgumentException(string.Format("List field types does not match: {0}, {1}", fs.ShortName, fieldSetting.ShortName));

                break;
            }

            UpdateFieldInternal(fieldSetting);
        }

        private void UpdateFieldInternal(FieldSetting fieldSetting)
        {
            XmlDocument doc;
            var node = FindFieldXmlNode(fieldSetting.Name, out doc);
            var fields = node.ParentNode;

            fields.RemoveChild(node);

            using (var writer = fields.CreateNavigator().AppendChild())
            {
                fieldSetting.WriteXml(writer);
            }

            this.ContentListDefinition = doc.OuterXml;
            this.Save();
        }

        public void DeleteField(FieldSetting fieldSetting)
        {
            // do not throw an exception, if field does not exist
            if (FieldExists(fieldSetting))
            {
                ClearFieldValues(fieldSetting);
                DeleteFieldInternal(fieldSetting);
            }
        }

        public void UpdateContentListDefinition(IEnumerable<FieldSettingContent> fieldSettings)
        {
            var doc = new XmlDocument();
            doc.LoadXml(this.ContentListDefinition);
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("x", ContentListDefinitionXmlNamespace);

            if (doc.DocumentElement == null)
                return;

            var fieldsNode = doc.DocumentElement.SelectSingleNode("/x:ContentListDefinition/x:Fields", nsmgr);
            fieldsNode.RemoveAll();

            using (var writer = fieldsNode.CreateNavigator().AppendChild())
            {
                foreach (var fieldSetting in fieldSettings)
                {
                    fieldSetting.FieldSetting.WriteXml(writer);
                }
            }

            this.ContentListDefinition = doc.OuterXml;
        }

        private void DeleteFieldInternal(FieldSetting fieldSetting)
        {
            DeleteFieldInternal(fieldSetting, true);
        }

        private void DeleteFieldInternal(FieldSetting fieldSetting, bool saveImmediately)
        {
            XmlDocument doc;
            var node = FindFieldXmlNode(fieldSetting.Name, out doc);

            if (node == null)
                return;

            node.ParentNode.RemoveChild(node);

            this.ContentListDefinition = doc.OuterXml;

            if (!saveImmediately)
                return;

            this.Save();
        }

        private XmlNode FindFieldXmlNode(string fieldName, out XmlDocument doc)
        {
            doc = new XmlDocument();
            doc.LoadXml(this.ContentListDefinition);

            if (string.IsNullOrEmpty(fieldName))
                return null;

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("x", ContentListDefinitionXmlNamespace);

            var xTemplate = string.Format("/x:ContentListDefinition/x:Fields/x:ContentListField[@name='{0}']", fieldName);

            return string.IsNullOrEmpty(fieldName) ? null :
                doc.DocumentElement.SelectSingleNode(xTemplate, nsmgr);
        }

        private bool FieldExists(FieldSetting fieldSetting)
        {
            if (fieldSetting == null || fieldSetting.Name == null)
                return false;

            return this.ContentListBindings.Keys.Contains(fieldSetting.Name);
        }

        private void ClearFieldValues(FieldSetting fieldSetting)
        {
            // TEMP: if this is a reference or longtext field, remove all the values before deleting.
            // This is a temporary solution as it needs to save ALL the content in the list.
            if (fieldSetting is ReferenceFieldSetting || fieldSetting is LongTextFieldSetting)
            {
                try
                {
                    using (new SystemAccount())
                    {
                        var fn = this.GetPropertySingleId(fieldSetting.Name);
                        var result = ContentQuery_NEW.Query(SafeQueries.InTree,
                            new QuerySettings { EnableAutofilters = FilterStatus.Disabled },
                            this.Path);

                        foreach (var node in result.Nodes.Where(node => node.HasProperty(fn)).OfType<GenericContent>())
                        {
                            // ensure that these values are preserved, and the Admin will not become the modifier
                            node.ModifiedBy = node.ModifiedBy;
                            node.CreatedBy = node.CreatedBy;

                            try
                            {
                                if (fieldSetting is ReferenceFieldSetting)
                                {
                                    node.ClearReference(fn);
                                    node.Save(SavingMode.KeepVersion);
                                }
                                else if (fieldSetting is LongTextFieldSetting && node[fn] != null)
                                {
                                    node[fn] = null;
                                    node.Save(SavingMode.KeepVersion);
                                }
                            }
                            catch (Exception ex)
                            {
                                // exception during technical content update
                                SnLog.WriteException(ex);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }
            }
        }

        // ================================================================================= Cached data

        private const string CONTENTLISTTYPEKEY = "ContentListType";
        private const string FIELDSETTINGSKEY = "FieldSettings";

        protected override void OnLoaded(object sender, NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            _contentListType = (ContentListType)base.GetCachedData(CONTENTLISTTYPEKEY);

            if (_contentListType != null)
            {
                _fieldSettings = (List<FieldSetting>)base.GetCachedData(FIELDSETTINGSKEY);

                // check if fields need to be rebuilt. if the content type manager was restarted, fieldsettings come from cache,
                // but indexinginfos are not yet present in the lazy-filled ContentTypeManager.IndexingInfo dictionary -> call Build() to include them
                bool fieldsIncluded = true;
                foreach (var fieldSetting in _fieldSettings)
                {
                    if (fieldSetting.IndexingInfo == null)
                    {
                        fieldsIncluded = false;
                        break;
                    }
                }
                if (fieldsIncluded)
                    return;
            }

            Build();
            base.SetCachedData(FIELDSETTINGSKEY, _fieldSettings);
            base.SetCachedData(CONTENTLISTTYPEKEY, _contentListType);
        }

        public static readonly string WorkflowContainerName = "Workflows";

        public Folder GetWorkflowContainer()
        {
            var path = RepositoryPath.Combine(this.Path, WorkflowContainerName);
            var container = (Folder)Node.LoadNode(path);
            if (container != null)
                return container;
            container = new SystemFolder(this);
            container.Name = WorkflowContainerName;
            using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
                container.Save();
            return container;
        }

        // ================================================================================ Inbox feature

        protected void AssertEmail()
        {
            var email = (string)this["ListEmail"];
            if (string.IsNullOrEmpty(email))
                return;

            int count;
            using (new SystemAccount())
            {
                if (StorageContext.Search.ContentQueryIsAllowed)
                {
                    count = Content.All.OfType<ContentList>().Count(cl => (string)cl["ListEmail"] == email && cl.Id != this.Id);
                }
                else
                {
                    count = NodeQuery.QueryNodesByTypeAndPathAndProperty(NodeType.GetByName("ContentList"), false, "/Root", false,
                                                                     new List<QueryPropertyData>
                                                                     {
                                                                         new QueryPropertyData { PropertyName = "ListEmail", QueryOperator = Operator.Equal, Value = email },
                                                                         new QueryPropertyData { PropertyName = "Id", QueryOperator = Operator.NotEqual, Value = this.Id }
                                                                     }).Count;
                }
            }

            if (count > 0)
                throw new InvalidContentException(SR.GetString(SR.Exceptions.ContentList.Error_EmailIsTaken));
        }

        private void SetAllowedChildTypesForEmails()
        {
            var additionalTypes = new List<string> { "File" };
            var groupAttachmentType = this["GroupAttachments"] as string;

            if (groupAttachmentType != null)
            {
                switch (groupAttachmentType)
                {
                    case "subject":
                    case "sender":
                        additionalTypes.Add("Folder");
                        break;
                    case "email":
                        additionalTypes.Add("Email");
                        break;
                }
            }

            this.AllowChildTypes(additionalTypes);
        }

        private void RemoveWorkflow()
        {
            // check if any workflow is running currently
            var targetPath = RepositoryPath.Combine(this.Path, "Workflows/MailProcess");
            IEnumerable<Node> runningWorkflows;

            if (StorageContext.Search.ContentQueryIsAllowed)
            {
                runningWorkflows = Content.All.DisableAutofilters().Where(
                    c => c.TypeIs("MailProcessorWorkflow") &&
                    (string)c["WorkflowStatus"] == "$1" &&
                    c.InFolder(targetPath)).AsEnumerable().Select(c => c.ContentHandler);
            }
            else
            {
                runningWorkflows =
                    NodeQuery.QueryNodesByTypeAndPathAndProperty(ActiveSchema.NodeTypes["MailProcessorWorkflow"], false,
                                                                 targetPath, false,
                                                                 new List<QueryPropertyData>
                                                                     {
                                                                         new QueryPropertyData
                                                                             {
                                                                                 PropertyName = "WorkflowStatus",
                                                                                 QueryOperator = Operator.Equal,
                                                                                 Value = "1"
                                                                             }
                                                                     }).Nodes;
            }

            foreach (var wfnode in runningWorkflows)
            {
                wfnode.ForceDelete();
            }
        }

        private void StartSubscription()
        {
            var subscribe = Settings.GetValue<MailProcessingMode>(
                    MailHelper.MAILPROCESSOR_SETTINGS,
                    MailHelper.SETTINGS_MODE,
                    this.Path) == MailProcessingMode.ExchangePush;

            if (subscribe)
            {
                // subscribe to email after saving content. this is done separately from saving the content, 
                // since subscriptionid must be persisted on the content and we use cyclic retrials for that
                ExchangeHelper.Subscribe(this);
            }

            var parent = GetMailProcessorWorkflowContainer(this);
            if (parent == null)
                return;

            // get the workflow to start
            var incomingEmailWorkflow = this.GetReference<Node>("IncomingEmailWorkflow");
            if (incomingEmailWorkflow == null)
                return;

            // set this list as the related content
            var workflowC = Content.CreateNew(incomingEmailWorkflow.Name, parent, incomingEmailWorkflow.Name);
            workflowC["RelatedContent"] = this;

            try
            {
                workflowC.Save();
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, categories: ExchangeHelper.ExchangeLogCategory);
            }

            // reflection: because we do not have access to the workflow engine here
            var t = TypeResolver.GetType("SenseNet.Workflow.InstanceManager");
            if (t != null)
            {
                var m = t.GetMethod("Start", BindingFlags.Static | BindingFlags.Public);
                m.Invoke(null, new object[] { workflowC.ContentHandler });
            }
        }

        private static Node GetMailProcessorWorkflowContainer(Node contextNode)
        {
            var parent = Node.LoadNode(RepositoryPath.Combine(contextNode.Path, "Workflows/MailProcess"));

            if (parent == null)
            {
                var workflows = Node.LoadNode(RepositoryPath.Combine(contextNode.Path, "Workflows"));
                if (workflows == null)
                {
                    using (new SystemAccount())
                    {
                        workflows = new SystemFolder(contextNode) { Name = "Workflows" };

                        try
                        {
                            workflows.Save();
                        }
                        catch (Exception ex)
                        {
                            SnLog.WriteException(ex, categories: ExchangeHelper.ExchangeLogCategory);
                            return null;
                        }
                    }
                }
                using (new SystemAccount())
                {
                    parent = new Folder(workflows) { Name = "MailProcess" };

                    try
                    {
                        parent.Save();
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex, categories: ExchangeHelper.ExchangeLogCategory);
                        return null;
                    }
                }
            }
            return parent;
        }

        // ================================================================================= ISupportsVirtualChildren members

        public Content GetChild(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            // add field name prefix character if necessary
            var fieldName = name.StartsWith("#") ? name : "#" + name;
            var fsc = this.FieldSettingContents.FirstOrDefault(fs => string.CompareOrdinal(fs.Name, fieldName) == 0);

            return fsc != null ? Content.Create(fsc) : null;
        }
    }
}
