using System;
using System.IO;
using System.Linq;
using System.Web;
using SenseNet.ContentRepository.Fields;
using SenseNet.Preview;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Events;


namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines a content handler for storing images in the Content Repository. Inherited from the <see cref="File"/> type.
    /// The image is stored in the Binary property as a blob.
    /// </summary>
    [ContentHandler]
    public class Image : File, IHttpHandler
    {
        private static readonly string SETDIMENSION_KEYNAME = "SetDimension";

        // ================================================================================= Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public Image(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public Image(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class during the loading process.
        /// Do not use this constructor directly in your code.
        /// </summary>
        protected Image(NodeToken nt) : base(nt) { }


        // ================================================================================= Properties

        /// <summary>
        /// Gets the extension part of the Name property.
        /// Note that the "jpg" is converted to "jpeg".
        /// </summary>
        public string Extension
        {
            get
            {
                string[] nameParts = Name.Split('.');
                string extension = nameParts[nameParts.Length - 1].ToLower();
                if (extension == "jpg")
                    extension = "jpeg";
                return extension;
            }
        }

        private const string WIDTH_PROPERTY = "Width";
        /// <summary>
        /// Gets or sets the width of the image. Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty(WIDTH_PROPERTY, RepositoryDataType.Int)]
        public int Width
        {
            get { return base.GetProperty<int>(WIDTH_PROPERTY); }
            protected set { base.SetProperty(WIDTH_PROPERTY, value); }
        }

        private const string HEIGHT_PROPERTY = "Height";
        /// <summary>
        /// Gets or sets the height of the image. Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty(HEIGHT_PROPERTY, RepositoryDataType.Int)]
        public int Height
        {
            get { return base.GetProperty<int>(HEIGHT_PROPERTY); }
            protected set { base.SetProperty(HEIGHT_PROPERTY, value); }
        }

        // ================================================================================= Methods

        /// <summary>
        /// Returns an <see cref="System.Drawing.Imaging.ImageFormat"/> value converted from the given string value.
        /// Note that "gif" is converted to ImageFormat.Png.
        /// </summary>
        /// <param name="contentType">String representation of an image format (e.g. png, jpeg) or an image file name.</param>
        public static System.Drawing.Imaging.ImageFormat getImageFormat(string contentType)
        {
            var lowerContentType = contentType.ToLower();

            if (lowerContentType.EndsWith("png"))
                return System.Drawing.Imaging.ImageFormat.Png;
            if (lowerContentType.EndsWith("bmp"))
                return System.Drawing.Imaging.ImageFormat.Bmp;
            if (lowerContentType.EndsWith("jpeg"))
                return System.Drawing.Imaging.ImageFormat.Jpeg;
            if (lowerContentType.EndsWith("jpg"))
                return System.Drawing.Imaging.ImageFormat.Jpeg;

            // gif -> png! resizing gif with gif imageformat ruins alpha values, therefore we return with png
            if (lowerContentType.EndsWith("gif"))
                return System.Drawing.Imaging.ImageFormat.Png;
            if (lowerContentType.EndsWith("tiff"))
                return System.Drawing.Imaging.ImageFormat.Tiff;
            if (lowerContentType.EndsWith("wmf"))
                return System.Drawing.Imaging.ImageFormat.Wmf;
            if (lowerContentType.EndsWith("emf"))
                return System.Drawing.Imaging.ImageFormat.Emf;
            if (lowerContentType.EndsWith("exif"))
                return System.Drawing.Imaging.ImageFormat.Exif;

            return System.Drawing.Imaging.ImageFormat.Jpeg;
        }
        /// <summary>
        /// Returns a new <see cref="Image"/> instance created from the given <see cref="BinaryData"/> 
        /// under the specified <see cref="IFolder"/> instance. The Content will not been saved yet.
        /// </summary>
        /// <param name="parent">An existing <see cref="IFolder"/> instance.</param>
        /// <param name="binaryData">The <see cref="BinaryData"/> instance that is the data source of the image.</param>
        /// <returns></returns>
        public new static Image CreateByBinary(IFolder parent, BinaryData binaryData)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            if (binaryData == null)
                return new Image(parent as Node);

            Image image = new Image(parent as Node);

            // set image name using the provided binary data
            if (!string.IsNullOrEmpty(binaryData.FileName))
                image.Name = binaryData.FileName;

            // Resolve filetype by binary-config matching
            if (string.IsNullOrEmpty(binaryData.ContentType))
                binaryData.FileName = binaryData.FileName;

            image.Binary = binaryData;

            return image;
        }
        /// <summary>
        /// Returns <see cref="Stream"/> of the dynamically generated thumbnail image.
        /// </summary>
        /// <param name="width">Desired width of the thumbnail image.</param>
        /// <param name="height">Desired height of the thumbnail image.</param>
        /// <param name="contentType">Image format specifier. The value can be:
        /// png, bmp, jpeg, jpg, gif, tiff, wmf, emf, exif. Note that gif will be converted to png.</param>
        public Stream GetDynamicThumbnailStream(int width, int height, string contentType)
        {
            return ImageResizer.CreateResizedImageFile(Binary.GetStream(), width, height, 80, getImageFormat(contentType));
        }
        /// <summary>
        /// Returns <see cref="Stream"/> of the resized version of the provided image stream.
        /// </summary>
        /// <param name="originalStream">The <see cref="Stream"/> of the image to be resized.</param>
        /// <param name="ext">Not used.</param>
        /// <param name="x">Desired width of the resized image.</param>
        /// <param name="y">Desired height of the resized image.</param>
        /// <param name="q">Not used.</param>
        /// <param name="contentType">Image format specifier. The value can be:
        /// png, bmp, jpeg, jpg, gif, tiff, wmf, emf, exif. Note that gif will be converted to png.</param>
        public static Stream CreateResizedImageFile(Stream originalStream, string ext, double x, double y, double q, string contentType)
        {
            return ImageResizer.CreateResizedImageFile(originalStream, x, y, q, getImageFormat(contentType));
        }

        /// <summary>
        /// Finalizes the object creation.
        /// Do not use this method directly in your code.
        /// </summary>
        protected override void OnCreated(object sender, SenseNet.ContentRepository.Storage.Events.NodeEventArgs e)
        {
            var image = sender as Image;
            if (image == null)
                return;

            // thumbnail has been loaded -> reference it in parent's imagefield (if such exists)
            if (image.Name.ToLower().StartsWith("thumbnail"))
            {
                var parent = image.Parent;
                var content = Content.Create(parent);

                // first available imagefield is used
                var imageField = content.Fields.Where(d => d.Value is ImageField).Select(d => d.Value as ImageField).FirstOrDefault();
                if (imageField != null)
                {
                    // initialize field (field inner data is not yet initialized from node properties!)
                    imageField.GetData();

                    // set reference
                    var result = imageField.SetThumbnailReference(image);
                    if (result)
                        content.Save();
                }
            }
            base.OnCreated(sender, e);

            // refresh image width/height than save the content again
            if (MustRefreshDimensions(image, e))
            {
                image.Save(SavingMode.KeepVersion);
            }
        }
        /// <summary>
        /// Finalizes the object creation.
        /// Do not use this method directly in your code.
        /// </summary>
        protected override void OnCreating(object sender, Storage.Events.CancellableNodeEventArgs e)
        {
            base.OnCreating(sender, e);
            if(!e.Cancel)
            {
                var img = sender as Image;
                if (img == null)
                    return;

                if (img.SavingState == ContentSavingState.Finalized)
                {
                    SetDimension(img);
                }
                else
                {
                    // postpone dimension setting because the content is not yet finalized
                    e.SetCustomData(SETDIMENSION_KEYNAME, true);
                }
            }
        }
        /// <summary>
        /// Finalizes the object modification.
        /// Do not use this method directly in your code.
        /// </summary>
        protected override void OnModified(object sender, Storage.Events.NodeEventArgs e)
        {
            base.OnModified(sender, e);

            var image = sender as Image;
            if (image == null)
                return;

            // refresh image width/height than save the content again
            if (MustRefreshDimensions(image, e))
            {
                image.Save(SavingMode.KeepVersion);
            }
        }
        /// <summary>
        /// Finalizes the object modification.
        /// Do not use this method directly in your code.
        /// </summary>
        protected override void OnModifying(object sender, Storage.Events.CancellableNodeEventArgs e)
        {
            base.OnModifying(sender, e);
            if (!e.Cancel)
            {
                var img = sender as Image;
                if (img == null)
                    return;

                if (img.SavingState == ContentSavingState.Finalized)
                {
                    SetDimension(img);
                }
                else
                {
                    // postpone dimension setting because the content is not yet finalized
                    e.SetCustomData(SETDIMENSION_KEYNAME, true);
                }
            }
        }

        /// <inheritdoc />
        public override void FinalizeContent()
        {
            base.FinalizeContent();

            // refresh image width/height than save the content again
            if (SetDimension(this))
                this.Save(SavingMode.KeepVersion);
        }

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case WIDTH_PROPERTY:
                    return this.Width;
                case HEIGHT_PROPERTY:
                    return this.Height;
                default:
                    return base.GetProperty(name);
            }
        }
        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case WIDTH_PROPERTY:
                    this.Width = (int)value;
                    break;
                case HEIGHT_PROPERTY:
                    this.Height = (int)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        private static readonly string[] SkippedImageExtensions = { "svg" };
        private static bool SetDimension(Image imgNode)
        {
            try
            {
                // skip special images that do not have real dimensions
                if (imgNode == null || SkippedImageExtensions.Contains(imgNode.Binary.FileName.Extension.ToLowerInvariant().Trim('.')))
                    return false;

                var originalWidth = imgNode.Width;
                var originalHeight = imgNode.Height;

                var imgStream = imgNode.Binary.GetStream();
                if (imgStream != null && imgStream.Length > 0)
                {
                    using (var img = System.Drawing.Image.FromStream(imgStream))
                    {
                        // if there is no need to modify the image, return false
                        if (originalWidth == img.Width && originalHeight == img.Height)
                            return false;

                        imgNode.Width = img.Width;
                        imgNode.Height = img.Height;
                    }
                }
            }
            catch(Exception ex)
            {
                SnLog.WriteWarning("Error during image processing. " + ex, properties: new Dictionary<string, object>() 
                { 
                    { "Path", imgNode == null ? string.Empty : imgNode.Path },
                    { "Name", imgNode == null ? string.Empty : imgNode.Name }
                });
            }

            return true;
        }

        private static bool MustRefreshDimensions(Image image, NodeEventArgs e)
        {
            var setDimensions = e.GetCustomData(SETDIMENSION_KEYNAME);
            if (setDimensions == null)
                return false;

            return Convert.ToBoolean(setDimensions) && 
                image.SavingState == ContentSavingState.Finalized &&
                SetDimension(image);
        }

        // ================================================================================= IHttpHandler members
        bool IHttpHandler.IsReusable
        {
            get { return false; }
        }
        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            Stream imageStream;
            BinaryData binaryData;
            var propNameParam = context.Request.QueryString["NodeProperty"];
            var propertyName = string.Empty;
            var widthParam = context.Request.QueryString["width"];
            var heightParam = context.Request.QueryString["height"];

            if (!string.IsNullOrEmpty(propNameParam))
            {
                propertyName = propNameParam.Replace("$", "#");
                binaryData = this.GetBinary(propertyName);
            }
            else
            {
                binaryData = this.Binary;
            }

            if (DocumentPreviewProvider.Current != null && DocumentPreviewProvider.Current.IsPreviewOrThumbnailImage(NodeHead.Get(this.Id)))
            {
                // get preview image with watermark or redaction if necessary
                imageStream = DocumentPreviewProvider.Current.GetRestrictedImage(this, new PreviewImageOptions() { BinaryFieldName = propertyName });
            }
            else
            {
                imageStream = binaryData.GetStream();
            }

            // set compressed encoding if necessary
            if (MimeTable.IsCompressedType(this.Extension))
                context.Response.Headers.Add("Content-Encoding", "gzip");

            context.Response.ContentType = binaryData.ContentType;

            imageStream.Position = 0;

            if (!string.IsNullOrEmpty(widthParam) && !string.IsNullOrEmpty(heightParam))
            {
                int width;
                int height;
                if (!int.TryParse(widthParam, out width))
                    width = 200;
                if (!int.TryParse(heightParam, out height))
                    height = 200;

                // compute a new, resized stream on-the-fly
                using (var resizedStream = ImageResizer.CreateResizedImageFile(imageStream, width, height, 80, getImageFormat(binaryData.ContentType)))
                {
                    resizedStream.CopyTo(context.Response.OutputStream);
                }
            }
            else
            {
                imageStream.CopyTo(context.Response.OutputStream);
            }

            imageStream.Close();
        }

        public Stream GetImageStream(string propertyName, IDictionary<string, object> parameters, out string contentType)
        {
            Stream imageStream;

            var widthParam = parameters?["width"];
            var heightParam = parameters?["height"];

            var binaryData = !string.IsNullOrEmpty(propertyName) ? GetBinary(propertyName) : Binary;

            contentType = binaryData?.ContentType ?? string.Empty;

            if (DocumentPreviewProvider.Current != null && DocumentPreviewProvider.Current.IsPreviewOrThumbnailImage(NodeHead.Get(Id)))
            {
                // get preview image with watermark or redaction if necessary
                imageStream = DocumentPreviewProvider.Current.GetRestrictedImage(this,
                    new PreviewImageOptions {BinaryFieldName = propertyName});
            }
            else
            {
                imageStream = binaryData?.GetStream();
            }

            if (imageStream == null)
                return new MemoryStream();

            imageStream.Position = 0;

            int ConvertImageParameter(object param)
            {
                if (param != null)
                {
                    // we recognize int and string values as well
                    switch (param)
                    {
                        case int i:
                            return i;
                        case string s when int.TryParse(s, out var pint):
                            return pint;
                    }
                }

                return 200;
            }

            // no resize parameters: return the original stream
            if (widthParam == null || heightParam == null)
                return imageStream;

            var width = ConvertImageParameter(widthParam);
            var height = ConvertImageParameter(heightParam);

            // compute a new, resized stream on-the-fly
            var resizedStream = ImageResizer.CreateResizedImageFile(imageStream, width, height, 80, getImageFormat(contentType));
                
            // in case the method created a new stream, we have to close the original to prevent memory leak
            if (resizedStream != imageStream)
                imageStream.Close();

            return resizedStream;
        }
    }
}
