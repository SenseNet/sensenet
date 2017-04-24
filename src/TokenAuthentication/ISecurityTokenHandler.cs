using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace SenseNet.TokenAuthentication
{
    public interface ISecurityTokenHandler
    {
        IDictionary<string, string> OutboundClaimTypeMap { get; set; }
        IDictionary<string, string> InboundClaimTypeMap { get; set; }
        int TokenLifetimeInMinutes { get; set; }
        bool CanWriteToken { get; }
        string WriteToken(SecurityToken token);
        ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParameters, out SecurityToken validatedToken);
    }
}