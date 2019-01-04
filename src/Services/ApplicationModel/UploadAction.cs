using System.IO;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.Portal.Handlers;
using System.Web;
using System.Web.Script.Serialization;
using System;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.i18n;
using SenseNet.Preview;

namespace SenseNet.ApplicationModel
{
    public class UploadAction : UrlAction
    {
        // ======================================================================== Properties
        private bool? _useChunk;
        protected bool UseChunk
        {
            get
            {
                if (!_useChunk.HasValue)
                    _useChunk = TryParseRangeHeader(out _chunkStart, out _chunkLength, out _fileLength);

                return _useChunk.Value;
            }
        }

        private long _chunkStart;
        protected long ChunkStart
        {
            get
            {
                if (!_useChunk.HasValue)
                    _useChunk = TryParseRangeHeader(out _chunkStart, out _chunkLength, out _fileLength);

                return _chunkStart;
            }
        }

        private int _chunkLength;
        protected int ChunkLength
        {
            get
            {
                if (!_useChunk.HasValue)
                    _useChunk = TryParseRangeHeader(out _chunkStart, out _chunkLength, out _fileLength);

                return _chunkLength;
            }
        }

        private long _fileLength;
        protected long FileLength
        {
            get
            {
                if (!_useChunk.HasValue)
                {
                    _useChunk = TryParseRangeHeader(out _chunkStart, out _chunkLength, out _fileLength);

                    // in case of the first request, the length comes from a manual parameter instead of the range header
                    if (_fileLength == 0)
                    {
                        long lengthValue;
                        var lengthText = HttpContext.Current.Request.Form["FileLength"];
                        if (!string.IsNullOrEmpty(lengthText) && long.TryParse(lengthText, out lengthValue))
                            _fileLength = lengthValue;
                    }
                }

                return _fileLength;
            }
        }

        protected static string GetContentTypeName(Content parent, string fileName)
        {
            // 1. if contenttype post parameter is defined we will use that without respect to allowed types
            // 2. otherwise check configured upload types (by extension) and use it if it is allowed
            // 3. otherwise get the first allowed type that is or is derived from file

            string contentTypeName = null;
            var requestedContentTypeName = HttpContext.Current.Request.Form["ContentType"];
            if (!string.IsNullOrEmpty(requestedContentTypeName))
            {
                // try resolving provided type
                var ct = ContentType.GetByName(requestedContentTypeName);
                if (ct != null)
                    contentTypeName = requestedContentTypeName;
            }
            else
            {
                var gc = parent.ContentHandler as GenericContent;
                if (gc == null)
                    return null;

                var allowedTypes = gc.GetAllowedChildTypes().ToArray();

                // check configured upload types (by extension) and use it if it is allowed
                var fileContentType = UploadHelper.GetContentType(fileName, parent.Path);
                if (!string.IsNullOrEmpty(fileContentType))
                {
                    if (allowedTypes.Select(ct => ct.Name).Contains(fileContentType))
                        contentTypeName = fileContentType;
                }

                if (string.IsNullOrEmpty(contentTypeName))
                {
                    // get the first allowed type that is or is derived from file
                    if (allowedTypes.Any(ct => ct.Name == "File"))
                    {
                        contentTypeName = "File";
                    }
                    else
                    {
                        var fileDescendant = allowedTypes.FirstOrDefault(ct => ct.IsInstaceOfOrDerivedFrom("File"));
                        if (fileDescendant != null)
                            contentTypeName = fileDescendant.Name;
                    }
                }
            }

            return contentTypeName;
        }

        private string _propertyName;
        protected string PropertyName
        {
            get
            {
                if (_propertyName == null)
                {
                    var propertyNameStr = HttpContext.Current.Request.Form["PropertyName"];
                    if (string.IsNullOrEmpty(propertyNameStr))
                        _propertyName = "Binary";
                    else
                        _propertyName = propertyNameStr;
                }

                return _propertyName;
            }
        }

