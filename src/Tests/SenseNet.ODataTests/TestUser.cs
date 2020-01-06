using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ODataTests
{
    public class TestUser : IUser
    {
        public TestUser(string userName, int id)
        {
            Id = id;
            Username = userName;
            Name = userName;
        }
        public int Id { get; }
        public IEnumerable<int> GetDynamicGroups(int entityId)
        {
            return new int[0];
        }

        public string Path { get; }
        public bool IsInGroup(int securityGroupId)
        {
            throw new NotImplementedException();
        }

        public string AuthenticationType { get; }
        public bool IsAuthenticated { get; }
        public string Name { get; }
        public bool Enabled { get; set; }
        public string Domain { get; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public string PasswordHash { get; set; }
        public string Username { get; }
        public bool IsInGroup(IGroup @group)
        {
            if (group.Id == Identifiers.EveryoneGroupId)
                return true;
            return false;
        }

        public bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit)
        {
            throw new NotImplementedException();
        }

        public bool IsInContainer(ISecurityContainer container)
        {
            throw new NotImplementedException();
        }

        public DateTime LastLoggedOut { get; set; }
        public MembershipExtension MembershipExtension { get; set; }
    }
}
