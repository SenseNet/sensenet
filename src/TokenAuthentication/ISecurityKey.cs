using Microsoft.IdentityModel.Tokens;

namespace SenseNet.TokenAuthentication
{
    public interface ISecurityKey
    {
        SecurityKey SecurityKey { get; }
    }
}