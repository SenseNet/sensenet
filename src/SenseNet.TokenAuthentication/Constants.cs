using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace SenseNet.TokenAuthentication
{
    public static class Constants
    {
        public static readonly IDictionary<string, string> EncryptionAlgorithms = new Dictionary<string, string>
        {
            {"HS256", SecurityAlgorithms.HmacSha256Signature}
            , {"HS512", SecurityAlgorithms.HmacSha512Signature }
            //, {"HS1", System.IdentityModel.Tokens.SecurityAlgorithms.HmacSha1Signature }
            , {"RS256", SecurityAlgorithms.RsaSha256Signature }
            , {"RS512", SecurityAlgorithms.RsaSha512Signature }
            //, {"RS1", System.IdentityModel.Tokens.SecurityAlgorithms.RsaSha1Signature }
        };

        public static readonly IDictionary<string, string> RsaAlgorithms = new Dictionary<string, string>
        {
            {"RS256", SecurityAlgorithms.RsaSha256Signature }
            , {"RS512", SecurityAlgorithms.RsaSha512Signature }
        };
    }
}