using System.IO;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable CheckNamespace

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Interface for blob providers. Implement this to create
    /// an external blob provider that stores binaries outside
    /// of the Content Repository (e.g. in the file system).
    /// </summary>
    public interface IBlobProvider
    {
        /// <summary>
        /// Allocates a place in the blob storage for the bytes to be stored
        /// (e.g. creates a folder in the file system for file chunks). It should
        /// fill the BlobProviderData property of the context with the data
        /// that is necessary to access the binaries later (e.g. the name of
        /// the folder).
        /// </summary>
        /// <param name="context">A context object that holds information about the binary data.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task AllocateAsync(BlobStorageContext context, CancellationToken cancellationToken);
        /// <summary>
        /// Writes a set of bytes into the blob storage. The offset must point to the
        /// start of one of the internal chunks. The buffer may contain bytes
        /// for multiple internal chunks.
        /// </summary>
        /// <param name="context">A context object that holds information about the binary data.</param>
        /// <param name="offset">Start position in the full stream where the buffer will be written.</param>
        /// <param name="buffer">The set of bytes to be written to the blob storage.</param>
        void Write(BlobStorageContext context, long offset, byte[] buffer);
        /// <summary>
        /// Writes a set of bytes into the blob storage. The offset must point to the
        /// start of one of the internal chunks. The buffer may contain bytes
        /// for multiple internal chunks.
        /// </summary>
        /// <param name="context">A context object that holds information about the binary data.</param>
        /// <param name="offset">Start position in the full stream where the buffer will be written.</param>
        /// <param name="buffer">The set of bytes to be written to the blob storage.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes a binary from the storage related to a binary record in the database.
        /// </summary>
        /// <param name="context">A context object that holds information about the binary data.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteAsync(BlobStorageContext context, CancellationToken cancellationToken);
        /// <summary>
        /// Returns a stream that serves bytes from the blob storage. This stream cannot be used to
        /// write bytes to the storage, it is a readonly stream, but it supports Seek.
        /// </summary>
        /// <param name="context">A context object that holds information about the binary data.</param>
        Stream GetStreamForRead(BlobStorageContext context);
        /// <summary>
        /// Returns a stream that can be used to write bytes to the blob storage. This is 
        /// a write-only, forward-only stream that does not support Read and Seek.
        /// </summary>
        /// <param name="context">A context object that holds information about the binary data.</param>
        Stream GetStreamForWrite(BlobStorageContext context);
        /// <summary>
        /// Creates an in-memory clone of an existing stream object. It does not copy binary data,
        /// it only creates a similar stream object that points to the same binary in the storage.
        /// </summary>
        /// <param name="context">A context object that holds information about the binary data.</param>
        /// <param name="stream">An existing stream that should be one of the known stream types
        /// that the provider works with (e.g. a chunked file stream in case of a file storage provider).</param>
        Stream CloneStream(BlobStorageContext context, Stream stream);
        /// <summary>
        /// Parses a provider-specific provider data from a string.
        /// </summary>
        /// <param name="providerData">String representation (usually in JSON format) of the provider data.</param>
        /// <returns>An object specific to this blob provider that contains information for connecting
        /// the record in the Files table with the binary stored in the external storage
        /// (e.g. the name of the folder in the file system where the bytes are stored).</returns>
        object ParseData(string providerData);
    }
}
