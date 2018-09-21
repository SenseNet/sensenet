using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Authentication.Tests
{
    internal class ServicesTestUser : IUser
    {
        public static ServicesTestUser Create(string token, string userName)
        {
            return new ServicesTestUser
            {
                Username = userName,
                Name = userName
            };
        }

        public int Id { get; }
        public string Path { get; }
        public bool IsInGroup(int securityGroupId)
        {
            throw new NotImplementedException();
        }

        public string Name { get; private set; }
        public string AuthenticationType { get; }
        public bool IsAuthenticated { get; }
        public IEnumerable<int> GetDynamicGroups(int entityId)
        {
            throw new NotImplementedException();
        }

        public bool Enabled { get; set; }
        public string Domain { get; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public string PasswordHash { get; set; }
        public string Username { get; set; }
        public bool IsInGroup(IGroup @group)
        {
            throw new NotImplementedException();
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