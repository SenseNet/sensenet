using System;
using System.Collections.Generic;
using SenseNet.ContentRepository;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
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

        internal static ActionBase CreateAction(string actionType, Content context, string backUri, object parameters,
            Func<string, Content, object, ActionBase> getDefaultAction = null, object state = null)
        {
            return CreateAction(actionType, null, context, backUri, parameters, getDefaultAction, state);
        }

        internal static ActionBase CreateAction(string actionType, Application application, Content context, string backUri, object parameters,
            Func<string, Content, object, ActionBase> getDefaultAction = null, object state = null)
        {
            var actionName = application != null ? application.Name : actionType;

            // check versioning action validity
            if (!SavingAction.IsValidVersioningAction(context?.ContentHandler, actionName))
                return null;
            
            if (string.IsNullOrEmpty(actionType))
                actionType = Actions.DefaultActionType;

            var action = ResolveActionType(actionType);
            if (action == null)
            {
                if (getDefaultAction != null)
                    action = getDefaultAction(actionType, context, state);
                if (action == null)
                {
                    SnTrace.System.WriteError($"ActionFactory: Unknown action {actionType} for content {context?.Path}");
                    throw new InvalidContentActionException(InvalidContentActionReason.UnknownAction, context.Path,
                        null, actionType);
                }
            }


            action.Initialize(context, backUri, application, parameters);          

            return action.Visible ? action : null;
        }
        
        // ======================================================================== Action type handling

        private static Dictionary<string, Type> _actionCache;
        private static readonly object ActionCacheLock = new object();

        private static ActionBase ResolveActionType(string name)
        {
            if (_actionCache == null)
            {
                lock (ActionCacheLock)
                {
                    if (_actionCache == null)
                    {
                        var actionCache = new Dictionary<string, Type>();
                        foreach (var t in TypeResolver.GetTypesByBaseType(typeof(ActionBase)))
                            actionCache[t.Name] = t;
                        _actionCache = actionCache;
                    }
                }
            }

            if (!_actionCache.TryGetValue(name, out Type actionType))
                return null;

            return (ActionBase)Activator.CreateInstance(actionType);
        }
    }
}
