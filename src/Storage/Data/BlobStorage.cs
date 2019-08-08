using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data
{
    internal class BlobStorage : BlobStorageBase
    {
        public new static Task InsertBinaryPropertyAsync(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode, SnDataContext dataContext)
        {
            return BlobStorageBase.InsertBinaryPropertyAsync(value, versionId, propertyTypeId, isNewNode, dataContext);
        }

        public new static Task UpdateBinaryPropertyAsync(BinaryDataValue value, SnDataContext dataContext)
        {
            return BlobStorageBase.UpdateBinaryPropertyAsync(value, dataContext);
        }

        public new static Task DeleteBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext)
        {
            return BlobStorageBase.DeleteBinaryPropertyAsync(versionId, propertyTypeId, dataContext);
        }

        public new static Task DeleteBinaryPropertiesAsync(IEnumerable<int> versionIds, SnDataContext dataContext)
        {
            return BlobStorageBase.DeleteBinaryPropertiesAsync(versionIds, dataContext);
        }

        public new static Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, CancellationToken cancellationToken, bool clearStream = false, int versionId = 0, int propertyTypeId = 0)
        {
            return BlobStorageBase.GetBlobStorageContextAsync(fileId, clearStream, versionId, propertyTypeId, cancellationToken);
        }

        public new static Task<BinaryDataValue> LoadBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext)
        {
            return BlobStorageBase.LoadBinaryPropertyAsync(versionId, propertyTypeId, dataContext);
        }

        public new static Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int nodeVersionId, int propertyTypeId, CancellationToken cancellationToken)
        {
            return BlobStorageBase.LoadBinaryCacheEntityAsync(nodeVersionId, propertyTypeId, cancellationToken);
        }
        public new static Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int nodeVersionId, int propertyTypeId, SnDataContext dataContext)
        {
            return BlobStorageBase.LoadBinaryCacheEntityAsync(nodeVersionId, propertyTypeId, dataContext);
        }

        public new static Task<byte[]> LoadBinaryFragmentAsync(int fileId, long position, int count, CancellationToken cancellationToken)
        {
            return BlobStorageBase.LoadBinaryFragmentAsync(fileId, position, count, cancellationToken);
        }

        public new static Task<string> StartChunkAsync(int versionId, int propertyTypeId, long fullSize,
            CancellationToken cancellationToken)
        {
            return BlobStorageBase.StartChunkAsync(versionId, propertyTypeId, fullSize, cancellationToken);
        }

        public new static Task WriteChunkAsync(int versionId, string token, byte[] buffer, long offset, long fullSize,
            CancellationToken cancellationToken)
        {
            return BlobStorageBase.WriteChunkAsync(versionId, token, buffer, offset, fullSize, cancellationToken);
        }

        public new static Task CommitChunkAsync(int versionId, int propertyTypeId, string token, long fullSize,
            CancellationToken cancellationToken )
        {
            return BlobStorageBase.CommitChunkAsync(versionId, propertyTypeId, token, fullSize, cancellationToken);
        }
        public new static Task CommitChunkAsync(int versionId, int propertyTypeId, string token, long fullSize,
            BinaryDataValue source, CancellationToken cancellationToken)
        {
            return BlobStorageBase.CommitChunkAsync(versionId, propertyTypeId, token, fullSize, source, cancellationToken);
        }

        public new static Task CopyFromStreamAsync(int versionId, string token, Stream input, CancellationToken cancellationToken)
        {
            return BlobStorageBase.CopyFromStreamAsync(versionId, token, input, cancellationToken);
        }

        /*================================================================== Maintenance*/

        public new static Task CleanupFilesSetFlagAsync(CancellationToken cancellationToken)
        {
            return BlobStorageBase.CleanupFilesSetFlagAsync(cancellationToken);
        }

        public new static Task<bool> CleanupFilesAsync(CancellationToken cancellationToken)
        {
            return BlobStorageBase.CleanupFilesAsync(cancellationToken);
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
