using System;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services.Wopi
{
    internal abstract class WopiOpenAction : ActionBase
    {
        public override string Uri { get; } = string.Empty;
        public abstract string WopiActionType { get; }

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);
            
            var officeOnlineUrl = Settings.GetValue("OfficeOnline", "OfficeOnlineUrl", context.Path, string.Empty);
            var initValues = InitializeInternal(context, WopiActionType, officeOnlineUrl);

            if (initValues.Forbidden.HasValue && initValues.Forbidden.Value)
                Forbidden = true;
            if (initValues.Visible.HasValue && !initValues.Visible.Value)
                Visible = false;
        }

        internal static (bool? Forbidden, bool? Visible) InitializeInternal(Content context, string actionType, string officeOnlineUrl)
        {
            if (!(context.ContentHandler is File))
                return (true, false);

            if (string.IsNullOrEmpty(officeOnlineUrl))
                return (true, false);

            var wd = WopiDiscovery.GetInstance(officeOnlineUrl);
            if (wd == null || !wd.Zones.Any())
                return (true, false);

            var extension = System.IO.Path.GetExtension(context.Name)?.Trim('.') ?? string.Empty;
            if (string.IsNullOrEmpty(extension))
                return (true, false);

            var actions = wd.Zones.SelectMany(z => z.Apps).SelectMany(app => app.Actions);

            if (!actions.Any(act => 
                string.Equals(act.Extension, extension, StringComparison.InvariantCultureIgnoreCase) &&
                string.Equals(act.Name, actionType, StringComparison.InvariantCultureIgnoreCase)))
                return (true, false);

            return (null, null);
        }
    }

    internal class WopiOpenViewAction : WopiOpenAction
    {
        public override string WopiActionType { get; } = "view";

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            // check permissions only if the action is not already forbidden
            if (!Forbidden && !context.Security.HasPermission(PermissionType.Open))
                Forbidden = true;
        }
    }
    internal class WopiOpenEditAction : WopiOpenAction
    {
        public override string WopiActionType { get; } = "edit";

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            // check permissions only if the action is not already forbidden
            if (!Forbidden && !context.Security.HasPermission(PermissionType.Save))
                Forbidden = true;
        }
    }
}