        protected string FileText
        {
            get
            {
                return HttpContext.Current.Request.Form["FileText"];
            }
        }

        // ======================================================================== Helper methods

        private const string contentDispHeaderPrefix = "filename=";
        protected string GetFileName(HttpPostedFile file)
        {
            if (UseChunk)
            {
                // Content-Disposition: attachment; filename="x.png"
                var contentDispHeader = HttpContext.Current.Request.Headers["Content-Disposition"];
                var idx = contentDispHeader.IndexOf(contentDispHeaderPrefix, StringComparison.InvariantCultureIgnoreCase);
                var fileInQuotes = contentDispHeader.Substring(idx + contentDispHeaderPrefix.Length);
                var fileName = fileInQuotes.Replace("\"", "");
                return HttpUtility.UrlDecode(fileName);
            }
            else
            {
                var fileNames = file.FileName.Split(new char[] { '\\' });
                var fileName = fileNames[fileNames.Length - 1];
                return fileName;
            }
        }

        protected bool TryParseRangeHeader(out long chunkStart, out int chunkLength, out long fullLength)
        {
            // parse chunk information
            chunkStart = 0;
            chunkLength = 0;
            fullLength = 0;
            var rangeHeader = HttpContext.Current.Request.Headers["Content-Range"];
            if (!string.IsNullOrEmpty(rangeHeader))
            {
                var fullinfo = rangeHeader.Substring("bytes ".Length).Split('/');
                fullLength = Int64.Parse(fullinfo[1]);
                var chunkinfo = fullinfo[0].Split('-');
                chunkStart = Int64.Parse(chunkinfo[0]);
                var chunkEnd = Int64.Parse(chunkinfo[1]);
                chunkLength = Convert.ToInt32(chunkEnd - chunkStart + 1);
                return true;
            }
            return false;
        }

        protected Content GetContent(Content parent)
        {
            bool overwrite;
            if (!bool.TryParse(HttpContext.Current.Request["Overwrite"], out overwrite))
                overwrite = true;

            var contentIdVal = HttpContext.Current.Request["ContentId"];
            int contentId;

            if (overwrite && !string.IsNullOrEmpty(contentIdVal) && int.TryParse(contentIdVal, out contentId))
            {
                var content = Content.Load(contentId);
                if (content != null)
                {
                    SetPreviewGenerationPriority(content);

                    return content;
                }
            }

            var fileName = HttpContext.Current.Request["FileName"];
            var contentTypeName = GetContentTypeName(parent, fileName);
            if (contentTypeName == null)
                throw new Exception(SenseNetResourceManager.Current.GetString("Action", "UploadExceptionInvalidContentType"));

            return GetContent(parent, fileName, contentTypeName, overwrite);
        }

        protected BinaryData CreateBinaryData(HttpPostedFile file, bool setStream = true)
        {
            var fileName = UseChunk ? GetFileName(file) : file?.FileName;

            return UploadHelper.CreateBinaryData(fileName, setStream ? file?.InputStream : null, file?.ContentType);
        }

        // ======================================================================== Virtual methods

        protected virtual Content GetContent(Content parent, string fileName, string contentTypeName, bool overwrite)
        {
            var contentname = ContentNamingProvider.GetNameFromDisplayName(fileName);
            var path = RepositoryPath.Combine(Content.Path, contentname);

            Content content;

            if (overwrite)
            {
                // check if content exists
                content = Content.Load(path);
                if (content != null)
                {
                    SetPreviewGenerationPriority(content);

                    return content;
                }
            }

            // create new content
            content = Content.CreateNew(contentTypeName, parent.ContentHandler, contentname);

            // prevent autonaming feature in case of preview images
            if (string.Compare(contentTypeName, DocumentPreviewProvider.PREVIEWIMAGE_CONTENTTYPE, StringComparison.InvariantCultureIgnoreCase) != 0)
                content.ContentHandler.AllowIncrementalNaming = true;

            SetPreviewGenerationPriority(content);

            return content;
        }

