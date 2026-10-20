using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SNCR = SenseNet.ContentRepository;

namespace SenseNet.Services.Core.Security;

/// <summary>
/// Defines roles, which are paths of sensenet Group type content.
/// The request will be forbidden if the user is not a member of any role.
/// </summary>
public class SnRolesAttribute : ActionFilterAttribute
{
    public string[] RolePaths { get; set; }

    public SnRolesAttribute(params string[] rolePaths)
    {
        RolePaths = rolePaths;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!CheckByRoles(RolePaths))
            context.Result = new ForbidResult();
        base.OnActionExecuting(context);
    }

    public virtual bool CheckByRoles(string[] expectedRoles)
    {
        var userId = SNCR.User.Current.Id;
        if (SNCR.User.Current.Id == Identifiers.SystemUserId)
            return true;
        if (userId == Identifiers.VisitorUserId && expectedRoles.Contains("/Root/IMS/BuiltIn/Portal/Visitor", StringComparer.InvariantCultureIgnoreCase))
            return true;
        if (expectedRoles.Contains("All"))
            return true;

        var actualRoles = NodeHead.Get(Providers.Instance.SecurityHandler.GetGroups()).Select(y => y.Path).ToArray();
        return actualRoles.Intersect(expectedRoles, StringComparer.InvariantCultureIgnoreCase).Any();
    }

}
