using System;
using System.Collections.Specialized;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using System.IO;

namespace SenseNet.Portal.Handlers
{
    public class UploadHelper
    {
        // ============================================================================ Consts
        public const string AUTOELEMENT = "Auto";


        // ============================================================================ Private methods
        private static NameValueCollection FileExtensions
        {
            get
            {
                return System.Configuration.ConfigurationManager.GetSection("sensenet/uploadFileExtensions") as NameValueCollection;
            }
        }


        // ============================================================================ Public methods
        /// <summary>
        /// Determines content type from fileextension or given contentType
        /// </summary>
        /// <param name="fileName">Name of the uploaded file. The extension will be used to determine the content type.</param>
        /// <param name="contextPath">The path where the file will be saved. This is needed for finding the most relevant Settings for the feature.</param>
        /// <returns>The specific content type name determined by the extension or NULL if the type cannot be derermined.</returns>
        public static string GetContentType(string fileName, string contextPath)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            // extension starts with a 'dot' (e.g. '.jpg')
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
                return null;

            extension = extension.ToLower();

            // look for the extension in portal settings
            var ctName = Settings.GetValue("Portal", "UploadFileExtensions" + extension, contextPath, string.Empty);
            if (!string.IsNullOrEmpty(ctName))
                return ctName;

            // Check if this is an executable file (e.g. an aspx). If it is, we cannot return the default
            // content type here. We have to check the fallback configuration for exact type definition
            // first, and if that does not exist, return with the executable file type name.
            // This is necessary to let developers customize the executable file type.
            var execExt = RepositoryTools.IsExecutableExtension(extension);

            if (!execExt)
            {
                // look for the default setting
                ctName = Settings.GetValue("Portal", "UploadFileExtensions.DefaultContentType", contextPath, string.Empty);
                if (!string.IsNullOrEmpty(ctName))
                    return ctName;
            }

            // Fallback: look for the extension configuration in web or app config
            if (FileExtensions == null)
                return execExt ? Repository.DefaultExecutableFileTypeName : null;

            var fileType = FileExtensions[extension];

            return !string.IsNullOrEmpty(fileType) ? fileType : (execExt ? Repository.DefaultExecutableFileTypeName : null);
        }

        /// <summary>
        /// Creates BinaryData from filename and stream.
        /// </summary>
        /// <param name="fileName">Binary file name.</param>
        /// <param name="stream">Binary stream or null.</param>
        /// <param name="contentType">Binary content type. This value is used only if the content type
        /// could not be computed from the file extension.</param>
        public static BinaryData CreateBinaryData(string fileName, Stream stream, string contentType = null)
        {
            var result = new BinaryData();

            // use only the file name
            var slashIndex = fileName.LastIndexOf("\\", StringComparison.Ordinal);
            if (slashIndex > -1)
                fileName = fileName.Substring(slashIndex + 1);

            result.FileName = new BinaryFileName(fileName);

            // set content type only if we were unable to recognise it
            if (string.IsNullOrEmpty(result.ContentType) && !string.IsNullOrEmpty(contentType))
                result.ContentType = contentType;

            result.SetStream(stream);

            return result;
        }

        /// <summary>
        /// Modify node's binary
        /// </summary>
        /// <param name="node"></param>
        /// <param name="stream"></param>
        public static void ModifyNode(Node node, Stream stream)
        {
            node.SetBinary("Binary", CreateBinaryData(node.Name, stream));
            node.Save();
        }
    }
}