        protected virtual void SaveFileToRepository(Content uploadedContent, Content parent, string token, bool mustFinalize, bool mustCheckIn, HttpPostedFile file)
        {
            if (uploadedContent.ContentHandler.Locked && uploadedContent.ContentHandler.LockedBy.Id != User.Current.Id)
                throw new Exception(SenseNetResourceManager.Current.GetString("Action", "UploadExceptionLocked"));

            if (UseChunk)
            {
                // get bytes from the uploaded stream
                byte[] chunkData;
                using (var br = new BinaryReader(file.InputStream))
                {
                    chunkData = br.ReadBytes(ChunkLength);
                }

                // save chunk
                BinaryData.WriteChunk(uploadedContent.Id, token, FileLength, chunkData, ChunkStart, PropertyName);

                // last chunk should commit the process
                if (ChunkStart + ChunkLength == FileLength)
                {
                    BinaryData.CommitChunk(uploadedContent.Id, token, FileLength, PropertyName, CreateBinaryData(file, false));

                    // finalize only if the multistep save was started by this process
                    if (mustFinalize || mustCheckIn)
                    {
                        uploadedContent = Content.Load(uploadedContent.Id);

                        SetPreviewGenerationPriority(uploadedContent);

                        uploadedContent.FinalizeContent();
                    }
                }
            }
            else
            {
                if (uploadedContent.IsNew || uploadedContent.ContentHandler.SavingState == ContentSavingState.Finalized)
                {
                    var binData = CreateBinaryData(file);
                    uploadedContent[PropertyName] = binData;
                    uploadedContent.Save();
                }
                else
                {
                    // Workaround for small existing content, in case the user started
                    // a multistep saving process manually: save the whole binary in one chunk
                    // (we cannot execute a real content Save here to avoid messing with saving state).

                    var chunkToken = BinaryData.StartChunk(uploadedContent.Id, file.InputStream.Length, PropertyName);
                    byte[] chunkData;
                    using (var br = new BinaryReader(file.InputStream))
                    {
                        chunkData = br.ReadBytes(Convert.ToInt32(file.InputStream.Length));
                    }

                    // save everything in one chunk and commit the process
                    BinaryData.WriteChunk(uploadedContent.Id, chunkToken, chunkData.Length, chunkData, 0, PropertyName);
                    BinaryData.CommitChunk(uploadedContent.Id, chunkToken, chunkData.Length, PropertyName, CreateBinaryData(file, false));

                    if (mustFinalize && uploadedContent.ContentHandler.SavingState != ContentSavingState.Finalized)
                        uploadedContent.FinalizeContent();
                }

                // checkin only if the content was created or checked out by this process
                if (uploadedContent.ContentHandler.Locked && mustCheckIn)
                    uploadedContent.CheckIn();
            }
        }

        // ======================================================================== Helper methods

        public static bool AllowCreationForEmptyAllowedContentTypes(Node node)
        {
            var parent = node as GenericContent;
            if (parent != null)
            {
                if (parent.GetAllowedChildTypes().Count() == 0)
                    return false;
            }
            return true;
        }

        protected string GetJsonFromContent(Content content, HttpPostedFile file)
        {
            if (content == null)
                return string.Empty;

            var result = new
            {
                Url = content.Path,
                Thumbnail_url = content.Path,
                Name = content.Name,
                Length = UseChunk ? FileLength : (file != null ? file.ContentLength : FileText.Length),
                Type = content.ContentType.Name,
                Id = content.Id
            };

            var js = new JavaScriptSerializer();
            var jsonObj = js.Serialize(result);
            return jsonObj;
        }

