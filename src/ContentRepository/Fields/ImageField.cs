using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using System.Xml;

namespace SenseNet.ContentRepository.Fields
{
    public enum ImageRequestMode
    {
        None,
        BinaryData,
        Reference
    }


    [ShortName("Image")]
    [DataSlot(0, RepositoryDataType.Reference, typeof(Node), typeof(Image))]
	[DataSlot(1, RepositoryDataType.Binary, typeof(BinaryData))]
    [DefaultFieldSetting(typeof(NullFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.Image")]
    [FieldDataType(typeof(ImageFieldData))]
    public class ImageField : Field, SenseNet.ContentRepository.Xpath.IXmlAttributeOwner
    {
        /* ============================================================================== ImageFieldData */
        public class ImageFieldData
        {
            [System.Xml.Serialization.XmlIgnore, System.Web.Script.Serialization.ScriptIgnore]
            public Field Field { get; set; }
            public Image ImgRef { get; set; }
            public BinaryData ImgData { get; set; }

            public ImageFieldData(Field field)
            {
                this.Field = field;
            }
            public ImageFieldData(Field field, Image imgRef, BinaryData imgData) : this(field)
            {
                ImgRef = imgRef;
                ImgData = imgData;
            }
        }


        /* ============================================================================== Members */
        private ImageFieldData _data;


        /* ============================================================================== Field methods */
        protected override bool HasExportData { get { return false; } }
        protected override void ImportData(XmlNode fieldNode, ImportContext context)
        {
            throw new SnNotSupportedException("The ImportData operation is not supported on ImageField.");
        }
        protected override object[] ConvertFrom(object value)
        {
            ImageFieldData data = null;
            if (value is int intValue)
            {
                if (Node.LoadNode(intValue) is Image refImage)
                    data = new ImageFieldData(this, refImage, null);
            }
            else if (value is string stringValue)
            {
                try
                {
                    if (Node.LoadNode(stringValue) is Image refImage)
                        data = new ImageFieldData(this, refImage, null);
                }
                catch
                {
                    // ignored
                }
            }
            else if (value is long longValue)
            {
                try
                {
                    var id = Convert.ToInt32(longValue);
                    if (Node.LoadNode(id) is Image refImage)
                        data = new ImageFieldData(this, refImage, null);
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                data = value as ImageFieldData;
            }

            if (data == null)
                throw new NotSupportedException("Field value is null or not a valid ImageFieldData or content id or path. FieldName: " + this.Name);
            _data = data;

            var result = new object[2];

            result[0] = data.ImgRef == null ? new List<Node>() : new List<Node>() { data.ImgRef };
            result[1] = data.ImgData ?? new BinaryData();

            return result;
        }
        protected override object ConvertTo(object[] handlerValues)
        {
            var result = new ImageFieldData(this);

            if (handlerValues[0] != null)
            {
                var nodeList = handlerValues[0] as IEnumerable<Node>;
                if (nodeList != null)
                {
                    result.ImgRef = nodeList.FirstOrDefault() as Image;
                }
            }

            if (handlerValues[1] != null)
                result.ImgData = handlerValues[1] as BinaryData;

            _data = result;
            return result;
        }
        protected override void WriteXmlData(XmlWriter writer)
        {
            // ImageRequestMode.BinaryData: /binaryhandler.ashx?nodeid=1129&propertyname=ImageData
            // ImageRequestMode.Reference: /Root/Default_Site/Demo_Website/myNode/myImage.jpg
            writer.WriteAttributeString("imageMode", this.ImageMode.ToString());
            writer.WriteString(this.ImageUrl);
        }

        /* ============================================================================== XPathNavigator helpers */
        protected override string GetXmlData()
        {
            return this.ImageUrl;
        }

        /* --------------------------------------------------------------------- IXmlAttributeOwner Members */
        private static string[] AttributeNames = new string[] { "imageMode" };
        public IEnumerable<string> GetXmlAttributeNames()
        {
            return AttributeNames;
        }
        public string GetXmlAttribute(string name)
        {
            if (name == "imageMode")
                return this.ImageMode.ToString();
            return null;
        }
        
        /* ============================================================================== Properties */
        public string BinaryPropertyName
        {
            get
            {
                return this.FieldSetting.Bindings[1];
            }
        }
        public ImageRequestMode ImageMode
        {
            get
            {
                return GetImageMode(_data);
            }
        }
        public string ImageUrl
        {
            // ImageRequestMode.BinaryData: /binaryhandler.ashx?nodeid=1129&propertyname=ImageData
            // ImageRequestMode.Reference: /Root/Default_Site/Demo_Website/myNode/myImage.jpg
            // otherwise empty string
            get
            {
                return GetImageUrl(this.ImageMode, _data, this.Content.Id, this.BinaryPropertyName);
            }
        }


        /* ============================================================================== Public methods */
        public static string GetSizeUrlParams(ImageRequestMode imageMode, int width, int height)
        {
            string sizeStr = string.Empty;
            if (width != 0 && width != 0)
            {
                switch (imageMode)
                {
                    case ImageRequestMode.BinaryData:
                        sizeStr = string.Format("width={0}&height={1}", width, height);
                        break;
                    case ImageRequestMode.Reference:
                        sizeStr = string.Format("width={0}&height={1}", width, height);
                        break;
                    default:
                        break;
                }
            }
            return sizeStr;
        }
        public bool SetThumbnailReference(Image thumbnailImage)
        {
            // if image is already set either as reference or binarydata, do nothing
            if (this.ImageMode != ImageRequestMode.None)
                return false;

            var data = new ImageFieldData(this, thumbnailImage, null);
            this.SetData(data);
            return true;
        }
        public static ImageRequestMode GetImageMode(ImageFieldData data)
        {
            if (data != null && data.ImgData != null && !data.ImgData.IsEmpty && data.ImgData.Size > 0)
            {
                return ImageRequestMode.BinaryData;
            }
            else
            {
                if (data != null && data.ImgRef != null)
                {
                    return ImageRequestMode.Reference;
                }
                else
                {
                    return ImageRequestMode.None;
                }
            }
        }
        public static string GetImageUrl(ImageRequestMode imageMode, ImageFieldData data, int contentId, string binaryPropertyName)
        {
            var imageUrl = string.Empty;

            switch (imageMode)
            {
                case ImageRequestMode.Reference:
                    imageUrl = data.ImgRef.Path;
                    break;
                case ImageRequestMode.BinaryData:
                    if (contentId > 0)
                        imageUrl = BinaryField.GetBinaryUrl(contentId, binaryPropertyName, data.ImgData.Timestamp);
                    break;
            }

            return imageUrl;
        }

    }
}
