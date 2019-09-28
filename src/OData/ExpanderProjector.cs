using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Fields;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using SenseNet.OData.Formatters;
using SenseNet.Search;
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.OData
{
    internal class ExpanderProjector : Projector
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

                var prop = name == "*" ? Joker : new Property { Name = name, Path = string.Concat(this.Path, "/", name) };
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

            public static readonly Property Joker;
            public static readonly List<Property> JokerList;
            static Property()
            {
                Joker = new Property { Name = "*", Path = "***" };
                JokerList = new List<Property>(new[] { Joker });
                Joker.Children = JokerList;
            }

            public string Path { get; private set; }
        }

        private List<Property> _expandTree;

        private List<Property> _selectTree;

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

        internal override ODataEntity Project(Content content, HttpContext httpContext)
        {
            if (content.ContentHandler.IsHeadOnly)
                return Project(content, new List<Property>(0), _selectTree, httpContext);
            return Project(content, _expandTree, _selectTree, httpContext);
        }
        private ODataEntity Project(Content content, List<Property> expandTree, List<Property> selectTree, HttpContext httpContext)
        {
            var outfields = new ODataEntity();
            var selfurl = GetSelfUrl(content);
            if (this.Request.EntityMetadata != MetadataFormat.None)
                outfields.Add("__metadata", GetMetadata(content, selfurl, this.Request.EntityMetadata, httpContext));

            var hasJoker = false;
            foreach (var property in selectTree)
            {
                var propertyName = property.Name;
                if (propertyName == "*")
                {
                    hasJoker = true;
                    continue;
                }
                if (!content.Fields.TryGetValue(propertyName, out var field))
                {
                    switch (propertyName)
                    {
                        case ACTIONSPROPERTY:
                            AddField(content, expandTree, outfields, ACTIONSPROPERTY, httpContext, GetActions);
                            break;
                        case ICONPROPERTY:
                            outfields.Add(ICONPROPERTY, content.Icon ?? content.ContentType.Icon);
                            break;
                        case ISFILEPROPERTY:
                            outfields.Add(ISFILEPROPERTY, content.Fields.ContainsKey(ODataMiddleware.BinaryPropertyName));
                            break;
                        case ODataMiddleware.ChildrenPropertyName:
                            var expansion = GetPropertyFromList(ODataMiddleware.ChildrenPropertyName, expandTree);
                            AddField(content, expansion, outfields, ODataMiddleware.ChildrenPropertyName, httpContext,
                                (c, ctx) =>
                                {
                                    // disable autofilters by default the same way as in ODataFormatter.WriteChildrenCollection
                                    c.ChildrenDefinition.EnableAutofilters =
                                        Request.AutofiltersEnabled != FilterStatus.Default
                                            ? Request.AutofiltersEnabled
                                            : FilterStatus.Disabled;

                                    return ProjectMultiRefContents(
                                        c.Children.AsEnumerable().Select(cnt => cnt.ContentHandler), expansion.Children,
                                        property.Children, httpContext);
                                });
                            break;
                        default:
                            outfields.Add(propertyName, null);
                            break;
                    }
                }
                else
                {
                    if (ODataMiddleware.DisabledFieldNames.Contains(field.Name))
                    {
                        outfields.Add(propertyName, null);
                    }
                    else
                    {
                        var expansion = GetPropertyFromList(propertyName, expandTree);
                        if (expansion != null)
                        {
                            outfields.Add(propertyName, Project(field, expansion.Children, property.Children ?? Property.JokerList, httpContext));
                        }
                        else
                        {
                            outfields.Add(propertyName,
                                IsAllowedField(content, field.Name)
                                    ? ODataFormatter.GetJsonObject(field, selfurl)
                                    : null);
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
                    outfields.Add(propertyName,
                        expansion != null
                            ? Project(contentField, expansion.Children, Property.JokerList, httpContext)
                            : ODataFormatter.GetJsonObject(contentField, selfurl));
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
            foreach (Property property in propertyTree)
            {
                if (property.Name == fieldName)
                    return property;
                else if (property == Property.Joker)
                    hasJoker = true;
            }
            if (hasJoker)
                return Property.Joker;

            return null;
        }
        private object Project(Field field, List<Property> expansion, List<Property> selection, HttpContext httpContext)
        {
            if (!(field is ReferenceField refField))
            {
                if (!(field is AllowedChildTypesField allowedChildTypesField))
                    return null;
                return ProjectMultiRefContents(allowedChildTypesField.GetData(), expansion, selection, httpContext);
            }

            var refFieldSetting = refField.FieldSetting as ReferenceFieldSetting;
            var isMultiRef = true;
            if (refFieldSetting != null)
                isMultiRef = refFieldSetting.AllowMultiple == true;

            return isMultiRef
                ? ProjectMultiRefContents(refField.GetData(), expansion, selection, httpContext)
                : (object)ProjectSingleRefContent(refField.GetData(), expansion, selection, httpContext);
        }
        private List<ODataEntity> ProjectMultiRefContents(object references, List<Property> expansion, List<Property> selection, HttpContext httpContext)
        {
            var contents = new List<ODataEntity>();
            if (references != null)
            {
                if (references is Node node)
                {
                    contents.Add(Project(Content.Create(node), expansion, selection, httpContext));
                }
                else
                {
                    var enumerable = references as IEnumerable;
                    var count = 0;
                    if (enumerable != null)
                    {
                        foreach (Node item in enumerable)
                        {
                            contents.Add(Project(Content.Create(item), expansion, selection, httpContext));
                            if (++count > ODataMiddleware.ExpansionLimit)
                                break;
                        }
                    }
                }
            }
            return contents;
        }
        private ODataEntity ProjectSingleRefContent(object references, List<Property> expansion, List<Property> selection, HttpContext httpContext)
        {
            if (references == null)
                return null;

            if (references is Node node)
                return Project(Content.Create(node), expansion, selection, httpContext);

            if (references is IEnumerable enumerable)
                foreach (Node item in enumerable)
                    return Project(Content.Create(item), expansion, selection, httpContext);

            return null;
        }

        private void AddField(Content content, List<Property> expandTree, ODataEntity fields,
            string fieldName, HttpContext httpContext, Func<Content, HttpContext, object> getFieldValue)
        {
            var expansion = GetPropertyFromList(fieldName, expandTree);

            AddField(content, expansion, fields, fieldName, httpContext, getFieldValue);
        }
        private void AddField(Content content, Property expansion, ODataEntity fields,
            string fieldName, HttpContext httpContext, Func<Content, HttpContext, object> getFieldValue)
        {
            fields.Add(fieldName,
                expansion == null
                    ? ODataReference.Create(string.Concat(GetSelfUrl(content), "/", fieldName))
                    : getFieldValue?.Invoke(content, httpContext));
        }
    }
}
