using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository;

namespace SenseNet.Preview
{
    /// <summary>
    /// This exception will be thrown when a preview image is expected but not found.
    /// The various reasons are described by the Image index and the DocumentState properties.
    /// Available combinations are here:
    /// ImageIndex: -1, DocumentState: less than 0 : cannot generate preview; the real reason is encoded in the DocumentState.
    /// ImageIndex = -1, DocumentState = 0 : document not found
    /// ImageIndex = 0 or greater, DocumentState = greater than 0 : image not found
    /// </summary>
    [Serializable]
    public class PreviewNotAvailableException : ApplicationException
    {
        public int ImageIndex { get; private set; }
        public int DocumentState { get; private set; }

        public PreviewNotAvailableException(string message, int imageIndex, int documentState) 
            : base(message)
        {
            this.ImageIndex = imageIndex;
            this.DocumentState = documentState;
        }
        public PreviewNotAvailableException(string message, int imageIndex, int documentState, Exception inner)
            : base(message, inner)
        {
            this.ImageIndex = imageIndex;
            this.DocumentState = documentState;
        }

        protected PreviewNotAvailableException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
