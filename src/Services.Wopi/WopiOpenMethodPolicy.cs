using System;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData;

namespace SenseNet.Services.Wopi
{
    internal abstract class WopiOpenMethodPolicy : IOperationMethodPolicy
    {
        public abstract string WopiActionType { get; }

        public abstract string Name { get; }

        public virtual OperationMethodVisibility GetMethodVisibility(IUser user, OperationCallingContext context)
        {
            var officeOnlineUrl = Settings.GetValue("OfficeOnline", "OfficeOnlineUrl", context.Content.Path, string.Empty);
            var (forbidden, visible) = InitializeInternal(context.Content, WopiActionType, officeOnlineUrl);

            if (visible.HasValue && !visible.Value)
                return OperationMethodVisibility.Invisible;
            if (forbidden.HasValue && forbidden.Value)
                return OperationMethodVisibility.Disabled;

            return OperationMethodVisibility.Enabled;
        }

        internal static (bool? forbidden, bool? visible) InitializeInternal(Content context, string actionType, string officeOnlineUrl)
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

    internal class WopiOpenViewMethodPolicy : WopiOpenMethodPolicy
    {
        public override string WopiActionType { get; } = "view";
        public override string Name { get; } = "WopiOpenView";
    }
    internal class WopiOpenEditMethodPolicy : WopiOpenMethodPolicy
    {
        public override string WopiActionType { get; } = "edit";
        public override string Name { get; } = "WopiOpenEdit";
    }
}
