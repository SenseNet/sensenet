using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Fields
{
    public class ReferenceFieldSetting : FieldSetting
    {
        public static readonly ReferenceFieldSetting DefaultSettings = new ReferenceFieldSetting();

        public const string AllowMultipleName = "AllowMultiple";
        public const string AllowedTypesName = "AllowedTypes";
        public const string SelectionRootName = "SelectionRoot";
        public const string QueryName = "Query";
        public const string TypeName = "Type";
        public const string PathName = "Path";
        public const string FieldNameName = "FieldName";

        private bool? _allowMultiple;
        private List<string> _allowedTypes;
        private List<string> _selectionRoots;
        private ContentQuery _query;
        private string _fieldName;

        public bool? AllowMultiple
        {
            get
            {
                if (_allowMultiple != null)
                    return _allowMultiple.Value;

                return this.ParentFieldSetting == null ? null :
                    ((ReferenceFieldSetting)this.ParentFieldSetting).AllowMultiple;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting AllowMultiple is not allowed within readonly instance.");
                _allowMultiple = value;
            }
        }
        public List<string> AllowedTypes
        {
            get
            {
                if (_allowedTypes != null)
                    return _allowedTypes;
                if (this.ParentFieldSetting == null)
                    return null;
                return ((ReferenceFieldSetting)this.ParentFieldSetting).AllowedTypes;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting AllowedTypes is not allowed within readonly instance.");
                _allowedTypes = value;
            }
        }
        public List<string> SelectionRoots
        {
            get
            {
                if (_selectionRoots != null)
                    return _selectionRoots;
                if (this.ParentFieldSetting == null)
                    return null;
                return ((ReferenceFieldSetting)this.ParentFieldSetting).SelectionRoots;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting SelectionRoots is not allowed within readonly instance.");
                _selectionRoots = value;
            }
        }
        public ContentQuery Query
        {
            get
            {
                return _query ?? (this.ParentFieldSetting == null ? null :
                    ((ReferenceFieldSetting)this.ParentFieldSetting).Query);
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting SelectionRoots is not allowed within readonly instance.");
                _query = value;
            }
        }
        public string FieldName
        {
            get
            {
                return _fieldName ??
                       (this.ParentFieldSetting == null ? null :
                       ((ReferenceFieldSetting)this.ParentFieldSetting).FieldName);
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting FieldName is not allowed within readonly instance.");
                _fieldName = value;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            // xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/SearchExpression"
            // <Configuration>
            //    <AllowMultiple>true<AllowMultiple>
            //    <AllowedTypes>
            //        <Type>Folder</Type>
            //        <Type>File</Type>
            //    </AllowedTypes>
            //    <SelectionRoot>
            //        <Path>/Root/.../1</Path>
            //        <Path>/Root/.../2</Path>
            //    <SelectionRoot>
            //    <Query>
            //        <q:And>
            //          <q:String op="StartsWith" property="Path">.</q:String>
            //          <q:String op="NotEqual" property="Name">Restricted</q:String>
            //        </q:And>
            //    </Query>
            // </Configuration>
            foreach (XPathNavigator element in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (element.LocalName)
                {
                    case AllowMultipleName:
                        _allowMultiple = element.InnerXml == "true";
                        break;
                    case AllowedTypesName:
                        _allowedTypes = new List<string>();
                        foreach (XPathNavigator typeElement in element.SelectChildren(TypeName, element.NamespaceURI))
                        {
                            string typeName = typeElement.InnerXml;
                            _allowedTypes.Add(typeName);
                        }
                        break;
                    case SelectionRootName:
                        _selectionRoots = new List<string>();
                        foreach (XPathNavigator pathElement in element.SelectChildren(PathName, element.NamespaceURI))
                        {
                            string path = pathElement.InnerXml;
                            if (path != ".")
                            {
                                try
                                {
                                    RepositoryPath.CheckValidPath(path);
                                }
                                catch (InvalidPathException e) // rethrow
                                {
                                    throw new InvalidPathException(String.Concat("Given path is invalid in SelectionRoot element. ContentType: ", contentType.Name,
                                        ", Field name: '", this.Name, "', path: '", path, "'. Reason: ", e.Message));
                                }
                            }
                            _selectionRoots.Add(path);
                        }
                        break;
                    case QueryName:
                        _query = ContentQuery.CreateQuery(element.InnerXml);
                        break;
                    case FieldNameName:
                        _fieldName = element.InnerXml;
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _allowMultiple = GetConfigurationNullableValue<bool>(info, AllowMultipleName, null);
            _fieldName = GetConfigurationStringValue(info, FieldNameName, null);
            object temp;
            if (info.TryGetValue(AllowedTypesName, out temp))
                _allowedTypes = new List<string>((string[])temp);
            if (info.TryGetValue(SelectionRootName, out temp))
                _selectionRoots = new List<string>((string[])temp);
            if (info.TryGetValue(QueryName, out temp))
            {
                var queryText = (string)temp;
                if (queryText != null)
                    _query = ParseQuery(queryText);
            }
            if (_selectionRoots != null)
            {
                foreach (var path in _selectionRoots)
                {
                    if (path != ".")
                    {
                        try
                        {
                            RepositoryPath.CheckValidPath(path);
                        }
                        catch (InvalidPathException e) // rethrow
                        {
                            throw new InvalidPathException(String.Concat("Given path is invalid in SelectionRoot element. Field name: '", this.Name, "', path: '", path, "'. Reason: ", e.Message));
                        }
                    }
                }
            }
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(AllowMultipleName, _allowMultiple);
            result.Add(FieldNameName, _fieldName);
            result.Add(AllowedTypesName, _allowedTypes);
            result.Add(SelectionRootName, _selectionRoots);
            result.Add(QueryName, _query);
            return result;
        }
        protected override void SetDefaults()
        {
            _allowMultiple = null;
            _allowedTypes = null;
            _selectionRoots = null;
            _query = null;
        }

        public override FieldValidationResult ValidateData(object value, Field field)
        {
            FieldValidationResult result;
            var list = GetNodeList(value, out result);
            if (list == null)
                return result;

            if ((this.Compulsory ?? false) && (list.Count == 0))
                return new FieldValidationResult(CompulsoryName);

            if (this.Query != null)
                if ((result = ValidateWithQuery(list, this.Query)) != FieldValidationResult.Successful)
                    return result;
            if ((result = ValidateCount(list)) != FieldValidationResult.Successful)
                return result;
            if (this.AllowedTypes != null)
                if ((result = ValidateTypes(list)) != FieldValidationResult.Successful)
                    return result;
            if (this.SelectionRoots != null)
                if ((result = ValidatePaths(list, field)) != FieldValidationResult.Successful)
                    return result;

            return FieldValidationResult.Successful;
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var refFieldSetting = (ReferenceFieldSetting)source;

            AllowMultiple = refFieldSetting.AllowMultiple;
            Query = refFieldSetting.Query;

            if (refFieldSetting.AllowedTypes != null)
                AllowedTypes = new List<string>(refFieldSetting.AllowedTypes);
            if (refFieldSetting.SelectionRoots != null)
                SelectionRoots = new List<string>(refFieldSetting.SelectionRoots);
        }

        private List<Node> GetNodeList(object value, out FieldValidationResult result)
        {
            result = FieldValidationResult.Successful;
            var list = new List<Node>();

            var node = value as Node;
            if (node != null)
            {
                list.Add(node);
                return list;
            }
            var enumerableNodes = value as IEnumerable<Node>;
            if (enumerableNodes != null)
            {
                return enumerableNodes.ToList();
            }
            var enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                foreach (Node item in enumerable)
                    if (item != null)
                        list.Add(item);
                return list;
            }

            result = new FieldValidationResult("ReferenceValue");
            return list;
        }
        private FieldValidationResult ValidateCount(List<Node> list)
        {
            // Compulsory
            bool required = this.Compulsory ?? false;
            bool allowMultiple = this.AllowMultiple ?? false;

            if (required && list.Count == 0)
                return new FieldValidationResult(CompulsoryName);

            // Multiple
            if (!allowMultiple && list.Count > 1)
                return new FieldValidationResult(AllowMultipleName);

            return FieldValidationResult.Successful;
        }

        private FieldValidationResult ValidateTypes(List<Node> list)
        {
            List<string> allowedTypeNames = CollectExactTypeNames(this.AllowedTypes);
            foreach (Node node in list)
            {
                if (!allowedTypeNames.Contains(node.NodeType.Name))
                {
                    var result = new FieldValidationResult(AllowedTypesName);
                    result.AddParameter(AllowedTypesName, String.Join(", ", allowedTypeNames.ToArray()));
                    result.AddParameter("Path", node.Path);
                    result.AddParameter("NotAllowedType", node.NodeType.Name);
                    return result;
                }
            }
            return FieldValidationResult.Successful;
        }
        private FieldValidationResult ValidatePaths(List<Node> list, Field field)
        {
            // Paths
            if (this.SelectionRoots.Count > 0)
            {
                // Convert relative paths to absolute
                var paths = new List<string>(); // test equality
                var roots = new List<string>(); // ends with PathSeparator
                foreach (string item in this.SelectionRoots)
                {
                    var handler = field.Content.ContentHandler;
                    var handlerPath = RepositoryPath.Combine(RepositoryPath.GetParentPath(handler.Path), handler.Name);
                    var path = "/";
                    if (item.StartsWith("/"))
                    {
                        path = item;
                    }
                    else if (item == ".")
                    {
                        path = handlerPath;
                    }
                    else
                    {
                        path = RepositoryPath.Combine(handlerPath, item);
                    }

                    if (path.EndsWith(RepositoryPath.PathSeparator))
                    {
                        paths.Add(path.Substring(0, path.Length - 1));
                        roots.Add(path);
                    }
                    else
                    {
                        paths.Add(path);
                        roots.Add(String.Concat(path, RepositoryPath.PathSeparator));
                    }
                }
                foreach (Node node in list)
                {
                    var ok = false;
                    for (int i = 0; i < paths.Count; i++)
                    {
                        if (node.Path == paths[i] || node.Path.StartsWith(roots[i]))
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (ok)
                        continue;

                    var result = new FieldValidationResult(SelectionRootName);
                    result.AddParameter(SelectionRootName, node.Path);
                    return result;
                }
            }
            return FieldValidationResult.Successful;
        }
        private FieldValidationResult ValidateWithQuery(List<Node> list, ContentQuery query)
        {
            var x = query.Execute();
            List<int> idList = x.Identifiers.ToList();
            idList.Sort();
            foreach (Node node in list)
            {
                if (!idList.Contains(node.Id))
                {
                    var result = new FieldValidationResult(QueryName);
                    result.AddParameter("Path", node.Path);
                    return result;
                }
            }
            return FieldValidationResult.Successful;
        }

        private List<string> CollectExactTypeNames(List<string> rootTypeNames)
        {
            var allowedTypeNames = new List<string>();
            foreach (string typeName in rootTypeNames)
            {
                if (ActiveSchema.NodeTypes[typeName] == null)
                    throw new ApplicationException(String.Concat("Unknown NodeType in ReferenceField: ", typeName));
                if (!allowedTypeNames.Contains(typeName))
                    allowedTypeNames.Add(typeName);
            }
            var index = 0;
            while (index < allowedTypeNames.Count)
            {
                foreach (var childType in ActiveSchema.NodeTypes[allowedTypeNames[index]].Children)
                    allowedTypeNames.Add(childType.Name);
                index++;
            }

            return allowedTypeNames;
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            WriteElement(writer, this._allowMultiple, AllowMultipleName);

            if (this._allowedTypes != null)
            {
                writer.WriteStartElement(AllowedTypesName);

                foreach (var typeName in this._allowedTypes)
                {
                    WriteElement(writer, typeName, TypeName);
                }

                writer.WriteEndElement();
            }

            if (this._selectionRoots != null)
            {
                writer.WriteStartElement(SelectionRootName);

                foreach (var selRootName in this._selectionRoots)
                {
                    WriteElement(writer, selRootName, PathName);
                }

                writer.WriteEndElement();
            }

            if (_query != null)
            {
                WriteElement(writer, _query.Text, QueryName);
            }

            if (_fieldName != null)
            {
                WriteElement(writer, _fieldName, FieldNameName);
            }
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            fmd.Add(AllowMultipleName, new FieldMetadata
            {
                FieldName = AllowMultipleName,
                PropertyType = typeof(bool),
                FieldType = DynamicContentTools.GetSuggestedFieldType(typeof(bool?)),
                DisplayName = GetTitleString(AllowMultipleName),
                Description = GetDescString(AllowMultipleName),
                CanRead = true,
                CanWrite = true
            });

            fmd.Add(AllowedTypesName, new FieldMetadata
            {
                FieldName = AllowedTypesName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new ReferenceFieldSetting
                {
                    Name = AllowedTypesName,
                    DisplayName = GetTitleString(AllowedTypesName),
                    Description = GetDescString(AllowedTypesName),
                    FieldClassName = typeof(ReferenceField).FullName,
                    AllowMultiple = true,
                    AllowedTypes = new List<string> { "ContentType" },
                    SelectionRoots = new List<string> { "/Root/System/Schema/ContentTypes" }
                }
            });

            fmd.Add(SelectionRootName, new FieldMetadata
            {
                FieldName = SelectionRootName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new LongTextFieldSetting
                {
                    Name = SelectionRootName,
                    DisplayName = GetTitleString(SelectionRootName),
                    Description = GetDescString(SelectionRootName),
                    FieldClassName = typeof(LongTextField).FullName,
                    Rows = 10
                }
            });

            fmd.Add(QueryName, new FieldMetadata
            {
                FieldName = QueryName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new LongTextFieldSetting
                {
                    Name = QueryName,
                    DisplayName = GetTitleString(QueryName),
                    Description = GetDescString(QueryName),
                    FieldClassName = typeof(LongTextField).FullName,
                    Rows = 10
                }
            });

            fmd.Add(FieldNameName, new FieldMetadata
            {
                FieldName = FieldNameName,
                PropertyType = typeof(string),
                FieldType = DynamicContentTools.GetSuggestedFieldType(typeof(string)),
                DisplayName = GetTitleString(FieldNameName),
                Description = GetDescString(FieldNameName),
                CanRead = true,
                CanWrite = true
            });

            return fmd;
        }

        public override object GetProperty(string name, out bool found)
        {
            var val = base.GetProperty(name, out found);

            if (!found)
            {
                switch (name)
                {
                    case QueryName:
                        val = _query?.Text;
                        found = true;
                        break;
                    case AllowedTypesName:
                        if (_allowedTypes != null)
                        {
                            val = (from ctName in _allowedTypes
                                   select ContentType.GetByName(ctName)).ToList();
                        }
                        found = true;
                        break;
                    case SelectionRootName:
                        if (_selectionRoots != null)
                            val = string.Join(";", _selectionRoots.ToArray());
                        found = true;
                        break;
                }
            }

            return found ? val : null;
        }

        public override bool SetProperty(string name, object value)
        {
            var found = base.SetProperty(name, value);

            if (found)
                return true;

            var sv = value as string;

            switch (name)
            {
                case QueryName:
                    if (!string.IsNullOrEmpty(sv))
                        _query = ParseQuery(sv);
                    found = true;
                    break;
                case AllowedTypesName:
                    var types = value as IEnumerable<Node>;
                    if (types != null && types.Count() > 0)
                    {
                        _allowedTypes = (from node in types
                                         select node.Name).ToList();
                    }
                    found = true;
                    break;
                case SelectionRootName:
                    if (!string.IsNullOrEmpty(sv))
                    {
                        var sl = sv.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        _selectionRoots = new List<string>(sl);
                    }
                    found = true;
                    break;
            }

            return found;
        }

        protected override IFieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            return new SenseNet.Search.Indexing.ReferenceIndexHandler();
        }

        private ContentQuery ParseQuery(string queryText)
        {
            if (queryText.StartsWith("<"))
            {
                SnLog.WriteWarning(
                    "ReferenceFieldSetting.Query cannot be initialized with a NodeQuery source xml. Use content query text instead.",
                    properties: new Dictionary<string, object> { { "InvalidFilter", queryText } });
                return null;
            }
            return ContentQuery.CreateQuery(queryText);
        }

    }
}