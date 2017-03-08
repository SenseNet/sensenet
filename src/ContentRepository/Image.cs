using System;
using System.IO;
using System.Linq;
using System.Web;
using SenseNet.ContentRepository.Fields;
using SenseNet.Preview;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using System.Collections.Generic;


namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class Image : File, IHttpHandler
    {
        private static readonly string SETDIMENSION_KEYNAME = "SetDimension";

        // ================================================================================= Constructors
        public Image(Node parent) : this(parent, null) { }
        public Image(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Image(NodeToken nt) : base(nt) { }


        // ================================================================================= Properties
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
        [RepositoryProperty(WIDTH_PROPERTY, RepositoryDataType.Int)]
        public int Width
        {
            get { return base.GetProperty<int>(WIDTH_PROPERTY); }
            protected set { base.SetProperty(WIDTH_PROPERTY, value); }
        }

        private const string HEIGHT_PROPERTY = "Height";
        [RepositoryProperty(HEIGHT_PROPERTY, RepositoryDataType.Int)]
        public int Height
        {
            get { return base.GetProperty<int>(HEIGHT_PROPERTY); }
            protected set { base.SetProperty(HEIGHT_PROPERTY, value); }
        }

        // ================================================================================= Methods
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
        public static new Image CreateByBinary(IFolder parent, BinaryData binaryData)
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
        public Stream GetDynamicThumbnailStream(int width, int height, string contentType)
        {
            return ImageResizer.CreateResizedImageFile(Binary.GetStream(), width, height, 80, getImageFormat(contentType));
        }
        public static Stream CreateResizedImageFile(Stream originalStream, string ext, double x, double y, double q, string contentType)
        {
            return ImageResizer.CreateResizedImageFile(originalStream, x, y, q, getImageFormat(contentType));
        }
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
            if (MustRefreshDimensions(image, e.CustomData))
            {
                image.Save(SavingMode.KeepVersion);
            }
        }
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
                    e.CustomData = new Dictionary<string, object>() { { SETDIMENSION_KEYNAME, true } };
                }
            }
        }
        protected override void OnModified(object sender, Storage.Events.NodeEventArgs e)
        {
            base.OnModified(sender, e);

            var image = sender as Image;
            if (image == null)
                return;

            // refresh image width/height than save the content again
            if (MustRefreshDimensions(image, e.CustomData))
            {
                image.Save(SavingMode.KeepVersion);
            }
        }
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
                    e.CustomData = new Dictionary<string, object>() { { SETDIMENSION_KEYNAME, true } };
                }
            }
        }

        public override void FinalizeContent()
        {
            base.FinalizeContent();

            // refresh image width/height than save the content again
            if (SetDimension(this))
                this.Save(SavingMode.KeepVersion);
        }

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

        private static bool MustRefreshDimensions(Image image, object customData)
        { 
            var dict = customData as Dictionary<string, object>;

            return dict != null && 
                dict.ContainsKey(SETDIMENSION_KEYNAME) && 
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
    }
}
