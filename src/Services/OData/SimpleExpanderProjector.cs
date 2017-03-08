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
    internal class SimpleExpanderProjector : Projector
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

                var prop = new Property { Name = name };
                Children.Add(prop);
                return prop;
            }
            public static Property EnsureChild(List<Property> globals, string name)
            {
                foreach (var property in globals)
                    if (property.Name == name)
                        return property;
                var prop = new Property { Name = name };
                globals.Add(prop);
                return prop;
            }
            internal bool ___debug(StringBuilder sb, int indent)
            {
                sb.Append(' ', indent * 2).AppendLine(Name);
                if (Children != null)
                    foreach (var p in Children)
                        p.___debug(sb, indent + 1);
                return true;
            }
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

        internal override void Initialize(Content container)
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
        }
        internal override Dictionary<string, object> Project(Content content)
        {
            return Project(content, _expandTree);
        }
        private Dictionary<string, object> Project(Content content, List<Property> expandTree)
        {
            Property expansion;

            var outfields = new Dictionary<string, object>();
            var selfurl = GetSelfUrl(content);
            if (this.Request.EntityMetadata != MetadataFormat.None)
                outfields.Add("__metadata", GetMetadata(content, selfurl, this.Request.EntityMetadata));
            var fields = content.Fields.Values;

            var expansionEnabled = !content.ContentHandler.IsHeadOnly;
            foreach (var field in fields)
            {
                if (ODataHandler.DisabledFieldNames.Contains(field.Name))
                    continue;

                var propertyName = field.Name;

                expansion = expansionEnabled ? GetExpansion(propertyName, expandTree) : null;

                if (expansion != null)
                {
                    outfields.Add(propertyName, Project(field, expansion.Children));
                }
                else
                {
                    if (base.IsAllowedField(content, field.Name))
                        outfields.Add(propertyName, ODataFormatter.GetJsonObject(field, selfurl));
                    else
                        outfields.Add(propertyName, null);
                }
            }

            var actionExpansion = GetExpansion(ACTIONSPROPERTY, expandTree);
            if (actionExpansion == null)
                outfields.Add(ACTIONSPROPERTY, ODataReference.Create(String.Concat(selfurl, "/", ODataHandler.PROPERTY_ACTIONS)));
            else
                outfields.Add(ACTIONSPROPERTY, GetActions(content));

            if (!outfields.ContainsKey(ICONPROPERTY))
                outfields.Add(ICONPROPERTY, content.Icon ?? content.ContentType.Icon);

            outfields.Add(ISFILEPROPERTY, content.Fields.ContainsKey(ODataHandler.PROPERTY_BINARY));

            return outfields;
        }
        private Property GetExpansion(string fieldName, List<Property> expandTree)
        {
            if (expandTree == null)
                return null;
            for (int i = 0; i < expandTree.Count; i++)
                if (expandTree[i].Name == fieldName)
                    return expandTree[i];
            return null;
        }
        private object Project(Field field, List<Property> expansion)
        {
            var refField = field as ReferenceField;
            if (refField == null)
            {
                var allowedChildTypesField = field as AllowedChildTypesField;
                if (allowedChildTypesField == null)
                    return null;
                return ProjectMultiRefContents(allowedChildTypesField.GetData(), expansion);
            }

            var refFieldSetting = refField.FieldSetting as ReferenceFieldSetting;
            var isMultiRef = true;
            if (refFieldSetting != null)
                isMultiRef = refFieldSetting.AllowMultiple == true;

            return isMultiRef
                ? (object)ProjectMultiRefContents(refField.GetData(), expansion)
                : (object)ProjectSingleRefContent(refField.GetData(), expansion);
        }
        private List<Dictionary<string, object>> ProjectMultiRefContents(object references, List<Property> expansion)
        {
            var contents = new List<Dictionary<string, object>>();
            if (references != null)
            {
                Node node = references as Node;
                if (node != null)
                {
                    contents.Add(Project(Content.Create(node), expansion));
                }
                else
                {
                    var enumerable = references as System.Collections.IEnumerable;
                    var count = 0;
                    if (enumerable != null)
                    {
                        foreach (Node item in enumerable)
                        {
                            contents.Add(Project(Content.Create(item), expansion));
                            if (++count > ODataHandler.EXPANSIONLIMIT)
                                break;
                        }
                    }
                }
            }
            return contents;
        }
        private Dictionary<string, object> ProjectSingleRefContent(object references, List<Property> expansion)
        {
            if (references == null)
                return null;

            Node node = references as Node;
            if (node != null)
                return Project(Content.Create(node), expansion);

            var enumerable = references as System.Collections.IEnumerable;
            if (enumerable != null)
                foreach (Node item in enumerable)
                    return Project(Content.Create(item), expansion);

            return null;
        }
    }
}
