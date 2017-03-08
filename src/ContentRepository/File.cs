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
    [ContentHandler]
    public class File : FileBase
    {
        public File(Node parent) : this(parent, null) { }
        public File(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected File(NodeToken nt) : base(nt) { }

        public override CheckInCommentsMode CheckInCommentsMode
        {
            get
            {
                return this.VersioningMode == VersioningType.None ? CheckInCommentsMode.None : Configuration.Versioning.CheckInCommentsMode;
            }
        }

        private const string WATERMARKPROPERTY = "Watermark";
        [RepositoryProperty(WATERMARKPROPERTY, RepositoryDataType.String)]
        public virtual string Watermark
        {
            get { return base.GetProperty<string>(WATERMARKPROPERTY); }
            set { base.SetProperty(WATERMARKPROPERTY, value); }
        }

        private const string PAGECOUNT_PROPERTY = "PageCount";
        [RepositoryProperty(PAGECOUNT_PROPERTY, RepositoryDataType.Int)]
        public int PageCount
        {
            get { return base.GetProperty<int>(PAGECOUNT_PROPERTY); }
            set { base.SetProperty(PAGECOUNT_PROPERTY, value); }
        }

        private TaskPriority _previewGenerationPriority = TaskPriority.Normal;
        public virtual TaskPriority PreviewGenerationPriority
        {
            get { return _previewGenerationPriority; }
            set { _previewGenerationPriority = value; }
        }

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

        public static void Downloaded(int fileId)
        {
            DownloadCounter.Increment(fileId);
        }

        public static void Downloaded(string filePath)
        {
            DownloadCounter.Increment(filePath);
        }

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