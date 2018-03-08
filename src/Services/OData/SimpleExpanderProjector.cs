using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Fields;
using System.Diagnostics;
// ReSharper disable CheckNamespace
// ReSharper disable RedundantBaseQualifier
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.Portal.OData
{
    internal class SimpleExpanderProjector : Projector
    {
        [DebuggerDisplay("{" + nameof(Name) + "}")]
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
        }

        private List<Property> _expandTree;

        internal override void Initialize(Content container)
        {
            // pre building property tree by expansions
            _expandTree = new List<Property>();
            foreach (var item in this.Request.Expand)
            {
                var chain = item.Split('/').Select(s => s.Trim()).ToArray();
                var prop = Property.EnsureChild(_expandTree, chain[0]);
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

                var expansion = expansionEnabled ? GetExpansion(propertyName, expandTree) : null;

                if (expansion != null)
                {
                    outfields.Add(propertyName, Project(field, expansion.Children));
                }
                else
                {
                    outfields.Add(propertyName,
                        base.IsAllowedField(content, field.Name) ? ODataFormatter.GetJsonObject(field, selfurl) : null);
                }
            }

            var actionExpansion = GetExpansion(ACTIONSPROPERTY, expandTree);
            if (actionExpansion == null)
                outfields.Add(ACTIONSPROPERTY, ODataReference.Create(String.Concat(selfurl, "/", ODataHandler.ActionsPropertyName)));
            else
                outfields.Add(ACTIONSPROPERTY, GetActions(content));

            if (!outfields.ContainsKey(ICONPROPERTY))
                outfields.Add(ICONPROPERTY, content.Icon ?? content.ContentType.Icon);

            outfields.Add(ISFILEPROPERTY, content.Fields.ContainsKey(ODataHandler.BinaryPropertyName));

            return outfields;
        }
        private Property GetExpansion(string fieldName, List<Property> expandTree)
        {
            if (expandTree == null)
                return null;
            foreach (Property property in expandTree)
                if (property.Name == fieldName)
                    return property;
            return null;
        }
        private object Project(Field field, List<Property> expansion)
        {
            if (!(field is ReferenceField refField))
            {
                if (!(field is AllowedChildTypesField allowedChildTypesField))
                    return null;
                return ProjectMultiRefContents(allowedChildTypesField.GetData(), expansion);
            }

            var refFieldSetting = refField.FieldSetting as ReferenceFieldSetting;
            var isMultiRef = true;
            if (refFieldSetting != null)
                isMultiRef = refFieldSetting.AllowMultiple == true;

            return isMultiRef
                ? ProjectMultiRefContents(refField.GetData(), expansion)
                : (object)ProjectSingleRefContent(refField.GetData(), expansion);
        }
        private List<Dictionary<string, object>> ProjectMultiRefContents(object references, List<Property> expansion)
        {
            var contents = new List<Dictionary<string, object>>();
            if (references != null)
            {
                if (references is Node node)
                {
                    contents.Add(Project(Content.Create(node), expansion));
                }
                else
                {
                    var enumerable = references as IEnumerable;
                    var count = 0;
                    if (enumerable != null)
                    {
                        foreach (Node item in enumerable)
                        {
                            contents.Add(Project(Content.Create(item), expansion));
                            if (++count > ODataHandler.ExpansionLimit)
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

            if (references is Node node)
                return Project(Content.Create(node), expansion);

            if (references is IEnumerable enumerable)
                foreach (Node item in enumerable)
                    return Project(Content.Create(item), expansion);

            return null;
        }
    }
}