        protected static void CollectUploadData(out int contentId, out string token, out bool mustFinalize, out bool mustCheckIn)
        {
            var context = HttpContext.Current;

            var uploadData = context.Request["ChunkToken"] ?? string.Empty;
            var uploadDataArray = uploadData.Split(new[] { '*' });

            if (uploadDataArray.Length != 4)
                throw new Exception(SenseNetResourceManager.Current.GetString("Action", "UploadExceptionInvalidRequest"));

            if (!Int32.TryParse(uploadDataArray[0], out contentId))
                throw new Exception(SenseNetResourceManager.Current.GetString("Action", "UploadExceptionInvalidRequest"));

            token = uploadDataArray[1];

            if (!bool.TryParse(uploadDataArray[2], out mustFinalize))
                throw new Exception(SenseNetResourceManager.Current.GetString("Action", "UploadExceptionInvalidRequest"));
            if (!bool.TryParse(uploadDataArray[3], out mustCheckIn))
                throw new Exception(SenseNetResourceManager.Current.GetString("Action", "UploadExceptionInvalidRequest"));
        }

        protected internal static void SetPreviewGenerationPriority(Content content)
        {
            var file = content?.ContentHandler as ContentRepository.File;
            if (file != null)
                file.PreviewGenerationPriority = TaskManagement.Core.TaskPriority.Important;
        }

        // ======================================================================== Action

        public override bool IsHtmlOperation { get { return true; } }
        public override bool IsODataOperation { get { return true; } }
        public override bool CausesStateChange { get { return true; } }

        public override ActionParameter[] ActionParameters { get { return new ActionParameter[0]; } }

        public override object Execute(Content content, params object[] parameters)
        {
            var context = HttpContext.Current;

            // 1st allowed types check: if allowed content types list is empty, no upload is allowed
            if (!AllowCreationForEmptyAllowedContentTypes(content.ContentHandler))
                throw new Exception(SenseNetResourceManager.Current.GetString("Action","UploadExceptionEmptyAllowedChildTypes"));

            if (context.Request["create"] != null)
            {
                var uploadedContent = GetContent(content);

                // check if the content is locked by someone else
                if (uploadedContent.ContentHandler.Locked && uploadedContent.ContentHandler.LockedBy.Id != User.Current.Id)
                    throw new Exception(SenseNetResourceManager.Current.GetString("Action", "UploadExceptionLocked"));

                var chunkToken = string.Empty;
                bool useChunk;
                if (!bool.TryParse(context.Request["UseChunk"], out useChunk))
                    useChunk = false;

                // If the content is not locked at the start of this process, it will be checked out by the multistep saving mechanism below
                // and it will be checked in at the end (either manually or by the finalizer method).
                var mustCheckIn = uploadedContent.IsNew || !uploadedContent.ContentHandler.Locked;

                // At the end we will finalize only if we started the multistep save.
                var mustFinalize = uploadedContent.ContentHandler.SavingState == ContentSavingState.Finalized;

                if (useChunk)
                {
                    // Start the multistep saving process only if it was not started by 
                    // somebody else before (e.g. with an initial POST request through OData).
                    if (mustFinalize)
                        uploadedContent.Save(SavingMode.StartMultistepSave);

                    chunkToken = BinaryData.StartChunk(uploadedContent.Id, FileLength, PropertyName);
                }

                return string.Format("{0}*{1}*{2}*{3}", uploadedContent.Id, chunkToken, mustFinalize, mustCheckIn);
            }
            else
            {
                // handle uploaded chunks/stream
                var file = context.Request.Files.Count > 0 ? context.Request.Files[0] : null;
                if (file != null && file.ContentLength == 0)
                {
                    // create content for an empty file if necessary
                    var emptyFile = GetContent(content);
                    if (emptyFile != null && emptyFile.IsNew)
                    {
                        emptyFile.Save();

                        return GetJsonFromContent(emptyFile, file);
                    }

                    return null;
                }

                if (file == null && string.IsNullOrEmpty(FileText))
                    return null;

                var contentId = 0;
                var chunkToken = string.Empty;
                var mustFinalize = false;
                var mustCheckIn = false;

                // collect data only if this is a real file, not a text
                if (file != null)
                    CollectUploadData(out contentId, out chunkToken, out mustFinalize, out mustCheckIn);

                // load the content using the posted chunk token or create a new one
                // (in case of a small file, when no chunk upload is used)
                var uploadedContent = UseChunk ? Content.Load(contentId) : GetContent(content);

                // in case we just loaded this content
                SetPreviewGenerationPriority(uploadedContent);

                if (file != null)
                {
                    SaveFileToRepository(uploadedContent, content, chunkToken, mustFinalize, mustCheckIn, file);
                }
                else
                {
                    // handle text data
                    var binData = new BinaryData { FileName = new BinaryFileName(uploadedContent.Name) };

                    // set content type only if we were unable to recognise it
                    if (string.IsNullOrEmpty(binData.ContentType))
                        binData.ContentType = "text/plain";

                    binData.SetStream(RepositoryTools.GetStreamFromString(FileText));

                    uploadedContent[PropertyName] = binData;
                    uploadedContent.Save();
                }

                return GetJsonFromContent(uploadedContent, file);
            }
        }
        
