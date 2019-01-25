using System;
using System.Security.Cryptography;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Supports AccessToken operations.
    /// </summary>
    public class AccessTokenVault
    {
        /// <summary>
        /// Deletes all AccessToken even if it is out of date.
        /// </summary>
        public static void DeleteAllAccessTokens()
        {
            DataProvider.Instance.DeleteAllAccessTokens();
        }

        /// <summary>
        /// Creates a new token for the given user with the specified timeout.
        /// The token can be bound a content or any specified feature name.
        /// </summary>
        /// <param name="userId">The ID of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout od the token.</param>
        /// <param name="contentId">An ID of a Content that is associated to the token.</param>
        /// <param name="feature">Any word that identifies the token.</param>
        /// <returns>The new AccessToken instance.</returns>
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
            DataProvider.Instance.SaveAccessToken(token);
            return token;
        }

        /// <summary>
        /// Designed for test purposes.
        /// Returns the AccessToken by the given Id.
        /// The token is null if it does not exist in the database.
        /// </summary>
        internal static AccessToken GetTokenById(int accessTokenId)
        {
            return DataProvider.Instance.LoadAccessTokenById(accessTokenId);
        }

        /// <summary>
        /// Returns the the token by the specified value and the given filters if there is any.
        /// The 'contentId' or a 'feature' parameters are necessary if the original token is emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">An ID of a Content that is associated to the token.</param>
        /// <param name="feature">Any word that identifies the token.</param>
        /// <returns>Existing AccessToken or null.</returns>
        public static AccessToken GetToken(string tokenValue, int contentId = 0, string feature = null)
        {
            return DataProvider.Instance.LoadAccessToken(tokenValue, contentId, feature);
        }

        /// <summary>
        /// Returs all tokens of the given User.
        /// </summary>
        /// <param name="userId">The token owner ID</param>
        /// <returns>An AccessToken array</returns>
        public static AccessToken[] GetTokens(int userId)
        {
            return DataProvider.Instance.LoadAccessTokens(userId);
        }

        /// <summary>
        /// Returns true if the specified token value is exists and has not yet expired.
        /// The 'contentId' or a 'feature' parameters are necessary if the original token is emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">An ID of a Content that is associated to the token.</param>
        /// <param name="feature">Any word that identifies the token.</param>
        /// <returns>true or false</returns>
        public static bool TokenExists(string tokenValue, int contentId = 0, string feature = null)
        {
            return GetToken(tokenValue, contentId, feature) != null;
        }

        /// <summary>
        /// Assumes the token value existence. Missing or expired token causes InvalidAccessTokenException.
        /// The 'contentId' or a 'feature' parameters are necessary if the original token is emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">An ID of a Content that is associated to the token.</param>
        /// <param name="feature">Any word that identifies the token.</param>
        public static void AssertTokenExists(string tokenValue, int contentId = 0, string feature = null)
        {
            if (!TokenExists(tokenValue, contentId, feature))
                throw new InvalidAccessTokenException("Token not found or it is expired.");
        }

        /// <summary>
        /// Updates the expiration date of the specified token value.
        /// Missing or expired token causes InvalidAccessTokenException.
        /// </summary>
        /// <param name="tokenValue">The value of the original token.</param>
        /// <param name="expirationDate">The new expiration date.</param>
        public static void UpdateToken(string tokenValue, DateTime expirationDate)
        {
            DataProvider.Instance.UpdateAccessToken(tokenValue, expirationDate);
        }

        /// <summary>
        /// Deletes the specified token regardless of expiration date.
        /// </summary>
        /// <param name="tokenValue">The value of the original token.</param>
        public static void DeleteToken(string tokenValue)
        {
            DataProvider.Instance.DeleteAccessToken(tokenValue);
        }

        /// <summary>
        /// Deletes all tokens of the given user regardless of expiration date.
        /// </summary>
        /// <param name="userId">The token owner ID</param>
        public static void DeleteTokensByUser(int userId)
        {
            DataProvider.Instance.DeleteAccessTokensByUser(userId);
        }

        /// <summary>
        /// Deletes the tokens associated by the specified contentId regardless of expiration date.
        /// </summary>
        /// <param name="contentId">The associated content id</param>
        public static void DeleteTokensByContent(int contentId)
        {
            DataProvider.Instance.DeleteAccessTokensByContent(contentId);
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
