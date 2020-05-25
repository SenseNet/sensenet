using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Fields;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using SenseNet.OData.Writers;
using SenseNet.Search;

// ReSharper disable RedundantBaseQualifier
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.OData
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
        internal override ODataEntity Project(Content content, HttpContext httpContext)
        {
            return Project(content, _expandTree, httpContext);
        }
        private ODataEntity Project(Content content, List<Property> expandTree, HttpContext httpContext)
        {
            var outfields = new ODataEntity();
            var selfurl = GetSelfUrl(content);
            if (this.Request.EntityMetadata != MetadataFormat.None)
                outfields.Add("__metadata", GetMetadata(content, selfurl, this.Request.EntityMetadata, httpContext));
            var fields = content.Fields.Values;

            var expansionEnabled = !content.ContentHandler.IsHeadOnly;
            foreach (var field in fields)
            {
                if (ODataMiddleware.DisabledFieldNames.Contains(field.Name))
                    continue;

                var propertyName = field.Name;

                var expansion = expansionEnabled ? GetExpansion(propertyName, expandTree) : null;

                if (expansion != null)
                {
                    outfields.Add(propertyName, Project(field, expansion.Children, httpContext));
                }
                else
                {
                    outfields.Add(propertyName,
                        base.IsAllowedField(content, field.Name) ? ODataWriter.GetJsonObject(field, selfurl) : null);
                }
            }

            AddField(content, expandTree, outfields, ACTIONSPROPERTY, httpContext, GetActions);
            AddField(content, expandTree, outfields, ODataMiddleware.ChildrenPropertyName, httpContext, (c, ctx) =>
            {
                // disable autofilters by default the same way as in ODataWriter.WriteChildrenCollection
                c.ChildrenDefinition.EnableAutofilters =
                    Request.AutofiltersEnabled != FilterStatus.Default
                        ? Request.AutofiltersEnabled
                        : FilterStatus.Disabled;

                var expansion = GetExpansion(ODataMiddleware.ChildrenPropertyName, expandTree);

                return ProjectMultiRefContents(c.Children.AsEnumerable().Select(cnt => cnt.ContentHandler), expansion.Children, httpContext);
            });

            if (!outfields.ContainsKey(ICONPROPERTY))
                outfields.Add(ICONPROPERTY, content.Icon ?? content.ContentType.Icon);

            outfields.Add(ISFILEPROPERTY, content.Fields.ContainsKey(ODataMiddleware.BinaryPropertyName));

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
        private object Project(Field field, List<Property> expansion, HttpContext httpContext)
        {
            if (!(field is ReferenceField refField))
            {
                if (field is BinaryField binaryField)
                    return TextFileHandler.ProjectBinaryField(binaryField, null, httpContext);
                if (!(field is AllowedChildTypesField allowedChildTypesField))
                    return null;
                return ProjectMultiRefContents(allowedChildTypesField.GetData(), expansion, httpContext);
            }

            var refFieldSetting = refField.FieldSetting as ReferenceFieldSetting;
            var isMultiRef = true;
            if (refFieldSetting != null)
                isMultiRef = refFieldSetting.AllowMultiple == true;

            return isMultiRef
                ? ProjectMultiRefContents(refField.GetData(), expansion, httpContext)
                : (object)ProjectSingleRefContent(refField.GetData(), expansion, httpContext);
        }
        private List<ODataEntity> ProjectMultiRefContents(object references, List<Property> expansion, HttpContext httpContext)
        {
            var contents = new List<ODataEntity>();
            if (references != null)
            {
                if (references is Node node)
                {
                    contents.Add(Project(Content.Create(node), expansion, httpContext));
                }
                else
                {
                    var enumerable = references as IEnumerable;
                    var count = 0;
                    if (enumerable != null)
                    {
                        foreach (Node item in enumerable)
                        {
                            contents.Add(Project(Content.Create(item), expansion, httpContext));
                            if (++count > ODataMiddleware.ExpansionLimit)
                                break;
                        }
                    }
                }
            }
            return contents;
        }
        private ODataEntity ProjectSingleRefContent(object references, List<Property> expansion, HttpContext httpContext)
        {
            if (references == null)
                return null;

            if (references is Node node)
                return Project(Content.Create(node), expansion, httpContext);

            if (references is IEnumerable enumerable)
                foreach (Node item in enumerable)
                    return Project(Content.Create(item), expansion, httpContext);

            return null;
        }

        private void AddField(Content content, List<Property> expandTree, IDictionary<string, object> fields, 
            string fieldName, HttpContext httpContext, Func<Content, HttpContext, object> getFieldValue)
        {
            var expansion = GetExpansion(fieldName, expandTree);
            fields.Add(fieldName,
                expansion == null
                    ? ODataReference.Create(string.Concat(GetSelfUrl(content), "/", fieldName))
                    : getFieldValue?.Invoke(content, httpContext));
        }
    }
}
