using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData.Operations;
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

            if (_actionCache.TryGetValue(name, out Type actionType))
                return (ActionBase)Activator.CreateInstance(actionType);

            return GetMethodBasedAction(name);
        }

        private static Dictionary<string, MethodBasedOperation> _methodBasedOperationCache;
        private static ActionBase GetMethodBasedAction(string name)
        {
            if (_methodBasedOperationCache == null)
                _methodBasedOperationCache = GetAvailableMethodBasedOperations();
            if (_methodBasedOperationCache.TryGetValue(name, out var action))
                return action;
            return null;
        }

        private static Dictionary<string, MethodBasedOperation> GetAvailableMethodBasedOperations()
        {
            var result = new Dictionary<string, MethodBasedOperation>();

            var decoratedMethods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods())
                .Where(m => m.IsStatic && m.GetCustomAttributes(true).Length > 0)
                .ToArray();

            foreach (var method in decoratedMethods)
            {
                var attributes = method.GetCustomAttributes(true);
                var actionAttr = attributes.FirstOrDefault(a => a is ODataAction);
                var functionAttr = attributes.FirstOrDefault(a => a is ODataFunction);

                var operationAttr = (ODataOperation)(actionAttr ?? functionAttr);
                if (null == operationAttr)
                    continue;

                if (result.ContainsKey(method.Name))
                {
                    int q = 1;
                }
                else
                {
                    var methodParams = method.GetParameters();
                    var parameters = (methodParams.Length > 0 ?  methodParams.Skip(1) : methodParams)
                            .Select(p => new ActionParameter(p.Name, p.ParameterType, p.IsOptional))
                            .ToArray();

                    result.Add(method.Name, new MethodBasedOperation(
                        method,
                        actionAttr != null,
                        parameters,
                        operationAttr.Description));
                }
            }

            return result;
        }
    }
}
