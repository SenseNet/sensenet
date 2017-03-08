using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Data
{
    [Serializable]
    public class RepositoryPathTooLongException : RepositoryException
    {
        public string Path
        {
            get; private set;
        }

        public RepositoryPathTooLongException(string path) : base(999, GetMessage(path))
        {
            Path = path;
        }

        protected RepositoryPathTooLongException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Path = info.GetString("path");
        }
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("path", Path);
        }

        private static string GetMessage(string path)
        {
            return SR.GetString(SR.Exceptions.General.Error_PathTooLong_1, path ?? string.Empty);
        }
    }
}
