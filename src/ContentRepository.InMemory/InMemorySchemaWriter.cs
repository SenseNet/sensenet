using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.InMemory
{
    internal class InMemorySchemaWriter : SchemaWriter
    {
        private readonly Action _finishedCallback;
        private readonly RepositorySchemaData _schema;

        public InMemorySchemaWriter(RepositorySchemaData originalSchema, Action finishedCallback)
        {
            _schema = originalSchema;
            _finishedCallback = finishedCallback;
        }

        public override STT.Task WriteSchemaAsync(RepositorySchemaData schema)
        {
            throw new NotSupportedException();
        }

        public override void Open()
        {
            // Do nothing
        }
        public override void Close()
        {
            _finishedCallback();
        }

        public override void CreatePropertyType(string name, DataType dataType, int mapping, bool isContentListProperty)
        {
            _schema.PropertyTypes.Add(new PropertyTypeData
            {
                Id = GetMaxId(_schema.PropertyTypes) + 1,
                Name = name,
                DataType = dataType,
                Mapping = mapping,
                IsContentListProperty = isContentListProperty
            });
        }

        public override void DeletePropertyType(PropertyType propertyType)
        {
            var pt = _schema.PropertyTypes.FirstOrDefault(p => p.Name == propertyType.Name &&
                                                               p.IsContentListProperty == propertyType.IsContentListProperty);
            if (pt != null)
                _schema.PropertyTypes.Remove(pt);
        }

        public override void CreateNodeType(NodeType parent, string name, string className)
        {
            _schema.NodeTypes.Add(new NodeTypeData
            {
                Id = Math.Max(GetMaxId(_schema.NodeTypes), GetMaxId(_schema.ContentListTypes)) + 1,
                Name = name,
                ParentName = parent?.Name,
                ClassName = className,
                Properties = new List<string>()
            });
        }

        public override void ModifyNodeType(NodeType nodeType, NodeType parent, string className)
        {
            var nt = _schema.NodeTypes.FirstOrDefault(p => p.Name == nodeType.Name);
            if (nt == null)
                return;
            nt.ParentName = parent?.Name;
            nt.ClassName = className;
        }

        public override void DeleteNodeType(NodeType nodeType)
        {
            var nt = _schema.NodeTypes.FirstOrDefault(p => p.Name == nodeType.Name);
            if (nt != null)
                _schema.NodeTypes.Remove(nt);
        }

        public override void CreateContentListType(string name)
        {
            _schema.ContentListTypes.Add(new ContentListTypeData
            {
                Id = Math.Max(GetMaxId(_schema.NodeTypes), GetMaxId(_schema.ContentListTypes)) + 1,
                Name = name,
                Properties = new List<string>()
            });
        }

        public override void DeleteContentListType(ContentListType contentListType)
        {
            var ct = _schema.ContentListTypes.FirstOrDefault(p => p.Name == contentListType.Name);
            if (ct != null)
                _schema.ContentListTypes.Remove(ct);
        }

        public override void AddPropertyTypeToPropertySet(PropertyType propertyType, PropertySet owner, bool isDeclared)
        {
            List<string> properties;
            var nt = _schema.NodeTypes.FirstOrDefault(p => p.Name == owner.Name);
            if (nt != null)
            {
                if (!isDeclared)
                    return;
                properties = nt.Properties;
            }
            else
            {
                var ct = _schema.ContentListTypes.FirstOrDefault(p => p.Name == owner.Name);
                if (ct == null)
                    return;
                properties = ct.Properties;
            }

            if (!properties.Contains(propertyType.Name))
                properties.Add(propertyType.Name);
        }

        public override void RemovePropertyTypeFromPropertySet(PropertyType propertyType, PropertySet owner)
        {
            var nt = _schema.NodeTypes.FirstOrDefault(p => p.Name == owner.Name);
            if (nt != null)
            {
                nt.Properties.Remove(propertyType.Name);
                return;
            }
            var ct = _schema.ContentListTypes.FirstOrDefault(p => p.Name == owner.Name);
            ct?.Properties.Remove(propertyType.Name);
        }

        public override void UpdatePropertyTypeDeclarationState(PropertyType propertyType, NodeType owner, bool isDeclared)
        {
            var nt = _schema.NodeTypes.FirstOrDefault(p => p.Name == owner.Name);
            if (nt == null)
                return;

            if (isDeclared)
            {
                if (!nt.Properties.Contains(propertyType.Name))
                    nt.Properties.Add(propertyType.Name);
            }
            else
            {
                nt.Properties.Remove(propertyType.Name);
            }
        }

        /* ========================================================================================================= Tools */

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private int GetMaxId(IEnumerable<ISchemaItemData> list)
        {
            return list.Any() ? list.Max(p => p.Id) : 0;
        }

    }
}
