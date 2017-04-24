using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace SenseNet.TokenAuthentication
{
    public static class Constants
    {
        public static readonly IDictionary<string, string> EncryptionAlgorithms = new Dictionary<string, string>
        {
            {"HS256", SecurityAlgorithms.HmacSha256Signature},
            {"HS512", SecurityAlgorithms.HmacSha512Signature },
            {"RS256", SecurityAlgorithms.RsaSha256Signature },
            {"RS512", SecurityAlgorithms.RsaSha512Signature }
        };

        public static readonly IDictionary<string, string> RsaAlgorithms = new Dictionary<string, string>
        {
            {"RS256", SecurityAlgorithms.RsaSha256Signature },
            {"RS512", SecurityAlgorithms.RsaSha512Signature }
        };
    }
}