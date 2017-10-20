using System.Runtime.Remoting.Messaging;
using System.Web;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Internal class responsible mainly for managing transaction-related context.
    /// </summary>
    public static class ContextHandler
    {
        /// <summary>
        /// Gets a context object by its id from the context store of the current environment 
        /// (e.g. HttpContext or logical CallContext).
        /// </summary>
        public static object GetObject(string ident)
        {
            if(Configuration.Common.IsWebEnvironment)
            {
                return HttpContext.Current.Items[ident];
            }
            else
            {
                // Works with the logical context that will be passed through 
                // async /await code blocks. Simple GetData is not sufficient.
                return CallContext.LogicalGetData(ident);
            }
        }
        /// <summary>
        /// Puts a context object by its id into the context store of the current environment 
        /// (e.g. HttpContext or logical CallContext).
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="value"></param>
        public static void SetObject(string identifier, object value)
        {
            if(Configuration.Common.IsWebEnvironment)
            {
                HttpContext.Current.Items[identifier] = value;
            }
            else
            {
                // Works with the logical context that will be passed through 
                // async /await code blocks. Simple SetData is not sufficient.
                CallContext.LogicalSetData(identifier, value);
            }
        }

		private const string TransactionIdent = "SnCr.Transaction";
		private const string TransactionQueueIdent = "SnCr.TransactionQueue";

        internal static ITransactionProvider GetTransaction()
        {
            return (ITransactionProvider)GetObject(TransactionIdent);
        }
        internal static void SetTransaction(ITransactionProvider transaction)
        {
            SetObject(TransactionIdent, transaction);
        }

		internal static TransactionQueue GetTransactionQueue()
        {
			return (TransactionQueue)GetObject(TransactionQueueIdent);
        }
		internal static void SetTransactionQueue(TransactionQueue queue)
        {
			SetObject(TransactionQueueIdent, queue);
        }

        public static void Reset()
        {
            SetObject(TransactionIdent, null);
            SetObject(TransactionQueueIdent, null);
        }

    }
}