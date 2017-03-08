using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Default PasswordHashProvider in the Sense/Net
    /// </summary>
    public class SenseNetPasswordHashProvider : PasswordHashProvider
    {
        protected override string Encode(string text, IPasswordSaltProvider saltProvider)
        {
            return BCrypt.HashPassword(text + GenerateSalt(saltProvider));
        }
        protected override bool Check(string text, string hash, IPasswordSaltProvider saltProvider)
        {
            return BCrypt.Verify(text + GenerateSalt(saltProvider), hash);
        }
    }

    public class Sha256PasswordHashProvider : PasswordHashProvider
    {
        protected override string Encode(string text, IPasswordSaltProvider saltProvider)
        {
            return EncodeRaw(text + GenerateSalt(saltProvider));
        }
        protected override bool Check(string text, string hash, IPasswordSaltProvider saltProvider)
        {
            return EncodeRaw(text + GenerateSalt(saltProvider)) == hash;
        }

        protected string EncodeRaw(string text)
        {
            if (string.IsNullOrEmpty(text))
                return String.Empty;

            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            using (System.Security.Cryptography.HashAlgorithm sha256 = new System.Security.Cryptography.SHA256Managed())
            {
                byte[] encryptedBytes = sha256.TransformFinalBlock(data, 0, data.Length);
                return Convert.ToBase64String(sha256.Hash);
            }
        }
    }

    /// <summary>
    /// Do not use this provider. It is only here for backward compatibility
    /// </summary>
    public class Sha256PasswordHashProviderWithoutSalt : Sha256PasswordHashProvider
    {
        protected override string Encode(string text, IPasswordSaltProvider saltProvider)
        {
            return EncodeRaw(text);
        }
        protected override bool Check(string text, string hash, IPasswordSaltProvider saltProvider)
        {
            return EncodeRaw(text) == hash;
        }
    }
}
