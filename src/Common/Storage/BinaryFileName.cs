using System;
using SenseNet.ContentRepository.Common;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// BinaryFileName handles the filename of the data was saved in a BinaryData. 
    /// </summary>
    public struct BinaryFileName
    {
        private const string fileNameExtensionSeparator = ".";

        private string _fileNameWithoutExtension;
        private string _extension;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryFileName"/> struct.
        /// </summary>
        /// <param name="fileNameWithoutExtension">The file name without extension.</param>
        /// <param name="extension">The extension.</param>
        public BinaryFileName(string fileNameWithoutExtension, string extension)
        {
            _fileNameWithoutExtension = fileNameWithoutExtension;
            _extension = extension;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryFileName"/> struct.
        /// </summary>
        /// <param name="fullFileName">Full name of the file.</param>
        public BinaryFileName(string fullFileName)
        {
            _fileNameWithoutExtension = GetFileNameWithoutExtension(fullFileName);
            _extension = GetExtension(fullFileName);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is valid.
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public bool IsValid
        {
			get { return String.IsNullOrEmpty(_extension); }
        }

        /// <summary>
        /// Gets or sets the full name of the file.
        /// </summary>
        /// <value>The full name of the file.</value>
        public string FullFileName
        {
            get
            {
                string fullFileName = string.Empty;
                if (!(string.IsNullOrEmpty(_fileNameWithoutExtension) && string.IsNullOrEmpty(_extension)))
                    fullFileName = string.Concat(_fileNameWithoutExtension, fileNameExtensionSeparator, _extension);
                return fullFileName;
            }
            set
            {
                _fileNameWithoutExtension = GetFileNameWithoutExtension(value);
                _extension = GetExtension(value);
            }
        }

        /// <summary>
        /// Gets or sets the file name without extension.
        /// </summary>
        /// <value>The file name without extension.</value>
        public string FileNameWithoutExtension
        {
            get
            {
                return _fileNameWithoutExtension;
            }
            set
            {
                _fileNameWithoutExtension = value;
            }
        }

        /// <summary>
        /// Gets or sets the extension.
        /// </summary>
        /// <value>The extension.</value>
        public string Extension
        {
            get
            {
                return _extension;
            }
            set
            {
                _extension = value;

            }
        }

        #endregion

        #region Methods (FileName-Extension, ToString())

        private static string GetFileNameWithoutExtension(string fileName)
        {
            if (fileName == null) return null;
            int lastSeparatorIndex = fileName.LastIndexOf(fileNameExtensionSeparator, StringComparison.Ordinal);
            if (lastSeparatorIndex < 0)
                return fileName;
            return fileName.Substring(0, lastSeparatorIndex);
        }

        private static string GetExtension(string fileName)
        {
            if (fileName == null) return null;
            int lastSeparatorIndex = fileName.LastIndexOf(fileNameExtensionSeparator, StringComparison.Ordinal);
            if (lastSeparatorIndex < 0)
                return string.Empty;
            return fileName.Substring(lastSeparatorIndex + 1);
        }

        /// <summary>
        /// Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> containing a fully qualified type name.
        /// </returns>
        public override string ToString()
        {
            return FullFileName;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            return FullFileName.GetHashCode();
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">Another object to compare to.</param>
        /// <returns>
        /// true if obj and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is BinaryFileName)) throw new ArgumentOutOfRangeException("obj", SR.Exceptions.General.Msg_ParamtereIsNotABinaryFileName);
            return this == (BinaryFileName) obj;
        }

        #endregion

        #region Operator overloads

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="SenseNet.ContentRepository.Storage.BinaryFileName"/>.
        /// </summary>
        /// <param name="fileNameStr">The file name STR.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator BinaryFileName(string fileNameStr)
        {
            if (fileNameStr == null)
                fileNameStr = string.Empty;
            return new BinaryFileName(fileNameStr);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SenseNet.ContentRepository.Storage.BinaryFileName"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator string(BinaryFileName fileName)
        {
            return fileName.FullFileName;
        }
        #endregion
    }

}