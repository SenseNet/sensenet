using Microsoft.AspNetCore.Http;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Operations
{
    public static class UploadActions
    {
        [ODataAction]
        [ContentTypes(N.CT.GenericContent)]
        [AllowedRoles(N.R.All)]
        [RequiredPermissions(N.P.AddNew)]
        public static Task<object> Upload(Content content, HttpContext context, 
            long? FileLength = null, string ContentType = null, string PropertyName = null, 
            string FileText = null, bool? Overwrite = null, int? ContentId = null, 
            string FileName = null, string ChunkToken = null, bool? UseChunk = null, string create = null)
        {
            var handler = new UploadHandler(content, context);

            if (FileLength.HasValue)
                handler.FileLength = FileLength.Value;
            if (ContentType != null)
                handler.ContentTypeName = ContentType;
            if (PropertyName != null)
                handler.PropertyName = PropertyName;
            if (FileText != null)
                handler.FileText = FileText;
            if (Overwrite.HasValue)
                handler.Overwrite = Overwrite.Value;
            if (ContentId.HasValue)
                handler.ContentId = ContentId.Value;
            if (FileName != null)
                handler.FileName = FileName;
            if (ChunkToken != null)
                handler.ChunkToken = ChunkToken;
            if (UseChunk.HasValue)
                handler.UseChunkRequestValue = UseChunk.Value;

            // this is a loosely coupled parameter, the value can be anything
            if (create != null)
                handler.Create = true;

            return handler.ExecuteAsync(context.RequestAborted);
        }

        [ODataAction]
        [ContentTypes(N.CT.GenericContent)]
        [AllowedRoles(N.R.All)]
        [RequiredPermissions(N.P.Save)]
        public static string FinalizeContent(Content content, HttpContext context)
        {
            var handler = new UploadHandler(content, context);
            return handler.FinalizeContent(content);
        }

        /// <summary>
        /// Starts a blob write operation by loading an existing content (determined by the
        /// requested parent resource and the provided file name) or creating a new one.
        /// It puts the content into a multistep saving state and calls StartChunk. 
        /// This method is used by clients who intend to use the blob storage client 
        /// to write files directly to the blob storage.
        /// </summary>
        /// <param name="content">Parent content to upload the new file to.</param>
        /// <param name="name">Name of the new (or existing) content.</param>
        /// <param name="contentType">Content type of the new content.</param>
        /// <param name="fullSize">Size of the whole binary.</param>
        /// <param name="fieldName">Optional custom binary field name, if it is other than 'Binary'.</param>
        /// <returns>Chunk write token, content id and version id in a JSON object.</returns>
        [ODataAction]
        [ContentTypes(N.CT.GenericContent)]
        [AllowedRoles(N.R.All)]
        [RequiredPermissions(N.P.AddNew)]
        public static Task<string> StartBlobUploadToParent(Content content, HttpContext context, string name, string contentType, long fullSize, string fieldName = null)
        {
            var handler = new UploadHandler(content, context);
            return handler.StartBlobUploadToParentAsync(name, contentType, fullSize, context.RequestAborted, fieldName);
        }

        /// <summary>
        /// Starts a blob write operation by putting the content into a multistep saving state
        /// and calling StartChunk. This method is used by clients who intend to use the blob
        /// storage client to write files directly to the blob storage.
        /// </summary>
        /// <param name="content">Existing content with a binary field to upload to. In most cases this is a file content.</param>
        /// <param name="fullSize">Size of the whole binary.</param>
        /// <param name="fieldName">Optional custom binary field name, if it is other than 'Binary'.</param>
        /// <returns>Chunk write token, content id and version id in a JSON object.</returns>
        [ODataAction]
        [ContentTypes(N.CT.GenericContent)]
        [AllowedRoles(N.R.All)]
        [RequiredPermissions(N.P.Save)]
        public static string StartBlobUpload(Content content, HttpContext context, long fullSize, string fieldName = null)
        {
            var handler = new UploadHandler(content, context);
            return handler.StartBlobUpload(fullSize, fieldName);
        }

        /// <summary>
        /// Finishes a blob write operation by calling CommitChunk and finalizing the content.
        /// This method is used by clients who intend to use the blob storage client 
        /// to write files directly to the blob storage.
        /// </summary>
        /// <param name="content">A content in a multistep saving state.</param>
        /// <param name="token">Binary token provided by the start operation before.</param>
        /// <param name="fullSize">Size of the whole binary.</param>
        /// <param name="fieldName">Optional custom binary field name, if it is other than 'Binary'.</param>
        /// <param name="fileName">Binary file name to save into the binary metadata.</param>
        [ODataAction]
        [ContentTypes(N.CT.GenericContent)]
        [AllowedRoles(N.R.All)]
        [RequiredPermissions(N.P.Save)]
        public static Task<string> FinalizeBlobUpload(Content content, HttpContext context, string token, long fullSize, string fieldName = null, string fileName = null)
        {
            var handler = new UploadHandler(content, context);
            return handler.FinalizeBlobUploadAsync(token, fullSize, context.RequestAborted, fieldName, fileName);
        }

        /// <summary>
        /// Gets a token from the Content Repository that represents the binary data stored in the specified
        /// field (by default Binary) of the provided content version.
        /// </summary>
        /// ///
        /// <param name="content">A content with a binary field.</param>
        /// <param name="fieldName">Optional custom binary field name, if it is other than 'Binary'.</param>
        [ODataFunction]
        [ContentTypes(N.CT.GenericContent)]
        [AllowedRoles(N.R.All)]
        public static string GetBinaryToken(Content content, HttpContext context, string fieldName = null)
        {
            var handler = new UploadHandler(content, context);
            return handler.GetBinaryToken(fieldName);
        }
    }
}
