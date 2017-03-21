using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace SenseNet.TokenAuthentication
{
    public class JwsSecurityTokenHandler: JwtSecurityTokenHandler, ISecurityTokenHandler
    {
        private readonly IDictionary<string, string> _claimTypes = new Dictionary<string, string>
        {
            {"name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" }
            , {"role", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" }
        };

        private static readonly int _lifeTimeInMinutes = 20;

        public JwsSecurityTokenHandler()
        {
            OutboundClaimTypeMap = _claimTypes;
            InboundClaimTypeMap = _claimTypes;
            TokenLifetimeInMinutes = _lifeTimeInMinutes;

        }
    }
}