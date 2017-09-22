using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Communication.Messaging;
using System.Diagnostics;
using SenseNet.Search;
using SafeQueries = SenseNet.ContentRepository.SafeQueries;

namespace SenseNet.ApplicationModel
{
    public sealed class ApplicationStorage
    {
        [DebuggerDisplay("{ToString()}")]
        private class AppPath
        {
            private static readonly char[] _pathSeparatorChars = RepositoryPath.PathSeparatorChars;

            public int[] Indices;
            public AppNodeType[] AppNodeTypes;
            public int TypeIndex;
            public int ActionIndex;
            public bool Truncated;

            internal int GetNextIndex(int currentIndex)
            {
                var i = currentIndex + 1;
                if (PathSegments[this.Indices[i]] == AppFolderName)
                    i++;
                return (i >= this.Indices.Length) ? -1 : i;
            }

            public static AppPath MakePath(string path)
            {
                var words = path.Split(_pathSeparatorChars, StringSplitOptions.RemoveEmptyEntries).ToList();
                var result = new List<int>();
                var typeIndex = -1;
                var actionIndex = -1;
                var isType = false;
                var i = 0;
                while (i < words.Count)
                {
                    var word = words[i];
                    if (isType)
                    {
                        // remove current word and insert words of type path
                        if (word != "This")
                        {
                            var ntype = ActiveSchema.NodeTypes[word];
                            if (ntype == null)
                                return null;
                            var typeNames = ntype.NodeTypePath.Split('/');
                            words.RemoveAt(i);
                            words.InsertRange(i, typeNames);
                            word = words[i];
                            actionIndex = i + typeNames.Length;
                        }
                        else
                        {
                            actionIndex = i + 1;
                        }
                        isType = false;
                    }
                    word = word.ToLowerInvariant();
                    if (word == AppFolderName)
                    {
                        typeIndex = i;
                        isType = true;
                        words.RemoveAt(i);
                        continue;
                    }

                    var index = PathSegments.IndexOf(word);
                    if (index < 0)
                    {
                        index = PathSegments.Count;
                        PathSegments.Add(word);
                    }
                    result.Add(index);
                    i++;
                }
                var appPath = new AppPath { Indices = result.ToArray(), TypeIndex = typeIndex, ActionIndex = actionIndex };
                appPath.Initialize();
                return appPath;
            }
            public static AppPath MakePath(NodeHead head, string actionName, string[] device)
            {
                var actionNameIndex = -1;
                if (actionName != null)
                {
                    actionName = actionName.ToLowerInvariant();
                    actionNameIndex = PathSegments.IndexOf(actionName);
                    if (actionNameIndex < 0)
                        return null;
                }

                var words = head.Path.Split(_pathSeparatorChars, StringSplitOptions.RemoveEmptyEntries).ToList();
                for (int i = 0; i < words.Count; i++)
                    words[i] = words[i].ToLowerInvariant();

                var result = new List<int>();
                var typeIndex = -1;
                var actionIndex = -1;
                var isTruncated = false;
                foreach (var word in words)
                {
                    var index = PathSegments.IndexOf(word);
                    if (index < 0)
                    {
                        isTruncated = true;
                        break;
                    }
                    result.Add(index);
                }
                typeIndex = result.Count;

                var ntype = ActiveSchema.NodeTypes.GetItemById(head.NodeTypeId);
                var typeNames = ntype.NodeTypePath.Split('/');
                foreach (var typeName in typeNames)
                {
                    var index = PathSegments.IndexOf(typeName.ToLowerInvariant());
                    if (index < 0)
                        break;
                    result.Add(index);
                }

                actionIndex = result.Count;
                result.Add(actionNameIndex); // can be -1
                if (device != null)
                {
                    for (int i = device.Length - 1; i >= 0; i--)
                    {
                        var index = PathSegments.IndexOf(device[i]);
                        if (index < 0)
                            break;
                        result.Add(index);
                    }
                }

                var appPath = new AppPath { Indices = result.ToArray(), TypeIndex = typeIndex, ActionIndex = actionIndex, Truncated = isTruncated };
                appPath.Initialize();
                return appPath;
            }

