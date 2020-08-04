using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services.Core
{
    public interface IMembershipExtender
    {
        MembershipExtension GetExtension(IUser user, HttpContext httpContext);
    }

    //UNDONE: Delete this class after demonstration.
    public class TestMembershipExtenderAdmin : IMembershipExtender
    {
        public MembershipExtension GetExtension(IUser user, HttpContext httpContext)
        {
            return new MembershipExtension(new[] { Group.Administrators });
        }
    }
    //UNDONE: Delete this class after demonstration.
    public class TestMembershipExtenderOperator : IMembershipExtender
    {
        public MembershipExtension GetExtension(IUser user, HttpContext httpContext)
        {
            return new MembershipExtension(new[] { Group.Operators });
        }
    }

}
