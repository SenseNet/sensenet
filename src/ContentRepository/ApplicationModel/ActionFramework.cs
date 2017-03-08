using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ApplicationModel
{
    public class ActionFramework
    {
        internal class ActionEnumerator : IEnumerator<ActionBase>
        {
            private List<Application> apps;
            private Content context;
            private string backUrl;
            private bool firstIsResolved;

            private ActionBase currentAction;
            private IEnumerator<Application> appEnumerator;

            public ActionEnumerator(List<Application> apps, Content context, string backUrl)
            {
                this.apps = apps;
                this.context = context;
                this.backUrl = backUrl;
            }

            public ActionBase Current { get { return currentAction; } }
            object System.Collections.IEnumerator.Current { get { return this.Current; } }
            public void Dispose() { }

            public void Reset()
            {
                currentAction = null;
                appEnumerator = null;
                firstIsResolved = false;
            }
            public bool MoveNext()
            {
                if (!firstIsResolved)
                {
                    Application browseApp = null;
                    foreach (var app in apps)
                    {
                        if (app.Name != "Browse")
                            continue;
                        browseApp = app;
                        break;
                    }
                    firstIsResolved = true;
                    if (browseApp != null)
                    {
                        currentAction = CreateActionWithPermissions(browseApp, context, backUrl, null);
                        return true;
                    }
                }

                if (appEnumerator == null)
                    appEnumerator = apps.GetEnumerator();

                ActionBase act = null;
                while (act == null)
                {
                    if (!appEnumerator.MoveNext())
                        return false;
                    if (appEnumerator.Current.Name != "Browse")
                        act = CreateActionWithPermissions(appEnumerator.Current, context, backUrl, null);
                }
                currentAction = act;
                return true;
            }
        }

        internal class ActionList : IEnumerable<ActionBase>
        {
            private List<Application> apps;
            private Content context;
            private string backUrl;

            public ActionList(List<Application> apps, Content context, string backUrl)
            {
                this.apps = apps;
                this.context = context;
                this.backUrl = backUrl;
            }
            public IEnumerator<ActionBase> GetEnumerator()
            {
                return new ActionEnumerator(apps, context, backUrl);
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static ActionBase GetAction(string name, Content context, object parameters)
        {
            if (context == null)
                return null;

            var backUrl = HttpContext.Current != null ? HttpContext.Current.Request.RawUrl : null;

            return GetAction(name, context, backUrl, parameters);
        }

        public static ActionBase GetAction(string name, Content context, string backUri, object parameters)
        {
            if (context == null)
                return null;

            bool existingApplication;
            var app = ApplicationStorage.Instance.GetApplication(name, context, out existingApplication, GetDevice());

            // if app is null, than create action in memory only if this is _not_ an existing application
            // (existing app can be null because of denied access or cleared/disabled status)
            // (we create Service and ClientAction types in memory this way - they do not exist in the tree)
            var action = app != null ? 
                CreateActionWithPermissions(app, context, backUri, parameters) :
                (existingApplication ? null : ActionFactory.CreateAction(name, context, backUri, parameters));

            return action;
        }

        public static string GetActionUrl(string nodePath, string actionName)
        {
            var backUrl = HttpContext.Current != null ? HttpUtility.UrlEncode(HttpContext.Current.Request.RawUrl) : null;

            return GetActionUrl(nodePath, actionName, backUrl);
        }

        public static string GetActionUrl(string nodePath, string actionName, string back)
        {
            if (string.IsNullOrEmpty(nodePath) || string.IsNullOrEmpty(actionName))
                return string.Empty;

            Content content = null;

            try
            {
                content = Content.Load(nodePath);
            }
            catch (SenseNetSecurityException)
            {
                // not enough permissions, return empty string
            }

            if (content == null)
                return string.Empty;

            var act = GetAction(actionName, content, back, null);
            
            return act == null ? string.Empty : act.Uri;
        }

        public static IEnumerable<ActionBase> GetActions(Content context)
        {
            return GetActions(context, null, null);
        }

        public static IEnumerable<ActionBase> GetActions(Content context, string scenario, string scenarioParameters)
        {
            var backUrl = HttpContext.Current != null ? HttpUtility.UrlEncode(HttpContext.Current.Request.RawUrl) : null;

            return GetActions(context, scenario, scenarioParameters, backUrl);
        }

        public static IEnumerable<ActionBase> GetActions(Content context, string scenario, string scenarioParameters, string backUri)
        {
            if (!string.IsNullOrEmpty(scenario))
            {
                // if the scenario name is given, try to load actions in that scenario
                var sc = ScenarioManager.GetScenario(scenario, scenarioParameters);
                if (sc != null)
                    return sc.GetActions(context, backUri);
            }

            return GetActionsFromContentRepository(context, scenario, backUri);
        }

        public static IEnumerable<ActionBase> GetActionsFromContentRepository(Content context, string scenario, string backUri)
        {
            var apps = ApplicationStorage.Instance.GetApplications(scenario, context, GetDevice());
            var actions = from app in apps
                          select CreateActionWithPermissions(app, context, backUri, null);

            // return only relevant actions in a list
            return (from action in actions
                    where action != null
                    select action).ToList();
        }
        
        internal static IEnumerable<ActionBase> GetActionsForContentNavigator(Content context)
        {
            var backUrl = HttpContext.Current != null ? HttpUtility.UrlEncode(HttpContext.Current.Request.RawUrl) : null;
            var apps = ApplicationStorage.Instance.GetApplications(null, context, GetDevice());
            var actionList = new ActionList(apps, context, backUrl);
            return actionList;
        }

        private static ActionBase CreateActionWithPermissions(Application app, Content context, string backUri, object parameters)
        {
            if (app == null)
                return null;

            var action = app.CreateAction(context, backUri, parameters);
            if (action == null)
                return null;

            CheckRequiredPermissions(action, context);

            return action;
        }

        public static void CheckRequiredPermissions(ActionBase action, Content context)
        {
            if (action == null || context == null)
                return;

            var app = action.GetApplication();
            if (app == null)
                return;

            if (!HasRequiredPermissions(app, NodeHead.Get(context.Id)))
                action.Forbidden = true;
        }

        public static bool HasRequiredPermissions(Application app, NodeHead contextHead)
        {
            if (app == null)
                return true;

            var perms = GetRequiredPermissions(app);
            return perms.All(permType => SecurityHandler.HasPermission(contextHead, permType) &&
                (!app.DeepPermissionCheck || SecurityHandler.HasSubTreePermission(contextHead, permType)));
        }

        public static IEnumerable<PermissionType> GetRequiredPermissions(Application app)
        {
            IEnumerable<PermissionType> permList = null;
            if (app != null)
                permList = app.RequiredPermissions;
            return permList ?? new PermissionType[0];
        }


        public static Dictionary<string, object> ParseParameters(string parameters)
        {
            var dict = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(parameters))
                return dict;

            var prms = parameters.Split(new[] { ';', ',', '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var prm in prms)
            {
                var p = prm.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length > 1)
                {
                    dict.Add(p[0], p[1]);
                }
            }

            return dict;
        }

        private static string GetDevice()
        {
            return HttpContext.Current != null ? (string)HttpContext.Current.Items[ApplicationStorage.DEVICEPARAMNAME] : null;
        }

    }
}