            private void Initialize()
            {
                AppNodeTypes = new AppNodeType[Indices.Length];
                for (int i = 0; i < Indices.Length; i++)
                    AppNodeTypes[i] = GetNodeType(i);
            }
            private AppNodeType GetNodeType(int pathIndex)
            {
                if (pathIndex < TypeIndex)
                    return AppNodeType.Path;
                if (pathIndex < ActionIndex)
                    return AppNodeType.Type;
                if (pathIndex == ActionIndex)
                    return AppNodeType.Action;
                return AppNodeType.Device;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                for (int i = 0; i < Indices.Length; i++)
                    sb.Append('/').Append(Indices[i] < 0 ? "[null]" : PathSegments[Indices[i]]);
                if (Truncated)
                    sb.Append(", truncated");
                return sb.ToString();
            }
        }

        private enum AppNodeType { Path, Type, Action, Device }
        private List<Application> EmptyApplicationList = new List<Application>(0);

        [DebuggerDisplay("{ToString()}")]
        private class AppNode
        {
            public AppNode(int name, AppNodeType type, AppNode parent) : this(name, type, parent, null) { }
            public AppNode(int name, AppNodeType type, AppNode parent, Application app)
            {
                Name = name;
                AppNodeType = type;
                Application = app;
                Children = new List<AppNode>();
                Parent = parent;
                if (parent != null)
                {
                    Parent.Children.Add(this);
                    Level = Parent.Level + 1;
                }
                if (app != null)
                {
                    Disabled = app.Disabled;
                    var list = app.ScenarioList;
                    _scenarioList = list.Count > 0 ? list : null;
                }
            }

            public int Name;
            public AppNodeType AppNodeType;
            public AppNode Parent;
            public List<AppNode> Children;
            public Application Application { get; private set; }
            public int Level;

            public void AddChild(Application app)
            {
                // appPath is null if app is invalid (e.g. path: .../(apps)/browse
                var appPath = AppPath.MakePath(app.Path);
                if (appPath != null)
                    AddChild(app, appPath, 1);
            }
            private void AddChild(Application app, AppPath appPath, int pathIndex)
            {
                if (pathIndex < 0)
                    return;
                var name = appPath.Indices[pathIndex];
                var appNodeType = appPath.AppNodeTypes[pathIndex];
                if (pathIndex < appPath.Indices.Length - 1)
                {
                    AppNode child = null;
                    foreach (var pathChild in this.Children)
                    {
                        if (pathChild.Name == name && pathChild.AppNodeType == appNodeType)
                        {
                            child = (AppNode)pathChild;
                            break;
                        }
                    }
                    var nextIndex = appPath.GetNextIndex(pathIndex);
                    if (child == null)
                    {
                        child = new AppNode(name, appNodeType, this);
                    }
                    child.AddChild(app, appPath, nextIndex);
                }
                else
                {
                    var child = new AppNode(name, appNodeType, this, app);
                }
            }
            public string GetPathString()
            {
                var names = new List<string>();
                var node = this;
                while (node != null)
                {
                    names.Insert(0, PathSegments[node.Name]);
                    node = node.Parent;
                }
                return String.Join("/", names);
            }

            public override string ToString()
            {
                return String.Concat(AppNodeType, ": ", GetPathString());
            }

            public bool Disabled;
            private List<string> _scenarioList;
            public bool HasScenario(string scenario)
            {
                if (_scenarioList == null)
                    return false;
                return _scenarioList.Contains(scenario);
            }
        }

        // ----------------------------------------------------------------
        public static string DEVICEPARAMNAME = "SnDevice";