        // ======================================================================== OData

        [ODataAction]
        public static string FinalizeContent(Content content)
        {
            UploadAction.SetPreviewGenerationPriority(content);

            content.FinalizeContent();

            return string.Empty;
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
        public static string StartBlobUploadToParent(Content content, string name, string contentType, long fullSize, string fieldName = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            
            // load or create the content
            var file = Content.Load(RepositoryPath.Combine(content.Path, name));
            if (file == null)
            {
                if (string.IsNullOrEmpty(contentType))
                    contentType = GetContentTypeName(content, name);

                // we have to create it in a multistep saving state because chunk upload needs that
                file = Content.CreateNew(contentType, content.ContentHandler, name);
                file.Save(SavingMode.StartMultistepSave);
            }

            return StartBlobUpload(file, fullSize, fieldName);
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
        public static string StartBlobUpload(Content content, long fullSize, string fieldName = null)
        {
            // we have to put the content into a state that enables chunk write operations
            if (content.ContentHandler.SavingState == ContentSavingState.Finalized)
                content.Save(SavingMode.StartMultistepSave);

            var token = BinaryData.StartChunk(content.Id, fullSize, fieldName);

            return $"{{ id: '{content.Id}', token: '{token}', versionId: {content.ContentHandler.VersionId} }}";
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
        public static string FinalizeBlobUpload(Content content, string token, long fullSize, string fieldName = null, string fileName = null)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            // In most cases this will be the file name, but in case of custom
            // binary fields it is possible to provide a different name.
            if (string.IsNullOrEmpty(fileName))
                fileName = content.Name;

            BinaryData.CommitChunk(content.Id, token, fullSize, fieldName, new BinaryData
            {
                FileName = new BinaryFileName(fileName)
            });
            
            // reload the content to have a fresh object after commit chunk
            return FinalizeContent(Content.Load(content.Id));
        }

        /// <summary>
        /// Gets a token from the Content Repository that represents the binary data stored in the specified
        /// field (by default Binary) of the provided content version.
        /// </summary>
        /// ///
        /// <param name="content">A content with a binary field.</param>
        /// <param name="fieldName">Optional custom binary field name, if it is other than 'Binary'.</param>
        [ODataFunction]
        public static string GetBinaryToken(Content content, string fieldName = null)
        {
            // workaround for empty string (not null, so an optional argument is not enough)
            if (string.IsNullOrEmpty(fieldName))
                fieldName = "Binary";

            Field field;
            if (!content.Fields.TryGetValue(fieldName, out field) || !(field is BinaryField))
                throw new InvalidOperationException("Unknown binary field: " + fieldName);

            var binaryData = content[fieldName] as BinaryData;
            if (binaryData == null)
                throw new InvalidOperationException("Empty binary value: " + fieldName);

            return $"{{ token: '{binaryData.GetToken()}', versionId: {content.ContentHandler.VersionId} }}";
        }
    }
}
