using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace SenseNet.ContentRepository.Storage.Data
{
	[global::System.Serializable]
	public class DataOperationException : RepositoryException
	{
	    public DataOperationResult Result
	    {
            get { return (DataOperationResult) base.ErrorNumber; }
	    }

		public DataOperationException(DataOperationResult errType) : this(errType, GetMessage(errType)) { }
		public DataOperationException(DataOperationResult errType, string message) : base((int)errType, message) { }
		public DataOperationException(DataOperationResult errType, string message, Exception inner) : base((int)errType, message, inner) { }
		protected DataOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

		private static string GetMessage(DataOperationResult errType)
		{
			switch (errType)
			{
				case DataOperationResult.Successful:
					return "Successful";
				case DataOperationResult.Copy_TargetContainsSameName:
					return "Copy_TargetContainsSameName";
				case DataOperationResult.Copy_PartialOpenMinorPermission:
					return "Copy_PartialOpenMinorPermission";
				case DataOperationResult.Copy_ExpectedAddNewPermission:
					return "Copy_ExpectedAddNewPermission";
				case DataOperationResult.Copy_ContentList:
                    return "Copy_ContentList";
				case DataOperationResult.Copy_NodeWithContentListContent:
                    return "Copy_NodeWithContentListContent";
				case DataOperationResult.Move_TargetContainsSameName:
					return "Move_TargetContainsSameName";
				case DataOperationResult.Move_PartiallyLockedSourceTree:
					return "Move_PartiallyLockedSourceTree";
				case DataOperationResult.Move_PartialOpenMinorPermission:
					return "Move_PartialOpenMinorPermission";
				case DataOperationResult.Move_PartialDeletePermission:
					return "Move_PartialDeletePermission";
				case DataOperationResult.Move_ExpectedAddNewPermission:
					return "Move_ExpectedAddNewPermission";
				case DataOperationResult.Move_ContentListUnderContentList:
                    return "Move_ContentListUnderContentList";
				case DataOperationResult.Move_NodeWithContentListContentUnderContentList:
                    return "Move_NodeWithContentListContentUnderContentList";
				default:
					return "";
			}
		}
	}

    [Serializable]
    public class NodeAlreadyExistsException : DataOperationException
    {
        public NodeAlreadyExistsException() : base(DataOperationResult.Save_NodeAlreadyExists) { }
        public NodeAlreadyExistsException(string message) : base(DataOperationResult.Save_NodeAlreadyExists, message) { }
        public NodeAlreadyExistsException(string message, Exception inner) : base(DataOperationResult.Save_NodeAlreadyExists, message, inner) { }
        protected NodeAlreadyExistsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}