        private static int PathSegmentThisIndex = 2;
        private static List<string> PathSegments;
        private AppNode __rootAppNode;
        private AppNode RootAppNode
        {
            get
            {
                if (__rootAppNode == null)
                {
                    lock (LockObject)
                    {
                        if (__rootAppNode == null)
                        {
                            __rootAppNode = LoadApps(out _appNames, out _appList, out _scenarioNames);
                        }
                    }
                }

                return __rootAppNode;
            }
        }

        private static AppNode LoadApps(out List<string> appNames, out List<Application> appList, out List<string> scenarioNames)
        {
            using (new SystemAccount())
            {
                //var result = nq.Execute();
                var result = ContentQuery.Query(SafeQueries.TypeIs, QuerySettings.AdminSettings, typeof(Application).Name);
                appList = result.Nodes.Cast<Application>().ToList();
                appList.Sort((xa, ya) => xa.Path.CompareTo(ya.Path));
            }

            PathSegments = new List<string>();
            PathSegments.Add("root");
            PathSegments.Add("this");
            PathSegmentThisIndex = 1;
            var root = new AppNode(0, AppNodeType.Path, null);

            appNames = new List<string>();
            scenarioNames = new List<string>();

            foreach (var node in appList)
            {
                // store scenario names
                foreach (var scenario in node.ScenarioList)
                {
                    if (!scenarioNames.Contains(scenario))
                        scenarioNames.Add(scenario);
                }

                root.AddChild(node);
                var appName = node.AppName;

                // ------------PATCH START: set AppName property if null (compatibility reason));
                if (string.IsNullOrEmpty(appName))
                {
                    var originalUser = AccessProvider.Current.GetCurrentUser();

                    try
                    {
                        // We can save content only with the Administrator at this point,
                        // because there is a possibility that the original user is the
                        // STARTUP user that cannot be used to save any content.
                        AccessProvider.Current.SetCurrentUser(User.Administrator);

                        node.Save(SavingMode.KeepVersion);
                        appName = node.AppName;
                    }
                    finally
                    {
                        AccessProvider.Current.SetCurrentUser(originalUser);
                    }
                }
                // ------------PATCH END

                if (!string.IsNullOrEmpty(appName) && !appNames.Contains(appName))
                    appNames.Add(appName);
            }

            scenarioNames.Sort();
            appList.Sort(new ApplicationComparer());

            return root;
        }

        private List<Application> GetApplicationsInternal(string appName, NodeHead head, string scenarioName, string requestedDevice)
        {
            if (head == null || RootAppNode == null)
                return new List<Application>();

            var device = requestedDevice == null
                ? new string[0]
                : DeviceManager.GetDeviceChain(requestedDevice.ToLowerInvariant()) ?? new string[0];

            var appPath = AppPath.MakePath(head, appName, device);
            if (appPath == null)
                return EmptyApplicationList;

            var lastNode = SearchLastNode(appPath);

            if (appName != null)
                return GetApplicationsByAppName(lastNode, appPath);
            return GetApplicationsByScenario(lastNode, appPath, scenarioName, device);
        }
        private AppNode SearchLastNode(AppPath appPath)
        {
            return SearchLastNode(RootAppNode, appPath, 1, true);
        }
        private AppNode SearchLastNode(AppNode appNode, AppPath appPath, int pathIndex, bool thisEnabled)
        {
            if (appNode == null)
                return null;

            if (pathIndex >= appPath.Indices.Length)
                return appNode;

            if (!appPath.Truncated && pathIndex == appPath.TypeIndex && appNode.Level == pathIndex - 1 && thisEnabled)
            {
                foreach (var child in appNode.Children)
                {
                    if (child.Name == PathSegmentThisIndex)
                    {
                        if (appPath.Indices[appPath.ActionIndex] < 0)
                            return child;
                        var thisNode = child;
                        var last = SearchLastNode(thisNode, appPath, appPath.ActionIndex, thisEnabled);
                        if (last != null)
                            return last;
                    }
                }
            }

            var name = appPath.Indices[pathIndex];
            var appNodeType = appPath.AppNodeTypes[pathIndex];
            if (name < 0)
                return appNode;

            foreach (var child in appNode.Children)
                if (child.Name == name && child.AppNodeType == appNodeType)
                    return SearchLastNode(child, appPath, pathIndex + 1, thisEnabled);

            if (pathIndex < appPath.TypeIndex)
            {
                return SearchLastNode(appNode, appPath, appPath.TypeIndex, thisEnabled);
            }
            if (pathIndex < appPath.ActionIndex)
            {
                return SearchLastNode(appNode, appPath, appPath.ActionIndex, thisEnabled);
            }
            return appNode;
        }

