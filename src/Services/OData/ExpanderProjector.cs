using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Fields;
using System.Diagnostics;

namespace SenseNet.Portal.OData
{
    internal class ExpanderProjector : Projector
    {
        [DebuggerDisplay("{Name}")]
        private class Property
        {
            public string Name;
            public List<Property> Children;
            public Property EnsureChild(string name)
            {
                if (Children != null)
                {
                    foreach (var property in Children)
                        if (property.Name == name)
                            return property;
                }
                else
                {
                    Children = new List<Property>();
                }

                var prop = name == "*" ? Joker : new Property { Name = name, Path = String.Concat(this.Path, "/", name) };
                Children.Add(prop);
                return prop;
            }
            public static Property EnsureChild(List<Property> globals, string name)
            {
                foreach (var property in globals)
                    if (property.Name == name)
                        return property;
                var prop = new Property { Name = name, Path = name };
                globals.Add(prop);
                return prop;
            }
            internal bool ___debug(StringBuilder sb, int indent)
            {
                sb.Append(' ', indent * 2).AppendLine(Name);
                if (Children != null && Name != "*")
                    foreach (var p in Children)
                        p.___debug(sb, indent + 1);
                return true;
            }

            public static Property Joker;
            public static readonly List<Property> JokerList;
            static Property()
            {
                Joker = new Property { Name = "*", Path = "***" };
                JokerList = new List<Property>(new[] { Joker });
                Joker.Children = JokerList;
            }

            public string Path { get; set; }
        }

