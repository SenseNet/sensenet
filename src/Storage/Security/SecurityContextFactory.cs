using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Security
{
    internal interface  ISecurityContextFactory
    {
        SnSecurityContext Create(IUser user);
    }
    internal class StaticSecurityContextFactory : ISecurityContextFactory
    {
        private readonly SecuritySystem _securitySystem;

        public StaticSecurityContextFactory(SecuritySystem securitySystem)
        {
            _securitySystem = securitySystem;
        }

        public SnSecurityContext Create(IUser user)
        {
            return new SnSecurityContext(user, _securitySystem);
        }
    }
    internal class DynamicSecurityContextFactory : ISecurityContextFactory
    {
        private readonly SecuritySystem _securitySystem;

        public DynamicSecurityContextFactory(SecuritySystem securitySystem)
        {
            _securitySystem = securitySystem;
        }

        public SnSecurityContext Create(IUser user)
        {
            return new SnSecurityContext(user, _securitySystem);
        }
    }
}
