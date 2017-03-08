using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository
{
    public enum InvalidContentActionReason
    {
        NotSpecified,
        UnknownAction,
        InvalidStateAction,
        CheckedOutToSomeoneElse,
        UndoSingleVersion,
        NotEnoughPermissions,
        MultistepSaveInProgress
    }

    [global::System.Serializable]
    public class InvalidContentActionException : Exception
    {
        protected static string Error_InvalidContentAction = "$Error_ContentRepository:InvalidContentAction_";
        protected static string ActionString = "$Explore:Action";

        public InvalidContentActionReason Reason { get; private set; }
        public string Path { get; private set; }
        public string ActionType { get; private set; }

        public InvalidContentActionException(InvalidContentActionReason reason, string path, string message=null, string actionType = null) : base(GetMessage(reason, actionType, message))
        {
            Reason = reason;
            Path = path;
            ActionType = actionType;
        }

        public InvalidContentActionException(string message) : base(message) { }
        public InvalidContentActionException(string message, Exception inner) : base(message, inner) { }

        protected InvalidContentActionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            Reason = (InvalidContentActionReason)info.GetInt32("reason");
            Path = info.GetString("path");
            ActionType = info.GetString("actionType");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("reason", (int)Reason);
            info.AddValue("path", Path);
            info.AddValue("actionType", ActionType);
        }

        protected static string GetMessage(InvalidContentActionReason reason, string actionType, string message)
        {
            if (message != null)
                return message;

            string customMessage = SR.GetString(Error_InvalidContentAction + Enum.GetName(typeof(InvalidContentActionReason), reason));
            if (!String.IsNullOrEmpty(actionType))
            {
                return customMessage + " " + SR.GetString(ActionString) + " " + actionType;
            }
            else
            {
                return customMessage;
            }
            
        }
    }
}
