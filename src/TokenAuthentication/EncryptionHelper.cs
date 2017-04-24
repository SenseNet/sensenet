using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace SenseNet.TokenAuthentication
{
    public static class EncryptionHelper
    {

        public static EncryptionKey CreateKey(string encription, int length = 64, int keylength = 2048)
        {
            if (Constants.RsaAlgorithms.Keys.Contains(encription) || Constants.RsaAlgorithms.Values.Contains(encription))
            {
                return new EncryptionKey(CreateRsaKey(keylength));
            }
            return new EncryptionKey(CreateSymmetricKey(length));
        }

        private static SymmetricSecurityKey CreateSymmetricKey(int length)
        {
            var secret = new byte[length];
            using (var rngService = new RNGCryptoServiceProvider())
            {
                rngService.GetBytes(secret);
            }
            return new SymmetricSecurityKey(secret);
        }

        public static EncryptionKey CreateSymmetricKey(string secret)
        {
            return new EncryptionKey(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)));
        }

        private static RsaSecurityKey CreateRsaKey(int keyLength)
        {
            using (var rsaService = new RSACryptoServiceProvider(keyLength, new CspParameters { Flags = CspProviderFlags.CreateEphemeralKey }))
            {
                var parameters = rsaService.ExportParameters(true);
                return new RsaSecurityKey(parameters);
            }
        }

    }
}