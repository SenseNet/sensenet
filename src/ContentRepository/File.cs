using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Versioning;
using SenseNet.BackgroundOperations;
using SenseNet.TaskManagement.Core;
using SenseNet.Tools;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines a class for handling Content instances with a primary blob.
    /// Designed for the representation of a filesystem's file in the sensenet repository.
    /// </summary>
    [ContentHandler]
    public class File : FileBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="File"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public File(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="File"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public File(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="File"/> class during the loading process.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected File(NodeToken nt) : base(nt) { }

        /// <summary>
        /// Gets the recommended <see cref="ContentRepository.CheckInCommentsMode"/> value for this instance.
        /// The return value depends on the versioning mode and the value of the <see cref="SenseNet.Configuration.Versioning.CheckInCommentsMode"/>.
        /// </summary>
        public override CheckInCommentsMode CheckInCommentsMode
        {
            get
            {
                return this.VersioningMode == VersioningType.None ? CheckInCommentsMode.None : Configuration.Versioning.CheckInCommentsMode;
            }
        }

        /// <summary>
        /// Defines a constant value for the name of the Watermark property.
        /// </summary>
        private const string WATERMARKPROPERTY = "Watermark";
        /// <summary>
        /// Gets or sets the watermark text of the preview pages. Persisted as <see cref="RepositoryDataType.String"/>.
        /// </summary>
        [RepositoryProperty(WATERMARKPROPERTY, RepositoryDataType.String)]
        public virtual string Watermark
        {
            get { return base.GetProperty<string>(WATERMARKPROPERTY); }
            set { base.SetProperty(WATERMARKPROPERTY, value); }
        }

        /// <summary>
        /// Defines a constant value for the name of the PageCount property.
        /// </summary>
        private const string PAGECOUNT_PROPERTY = "PageCount";
         /// <summary>
        /// Gets or sets the count of preview pages. Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
       [RepositoryProperty(PAGECOUNT_PROPERTY, RepositoryDataType.Int)]
        public int PageCount
        {
            get { return base.GetProperty<int>(PAGECOUNT_PROPERTY); }
            set { base.SetProperty(PAGECOUNT_PROPERTY, value); }
        }

        private TaskPriority _previewGenerationPriority = TaskPriority.Normal;
        /// <summary>
        /// Gets or sets the <see cref="TaskPriority"/> value that defines the proirity of the preview generation task.
        /// </summary>
        public virtual TaskPriority PreviewGenerationPriority
        {
            get { return _previewGenerationPriority; }
            set { _previewGenerationPriority = value; }
        }

        /// <summary>
        /// Gets the name of the icon
        /// </summary>
        public override string Icon
        {
            // hack, this is an ugly workaround before we implement this into the mime system
            get
            {
                var formats = new Dictionary<string, string>
              {
                {"\\.(doc(x)?|docm|rtf)$", "word"},
                {"\\.(xls(x)?|xlsm|xltm|xltx||csv)$", "excel"},
                {"\\.(ppt(x)?|pot(x)?|pps(x)?)$", "powerpoint"},
                {"\\.(vdw|vdx|vsd|vss|vst|vsx|vtx)$", "visio"},
                {"\\.(ttf|eot|woff|woff2)$", "font"},
                {"\\.one$", "one"},
                {"\\.pdf$", "acrobat"},
                {"\\.odp$", "odp"},
                {"\\.ods$", "ods"},
                {"\\.odt$", "odt"},
                {"\\.(txt|xml)$", "document"},
                {"\\.mpp$", "mpp"},
                {"\\.(jp(e)?g|gif|bmp|png|tif(f)?|psd|ai|cdr)$", "image"}
              };

                foreach (KeyValuePair<string, string> f in formats)
                {
                    var r = new Regex(f.Key, RegexOptions.IgnoreCase);
                    if (r.IsMatch(Name))
                        return f.Value;
                }

                return base.Icon;
            }
        }

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case WATERMARKPROPERTY:
                    return this.Watermark;
                case PAGECOUNT_PROPERTY:
                    return this.PageCount;
                default:
                    return base.GetProperty(name);
            }
        }
        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case WATERMARKPROPERTY:
                    this.Watermark = (string)value;
                    break;
                case PAGECOUNT_PROPERTY:
                    this.PageCount = (int)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        /// <summary>
        /// Increments the counter in the existing <see cref="IDownloadCounter"/> instance by the given file id.
        /// </summary>
        public static void Downloaded(int fileId)
        {
            DownloadCounter.Increment(fileId);
        }

        /// <summary>
        /// Increments the counter in the existing <see cref="IDownloadCounter"/> instance by the given file path.
        /// </summary>
        public static void Downloaded(string filePath)
        {
            DownloadCounter.Increment(filePath);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="File"/> or appropriate inherited class by the given 
        /// <see cref="BinaryData"/> under the <see cref="IFolder"/>. The instance has not been saved yet.
        /// </summary>
        /// <param name="parent">The parent <see cref="IFolder"/> of the new <see cref="File"/>.</param>
        /// <param name="binaryData">The <see cref="BinaryData"/> that is the base of the creation.</param>
        /// <returns></returns>
        public static File CreateByBinary(IFolder parent, BinaryData binaryData)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            if (binaryData == null)
                return new File(parent as Node);

            File file;
            // Resolve filetype by binary-config matching
            BinaryTypeResolver resolver = new BinaryTypeResolver();
            if (!resolver.ParseBinary(binaryData))
            {
                // Unknown file type
                file = new File(parent as Node);
            }
            else
            {
                // Specific File subtype has been found
                file = TypeResolver.CreateInstance<File>(resolver.NodeType.ClassName, parent);

                var fname = binaryData.FileName.FileNameWithoutExtension;
                if (string.IsNullOrEmpty(fname))
                    fname = file.Name;
                else if (fname.Contains("\\"))
                    fname = System.IO.Path.GetFileNameWithoutExtension(fname);

                binaryData.FileName = new BinaryFileName(fname, resolver.FileNameExtension);
                binaryData.ContentType = resolver.ContentType;
            }

            file.Binary = binaryData;
            return file;
        }
    }
}