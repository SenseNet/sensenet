using System;
using System.Security.Cryptography;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage.Security
{
    public class AccessTokenVault
    {
        public static void DeleteAllAccessTokens()
        {
            DataProvider.Current.DeleteAllAccessTokens();
        }

        public static AccessToken CreateToken(int userId, TimeSpan timeout, int contentId = 0, string feature = null)
        {
            var now = DateTime.UtcNow;
            var token = new AccessToken
            {
                Value = GenerateTokenValue(),
                UserId = userId,
                ContentId = contentId,
                Feature = feature,
                CreationDate = now,
                ExpirationDate = now.Add(timeout)
            };
            DataProvider.Current.SaveAccessToken(token);
            return token;
        }

        /// <summary>
        /// Designed for test purposes.
        /// Returns the AccessToken by the given Id.
        /// The token is null if it does not exist in the database.
        /// </summary>
        internal static AccessToken GetTokenById(int accessTokenId)
        {
            return DataProvider.Current.LoadAccessTokenById(accessTokenId);
        }
        public static AccessToken GetToken(string tokenValue, int contentId = 0, string feature = null)
        {
            return DataProvider.Current.LoadAccessToken(tokenValue, contentId, feature);
        }
        public static AccessToken[] GetTokens(int userId)
        {
            return DataProvider.Current.LoadAccessTokens(userId);
        }

        public static bool TokenExists(string tokenValue, int contentId = 0, string feature = null)
        {
            return GetToken(tokenValue, contentId, feature) != null;
        }

        public static void AssertTokenExists(string tokenValue, int contentId = 0, string feature = null)
        {
            if (!TokenExists(tokenValue, contentId, feature))
                throw new InvalidAccessTokenException("Token not found or it is expired.");
        }

        public static void UpdateToken(string tokenValue, DateTime expirationDate)
        {
            DataProvider.Current.UpdateAccessToken(tokenValue, expirationDate);
        }

        public static void DeleteToken(string tokenValue)
        {
            DataProvider.Current.DeleteAccessToken(tokenValue);
        }
        public static void DeleteTokensByUser(int userId)
        {
            DataProvider.Current.DeleteAccessTokensByUser(userId);
        }
        public static void DeleteTokensByContent(int contentId)
        {
            DataProvider.Current.DeleteAccessTokensByContent(contentId);
        }

        /* =========================================================================================== Token value generator */

        private static readonly char[] AllowedValueChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
        private static int ValueSize = 83;
        public static string GenerateTokenValue()
        {
            var data = new byte[ValueSize];
            var chars = new char[ValueSize];
            var charCount = AllowedValueChars.Length;

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                rng.GetBytes(data);
            for (int i = 0; i < ValueSize; i++)
                chars[i] = AllowedValueChars[data[i] % charCount];

            return new string(chars);
        }

    }
}
