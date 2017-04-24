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
