using System.Collections.Specialized;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using System.IO;
using System.Web;

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
            var ctName = Settings.GetValue("PortalSettings", "UploadFileExtensions" + extension, contextPath, string.Empty);
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
                ctName = Settings.GetValue("PortalSettings", "UploadFileExtensions.DefaultContentType", contextPath, string.Empty);
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
        /// Creates BinaryData from filename and stream
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static BinaryData CreateBinaryData(string fileName, Stream stream)
        {
            var binaryData = new BinaryData();
            binaryData.FileName = fileName;
            binaryData.SetStream(stream);
            return binaryData;
        }

        /// <summary>
        /// Creates BinaryData from HttpPostedFile
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static BinaryData CreateBinaryData(HttpPostedFile file, bool setStream = true)
        {
            var result = new BinaryData();

            string fileName = file.FileName;
            if (file.FileName.LastIndexOf("\\") > -1)
                fileName = file.FileName.Substring(file.FileName.LastIndexOf("\\") + 1);

            result.FileName = new BinaryFileName(fileName);

            // set content type only if we were unable to recognise it
            if (string.IsNullOrEmpty(result.ContentType))
                result.ContentType = file.ContentType;

            if (setStream)
                result.SetStream(file.InputStream);

            return result;
        }

        /// <summary>
        /// Modify node's binary
        /// </summary>
        /// <param name="node"></param>
        /// <param name="stream"></param>
        public static void ModifyNode(Node node, Stream stream)
        {
            node.SetBinary("Binary", UploadHelper.CreateBinaryData(node.Name, stream));
            node.Save();
        }
    }
}
