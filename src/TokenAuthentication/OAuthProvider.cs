using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.IdentityModel.Tokens;
using SenseNet.TokenAuthentication;

namespace SenseNet.Services.Virtualization
{
    public class OAuthProvider
    {
        public ClaimsPrincipal VerifyToken(string token)
        {
            SecurityToken st;
            var th = new JwsSecurityTokenHandler();

            return th.ValidateToken(token, new TokenValidationParameters(), out st);
        }

        //public User LoadOrCreateUser(ClaimsPrincipal principal)
        //{
            
        //}
    }
}
