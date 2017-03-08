using System;
using Microsoft.Practices.Unity;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ApplicationModel
{
    internal class ActionFactory
    {
        internal static ActionBase CreateAction(Type actionType, Application application, Content context, string backUri, object parameters)
        {
            var act = TypeResolver.CreateInstance(actionType.FullName) as ActionBase;
            if (act != null)
                act.Initialize(context, backUri, application, parameters);

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

            var generic = context.ContentHandler as GenericContent;
            if (generic == null)
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

        private static UnityContainer _actionContainer;
        private static object _actionContainerLock = new object();

        /// <summary>
        /// Contains all the action types and provides a way to instantiate them.
        /// </summary>
        private static UnityContainer ActionContainer
        {
            get
            {
                if (_actionContainer == null)
                {
                    lock (_actionContainerLock)
                    {
                        if (_actionContainer == null)
                        {
                            _actionContainer = GetUnityContainerForActions();
                        }
                    }
                }
                return _actionContainer;
            }
        }

        private static ActionBase ResolveActionType(string name)
        {
            try
            {
                return ActionContainer.Resolve<ActionBase>(name);
            }
            catch (ResolutionFailedException)
            {
                return null;
            }
        }

        private static UnityContainer GetUnityContainerForActions()
        {
            var container = new UnityContainer();

            var actionBaseType = typeof(ActionBase);
            var actionTypes = TypeResolver.GetTypesByBaseType(actionBaseType);

            foreach (var actionType in actionTypes)
            {
                try
                {
                    // Register all action types with the base type ActionBase.
                    // E.g. register 'SenseNet.ApplicationModel.UploadAction' type with the name 'UploadAction' to
                    // be able to resolve an instance of the type by its simple name.
                    // Since action objects are NOT stateless, they need to be instantiated every time, we 
                    // cannot hold a singleton object for actions.
                    container.RegisterType(actionBaseType, actionType, actionType.Name);
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, "Error during action type registration. Type name: " + actionType.FullName);
                }
            }

            return container;
        }
    }
}
