using System;
using SenseNet.ContentRepository;
using SenseNet.Portal;

namespace SenseNet.ApplicationModel
{
    public class RestoreAction : UrlAction
    {
        public override bool IsODataOperation { get { return true; } }

        public override ActionParameter[] ActionParameters { get; } =
        {
            new ActionParameter("destination", typeof (string)),
            new ActionParameter("newname", typeof (bool))
        };

        public override object Execute(Content content, params object[] parameters)
        {
            // optional destination
            var destination = parameters != null && parameters.Length > 0 ? (string)parameters[0] : null;

            // optional new name parameter
            var addNewName = parameters != null && parameters.Length > 1 && parameters[1] != null ? (bool)parameters[1] : (bool?)null;

            var tb = content.ContentHandler as TrashBag;
            if (tb == null)
                throw new InvalidContentActionException("The resource content must be a trashbag.");

            if (string.IsNullOrEmpty(destination))
                destination = tb.OriginalPath;

            try
            {
                if (addNewName.HasValue)
                    TrashBin.Restore(tb, destination, addNewName.Value);
                else
                    TrashBin.Restore(tb, destination);
            }
            catch (RestoreException rex)
            {
                string msg;

                switch (rex.ResultType)
                {
                    case RestoreResultType.ExistingName:
                        msg = SNSR.GetString(SNSR.Exceptions.OData.RestoreExistingName);
                        break;
                    case RestoreResultType.ForbiddenContentType:
                        msg = SNSR.GetString(SNSR.Exceptions.OData.RestoreForbiddenContentType);
                        break;
                    case RestoreResultType.NoParent:
                        msg = SNSR.GetString(SNSR.Exceptions.OData.RestoreNoParent);
                        break;
                    case RestoreResultType.PermissionError:
                        msg = SNSR.GetString(SNSR.Exceptions.OData.RestorePermissionError);
                        break;
                    default:
                        msg = rex.Message;
                        break;
                }

                throw new Exception(msg);
            }

            return null;
        }
    }
}
