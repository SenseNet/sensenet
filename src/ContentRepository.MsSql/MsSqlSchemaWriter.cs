using System;
using System.Linq;
using STT=System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    public class MsSqlSchemaWriter : SchemaWriter
    {
        private readonly string _connectionString;

        public MsSqlSchemaWriter(IOptions<ConnectionStringOptions> connectionStrings)
        {
            _connectionString = connectionStrings.Value.Repository;
        }

        public override bool CanWriteDifferences => false;

        public override async STT.Task WriteSchemaAsync(RepositorySchemaData schema)
        {
            var propertyTypes = schema.PropertyTypes.Where(x => x.Id == 0).ToArray();
            if (propertyTypes.Any())
            {
                var lastId = schema.PropertyTypes.Max(x => x.Id);
                foreach (var propertyType in propertyTypes)
                    propertyType.Id = ++lastId;
            }
            var nodeTypes = schema.NodeTypes.Where(x => x.Id == 0).ToArray();
            if (nodeTypes.Any())
            {
                var lastId = schema.NodeTypes.Max(x => x.Id);
                foreach (var nodeType in nodeTypes)
                    nodeType.Id = ++lastId;
            }
            var contentListTypes = schema.ContentListTypes.Where(x => x.Id == 0).ToArray();
            if (contentListTypes.Any())
            {
                var lastId = schema.ContentListTypes.Max(x => x.Id);
                foreach (var contentListType in contentListTypes)
                    contentListType.Id = ++lastId;
            }

            var installer = new MsSqlSchemaInstaller(_connectionString);
            await installer.InstallSchemaAsync(schema).ConfigureAwait(false);
        }

        #region unused methods
        public override void Open()
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            throw new NotSupportedException();
        }

        public override void CreatePropertyType(string name, DataType dataType, int mapping, bool isContentListProperty)
        {
            throw new NotSupportedException();
        }

        public override void DeletePropertyType(PropertyType propertyType)
        {
            throw new NotSupportedException();
        }

        public override void CreateNodeType(NodeType parent, string name, string className)
        {
            throw new NotSupportedException();
        }

        public override void ModifyNodeType(NodeType nodeType, NodeType parent, string className)
        {
            throw new NotSupportedException();
        }

        public override void DeleteNodeType(NodeType nodeType)
        {
            throw new NotSupportedException();
        }

        public override void CreateContentListType(string name)
        {
            throw new NotSupportedException();
        }

        public override void DeleteContentListType(ContentListType contentListType)
        {
            throw new NotSupportedException();
        }

        public override void AddPropertyTypeToPropertySet(PropertyType propertyType, PropertySet owner, bool isDeclared)
        {
            throw new NotSupportedException();
        }

        public override void RemovePropertyTypeFromPropertySet(PropertyType propertyType, PropertySet owner)
        {
            throw new NotSupportedException();
        }

        public override void UpdatePropertyTypeDeclarationState(PropertyType propertyType, NodeType owner, bool isDeclared)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
