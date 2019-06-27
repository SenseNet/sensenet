using System;
using System.Collections.Generic;
using System.IO;

using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Portal.Virtualization;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using SenseNet.Configuration;


namespace SenseNet.Portal.ApplicationModel
{
    public enum ResizeTypeList
    {
        Resize = 1,
        Crop
    }
    [ContentHandler]
    public class ImgResizeApplication : Application, IHttpHandler
    {
        public ImgResizeApplication(Node parent) : this(parent, "ImgResizeApplication") { }
        public ImgResizeApplication(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ImgResizeApplication(NodeToken nt) : base(nt) { }

        [RepositoryProperty("ImageType", RepositoryDataType.String)]
        public string ImageType
        {
            get { return this.GetProperty<string>("ImageType"); }
            set { this["ImageType"] = value; }
        }

        [RepositoryProperty("ImageFieldName", RepositoryDataType.String)]
        public string ImageFieldName
        {
            get { return this.GetProperty<string>("ImageFieldName"); }
            set { this["ImageFieldName"] = value; }
        }

        [RepositoryProperty("Width", RepositoryDataType.Int)]
        public int Width
        {
            get { return this.GetProperty<int>("Width"); }
            set { this["Width"] = value; }
        }

        [RepositoryProperty("Height", RepositoryDataType.Int)]
        public int Height
        {
            get { return this.GetProperty<int>("Height"); }
            set { this["Height"] = value; }
        }

        [RepositoryProperty("Stretch", RepositoryDataType.Int)]
        public virtual bool Stretch
        {
            get { return (this.GetProperty<int>("Stretch") != 0); }
            set { this["Stretch"] = value ? 1 : 0; }
        }

        public bool IsAutoOutputFormat
        {
            get { return ResizeOutputFormat == null; }
        }

        private string resizedImageExtension = null;
        /// <summary>
        /// The extension of the resized image file according to the value of OutputFormat field. It returns null when set to 'Auto'.
        /// </summary>
        public string ResizedImageExtension
        {
            get
            {
                if (!string.IsNullOrEmpty(resizedImageExtension)) 
                    return resizedImageExtension;

                var of = this.GetProperty<string>("OutputFormat") ?? string.Empty; 
                switch (of.ToLower())
                {
                    case "jpeg": resizedImageExtension = ".jpg"; break;
                    case "png": resizedImageExtension = ".png"; break;
                    case "icon": resizedImageExtension = ".ico"; break;
                    case "tiff": resizedImageExtension = ".tif"; break;
                    case "gif": resizedImageExtension = ".gif"; break;
                    case "auto": resizedImageExtension = null; break;
                    default: resizedImageExtension = ".png"; break;
                }
                return resizedImageExtension;
            }
        }

        /// <summary>
        /// Specifies the output format of the resized Image. When it's set to 'Auto' it returns null.
        /// </summary>
        [RepositoryProperty("OutputFormat", RepositoryDataType.String)]
        public ImageFormat ResizeOutputFormat
        {
            get
            {
                if (this.GetProperty<string>("OutputFormat") == null) return null;

                switch (this.GetProperty<string>("OutputFormat").ToLower())
                {
                    case "jpeg": return ImageFormat.Jpeg;
                    case "png": return ImageFormat.Png;
                    case "icon": return ImageFormat.Icon;
                    case "tiff": return ImageFormat.Tiff;
                    case "gif": return ImageFormat.Gif;
                    case "auto": return null;
                    default: return ImageFormat.Png;
                }
            }
            set
            {
                if (value == ImageFormat.Jpeg) this["OutputFormat"] = "Jpeg";
                else if (value == ImageFormat.Png) this["OutputFormat"] = "Png";
                else if (value == ImageFormat.Gif) this["OutputFormat"] = "Gif";
                else if (value == ImageFormat.Tiff) this["OutputFormat"] = "Tiff";
                else if (value == ImageFormat.Icon) this["OutputFormat"] = "Icon";
                else if (value == null) { this["OutputFormat"] = "Auto"; }
                else this["OutputFormat"] = "Png";
            }
        }

        [RepositoryProperty("SmoothingMode", RepositoryDataType.String)]
        public SmoothingMode ResizeSmoothingMode
        {
            get
            {
                if (this.GetProperty<string>("SmoothingMode") == null) return SmoothingMode.AntiAlias;
                return (SmoothingMode)Enum.Parse(typeof(System.Drawing.Drawing2D.SmoothingMode), this.GetProperty<string>("SmoothingMode"), true);
            }
            set { this["SmoothingMode"] = ((SmoothingMode)value).ToString().ToLower(); }
        }

        [RepositoryProperty("InterpolationMode", RepositoryDataType.String)]
        public InterpolationMode ResizeInterpolationMode
        {
            get
            {
                if (this.GetProperty<string>("InterpolationMode") == null) return InterpolationMode.HighQualityBicubic;
                return (InterpolationMode)Enum.Parse(typeof(System.Drawing.Drawing2D.InterpolationMode), this.GetProperty<string>("InterpolationMode"), true);
            }
            set { this["InterpolationMode"] = ((InterpolationMode)value).ToString().ToLower(); }
        }

        [RepositoryProperty("PixelOffsetMode", RepositoryDataType.String)]
        public PixelOffsetMode ResizePixelOffsetMode
        {
            get
            {
                if (this.GetProperty<string>("PixelOffsetMode") == null) return PixelOffsetMode.HighQuality;
                return (PixelOffsetMode)Enum.Parse(typeof(System.Drawing.Drawing2D.PixelOffsetMode), this.GetProperty<string>("PixelOffsetMode"), true);
            }
            set { this["PixelOffsetMode"] = ((PixelOffsetMode)value).ToString().ToLower(); }
        }

        [RepositoryProperty("ResizeTypeMode", RepositoryDataType.String)]
        public ResizeTypeList ResizeType
        {
            get
            {
                if (this.GetProperty<string>("ResizeTypeMode") == null) return ResizeTypeList.Resize;
                return (ResizeTypeList)Enum.Parse(typeof(ResizeTypeList), this.GetProperty<string>("ResizeTypeMode"), true);
            }
            set { this["ResizeTypeMode"] = ((ResizeTypeList)value).ToString().ToLower(); }
        }

        [RepositoryProperty("CropVAlign", RepositoryDataType.String)]
        public string CropVAlign
        {
            get { return this.GetProperty<string>("CropVAlign"); }
            set { this["CropVAlign"] = value; }
        }

        [RepositoryProperty("CropHAlign", RepositoryDataType.String)]
        public string CropHAlign
        {
            get { return this.GetProperty<string>("CropHAlign"); }
            set { this["CropHAlign"] = value; }
        }

        public override object GetProperty(string name)
        {
            switch (name.ToLower())
            {
                case "imagetype": return this.ImageType;
                case "imagefieldname": return this.ImageFieldName;
                case "width": return this.Width;
                case "height": return this.Height;
                case "stretch": return this.Stretch;
                case "outputformat": return this.ResizeOutputFormat;
                case "smoothingmode": return this.ResizeSmoothingMode;
                case "interpolationmode": return this.ResizeInterpolationMode;
                case "pixeloffsetmode": return this.ResizePixelOffsetMode;
                case "resizetypemode": return this.ResizeType;
                case "cropvalign": return this.CropVAlign;
                case "crophalign": return this.CropHAlign;

                default: return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name.ToLower())
            {
                case "imagetype":
                    this.ImageType = GetStringValue(value);
                    break;
                case "imagefieldname":
                    this.ImageFieldName = GetStringValue(value);
                    break;
                case "width":
                    try
                    {
                        int w = Int32.Parse(GetStringValue(value));
                        if (w < 0) throw new Exception();
                        this.Width = w;
                    }
                    catch (Exception)
                    {
                        throw new Exception("Property 'Width' is not a valid number or less than zero.");
                    }
                    break;
                case "height":
                    try
                    {
                        int h = Int32.Parse(GetStringValue(value));
                        if (h < 0) throw new Exception();
                        this.Height = h;
                    }
                    catch (Exception)
                    {
                        throw new Exception("Property 'Height' is not a valid number or less than zero.");
                    }
                    break;
                case "stretch":
                    try
                    {
                        this.Stretch = Boolean.Parse(GetStringValue(value));
                    }
                    catch (Exception)
                    {
                        throw new Exception("Property 'Stretch' is not a valid boolean value.");
                    }
                    break;
                case "outputformat": this.ResizeOutputFormat = GetImageFormat(GetStringValue(value)); break;
                case "smoothingmode": this.ResizeSmoothingMode = (SmoothingMode)Enum.Parse(ResizeSmoothingMode.GetType(), GetStringValue(value), true); break;
                case "interpolationmode": this.ResizeInterpolationMode = (InterpolationMode)Enum.Parse(ResizeInterpolationMode.GetType(), GetStringValue(value), true); break;
                case "pixeloffsetmode": this.ResizePixelOffsetMode = (PixelOffsetMode)Enum.Parse(ResizePixelOffsetMode.GetType(), GetStringValue(value), true); break;
                case "resizetypemode": this.ResizeType = (ResizeTypeList)Enum.Parse(ResizeType.GetType(), GetStringValue(value), true); break;
                case "cropvalign": 
                        this.CropVAlign = GetStringValue(value);
                    break;
                case "crophalign": 
                        this.CropHAlign = GetStringValue(value);
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        private static string GetStringValue(object value)
        {
            return value == null ? string.Empty : value.ToString();
        }


        /// <summary>
        /// Property of this application's cache folder where to put the resized images. Returns a full path pointing to a folder on the disk.
        /// </summary>
        private string AppCacheFolder
        {
            get
            {
                var cacheFolder = Configuration.CacheConfiguration.ResizedImagesCacheFolder;
                if (string.IsNullOrEmpty(cacheFolder))
                    throw new Exception("Configuration for Image Resize Application could not be found.");

                var cacheFolderPathRoot = System.IO.Path.GetPathRoot(cacheFolder);

                // check if it is a path under the website or an absolute path to a folder on a disk
                if (cacheFolderPathRoot == @"\" || String.IsNullOrEmpty(cacheFolderPathRoot))
                {
                    // it is a directory under the website's root folder
                    return HttpContext.Current.Server.MapPath(cacheFolder + "/" + String.Format("{0}_{1}", DisplayName, this.Id.ToString()));
                }

                // it is a folder somewhere on a disk
                return cacheFolder;
            }
        }

        /// <summary>
        /// Returns a string representing the virtual path of the given image (as a node) in the cache folder where it is supposed to be located.
        /// </summary>
        /// <param name="contentPath">Path of image.</param>
        /// <returns>Returns the virtual cache path of the given image.</returns>
        private string GetImageCachePath(string contentPath)
        {
            // We need to cut the starting slash before "Root" and we also need to replace the other slashes in order to able to combine the paths.
            string fileName = System.IO.Path.Combine(AppCacheFolder, contentPath.Replace("/Root", "Root").Replace("/", "\\"));

            if (!String.IsNullOrEmpty(ResizedImageExtension) && System.IO.Path.GetExtension(fileName) != ResizedImageExtension)
            {
                fileName = fileName.Replace(System.IO.Path.GetExtension(fileName), ResizedImageExtension);
            }

            return fileName;
        }

        /// <summary>
        /// Checks if the cache folder is exists and if it doesn't the folder will be created.
        /// </summary>
        private void CheckCacheFolder()
        {
            if (!Directory.Exists(AppCacheFolder))
            {
                try
                {
                    Directory.CreateDirectory(AppCacheFolder);
                }
                catch (Exception)
                {
                    throw new Exception("Could not create this Image Resize Application's cache folder.");
                }
            }
        }

        /// <summary>
        /// Creates this application's cache folder. If it already exists an exception will be thrown.
        /// </summary>
        private void CreateCacheFolder()
        {
            if (!Directory.Exists(AppCacheFolder))
            {
                try
                {
                    Directory.CreateDirectory(AppCacheFolder);
                }
                catch (Exception)
                {
                    throw new Exception("Could not create this Image Resize Application's cache folder.");
                }
            }
            else
            {
                throw new Exception("Image Resize Application's cache folder is already exists.");
            }
        }

        /// <summary>
        /// Deletes this application's cache folder if it exists.
        /// </summary>
        private void DeleteCacheFolder()
        {
            if (Directory.Exists(AppCacheFolder))
            {
                try
                {
                    Directory.Delete(AppCacheFolder, true);
                }
                catch (Exception)
                {
                    throw new Exception("Could not delete this Image Resize Application's cache folder.");
                }
            }
        }

        /// <summary>
        /// Recreates cache folder. If folder does exist then it will be deleted.
        /// </summary>
        private void ReCreateCacheFolder()
        {
            DeleteCacheFolder();
            CreateCacheFolder();
        }

        /// <summary>
        /// Returns the mime type of the given image.
        /// </summary>
        /// <param name="imagePath">Path of image.</param>
        /// <returns>Returns the mime type.</returns>
        private string GetMimeType(string imagePath)
        {
            string ext = System.IO.Path.GetExtension(imagePath).ToLower();
            if (String.IsNullOrEmpty(ext))
                if (!string.IsNullOrEmpty(imagePath))
                    if (imagePath[0] == '/')//if path starts '/' it's a virtual path and could use MapPath
                        ext = System.IO.Path.GetExtension(HttpContext.Current.Server.MapPath(imagePath));
                    else // it's a physical don't need MapPath
                        ext = System.IO.Path.GetExtension(imagePath);
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                case ".jif":
                case ".jfif":
                case ".jfi":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".png":
                    return "image/png";
                case ".ico":
                    return "image/ico";
                case ".svg":
                case ".svgz":
                    return "image/svg+xml";
                case ".tif":
                case ".tiff":
                    return "image/tiff";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Checks if the given image is cached on the disk.
        /// </summary>
        /// <param name="contentPath">Path of image in the Repository to be checked for existance on the disk.</param>
        /// <returns>Returns true if the given image can be found on the disk.</returns>
        private bool IsCached(string contentPath)
        {
            return System.IO.File.Exists(GetImageCachePath(contentPath));
        }

        protected override void OnCreating(object sender, SenseNet.ContentRepository.Storage.Events.CancellableNodeEventArgs e)
        {
            if (HttpContext.Current != null)
                ReCreateCacheFolder();
        }

        protected override void OnModifying(object sender, SenseNet.ContentRepository.Storage.Events.CancellableNodeEventArgs e)
        {
            if (HttpContext.Current != null)
                ReCreateCacheFolder();
        }

        protected override void OnDeleted(object sender, SenseNet.ContentRepository.Storage.Events.NodeEventArgs e)
        {
            if (HttpContext.Current != null)
                DeleteCacheFolder();
        }

        public void ResizeImage(HttpContext context)
        {
            Stream imageStream = null;
            var imageNodePath = HttpContext.Current.Request.FilePath;

            var content = Content.Create(PortalContext.Current.ContextNode);
            var contentPath = "";

            var contentFileName = "";
            var contentId = -1;

            CheckCacheFolder();

            if (!string.IsNullOrEmpty(imageNodePath))
            {
                if (ImageType == "Binary")
                {
                    var contentBinary = content.Fields["Binary"].GetData() as BinaryData;

                    if (contentBinary == null)
                    {
                        throw new Exception("Can not read Binary field from the given Content. Doesn't exists?");
                    }

                    imageStream = contentBinary.GetStream();
                    contentFileName = content.Name;
                    contentId = content.Id;
                }
                else if (ImageType == "ImageData")
                {
                    if (!String.IsNullOrEmpty(ImageFieldName))
                    {
                        try
                        {
                            var contentImageFieldData = content.Fields[ImageFieldName].GetData() as SenseNet.ContentRepository.Fields.ImageField.ImageFieldData;

                            if (contentImageFieldData.ImgData.Size > 0)
                            {
                                imageStream = contentImageFieldData.ImgData.GetStream();
                                contentFileName = new FileInfo(contentImageFieldData.ImgData.FileName.FullFileName).Name;
                                contentId = contentImageFieldData.ImgData.Id;
                            }
                            else
                            {
                                imageStream = contentImageFieldData.ImgRef.Binary.GetStream();
                                contentFileName = new FileInfo(contentImageFieldData.ImgRef.Path).Name;
                                contentId = contentImageFieldData.ImgRef.Id;
                            }
                        }
                        catch (Exception)
                        {
                            throw new Exception("Invalid Image Field Name was given in the application.");
                        }
                    }
                    else
                    {
                        throw new Exception("There was no ImageFieldName specified when using ImageData as ImageType.");
                    }
                }
                else if (ImageType == "Reference")
                {
                    if (!String.IsNullOrEmpty(ImageFieldName))
                    {
                        try
                        {
                            //TODO: GetData can return with null, a Node or a List<node>
                            var referenceField = content.Fields[ImageFieldName].GetData() as List<Node>;
                            var refContent = Content.Create(referenceField[0]);
                            var refContentBinary = refContent.Fields["Binary"].GetData() as BinaryData;
                            imageStream = refContentBinary.GetStream();
                            contentFileName = refContent.Name;
                            contentId = refContent.Id;
                        }
                        catch (Exception)
                        {
                            //TODO: empty catch block
                        }
                    }
                    else
                    {
                        throw new Exception("There was no ImageFieldName specified when using ImageData as ImageType.");
                    }
                }
                else if (ImageType == "Attachment")
                {
                    if (!String.IsNullOrEmpty(ImageFieldName))
                    {
                        try
                        {
                            var binary = content.Fields[ImageFieldName].GetData() as BinaryData;
                            imageStream = binary.GetStream();
                            contentFileName = new FileInfo(binary.FileName.FullFileName).Name;
                            contentId = binary.Id;
                        }
                        catch (Exception)
                        {
                            throw new Exception(
                                String.Format("The given image field field '{0}' is not a valid binary field of an image.", ImageFieldName));
                        }
                    }
                }

                // generating contentPath
                int lastDotIndex = contentFileName.LastIndexOf('.');
                contentPath = lastDotIndex != -1
                                  ? contentFileName.Insert(lastDotIndex, String.Format("_{1}{0}", contentId.ToString(), ResizeType == ResizeTypeList.Resize ? "R_" : "C_"))
                                  : String.Format("{0}_{2}{1}", contentFileName, contentId.ToString(), ResizeType == ResizeTypeList.Resize ? "R_" : "C_");

                if (IsCached(contentPath))
                {
                    FlushCachedFile(contentPath, context);
                    return;
                }

                if (ResizeType == ResizeTypeList.Resize)
                {
                    // Resize image
                    imageStream = ImageResizer.CreateResizedImageFile(imageStream, Width, Height, 0, Stretch,
                                                                      IsAutoOutputFormat ? GetImageFormat(GetMimeType(contentPath)) : this.ResizeOutputFormat,
                                                                      this.ResizeSmoothingMode,
                                                                      this.ResizeInterpolationMode,
                                                                      this.ResizePixelOffsetMode);
                }
                else
                {
                    double verticalDiff;
                    double horizontalDiff;
                    
                    switch(CropVAlign.ToLower())
                    {
                        case "top":
                            verticalDiff = 0;
                            break;
                        case "center":
                            verticalDiff = -1;
                            break;
                        case "bottom":
                            verticalDiff = double.MaxValue;
                            break;
                        default:
                            try
                            {
                                verticalDiff = Convert.ToDouble(CropVAlign);
                            }
                            catch (Exception ex)
                            {
                                SnLog.WriteException(ex);
                                verticalDiff = 0;
                            }
                            break;
                    }

                    switch (CropHAlign.ToLower())
                    {
                        case "left":
                            horizontalDiff = 0;
                            break;
                        case "center":
                            horizontalDiff = -1;
                            break;
                        case "right":
                            horizontalDiff = double.MaxValue;
                            break;
                        default:
                            try
                            {
                                horizontalDiff = Convert.ToDouble(CropHAlign);
                            }
                            catch (Exception ex)
                            {
                                SnLog.WriteException(ex);
                                horizontalDiff = 0;
                            }
                            break;
                    }
                    // Crop image
                    imageStream = ImageResizer.CreateCropedImageFile(imageStream, Width, Height, 0, 
                                                                      IsAutoOutputFormat ? GetImageFormat(GetMimeType(contentPath)) : this.ResizeOutputFormat,
                                                                      this.ResizeSmoothingMode,
                                                                      this.ResizeInterpolationMode,
                                                                      this.ResizePixelOffsetMode,verticalDiff,horizontalDiff);
                }

                Cache(imageStream, GetImageCachePath(contentPath));
                FlushStream(imageStream, context, GetMimeType(contentPath));
                return;
            }
            else
            {
                throw new Exception("There was no image in the requested file path.");
            }
        }

        /// <summary>
        /// Flushes (writes) the image to the output from cache.
        /// </summary>
        /// <param name="imageNodePath">Path of image in the Repository.</param>
        /// <param name="context">Output context where the image should be flushed to.</param>
        private void FlushCachedFile(string imageNodePath, HttpContext context)
        {
            var fileName = GetImageCachePath(imageNodePath);
            
            // Open the file in ReadOnly mode
            using (var imageStream = System.IO.File.OpenRead(fileName))
            {
                FlushStream(imageStream, context, GetMimeType(fileName));
            }
        }

        /// <summary>
        /// Flushes (writes) the image to the output from the given stream.
        /// </summary>
        /// <param name="imageStream">Stream of image to flush.</param>
        /// <param name="context">Output context where the image should be flushed to.</param>
        /// <param name="mimeType">Mime type of the given image.s</param>
        private void FlushStream(Stream imageStream, HttpContext context, string mimeType)
        {
            context.Response.ContentType = mimeType;
            context.Response.Clear();

            const int bufferSize = 256;
            int bytesRead;
            var buffer = new byte[bufferSize];

            imageStream.Position = 0;
            while ((bytesRead = imageStream.Read(buffer, 0, bufferSize)) > 0)
            {
                context.Response.OutputStream.Write(buffer, 0, bytesRead);
            }
            context.Response.Flush();
        }

        /// <summary>
        /// Caches (creates) the image into the Application's Cache Folder.
        /// </summary>
        /// <param name="imageStream">Stream of the image.</param>
        /// <param name="imgCachePath">Where the images should be created.</param>
        private void Cache(Stream imageStream, string imgCachePath)
        {
            // Create the directory for the image
            if (!Directory.Exists(new FileInfo(imgCachePath).DirectoryName))
            {
                try
                {
                    Directory.CreateDirectory(new FileInfo(imgCachePath).DirectoryName);
                }
                catch (Exception)
                {
                    throw new Exception("Cannot create cache folder for image.");
                }
            }

            // Create the image
            using (var fs = System.IO.File.Create(imgCachePath))
            {
                const int bufferSize = 256;
                var buffer = new byte[bufferSize];

                imageStream.Position = 0;
                int bytesRead;
                while ((bytesRead = imageStream.Read(buffer, 0, bufferSize)) > 0)
                {
                    fs.Write(buffer, 0, bytesRead);
                }
            }
        }

        private ImageFormat GetImageFormat(string imageType)
        {
            ImageFormat imf = null;
            switch (imageType.ToLower())
            {

                case "image/jpeg":
                case "jpeg": imf = ImageFormat.Jpeg; break;

                case "image/gif":
                case "gif": imf = ImageFormat.Gif; break;

                case "image/png":
                case "png": imf = ImageFormat.Png; break;

                case "image/ico":
                case "icon": imf = ImageFormat.Icon; break;

                case "image/svg+xml": imf = ImageFormat.Png; break;

                case "image/tiff":
                case "tiff": imf = ImageFormat.Tiff; break;

                case "auto": imf = null; break;

                default: imf = ImageFormat.Png; break;
            }
            return imf;
        }

        // =================== IHttpHandler members ===================
        #region IHttpHandler functions

        bool IHttpHandler.IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            ResizeImage(context);
        }

        #endregion
    }
}