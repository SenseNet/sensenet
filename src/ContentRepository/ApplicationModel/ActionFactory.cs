using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Configuration;
using SenseNet.Tools;
// ReSharper disable CheckNamespace

namespace SenseNet.ApplicationModel
{
    internal class ActionFactory
    {
        internal static ActionBase CreateAction(Type actionType, Application application, Content context, string backUri, object parameters)
        {
            var act = TypeResolver.CreateInstance(actionType.FullName) as ActionBase;
            act?.Initialize(context, backUri, application, parameters);

            return act == null || !act.Visible ? null : act;
        }

        internal static ActionBase CreateAction(string actionType, Content context, string backUri, object parameters)
        {
            return CreateAction(actionType, null, context, backUri, parameters);
        }

        internal static ActionBase CreateAction(string actionType, Application application, Content context, string backUri, object parameters)
        {
            var actionName = application != null ? application.Name : actionType;

            // check versioning action validity
            if (IsInvalidVersioningAction(context, actionName))
                return null;

            if (string.IsNullOrEmpty(actionType))
                actionType = Actions.DefaultActionType;

            var act = ResolveActionType(actionType);
            if (act == null)
                throw new InvalidContentActionException(InvalidContentActionReason.UnknownAction, context.Path, null, actionType);

            act.Initialize(context, backUri, application, parameters);          

            return act.Visible ? act : null;
        }

        private static bool IsInvalidVersioningAction(Content context, string actionName)
        {
            if (string.IsNullOrEmpty(actionName) || context == null)
                return false;

            actionName = actionName.ToLower();

            if (!(context.ContentHandler is GenericContent generic))
                return false;

            switch (actionName)
            {
                case "checkin":
                    return !SavingAction.HasCheckIn(generic);
                case "checkout":
                    return (generic.VersioningMode <= VersioningType.None && !(generic is IFile || generic.NodeType.IsInstaceOfOrDerivedFrom("Page"))) || !SavingAction.HasCheckOut(generic);
                case "undocheckout":
                    return !SavingAction.HasUndoCheckOut(generic);
                case "forceundocheckout":
                    return !SavingAction.HasForceUndoCheckOutRight(generic);
                case "publish":
                    return (generic.VersioningMode <= VersioningType.None || !SavingAction.HasPublish(generic));
                case "approve":
                case "reject":
                    return !generic.Approvable;
                default:
                    return false;
            }
        }

        // ======================================================================== Action type handling

        private static Dictionary<string, Type> _actionCache;
        private static readonly object ActionCacheLock = new object();

        private static ActionBase ResolveActionType(string name)
        {
            if (_actionCache == null)
                lock (ActionCacheLock)
                    if (_actionCache == null)
                        _actionCache = TypeResolver.GetTypesByBaseType(typeof(ActionBase))
                            .ToDictionary(t => t.Name, t => t);

            if (!_actionCache.TryGetValue(name, out Type actionType))
                return null;

            return (ActionBase)Activator.CreateInstance(actionType);
        }
    }
}
