using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    [Serializable]
    public class RestoreException : ApplicationException
    {
        public RestoreResultType ResultType { get; private set; }
        public string ContentPath { get; private set; }

        // ============================================================================== Constructors

        public RestoreException(RestoreResultType resultType)
            : this(resultType, string.Empty)
        {
        }

        public RestoreException(RestoreResultType resultType, string contentPath)
            : this(resultType, contentPath, null)
        {
        }

        public RestoreException(RestoreResultType resultType, string contentPath, Exception inner)
            : base(string.Empty, inner)
        {
            ResultType = resultType;
            ContentPath = contentPath;
        }

        // ============================================================================== Serialization

        protected RestoreException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ResultType = (RestoreResultType)info.GetInt32("rt");
            ContentPath = info.GetString("cp");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("rt", (int)ResultType);
            info.AddValue("cp", ContentPath);
        }

        // ============================================================================== Properties

        public override string Message
        {
            get
            {
                if (InnerException != null)
                    return InnerException.Message;

                var msg = base.Message;

                if (string.IsNullOrEmpty(msg))
                    msg = ContentPath;

                if (string.IsNullOrEmpty(msg))
                    msg = Enum.GetName(typeof(RestoreResultType), ResultType);

                return msg;
            }
        }

    }
}