        private List<Property> _expandTree;
        internal string ____expandTree
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var p in _expandTree)
                    p.___debug(sb, 0);
                return sb.ToString();
            }
        }

        private List<Property> _selectTree;
        internal string ____selectTree
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var p in _selectTree)
                    p.___debug(sb, 0);
                return sb.ToString();
            }
        }

        internal override void Initialize(Content container)
        {
            InitializeTrees();
            CheckSelectTree(_expandTree, _selectTree);
        }
        private void InitializeTrees()
        {
            Property prop;

            // pre building property tree by expansions
            _expandTree = new List<Property>();
            foreach (var item in this.Request.Expand)
            {
                var chain = item.Split('/').Select(s => s.Trim()).ToArray();
                prop = Property.EnsureChild(_expandTree, chain[0]);
                for (int i = 1; i < chain.Length; i++)
                    prop = prop.EnsureChild(chain[i]);
            }

            // pre building property tree by selection
            _selectTree = new List<Property>();
            foreach (var item in this.Request.Select)
            {
                var chain = item.Split('/').Select(s => s.Trim()).ToArray();
                prop = Property.EnsureChild(_selectTree, chain[0]);
                for (int i = 0; i < chain.Length; i++)
                {
                    if (i > 0)
                        prop = prop.EnsureChild(chain[i]);
                    if (i == chain.Length - 1)
                        prop.EnsureChild("*");
                }
            }
        }
        private void CheckSelectTree(List<Property> expandTree, List<Property> selectTree)
        {
            try
            {
                if (selectTree == null)
                    return;

                if (selectTree.Count == 1 && selectTree[0].Name == "*")
                    return;

                foreach (var selectNode in selectTree)
                {
                    var children = selectNode.Children;

                    if (children == null || (children.Count == 1 && children[0].Name == "*"))
                        continue;
                    var expandNode = GetPropertyFromList(selectNode.Name, expandTree);
                    if (expandNode == null)
                        throw new ODataException("Bad item in $select: " + children[0].Path, ODataExceptionCode.InvalidSelectParameter);
                    else
                        CheckSelectTree(selectNode.Children, expandNode.Children);
                }
            }
            catch
            {
                throw;
            }
        }

        internal override Dictionary<string, object> Project(Content content)
        {
            if (content.ContentHandler.IsHeadOnly)
                return Project(content, new List<Property>(0), _selectTree);
            return Project(content, _expandTree, _selectTree);
        }
        private Dictionary<string, object> Project(Content content, List<Property> expandTree, List<Property> selectTree)
        {
            Field field;

            var outfields = new Dictionary<string, object>();
            var selfurl = GetSelfUrl(content);
            if (this.Request.EntityMetadata != MetadataFormat.None)
                outfields.Add("__metadata", GetMetadata(content, selfurl, this.Request.EntityMetadata));

            var hasJoker = false;
            foreach (var property in selectTree)
            {
                var propertyName = property.Name;
                if (propertyName == "*")
                {
                    hasJoker = true;
                    continue;
                }
                if (!content.Fields.TryGetValue(propertyName, out field))
                {
                    switch (propertyName)
                    {
                        case ACTIONSPROPERTY:
                            var actionExpansion = GetPropertyFromList(ACTIONSPROPERTY, expandTree);
                            if (actionExpansion == null)
                                outfields.Add(ACTIONSPROPERTY, ODataReference.Create(String.Concat(selfurl, "/", ODataHandler.PROPERTY_ACTIONS)));
                            else
                                outfields.Add(ACTIONSPROPERTY, GetActions(content));
                            break;
                        case ICONPROPERTY:
                            outfields.Add(ICONPROPERTY, content.Icon ?? content.ContentType.Icon);
                            break;
                        case ISFILEPROPERTY:
                            outfields.Add(ISFILEPROPERTY, content.Fields.ContainsKey(ODataHandler.PROPERTY_BINARY));
                            break;
                        default:
                            outfields.Add(propertyName, null);
                            break;
                    }
                }
                else
                {
                    if (ODataHandler.DisabledFieldNames.Contains(field.Name))
                    {
                        outfields.Add(propertyName, null);
                    }
                    else
                    {
                        var expansion = GetPropertyFromList(propertyName, expandTree);
                        if (expansion != null)
                        {
                            outfields.Add(propertyName, Project(field, expansion.Children, property.Children ?? Property.JokerList));
                        }
                        else
                        {
                            if(base.IsAllowedField(content, field.Name))
                                outfields.Add(propertyName, ODataFormatter.GetJsonObject(field, selfurl));
                            else
                                outfields.Add(propertyName, null);
                        }
                    }
                }
            }

            if (hasJoker)
            {
                foreach (var contentField in content.Fields.Values)
                {
                    if (outfields.ContainsKey(contentField.Name))
                        continue;
                    var propertyName = contentField.Name;
                    var expansion = GetPropertyFromList(propertyName, expandTree);
                    if (expansion != null)
                        outfields.Add(propertyName, Project(contentField, expansion.Children, Property.JokerList));
                    else
                        outfields.Add(propertyName, ODataFormatter.GetJsonObject(contentField, selfurl));
                }
            }

            return outfields;
        }
        private Property GetPropertyFromList(string fieldName, List<Property> propertyTree)
        {
            if (propertyTree == null)
                return null;

            if (propertyTree == Property.JokerList)
                return Property.Joker;

            bool hasJoker = false;
            for (int i = 0; i < propertyTree.Count; i++)
            {
                if (propertyTree[i].Name == fieldName)
                    return propertyTree[i];
                else if (propertyTree[i] == Property.Joker)
                    hasJoker = true;
            }
            if (hasJoker)
                return Property.Joker;

            return null;
        }
        private object Project(Field field, List<Property> expansion, List<Property> selection)
        {
            var refField = field as ReferenceField;
            if (refField == null)
            {
                var allowedChildTypesField = field as AllowedChildTypesField;
                if (allowedChildTypesField == null)
                    return null;
                return ProjectMultiRefContents(allowedChildTypesField.GetData(), expansion, selection);
            }

            var refFieldSetting = refField.FieldSetting as ReferenceFieldSetting;
            var isMultiRef = true;
            if (refFieldSetting != null)
                isMultiRef = refFieldSetting.AllowMultiple == true;

            return isMultiRef
                ? (object)ProjectMultiRefContents(refField.GetData(), expansion, selection)
                : (object)ProjectSingleRefContent(refField.GetData(), expansion, selection);
        }
        private List<Dictionary<string, object>> ProjectMultiRefContents(object references, List<Property> expansion, List<Property> selection)
        {
            var contents = new List<Dictionary<string, object>>();
            if (references != null)
            {
                Node node = references as Node;
                if (node != null)
                {
                    contents.Add(Project(Content.Create(node), expansion, selection));
                }
                else
                {
                    var enumerable = references as System.Collections.IEnumerable;
                    var count = 0;
                    if (enumerable != null)
                    {
                        foreach (Node item in enumerable)
                        {
                            contents.Add(Project(Content.Create(item), expansion, selection));
                            if (++count > ODataHandler.EXPANSIONLIMIT)
                                break;
                        }
                    }
                }
            }
            return contents;
        }
        private Dictionary<string, object> ProjectSingleRefContent(object references, List<Property> expansion, List<Property> selection)
        {
            if (references == null)
                return null;

            Node node = references as Node;
            if (node != null)
                return Project(Content.Create(node), expansion, selection);

            var enumerable = references as System.Collections.IEnumerable;
            if (enumerable != null)
                foreach (Node item in enumerable)
                    return Project(Content.Create(item), expansion, selection);

            return null;
        }

    }
}
