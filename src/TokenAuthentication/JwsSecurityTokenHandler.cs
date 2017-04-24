using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace SenseNet.TokenAuthentication
{
    public class JwsSecurityTokenHandler: ISecurityTokenHandler
    {
        private readonly IDictionary<string, string> _claimTypes = new Dictionary<string, string>
        {
            {"name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" }
            , {"role", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" }
        };

        private static readonly int _lifeTimeInMinutes = 20;

        private JwtSecurityTokenHandler _tokenHandler;
        public JwsSecurityTokenHandler()
        {
            _tokenHandler = new JwtSecurityTokenHandler();
            _tokenHandler.OutboundClaimTypeMap = _claimTypes;
            _tokenHandler.InboundClaimTypeMap = _claimTypes;
            _tokenHandler.TokenLifetimeInMinutes = _lifeTimeInMinutes;

        }

        public IDictionary<string, string> OutboundClaimTypeMap
        {
            get { return _tokenHandler.OutboundClaimTypeMap; }
            set { _tokenHandler.OutboundClaimTypeMap = value; }
        }

        public IDictionary<string, string> InboundClaimTypeMap
        {
            get { return _tokenHandler.InboundClaimTypeMap; }
            set { _tokenHandler.InboundClaimTypeMap = value; }
        }

        public int TokenLifetimeInMinutes
        {
            get { return _tokenHandler.TokenLifetimeInMinutes; }
            set { _tokenHandler.TokenLifetimeInMinutes = value; }
        }
        public bool CanWriteToken {
            get { return _tokenHandler.CanWriteToken; } 
        }
        public string WriteToken(SecurityToken token)
        {
            return _tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
        {
            return _tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
        }
    }
}