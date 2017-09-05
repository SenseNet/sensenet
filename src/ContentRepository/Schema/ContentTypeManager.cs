using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.XPath;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using SnCS = SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Schema
{
    internal sealed class ContentTypeManager
    {
        [Serializable]
        internal sealed class ContentTypeManagerResetDistributedAction : DistributedAction
        {
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                // Local echo of my action: Return without doing anything
                if (onRemote && isFromMe)
                    return;
                ContentTypeManager.ResetPrivate();
            }
        }

        // ======================================================================= Static interface

        private static object _syncRoot = new Object();

        private static bool _initializing = false;

        private static ContentTypeManager _current;
        public static ContentTypeManager Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_syncRoot)
                    {
                        if (_current == null)
                        {
                            _initializing = true;
                            var current = new ContentTypeManager();
                            current.Initialize();
                            _initializing = false;
                            _current = current;
                            SnLog.WriteInformation("ContentTypeManager created. Content types: " + _current._contentTypes.Count);
                        }
                    }
                }
                return _current;
            }
        }

        // =======================================================================

        private Dictionary<string, string> _contentPaths;
        private Dictionary<string, ContentType> _contentTypes;

        internal Dictionary<string, ContentType> ContentTypes
        {
            get { return _contentTypes; }
        }

        #region GetContentTypeNameByType
        private Dictionary<Type, NodeType> _contentTypeNamesByType;
        public static string GetContentTypeNameByType(Type t)
        {
            if (Current._contentTypeNamesByType == null)
            {
                var contentTypeNamesByType = new Dictionary<Type, NodeType>();
                foreach (var nt in ActiveSchema.NodeTypes)
                {
                    var type = TypeResolver.GetType(nt.ClassName);
                    NodeType prevNt;
                    if (type == typeof(GenericContent))
                    {
                        if (nt.Name == "GenericContent")
                            contentTypeNamesByType.Add(type, nt);
                    }
                    else if (!contentTypeNamesByType.TryGetValue(type, out prevNt))
                        contentTypeNamesByType.Add(type, nt);
                    else
                        if (prevNt.IsInstaceOfOrDerivedFrom(nt))
                        contentTypeNamesByType[type] = nt;
                }
                Current._contentTypeNamesByType = contentTypeNamesByType;
            }
            NodeType nodeType;
            if (Current._contentTypeNamesByType.TryGetValue(t, out nodeType))
                return nodeType.Name;
            return null;
        }
        #endregion

        private ContentTypeManager()
        {
        }
        static ContentTypeManager()
        {
            Node.AnyContentListDeleted += new EventHandler(Node_AnyContentListDeleted);
        }
        private static void Node_AnyContentListDeleted(object sender, EventArgs e)
        {
            Reset();
        }

        private void Initialize()
        {
            using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
            {
                _contentPaths = new Dictionary<string, string>();
                _contentTypes = new Dictionary<string, ContentType>();

                // temporary save: read enumerator only once
                var contentTypes = new List<ContentType>();

                var result = NodeQuery.QueryNodesByTypeAndPath(ActiveSchema.NodeTypes["ContentType"], false, String.Concat(Repository.ContentTypesFolderPath, SnCS.RepositoryPath.PathSeparator), true);

                foreach (ContentType contentType in result.Nodes)
                {
                    contentTypes.Add(contentType);
                    _contentPaths.Add(contentType.Name, contentType.Path);
                    _contentTypes.Add(contentType.Name, contentType);
                }
                foreach (ContentType contentType in contentTypes)
                {
                    if (contentType.ParentTypeName == null)
                        contentType.SetParentContentType(null);
                    else
                        contentType.SetParentContentType(_contentTypes[contentType.ParentTypeName]);
                }
                AllFieldNames = contentTypes.SelectMany(t => t.FieldSettings.Select(f => f.Name)).Distinct().ToList();
                FinalizeAllowedChildTypes(AllFieldNames);
                FinalizeIndexingInfo();
            }
        }

        internal static void Start()
        {
            if (_initializing)
                return;
            ContentTypeManager m = Current;
        }
        internal static void Reset()
        {
            SnLog.WriteInformation("ContentTypeManager.Reset called.", EventId.RepositoryRuntime,
                properties: new Dictionary<string, object> { { "AppDomain", AppDomain.CurrentDomain.FriendlyName } });

            new ContentTypeManagerResetDistributedAction().Execute();
        }
        private static void ResetPrivate()
        {
            lock (_syncRoot)
            {
                SnLog.WriteInformation("ContentTypeManager.Reset executed.", EventId.RepositoryRuntime,
                   properties: new Dictionary<string, object> { { "AppDomain", AppDomain.CurrentDomain.FriendlyName } });

                // Do not call ActiveSchema.Reset();
                _current = null;
                _indexingInfoTable = new Dictionary<string, PerFieldIndexingInfo>();
                ContentType.OnTypeSystemRestarted();
            }
        }

        internal static void Reload()
        {
            ResetPrivate();
            var c = Current;
        }

        internal ContentType GetContentTypeByHandler(Node contentHandler)
        {
            var nodeType = contentHandler.NodeType;
            if (nodeType == null)
                return null;
            return GetContentTypeByName(nodeType.Name);
        }
        internal ContentType GetContentTypeByName(string contentTypeName)
        {
            ContentType contentType;
            if (_contentTypes.TryGetValue(contentTypeName, out contentType))
                return contentType;

            lock (_syncRoot)
            {
                if (_contentTypes.TryGetValue(contentTypeName, out contentType))
                    return contentType;

                string path;
                if (_contentPaths.TryGetValue(contentTypeName, out path))
                {
                    contentType = ContentType.LoadAndInitialize(path);
                    if (contentType != null)
                    {
                        _contentTypes.Add(contentTypeName, contentType);
                        _contentPaths.Add(contentTypeName, contentType.Path);
                    }
                }
            }
            return contentType;
        }

        internal void RemoveContentType(string name)
        {
            // Caller: ContentType.Delete()
            lock (_syncRoot)
            {
                ContentType contentType;
                if (_contentTypes.TryGetValue(name, out contentType))
                {
                    SchemaEditor editor = new SchemaEditor();
                    editor.Load();
                    RemoveContentType(contentType, editor);
                    editor.Register();

                    // The ContentTypeManager distributes its reset, no custom DistributedAction call needed
                    ContentTypeManager.Reset();
                }
            }
        }
        private void RemoveContentType(ContentType contentType, SchemaEditor editor)
        {
            // Remove recursive
            foreach (FieldSetting fieldSetting in contentType.FieldSettings)
                if (fieldSetting.Owner == contentType)
                    fieldSetting.ParentFieldSetting = null;
            foreach (ContentType childType in contentType.ChildTypes)
                RemoveContentType(childType, editor);
            NodeType nodeType = editor.NodeTypes[contentType.Name];
            if (nodeType != null)
                editor.DeleteNodeType(nodeType);
            _contentTypes.Remove(contentType.Name);
            _contentPaths.Remove(contentType.Name);
        }

        // ====================================================================== Registration interface

        private const BindingFlags _publicPropertyBindingFlags = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags _nonPublicPropertyBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        internal static ContentType LoadOrCreateNew(string contentTypeDefinitionXml)
        {
            return LoadOrCreateNew(new XPathDocument(new StringReader(contentTypeDefinitionXml)));
        }
        internal static ContentType LoadOrCreateNew(IXPathNavigable contentTypeDefinitionXml)
        {
            // ==== saves and puts the holder

            // #1 Determine name and parent's name
            XPathNavigator nav = contentTypeDefinitionXml.CreateNavigator().SelectSingleNode("/*[1]");
            string name = nav.GetAttribute("name", "");
            string parentTypeName = nav.GetAttribute("parentType", "");

            // #2 Load ContentType
            ContentType contentType = ContentTypeManager.Current.GetContentTypeByName(name);

            // #3 Parent Node: if it is loaded yet use it (ReferenceEqals)
            Node parentNode;
            if (String.IsNullOrEmpty(parentTypeName))
            {
                parentNode = (Folder)Node.LoadNode(Repository.ContentTypesFolderPath);
            }
            else
            {
                parentNode = ContentTypeManager.Current.GetContentTypeByName(parentTypeName);
                if (parentNode == null)
                    throw new ApplicationException(String.Concat(SR.Exceptions.Content.Msg_UnknownContentType, ": ", parentTypeName));
            }

            // #4 Create ContentType if it does not exist
            if (contentType == null)
            {
                contentType = new ContentType(parentNode);
                contentType.Name = name;
            }

            // #5 Update hierarchy if parent is changed
            if (contentType.ParentId != parentNode.Id)
            {
                throw new SnNotSupportedException("Change ContentType hierarchy is not supported");
            }

            // #6 Set Binary data
            BinaryData binaryData = new BinaryData();
            binaryData.FileName = new BinaryFileName(name, ContentType.ContentTypeFileNameExtension);
            binaryData.SetStream(RepositoryTools.GetStreamFromString(contentTypeDefinitionXml.CreateNavigator().OuterXml));
            contentType.Binary = binaryData;

            return contentType;
        }

        internal void AddContentType(ContentType contentType)
        {
            lock (_syncRoot)
            {
                var parentContentTypeName = contentType.ParentName;

                ContentType parentContentType;
                _contentTypes.TryGetValue(parentContentTypeName, out parentContentType);

                string name = contentType.Name;
                if (!_contentTypes.ContainsKey(name))
                    _contentTypes.Add(name, contentType);
                if (!_contentPaths.ContainsKey(name))
                    _contentPaths.Add(name, contentType.Path);
                contentType.SetParentContentType(parentContentType);
            }
        }

        internal static void ApplyChanges(ContentType settings)
        {
            SchemaEditor editor = new SchemaEditor();
            editor.Load();
            ApplyChangesInEditor(settings, editor);
            editor.Register();

            // The ContentTypeManager distributes its reset, no custom DistributedAction call needed
            ContentTypeManager.Reset();
        }
        internal static void ApplyChangesInEditor(ContentType contentType, SchemaEditor editor)
        {
            // Find ContentHandler
            var handlerType = TypeResolver.GetType(contentType.HandlerName, false);
            if (handlerType == null)
                throw new RegistrationException(string.Concat(
                    SR.Exceptions.Registration.Msg_ContentHandlerNotFound, ": ", contentType.HandlerName));

            // parent type
            NodeType parentNodeType = null;
            if (contentType.ParentTypeName != null)
            {
                parentNodeType = editor.NodeTypes[contentType.ParentTypeName];
                if (parentNodeType == null)
                    throw new ContentRegistrationException(SR.Exceptions.Registration.Msg_UnknownParentContentType, contentType.Name);
            }

            // handler type
            NodeType nodeType = editor.NodeTypes[contentType.Name];
            if (nodeType == null)
                nodeType = editor.CreateNodeType(parentNodeType, contentType.Name, contentType.HandlerName);
            if (nodeType.ClassName != contentType.HandlerName)
                editor.ModifyNodeType(nodeType, contentType.HandlerName);
            if (nodeType.Parent != parentNodeType)
                editor.ModifyNodeType(nodeType, parentNodeType);

            // 1: ContentHandler properties
            NodeTypeRegistration ntReg = ParseAttributes(handlerType);
            if (ntReg == null)
                throw new ContentRegistrationException(
                    SR.Exceptions.Registration.Msg_DefinedHandlerIsNotAContentHandler, contentType.Name);

            // 2: Field properties
            foreach (FieldSetting fieldSetting in contentType.FieldSettings)
            {
                Type[][] slots = fieldSetting.HandlerSlots;
                int fieldSlotCount = slots.GetLength(0);

                if (fieldSetting.Bindings.Count != fieldSlotCount)
                    throw new ContentRegistrationException(String.Format(CultureInfo.InvariantCulture,
                        SR.Exceptions.Registration.Msg_FieldBindingsCount_1, fieldSlotCount), contentType.Name, fieldSetting.Name);
                for (int i = 0; i < fieldSetting.Bindings.Count; i++)
                {
                    string propName = fieldSetting.Bindings[i];
                    var dataType = fieldSetting.DataTypes[i];
                    CheckDataType(propName, dataType, contentType.Name, editor);
                    PropertyInfo propInfo = handlerType.GetProperty(propName);
                    if (propInfo != null)
                    {
                        // #1: there is a property under the slot:
                        bool ok = false;
                        for (int j = 0; j < slots[i].Length; j++)
                        {
                            if (slots[i][j].IsAssignableFrom(propInfo.PropertyType))
                            {
                                PropertyTypeRegistration propReg = ntReg.PropertyTypeRegistrationByName(propName);
                                if (propInfo.DeclaringType != handlerType)
                                {
                                    if (propReg == null)
                                    {
                                        object[] attrs = propInfo.GetCustomAttributes(typeof(RepositoryPropertyAttribute), false);
                                        if (attrs.Length > 0)
                                        {
                                            propReg = new PropertyTypeRegistration(propInfo, (RepositoryPropertyAttribute)attrs[0]);
                                            ntReg.PropertyTypeRegistrations.Add(propReg);
                                        }
                                    }
                                }
                                if (propReg != null && propReg.DataType != fieldSetting.DataTypes[i])
                                    throw new ContentRegistrationException(String.Concat(
                                        "The data type of the field in the content type definition does not match the data type of its content handler's property. ",
                                        "Please modify the field type in the content type definition. ",
                                        "ContentTypeDefinition: '", contentType.Name,
                                        "', FieldName: '", fieldSetting.Name,
                                        "', DataType of Field's binding: '", fieldSetting.DataTypes[i],
                                        "', ContentHandler: '", handlerType.FullName,
                                        "', PropertyName: '", propReg.Name,
                                        "', DataType of property: '", propReg.DataType,
                                        "'"));

                                ok = true;
                                fieldSetting.HandlerSlotIndices[i] = j;
                                fieldSetting.PropertyIsReadOnly = !PropertyHasPublicSetter(propInfo);
                                break;
                            }
                        }
                        if (!ok)
                        {
                            if (fieldSetting.ShortName == "Reference" || fieldSetting.DataTypes[i] == RepositoryDataType.Reference)
                                CheckReference(propInfo, slots[i], contentType, fieldSetting);
                            else
                                throw new ContentRegistrationException(SR.Exceptions.Registration.Msg_PropertyAndFieldAreNotConnectable,
                                    contentType.Name, fieldSetting.Name);
                        }
                    }
                    else
                    {
                        // #2: there is not a property under the slot:
                        PropertyTypeRegistration propReg = new PropertyTypeRegistration(propName, dataType);
                        ntReg.PropertyTypeRegistrations.Add(propReg);
                    }
                }
            }

            // Collect deletables. Check equals
            foreach (PropertyType propType in nodeType.PropertyTypes.ToArray())
            {
                PropertyTypeRegistration propReg = ntReg.PropertyTypeRegistrationByName(propType.Name);
                if (propReg == null)
                {
                    editor.RemovePropertyTypeFromPropertySet(propType, nodeType);
                }
            }


            // Register
            foreach (PropertyTypeRegistration ptReg in ntReg.PropertyTypeRegistrations)
            {
                PropertyType propType = nodeType.PropertyTypes[ptReg.Name];
                if (propType == null)
                {
                    propType = editor.PropertyTypes[ptReg.Name];
                    if (propType == null)
                        propType = editor.CreatePropertyType(ptReg.Name, ConvertDataType(ptReg.DataType));
                    editor.AddPropertyTypeToPropertySet(propType, nodeType);
                }
            }
        }

        private static void CheckDataType(string propName, RepositoryDataType dataType, string nodeTypeName, SchemaEditor editor)
        {
            var propType = editor.PropertyTypes[propName];

            if (propType == null)
                return;
            if (dataType == (RepositoryDataType)propType.DataType)
                return;

            // "DataType collision in two properties. NodeType = '{0}', PropertyType = '{1}', original DataType = {2}, passed DataType = {3}.";
            throw new RegistrationException(String.Format(SR.Exceptions.Registration.Msg_DataTypeCollisionInTwoProperties_4,
                nodeTypeName, propName, propType.DataType, dataType));
        }

        private static DataType ConvertDataType(RepositoryDataType source)
        {
            if (source == RepositoryDataType.NotDefined)
                throw new ArgumentOutOfRangeException("source", "Source DataType cannot be NotDefined");
            return (DataType)source;
        }
        private static void CheckReference(PropertyInfo propInfo, Type[] type, ContentType cts, FieldSetting fs)
        {
            if (propInfo.PropertyType == (typeof(Node)))
                return;
            if (propInfo.PropertyType.IsSubclassOf(typeof(Node)))
                return;
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propInfo.PropertyType))
                return;
            throw new NotSupportedException(String.Format(CultureInfo.InvariantCulture,
                SR.Exceptions.Registration.Msg_InvalidReferenceField_2, cts.Name, fs.Name));
        }

        // ---------------------------------------------------------------------- Attribute parsing

        private static NodeTypeRegistration ParseAttributes(Type type)
        {
            NodeTypeRegistration ntReg = null;
            ContentHandlerAttribute contentHandlerAttribute = null;

            foreach (object attrObject in type.GetCustomAttributes(false))
                if ((contentHandlerAttribute = attrObject as ContentHandlerAttribute) != null)
                    break;

            // Finish if there is not a ContentHandlerAttribute
            if (contentHandlerAttribute == null)
                return ntReg;

            // Must inherit from Node.
            if (!IsInheritedFromNode(type))
                throw new ContentRegistrationException(String.Format(CultureInfo.CurrentCulture,
                    SR.Exceptions.Registration.Msg_NodeTypeMustBeInheritedFromNode_1,
                    type.FullName));

            // Property checks
            RepositoryPropertyAttribute propertyAttribute = null;
            List<PropertyTypeRegistration> propertyTypeRegistrations = new List<PropertyTypeRegistration>();
            Dictionary<string, RepositoryPropertyAttribute> propertyAttributes = new Dictionary<string, RepositoryPropertyAttribute>();

            List<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties(_publicPropertyBindingFlags));
            props.AddRange(type.GetProperties(_nonPublicPropertyBindingFlags));

            foreach (PropertyInfo propInfo in props)
            {
                string propName = propInfo.Name;

                propertyAttribute = null;
                foreach (object attrObject in propInfo.GetCustomAttributes(false))
                    if ((propertyAttribute = attrObject as RepositoryPropertyAttribute) != null)
                        break;

                if (propertyAttribute == null)
                    continue;

                if (propertyAttributes.ContainsKey(propName))
                    throw new RegistrationException(String.Format(CultureInfo.CurrentCulture,
                        SR.Exceptions.Registration.Msg_PropertyTypeAttributesWithTheSameName_2,
                        type.FullName, propInfo.Name));
                propertyAttributes.Add(propName, propertyAttribute);

                // Override default name with passed name
                if (propertyAttribute.PropertyName != null)
                    propName = propertyAttribute.PropertyName;

                // Build PropertyTypeRegistration
                PropertyTypeRegistration propReg = new PropertyTypeRegistration(propInfo, propertyAttribute);
                propertyTypeRegistrations.Add(propReg);
            }

            // Build NodeTypeRegistration
            ntReg = new NodeTypeRegistration(type, null, propertyTypeRegistrations);

            return ntReg;
        }
        private static bool IsInheritedFromNode(Type type)
        {
            Type t = type;
            while (t != typeof(Object))
            {
                if (t == typeof(Node))
                    return true;
                t = t.BaseType;
            }
            return false;
        }

        // ---------------------------------------------------------------------- Information methods

        internal ContentType[] GetContentTypes()
        {
            ContentType[] array = new ContentType[_contentTypes.Count];
            _contentTypes.Values.CopyTo(array, 0);
            return array;
        }
        internal string[] GetContentTypeNames()
        {
            string[] array = new string[_contentTypes.Count];
            _contentTypes.Keys.CopyTo(array, 0);
            return array;
        }

        internal ContentType[] GetRootTypes()
        {
            List<ContentType> list = new List<ContentType>();
            foreach (ContentType ct in GetContentTypes())
                if (ct.ParentType == null)
                    list.Add(ct);
            return list.ToArray();
        }
        internal string[] GetRootTypeNames()
        {
            List<string> list = new List<string>();
            foreach (ContentType ct in GetRootTypes())
                list.Add(ct.Name);
            return list.ToArray();
        }

        internal List<string> AllFieldNames { get; private set; }

        internal string TraceContentSchema()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (ContentType ct in GetRootTypes())
            {
                if (!first)
                {
                    sb.Append(", ");
                    first = false;
                }
                TraceContentSchema(sb, ct);
            }
            sb.Append("}");
            return sb.ToString();
        }
        private void TraceContentSchema(StringBuilder sb, ContentType root)
        {
            sb.Append(root.Name);
            if (root.ChildTypes.Count > 0)
                sb.Append("{");
            bool first = true;
            foreach (ContentType child in root.ChildTypes)
            {
                if (!first)
                {
                    sb.Append(", ");
                    first = false;
                }
                TraceContentSchema(sb, child);
            }
            if (root.ChildTypes.Count > 0)
                sb.Append("}");
        }

        internal static bool PropertyHasPublicSetter(PropertyInfo prop)
        {
            return prop.GetSetMethod() != null;
        }

        // ====================================================================== Indexing

        private static Dictionary<string, PerFieldIndexingInfo> _indexingInfoTable = new Dictionary<string, PerFieldIndexingInfo>();
        internal Dictionary<string, PerFieldIndexingInfo> IndexingInfo { get { return _indexingInfoTable; } }

        internal static Dictionary<string, PerFieldIndexingInfo> GetPerFieldIndexingInfo()
        {
            return Current.IndexingInfo;
        }
        internal static PerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            var ensureStart = Current;

            PerFieldIndexingInfo info = null;
            if (fieldName.Contains('.'))
                info = Aspect.GetPerFieldIndexingInfo(fieldName);

            if (info != null || Current.IndexingInfo.TryGetValue(fieldName, out info))
                return info;

            return null;
        }
        internal static void SetPerFieldIndexingInfo(string fieldName, string contentTypeName, PerFieldIndexingInfo indexingInfo)
        {
            PerFieldIndexingInfo origInfo;

            if (!_indexingInfoTable.TryGetValue(fieldName, out origInfo))
            {
                lock (_syncRoot)
                {
                    if (!_indexingInfoTable.TryGetValue(fieldName, out origInfo))
                    {
                        _indexingInfoTable.Add(fieldName, indexingInfo);
                        return;
                    }
                }
            }

            if (origInfo.IndexingMode == IndexingMode.Default)
                origInfo.IndexingMode = indexingInfo.IndexingMode;
            else if (indexingInfo.IndexingMode != IndexingMode.Default && indexingInfo.IndexingMode != origInfo.IndexingMode)
                throw new ContentRegistrationException("Cannot override IndexingMode", contentTypeName, fieldName);

            if (origInfo.IndexStoringMode == IndexStoringMode.Default)
                origInfo.IndexStoringMode = indexingInfo.IndexStoringMode;
            else if (indexingInfo.IndexStoringMode != IndexStoringMode.Default && indexingInfo.IndexStoringMode != origInfo.IndexStoringMode)
                throw new ContentRegistrationException("Cannot override IndexStoringMode", contentTypeName, fieldName);

            if (origInfo.TermVectorStoringMode == IndexTermVector.Default)
                origInfo.TermVectorStoringMode = indexingInfo.TermVectorStoringMode;
            else if (indexingInfo.TermVectorStoringMode != IndexTermVector.Default && indexingInfo.TermVectorStoringMode != origInfo.TermVectorStoringMode)
                throw new ContentRegistrationException("Cannot override TermVectorStoringMode", contentTypeName, fieldName);

            if (String.IsNullOrEmpty(origInfo.Analyzer))
                origInfo.Analyzer = indexingInfo.Analyzer;
            else if (!String.IsNullOrEmpty(indexingInfo.Analyzer) && indexingInfo.Analyzer != origInfo.Analyzer)
                throw new ContentRegistrationException("Cannot override Analyzer", contentTypeName, fieldName);
        }

        internal static Exception AnalyzerViolationExceptionHelper(string contentTypeName, string fieldSettingName)
        {
            return new ContentRegistrationException(
                String.Concat("Change analyzer in a field is not allowed. ContentType: ", contentTypeName, ", Field: ", fieldSettingName)
                , null, contentTypeName, fieldSettingName);
        }
        internal static Exception ParserViolationExceptionHelper(string contentTypeName, string fieldSettingName)
        {
            return new ContentRegistrationException(
                String.Concat("Change FieldIndexHandler in a field is not allowed. ContentType: ", contentTypeName, ", Field: ", fieldSettingName)
                , null, contentTypeName, fieldSettingName);
        }

        private void FinalizeAllowedChildTypes(List<string> allFieldNames)
        {
            foreach (var ct in this.ContentTypes.Values)
                ct.FinalizeAllowedChildTypes(this.ContentTypes, allFieldNames);
        }

        private void FinalizeIndexingInfo()
        {
            StorageContext.Search.SearchEngine.SetIndexingInfo(_indexingInfoTable);
        }

        public static long _GetTimestamp()
        {
            if (_current == null)
                return 0L;
            ContentType ct = null;
            Current.ContentTypes.TryGetValue("Automobile", out ct);
            if (ct == null)
                return -1;
            return ct.NodeTimestamp;
        }
    }
}