        private List<Application> GetApplicationsByAppName(AppNode lastNode, AppPath appPath)
        {
            // resolve one application
            Application app = null;
            Application ovr = null;

            while (lastNode != null)
            {
                if (lastNode.AppNodeType == AppNodeType.Action || lastNode.AppNodeType == AppNodeType.Device)
                {
                    app = SearchAppInAction(lastNode, appPath, null);
                    if (app != null)
                    {
                        if (app.Clear)
                        {
                            app = null;
                            break;
                        }
                        if (app.IsOverride)
                        {
                            if (ovr != null)
                                app = Override(app, ovr);// app.Override = ovr;
                            ovr = app;
                            app = null;
                            lastNode = lastNode.Parent;
                        }
                    }
                    else
                    {
                        while (lastNode.AppNodeType != AppNodeType.Type)
                            lastNode = lastNode.Parent;
                    }
                }
                if (app == null)
                {
                    // parent typelevel or pathlevel
                    if (lastNode.Name == PathSegmentThisIndex)
                    {
                        lastNode = SearchLastNode(lastNode.Parent, appPath, appPath.TypeIndex, false);
                    }
                    else if (lastNode.AppNodeType == AppNodeType.Type)
                    {
                        lastNode = SearchLastNode(lastNode.Parent, appPath, appPath.ActionIndex, true);
                    }
                    else if (lastNode.AppNodeType == AppNodeType.Path)
                    {
                        lastNode = SearchLastNode(lastNode.Parent, appPath, appPath.TypeIndex, true);
                    }
                    else
                    {
                        throw new SnNotSupportedException("@@@@");
                    }
                }
                else
                {
                    break;
                }
            }
            if (app == null)
                return new List<Application>();
            if (ovr != null)
                app = Override(app, ovr); // app.Override = ovr;
            return new List<Application>(new[] { app });
        }
        private Application SearchAppInAction(AppNode appNodeInAction, AppPath appPath, string scenario)
        {
            var ovrList = new List<Application>();
            Application resultApp = null;

            var appNode = appNodeInAction;
            while (appNode.AppNodeType != AppNodeType.Type)
            {
                var app = appNode.Application;
                if (app != null)
                {
                    if (app.Clear)
                        return app;

                    if (String.IsNullOrEmpty(scenario) || appNode.HasScenario(scenario))
                    {
                        if (!appNode.Disabled && (app.Security.HasPermission(PermissionType.Open) || app.Security.HasPermission(PermissionType.RunApplication)))
                        {
                            if (app.IsOverride)
                            {
                                if (resultApp != null)
                                    app = Override(app, resultApp); // app.Override = resultApp;
                                resultApp = app;
                            }
                            else
                            {
                                resultApp = app;
                                break;
                            }
                        }
                    }
                }
                appNode = appNode.Parent;
            }
            return resultApp;
        }
        private Application Override(Application app, Application @override)
        {
            using (new SystemAccount())
            {
                var reloadedApp = Node.Load<Application>(app.Id);
                reloadedApp.Override = @override;
                return reloadedApp;
            }
        }

