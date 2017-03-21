using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace SenseNet.TokenAuthentication
{
    public class EncryptionKey : ISecurityKey
    {
        public EncryptionKey(SecurityKey securityKey)
        {
            SecurityKey = securityKey;
        }

        public SecurityKey SecurityKey { get; }
    }
}
