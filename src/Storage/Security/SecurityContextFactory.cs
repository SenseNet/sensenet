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
        public SnSecurityContext Create(IUser user)
        {
            return new SnSecurityContext(user);
        }
    }
    internal class DynamicSecurityContextFactory : ISecurityContextFactory
    {
        public SnSecurityContext Create(IUser user)
        {
            return new SnSecurityContext(user);
        }
    }
}