        private List<Application> GetApplicationsByScenario(AppNode lastNode, AppPath appPath, string scenarioName, string[] device)
        {
            // resolve all applications filtered by scenario
            var apps = new Dictionary<int, Application>();

            while (lastNode != null)
            {
                if (lastNode.AppNodeType == AppNodeType.Type)
                {
                    GetApplicationsInType(lastNode, appPath, scenarioName, device, apps);
                }

                // parent typelevel or pathlevel
                if (lastNode.Name == PathSegmentThisIndex)
                    lastNode = SearchLastNode(lastNode.Parent, appPath, appPath.TypeIndex, false);
                else if (lastNode.AppNodeType == AppNodeType.Type)
                    lastNode = SearchLastNode(lastNode.Parent, appPath, appPath.ActionIndex, true);
                else
                    lastNode = SearchLastNode(lastNode.Parent, appPath, appPath.TypeIndex, true);
            }
            var result = apps.Values.ToList();
            for (int i = result.Count - 1; i >= 0; i--)
                if (result[i].Clear || result[i].IsOverride)
                    result.RemoveAt(i);

            result.Sort(new ApplicationComparer());
            return result;
        }
        private void GetApplicationsInType(AppNode typeNode, AppPath appPath, string scenario, string[] device, Dictionary<int, Application> allApps)
        {
            foreach (var child in typeNode.Children)
            {
                if (child.AppNodeType == AppNodeType.Action)
                {
                    var search = true;
                    Application existingApp;
                    if (allApps.TryGetValue(child.Name, out existingApp))
                        if (existingApp.Clear || !existingApp.IsOverride)
                            search = false;

                    if (search)
                    {
                        // deepest device
                        var lastDeviceNode = SearchLastNode(child, appPath, appPath.ActionIndex + 1, true);
                        var app = SearchAppInAction(lastDeviceNode, appPath, scenario);
                        if (app != null)
                        {
                            if (existingApp == null)
                            {
                                allApps.Add(child.Name, app);
                            }
                            else
                            {
                                app = Override(app, existingApp);
                                allApps[child.Name] = app;
                            }
                        }
                    }
                }
            }
        }

        // ================================================================

        private const string AppFolderName = "(apps)";

        private static ApplicationStorage _instance;
        private static readonly object LockObject = new Object();

        private List<Application> _appList;
        private List<string> _appNames;
        private List<string> _scenarioNames;

        /// <summary>
        /// All of the scenario names in the system tha were set on an application
        /// </summary>
        public IEnumerable<string> ScenarioNames
        {
            get
            {
                // preload apps and scenarios if needed
                if (_scenarioNames == null)
                {
                    var root = RootAppNode;
                }

                return _scenarioNames;
            }
        }

