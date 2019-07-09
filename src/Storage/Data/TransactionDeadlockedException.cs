using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    [Serializable]
    internal class TransactionDeadlockedException : Exception
    {
        public TransactionDeadlockedException()
        {
        }
        public TransactionDeadlockedException(string message) : base(message)
        {
        }
        public TransactionDeadlockedException(string message, Exception inner) : base(message, inner)
        {
        }
        protected TransactionDeadlockedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
