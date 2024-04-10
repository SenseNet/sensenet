using System.Collections.Generic;
using System.Linq;
using SenseNet.ApplicationModel;

namespace SenseNet.OData.Operations
{
    public class HelpController : ODataController
    {
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators, N.R.Developers)]
        public IEnumerable<object> GetOperations()
        {
            string FormatArray(string[] array) => array.Length == 0 ? string.Empty : string.Join(", ", array);

            return OperationCenter.Operations.Values.SelectMany(x => x)
                .OrderBy(op => op.Name).Select(op => new
                {
                    Controller = op.ControllerName ?? string.Empty,
                    Signature = op.ToString().Replace("<", "&lt;").Replace(">", "&gt;"),
                    Kind = op.CausesStateChange ? "Action" : "Function",
                    ContentTypes = FormatArray(op.ContentTypes) ?? "All",
                    Roles = FormatArray(op.Roles.Select(r => r
                        .Replace("/Root/IMS/BuiltIn/Portal/", "")
                        .Replace("/Root/IMS/Public/", "Public/")).ToArray()),
                    Permissions = FormatArray(op.Permissions),
                    Policies = FormatArray(op.Policies),
                    Scenarios = FormatArray(op.Scenarios),
                });
        }
    }
}