        public static ApplicationStorage Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (LockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new ApplicationStorage();
                        }
                    }
                }

                return _instance;
            }
        }

        private ApplicationStorage()
        {
        }

        /*=================================================================================== Get Apps */

        // caller: Scenarios and tests
        [Obsolete("Use an override with device")]
        public Application GetApplication(string applicationName, Content context)
        {
            return GetApplication(applicationName, context, null);
        }
        public Application GetApplication(string applicationName, Content context, string device)
        {
            bool existingApplication;
            return GetApplication(applicationName, context, out existingApplication, device);
        }

        // caller: nobody
        [Obsolete("Use an override with device")]
        public Application GetApplication(string applicationName, NodeHead head)
        {
            return GetApplication(applicationName, head, null);
        }
        public Application GetApplication(string applicationName, NodeHead head, string device)
        {
            bool existingApplication;
            return GetApplication(applicationName, head, out existingApplication, device);
        }

        // caller: ActionFramework
        [Obsolete("Use an override with device")]
        public Application GetApplication(string applicationName, Content context, out bool existingApplication)
        {
            return GetApplication(applicationName, context, out existingApplication, null);
        }
        public Application GetApplication(string applicationName, Content context, out bool existingApplication, string device)
        {
            var app = GetApplicationsInternal(applicationName, context, null, device).FirstOrDefault();

            existingApplication = app != null || Exists(applicationName);

            return app;
        }

        // caller: nobody
        [Obsolete("Use an override with device")]
        public Application GetApplication(string applicationName, NodeHead head, out bool existingApplication)
        {
            return GetApplication(applicationName, head, out existingApplication, null);
        }
        public Application GetApplication(string applicationName, NodeHead head, out bool existingApplication, string device)
        {
            var app = GetApplicationsInternal(applicationName, head, null, device).FirstOrDefault();

            existingApplication = app != null || Exists(applicationName);

            return app;
        }

        // caller: ApplicationListPresenterPortlet
        [Obsolete("Use an override with device")]
        public List<Application> GetApplications(Content context)
        {
            return GetApplications(context, null);
        }
        public List<Application> GetApplications(Content context, string device)
        {
            return GetApplicationsInternal(null, context, null, device);
        }

        // caller: nobody
        [Obsolete("Use an override with device")]
        public List<Application> GetApplications(NodeHead head)
        {
            return GetApplications(head, null);
        }
        public List<Application> GetApplications(NodeHead head, string device)
        {
            return GetApplicationsInternal(null, head, null, device);
        }

        // caller: ActionFramework
        [Obsolete("Use an override with device")]
        public List<Application> GetApplications(string scenarioName, Content context)
        {
            return GetApplications(scenarioName, context, null);
        }
        public List<Application> GetApplications(string scenarioName, Content context, string device)
        {
            return GetApplicationsInternal(null, context, scenarioName, device);
        }

        // caller: nobody
        [Obsolete("Use an override with device")]
        public List<Application> GetApplications(string scenarioName, NodeHead head)
        {
            return GetApplications(scenarioName, head, null);
        }
        public List<Application> GetApplications(string scenarioName, NodeHead head, string device)
        {
            return GetApplicationsInternal(null, head, scenarioName, device);
        }

        private List<Application> GetApplicationsInternal(string appName, Content context, string scenarioName, string device)
        {
            return GetApplicationsInternal(appName, context == null ? null : NodeHead.Get(context.Path), scenarioName, device);
        }

        public bool Exists(string applicationName)
        {
            if (string.IsNullOrEmpty(applicationName) || _appNames == null)
                return false;

            applicationName = applicationName.ToLowerInvariant();
            return (_appNames.Count(a => a.ToLowerInvariant() == applicationName) > 0);
        }

        // =================================================================================== Distributed invalidate

        [Serializable]
        internal class ApplicationStorageInvalidateDistributedAction : DistributedAction
        {
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                ApplicationStorage.InvalidatePrivate();
            }
        }

        private static void DistributedInvalidate()
        {
            new ApplicationStorageInvalidateDistributedAction().Execute();
        }

        private static void InvalidatePrivate()
        {
            SnLog.WriteInformation("ApplicationStorage invalidate");
            _instance = null;
        }

        public static void Invalidate()
        {
            DistributedInvalidate();
        }

        public static bool InvalidateByNode(Node node)
        {
            return ApplicationStorage.Instance.InvalidateByNodeInternal(node);
        }

        public static bool InvalidateByPath(string path)
        {
            return ApplicationStorage.Instance.InvalidateByPathInternal(path);
        }

        private bool InvalidateByNodeInternal(Node node)
        {
            if (__rootAppNode == null || node == null)
                return false;

            if (node is Application)
            {
                Invalidate();
                return true;
            }

            return InvalidateByPathInternal(node.Path);
        }

        private bool InvalidateByPathInternal(string path)
        {
            if (__rootAppNode == null || string.IsNullOrEmpty(path))
                return false;

            if ((from app in _appList
                 where app.Path.StartsWith(path)
                 select app).Count() > 0)
            {
                Invalidate();
                return true;
            }

            return false;
        }

        // =================================================================================== Comparer class

        private class ApplicationComparer : IComparer<Application>
        {
            public int Compare(Application x, Application y)
            {
                if (x == null || y == null)
                    return 0;
                return x.Index.CompareTo(y.Index);
            }
        }
    }
}
