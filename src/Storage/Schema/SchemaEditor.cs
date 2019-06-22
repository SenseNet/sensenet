using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Diagnostics;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Schema
{
    public sealed class SchemaEditor : SchemaRoot
    {
        public SchemaEditor() { }

        public void Register()
        {
            var origSchema = Retrier.Retry(3, 100, typeof(InvalidSchemaException), () =>
            {
                var sche = new SchemaEditor();
                sche.Load();
                return sche;
            });

            if (DataStore.Enabled)
            {
                var schemaLock = DataStore.StartSchemaUpdateAsync(this.SchemaTimestamp).Result;
                var schemaWriter = DataStore.CreateSchemaWriter();
                try
                {
                    RegisterSchema(origSchema, this, schemaWriter);
                }
                finally
                {
                    DataStore.FinishSchemaUpdateAsync(schemaLock).Wait();
                }
            }
            else
            {
                DataProvider.Current.AssertSchemaTimestampAndWriteModificationDate(this.SchemaTimestamp); //DB:ok
                var schemaWriter = DataProvider.Current.CreateSchemaWriter(); //DB:ok
                RegisterSchema(origSchema, this, schemaWriter);
            }
        }

        private static void RegisterSchema(SchemaEditor origSchema, SchemaEditor newSchema, SchemaWriter schemaWriter)
        {
            if (schemaWriter.CanWriteDifferences)
            {
                using (var op = SnTrace.Database.StartOperation("Write storage schema modifications."))
                {
                    // Ensure transaction encapsulation
                    bool isLocalTransaction = !TransactionScope.IsActive;
                    if (isLocalTransaction)
                        TransactionScope.Begin();
                    try
                    {
                        List<PropertySet> modifiedPropertySets = new List<PropertySet>();
                        schemaWriter.Open();
                        WriteSchemaModifications(origSchema, newSchema, schemaWriter, modifiedPropertySets);
                        foreach (PropertySet modifiedPropertySet in modifiedPropertySets)
                        {
                            NodeTypeDependency.FireChanged(modifiedPropertySet.Id);
                        }
                        schemaWriter.Close();
                        if (isLocalTransaction)
                            TransactionScope.Commit();
                        ActiveSchema.Reset();
                        op.Successful = true;
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex, null, EventId.RepositoryRuntime);
                        throw new SchemaEditorCommandException("Error during schema registration.", ex);
                    }
                    finally
                    {
                        IDisposable unmanagedWriter = schemaWriter as IDisposable;
                        if (unmanagedWriter != null)
                            unmanagedWriter.Dispose();
                        try
                        {
                            if (isLocalTransaction && TransactionScope.IsActive)
                                TransactionScope.Rollback();
                        }
                        catch (Exception ex2)
                        {
                            // This catch block will handle any errors that may have occurred 
                            // on the server that would cause the rollback to fail, such as 
                            // a closed connection (MSDN).
                            const string msg = "Error during schema transaction rollback.";
                            SnLog.WriteException(ex2, msg, EventId.RepositoryRuntime);

                            throw new SchemaEditorCommandException(msg, ex2);
                        }
                    }
                }
            }
            else
            {
                using (var op = SnTrace.Database.StartOperation("Update storage schema."))
                {
                    schemaWriter.WriteSchemaAsync(newSchema.ToRepositorySchemaData()).Wait();
                    ActiveSchema.Reset();
                    op.Successful = true;
                }
            }
        }

        private static void WriteSchemaModifications(SchemaEditor origSchema, SchemaEditor newSchema, SchemaWriter writer, List<PropertySet> modifiedPropertySets)
        {
            // #1: Delete types

            foreach (NodeType type in GetNodeTypeRootsToDelete(origSchema.NodeTypes, newSchema.NodeTypes))
                WriteDeleteNodeType(writer, type, origSchema, modifiedPropertySets);
            foreach (var type in GetTypesToDelete<ContentListType>(origSchema.ContentListTypes, newSchema.ContentListTypes))
                WriteDeleteContentListType(writer, type, origSchema, modifiedPropertySets);

            // #2: Create or modify types

            WriteCreateOrModifyPropertyTypes(origSchema, newSchema, writer);
            WriteCreateOrModifyNodeTypes(origSchema, newSchema, modifiedPropertySets, writer);
            WriteCreateOrModifyContentListTypes(origSchema, newSchema, modifiedPropertySets, writer);

            // #3: Delete PropertyTypes
            foreach (PropertyType type in GetTypesToDelete<PropertyType>(origSchema.PropertyTypes, newSchema.PropertyTypes))
                WriteDeletePropertyType(writer, type);
        }

        // -------------------------------------------------------------- Remove commands

        private static void WriteDeletePropertyType(SchemaWriter writer, PropertyType propType)
        {
            writer.DeletePropertyType(propType);
        }
        private static void WriteDeleteNodeType(SchemaWriter writer, NodeType nodeType, SchemaEditor origSchema, List<PropertySet> modifiedPropertySets)
        {
            // recursive
            foreach (NodeType childType in nodeType.Children)
                WriteDeleteNodeType(writer, childType, origSchema, modifiedPropertySets);

            writer.DeleteNodeType(nodeType);
            if (!modifiedPropertySets.Contains(nodeType))
                modifiedPropertySets.Add(nodeType);
        }
        private static void WriteDeleteContentListType(SchemaWriter writer, ContentListType contentListType, SchemaEditor origSchema, List<PropertySet> modifiedPropertySets)
        {
            writer.DeleteContentListType(contentListType);
            if (!modifiedPropertySets.Contains(contentListType))
                modifiedPropertySets.Add(contentListType);
        }

        // -------------------------------------------------------------- CreateOrRemove commands

        private static void WriteCreateOrModifyPropertyTypes(SchemaEditor origSchema, SchemaEditor newSchema, SchemaWriter writer)
        {
            // new
            foreach (PropertyType newType in newSchema.PropertyTypes)
            {
                if (NeedToCreate<PropertyType>(origSchema.PropertyTypes, newType))
                    writer.CreatePropertyType(newType.Name, newType.DataType, newType.Mapping, newType.IsContentListProperty);
            }
        }
        private static void WriteCreateOrModifyNodeTypes(SchemaEditor origSchema, SchemaEditor newSchema, List<PropertySet> modifiedPropertySets, SchemaWriter writer)
        {
            List<NodeType> _nodeTypesToEnumerate = new List<NodeType>();

            // collect only roots
            foreach (NodeType rootNodeType in newSchema.NodeTypes)
                if (rootNodeType.Parent == null)
                    _nodeTypesToEnumerate.Add(rootNodeType);

            int index = 0;
            while (index < _nodeTypesToEnumerate.Count)
            {
                NodeType currentType = _nodeTypesToEnumerate[index++];
                NodeType origType = null;

                if (NeedToCreate<NodeType>(origSchema.NodeTypes, currentType))
                {
                    writer.CreateNodeType(currentType.Parent, currentType.Name, currentType.ClassName);
                }
                else
                {
                    origType = origSchema.NodeTypes[currentType.Name];
                    string origParentName = origType.Parent == null ? null : origType.Parent.Name;
                    string newParentName = currentType.Parent == null ? null : currentType.Parent.Name;
                    bool parentChanged = origParentName != newParentName;
                    if (parentChanged || origType.ClassName != currentType.ClassName)
                    {
                        writer.ModifyNodeType(origType, currentType.Parent, currentType.ClassName);
                        if (!modifiedPropertySets.Contains(origType))
                            modifiedPropertySets.Add(origType);
                    }
                }

                // Property list (origType can be null)
                WriteAddOrRemovePropertyTypes(origType, currentType, modifiedPropertySets, writer);

                // Add children to enumerator
                _nodeTypesToEnumerate.AddRange(currentType.GetChildren());
            }
        }
        private static void WriteCreateOrModifyContentListTypes(SchemaEditor origSchema, SchemaEditor newSchema, List<PropertySet> modifiedPropertySets, SchemaWriter writer)
        {
            foreach (var newType in newSchema.ContentListTypes)
            {
                if (NeedToCreate<ContentListType>(origSchema.ContentListTypes, newType))
                    writer.CreateContentListType(newType.Name);
                WriteAddOrRemovePropertyTypes(origSchema.ContentListTypes[newType.Name], newType, modifiedPropertySets, writer);
            }
        }

        private static void WriteAddOrRemovePropertyTypes(NodeType origSet, NodeType newSet, List<PropertySet> modifiedPropertySets, SchemaWriter writer)
        {
            if (origSet == null)
            {
                // New NodeType: add all property
                foreach (PropertyType propType in newSet.PropertyTypes)
                    writer.AddPropertyTypeToPropertySet(propType, newSet, newSet.DeclaredPropertyTypes.Contains(propType));
                return;
            }
            bool origSetChanged = false;
            // Delete PropertyType if needed
            foreach (PropertyType propType in GetTypesToDelete<PropertyType>(origSet.PropertyTypes, newSet.PropertyTypes))
            {
                writer.RemovePropertyTypeFromPropertySet(propType, newSet);
                origSetChanged = true;
            }

            // Create or modify PropertyTypes
            foreach (PropertyType propType in newSet.PropertyTypes)
            {
                if (NeedToCreate<PropertyType>(origSet.PropertyTypes, propType))
                {
                    bool isDeclared = newSet.DeclaredPropertyTypes.Contains(propType);
                    writer.AddPropertyTypeToPropertySet(propType, newSet, isDeclared);
                    origSetChanged = true;
                }
                else
                {
                    // Modify Property declaration if needed (by the modifications in DeclaredPropertyTypes)
                    bool newIsDeclared = newSet.DeclaredPropertyTypes.Contains(propType);
                    bool origIsDeclared = origSet.DeclaredPropertyTypes[propType.Name] != null;
                    if (newIsDeclared != origIsDeclared)
                        writer.UpdatePropertyTypeDeclarationState(propType, newSet, newIsDeclared);
                }
            }

            if (origSetChanged && !modifiedPropertySets.Contains(origSet))
                modifiedPropertySets.Add(origSet);
        }
        private static void WriteAddOrRemovePropertyTypes(ContentListType origSet, ContentListType newSet, List<PropertySet> modifiedPropertySets, SchemaWriter writer)
        {
            bool origSetChanged = false;
            if (origSet == null)
            {
                // New NodeType: add all property
                foreach (PropertyType propType in newSet.PropertyTypes)
                    writer.AddPropertyTypeToPropertySet(propType, newSet, true);
                return;
            }
            // Delete PropertyType if needed
            foreach (PropertyType propType in GetTypesToDelete<PropertyType>(origSet.PropertyTypes, newSet.PropertyTypes))
            {
                writer.RemovePropertyTypeFromPropertySet(propType, newSet);
                origSetChanged = true;
            }

            // Create or modify PropertyTypes
            foreach (PropertyType propType in newSet.PropertyTypes)
            {
                if (NeedToCreate<PropertyType>(origSet.PropertyTypes, propType))
                {
                    writer.AddPropertyTypeToPropertySet(propType, newSet, true);
                    origSetChanged = true;
                }
            }

            if (origSetChanged && !modifiedPropertySets.Contains(origSet))
                modifiedPropertySets.Add(origSet);
        }

        // ============================================================== Tools

        private static List<T> GetTypesToDelete<T>(TypeCollection<T> origTypes, TypeCollection<T> newTypes) where T : SchemaItem
        {
            List<T> toDelete = new List<T>();
            foreach (T origType in origTypes)
            {
                T newType = newTypes[origType.Name];
                // Need to delete if newTypes does not contain this name or
                //    Id of new is 0 and Id of old is not 0.
                if (newType == null || (newType.Id == 0 && newType.Id != origType.Id))
                    toDelete.Add(origType);
            }
            return toDelete;
        }
        private static bool NeedToCreate<T>(TypeCollection<T> origTypes, T newType) where T : SchemaItem
        {
            // Need to create if origTypes does not contain this name or
            //    Id of new is 0 and Id of old is not 0.
            T origType = origTypes[newType.Name];
            return origType == null || (newType.Id == 0 && origType.Id != 0);
        }
        private static List<NodeType> GetNodeTypeRootsToDelete(TypeCollection<NodeType> origSet, TypeCollection<NodeType> newSet)
        {
            // Walks (preorder) origSet NodeType tree and collects only deletable subtree roots.

            List<NodeType> _nodeTypeRootsToDelete = new List<NodeType>();
            List<NodeType> _nodeTypesToEnumerate = new List<NodeType>();

            // collect only roots
            foreach (NodeType rootNodeType in origSet)
                if (rootNodeType.Parent == null)
                    _nodeTypesToEnumerate.Add(rootNodeType);

            int index = 0;
            while (index < _nodeTypesToEnumerate.Count)
            {
                NodeType currentType = _nodeTypesToEnumerate[index++];

                // delete currentType if newSet does not contain it otherwise add its children to enumerator
                if (newSet.GetItemById(currentType.Id) == null)
                    _nodeTypeRootsToDelete.Add(currentType);
                else
                    _nodeTypesToEnumerate.AddRange(currentType.GetChildren());
            }

            return _nodeTypeRootsToDelete;
        }
    }
}