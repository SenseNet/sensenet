using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    [global::System.Serializable]
    public abstract class RepositoryException : ApplicationException
    {
        private int _errorNumber;

        public int ErrorNumber
        {
            get { return _errorNumber; }
        }
        public string ErrorToken
        {
            get { return String.Concat(this.GetType().FullName, ".", ErrorNumber); }
        }

        public RepositoryException(int errorNumber)
        {
            _errorNumber = errorNumber;
        }
        public RepositoryException(int errorNumber, string message)
            : base(message)
        {
            _errorNumber = errorNumber;
        }
        public RepositoryException(int errorNumber, string message, Exception inner)
            : base(message, inner)
        {
            _errorNumber = errorNumber;
        }
        protected RepositoryException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            _errorNumber = info.GetInt32("errNo");
        }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("errNo", _errorNumber);
        }
    }
}