using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using SenseNet.Common.Storage.Data;

namespace SenseNet.ContentRepository.Storage.Data
{
    //UNDONE:DB: BlobStorage ASYNC API
    internal class BlobStorage : BlobStorageBase
    {
        public new static void InsertBinaryProperty(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
        {
            BlobStorageBase.InsertBinaryProperty(value, versionId, propertyTypeId, isNewNode);
        }

        public new static void UpdateBinaryProperty(BinaryDataValue value)
        {
            BlobStorageBase.UpdateBinaryProperty(value);
        }

        public new static void DeleteBinaryProperty(int versionId, int propertyTypeId)
        {
            BlobStorageBase.DeleteBinaryProperty(versionId, propertyTypeId);
        }

        public new static void DeleteBinaryProperties(IEnumerable<int> versionIds, SnDataContext dataContext = null)
        {
            BlobStorageBase.DeleteBinaryProperties(versionIds, dataContext);
        }

        public new static BlobStorageContext GetBlobStorageContext(int fileId, bool clearStream = false, int versionId = 0, int propertyTypeId = 0)
        {
            return BlobStorageBase.GetBlobStorageContext(fileId, clearStream, versionId, propertyTypeId);
        }

        public new static BinaryDataValue LoadBinaryProperty(int versionId, int propertyTypeId)
        {
            return BlobStorageBase.LoadBinaryProperty(versionId, propertyTypeId);
        }

        public new static BinaryCacheEntity LoadBinaryCacheEntity(int nodeVersionId, int propertyTypeId)
        {
            return BlobStorageBase.LoadBinaryCacheEntity(nodeVersionId, propertyTypeId);
        }

        public new static byte[] LoadBinaryFragment(int fileId, long position, int count)
        {
            return BlobStorageBase.LoadBinaryFragment(fileId, position, count);
        }

        public new static string StartChunk(int versionId, int propertyTypeId, long fullSize)
        {
            return BlobStorageBase.StartChunk(versionId, propertyTypeId, fullSize);
        }

        public new static void WriteChunk(int versionId, string token, byte[] buffer, long offset, long fullSize)
        {
            BlobStorageBase.WriteChunk(versionId, token, buffer, offset, fullSize);
        }

        public new static void CommitChunk(int versionId, int propertyTypeId, string token, long fullSize, BinaryDataValue source = null)
        {
            BlobStorageBase.CommitChunk(versionId, propertyTypeId, token, fullSize, source);
        }

        public new static void CopyFromStream(int versionId, string token, Stream input)
        {
            BlobStorageBase.CopyFromStream(versionId, token, input);
        }

        /*================================================================== Maintenance*/

        public new static void CleanupFilesSetFlag()
        {
            BlobStorageBase.CleanupFilesSetFlag();
        }

        public new static bool CleanupFiles()
        {
            return BlobStorageBase.CleanupFiles();
        }

        /*==================================================================== Provider */

        public new static IBlobProvider BuiltInProvider => BlobStorageBase.BuiltInProvider;
        public new static Dictionary<string, IBlobProvider> Providers {
            get { return BlobStorageBase.Providers; }
            set { BlobStorageBase.Providers = value; }
        }

        public new static IBlobProvider GetProvider(long fullSize)
        {
            return BlobStorageBase.GetProvider(fullSize);
        }
        public new static IBlobProvider GetProvider(string providerName)
        {
            return BlobStorageBase.GetProvider(providerName);
        }
    }
}
