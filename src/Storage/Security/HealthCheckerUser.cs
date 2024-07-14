using System;
using System.Collections.Generic;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Storage.Security
{
    public sealed class HealthCheckerUser : IUser
    {
        public static readonly HealthCheckerUser Instance = new HealthCheckerUser();

        public int Id => -3;
        public IEnumerable<int> GetDynamicGroups(int entityId) => Array.Empty<int>();

        public string Path =>
            throw new InvalidOperationException("You cannot get the Path property of the HealthChecker user.");

        public bool IsInGroup(int securityGroupId) =>
            throw new InvalidOperationException("The HealthChecker user is not a member of any group.");

        public string AuthenticationType => "Portal";
        public bool IsAuthenticated => true;
        public string Name => "HealthChecker";

        public bool Enabled
        {
            get => true;
            set => throw new InvalidOperationException("You cannot set a property of the HealthChecker user.");
        }

        public string Domain => IdentityManagement.BuiltInDomainName;

        public string Email
        {
            get => throw new InvalidOperationException("You cannot get the Email property of the HealthChecker user.");
            set => throw new InvalidOperationException("You cannot set a property of the HealthChecker user.");
        }

        public string FullName
        {
            get { return "Health Checker"; }
            set { throw new InvalidOperationException("You cannot set a property of the HealthChecker user."); }
        }

        public string Password
        {
            get => throw new InvalidOperationException("You cannot get the Password property of the HealthChecker user.");
            set => throw new InvalidOperationException("You cannot set a property of the HealthChecker user.");
        }

        public string PasswordHash
        {
            get => throw new InvalidOperationException(
                "You cannot get the PasswordHash property of the HealthChecker user.");
            set => throw new InvalidOperationException("You cannot set a property of the HealthChecker user.");
        }

        public string Username
        {
            get => "HealthChecker";
            set => throw new InvalidOperationException("You cannot set a property of the HealthChecker user.");
        }

        public bool IsOperator => true;

        public bool IsInGroup(IGroup group) =>
            throw new InvalidOperationException("The HealthChecker user is not a member of any group.");

        public bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit) =>
            throw new InvalidOperationException("The HealthChecker user is not a member of any organizational unit.");

        public bool IsInContainer(ISecurityContainer container) =>
            throw new InvalidOperationException(
                "The HealthChecker user is not a member of any container (group or organizational unit).");

        public DateTime LastLoggedOut { get; set; }

        public MembershipExtension MembershipExtension { get => null; set { } }
    }
}
