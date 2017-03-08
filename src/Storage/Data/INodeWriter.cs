using System;
using System.IO;
using SenseNet.ContentRepository.Storage.Schema;
using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Data
{
    public interface INodeWriter
    {
        void Open();
        void Close();

        void InsertNodeAndVersionRows(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);

        // ============================================================================ Node Insert/Update

        void UpdateSubTreePath(string oldPath, string newPath);
        void UpdateNodeRow(NodeData nodeData);

        // ============================================================================ Version Insert/Update

        void UpdateVersionRow(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        void CopyAndUpdateVersion(NodeData nodeData, int previousVersionId, out int lastMajorVersionId, out int lastMinorVersionId);
        void CopyAndUpdateVersion(NodeData nodeData, int previousVersionId, int destinationVersionId, out int lastMajorVersionId, out int lastMinorVersionId);

        // ============================================================================ Property Insert/Update

        void SaveStringProperty(int versionId, PropertyType propertyType, string value);
        void SaveDateTimeProperty(int versionId, PropertyType propertyType, DateTime value);
        void SaveIntProperty(int versionId, PropertyType propertyType, int value);
        void SaveCurrencyProperty(int versionId, PropertyType propertyType, decimal value);
        void SaveTextProperty(int versionId, PropertyType propertyType, bool isLoaded, string value);
        void SaveReferenceProperty(int versionId, PropertyType propertyType, IEnumerable<int> value);
        void InsertBinaryProperty(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode);
        void UpdateBinaryProperty(BinaryDataValue value);
        void DeleteBinaryProperty(int versionId, PropertyType propertyType);

    }
}