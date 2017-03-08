using System.Collections.Generic;
using System.Xml;
using System.IO;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository
{
	public class ImportContext
	{
		public string CurrentDirectory { get; private set; }
		public XmlNodeList FieldData { get; private set; }
		public bool IsNewContent { get; private set; }
		public bool NeedToValidate { get; private set; }
		public string ErrorMessage { get; set; }
		public bool UpdateReferences { get; set; }
		public List<string> PostponedReferenceFields { get; private set; }
		public bool HasReference
		{
			get { return PostponedReferenceFields.Count > 0; }
		}

		public ImportContext(XmlNodeList fieldData, string currentDirectory, bool isNewContent, bool needToValidate, bool updateReferences)
		{
			CurrentDirectory = currentDirectory;
			FieldData = fieldData;
			IsNewContent = isNewContent;
			NeedToValidate = needToValidate;
			UpdateReferences = updateReferences;
			PostponedReferenceFields = new List<string>();
		}

        internal Stream GetAttachmentStream(string attachmentName)
        {
            string path = Path.Combine(CurrentDirectory, attachmentName);
            if (!System.IO.File.Exists(path))
            {
                if (RepositoryEnvironment.SkipBinaryImportIfFileDoesNotExist)
                    return null;
                else
                    throw new FileNotFoundException("Attachment is not found", path);
            }
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

		public string UnescapeFileName(string path)
		{
			return path.Replace("$amp;", "&");

		}
	}
}