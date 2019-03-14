using System;
using System.Linq;
using System.Security.Cryptography;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Supports AccessToken operations. Developers may use this API for securing access to a feature.
    /// Token storage is handled through a configurable dataprovider extension that implements
    /// the <see cref="IAccessTokenDataProviderExtension"/> interface.
    /// </summary>
    public class AccessTokenVault
    {
        private const int MinimumTokenExpirationMinutes = 5;

        private static IAccessTokenDataProviderExtension Storage => DataProvider.GetExtension<IAccessTokenDataProviderExtension>();

        /// <summary>
        /// Deletes all AccessTokens even if they are still valid.
        /// </summary>
        public static void DeleteAllAccessTokens()
        {
            Storage.DeleteAllAccessTokens();
        }

        /// <summary>
        /// Creates a new token for the given user with the specified timeout.
        /// The token can be bound to a content or any specified feature name.
        /// </summary>
        /// <param name="userId">The ID of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout of the token.</param>
        /// <param name="contentId">An ID of a Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
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
            Storage.SaveAccessToken(token);
            return token;
        }

        /// <summary>
        /// Loads an existing token or creates a new one for the given user with the specified timeout.
        /// If there is an existing token that expires in less then 5 minutes, this method issues a new one.
        /// The token can be bound to a content or any specified feature name.
        /// </summary>
        /// <param name="userId">The ID of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout of the token.</param>
        /// <param name="contentId">An ID of a Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <returns>The eisting or new AccessToken instance.</returns>
        public static AccessToken GetOrAddToken(int userId, TimeSpan timeout, int contentId = 0, string feature = null)
        {
            var maxExpiration = DateTime.UtcNow.Add(timeout);
            var existingToken = Storage.LoadAccessTokens(userId)
                .OrderBy(at => at.ExpirationDate)
                .LastOrDefault(at => at.ContentId == contentId &&
                                     at.Feature == feature &&
                                     at.ExpirationDate <= maxExpiration);

            // if the found token expires in less then a minimum expiration, we issue a new one
            return existingToken?.ExpirationDate > DateTime.UtcNow.AddMinutes(MinimumTokenExpirationMinutes)
                ? existingToken
                : CreateToken(userId, timeout, contentId, feature);
        }

        /// <summary>
        /// Designed for test purposes.
        /// Returns the AccessToken by the given Id.
        /// The token is null if it does not exist in the database.
        /// </summary>
        internal static AccessToken GetTokenById(int accessTokenId)
        {
            return Storage.LoadAccessTokenById(accessTokenId);
        }

        /// <summary>
        /// Returns the the token by the specified value and the given filters if there is any.
        /// The 'contentId' or 'feature' parameters are necessary if the original token is emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">An ID of a Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <returns>Existing AccessToken or null.</returns>
        public static AccessToken GetToken(string tokenValue, int contentId = 0, string feature = null)
        {
            return Storage.LoadAccessToken(tokenValue, contentId, feature);
        }

        /// <summary>
        /// Returs all tokens of the given User.
        /// </summary>
        /// <param name="userId">The token owner ID.</param>
        /// <returns>An AccessToken array.</returns>
        public static AccessToken[] GetAllTokens(int userId)
        {
            return Storage.LoadAccessTokens(userId);
        }

        /// <summary>
        /// Returns true if the specified token value exists and has not yet expired.
        /// The 'contentId' or 'feature' parameters are necessary if the original token is emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">An ID of a Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <returns>true or false</returns>
        public static bool TokenExists(string tokenValue, int contentId = 0, string feature = null)
        {
            return GetToken(tokenValue, contentId, feature) != null;
        }

        /// <summary>
        /// Assumes the token value existence. Missing or expired token causes InvalidAccessTokenException.
        /// The 'contentId' or 'feature' parameters are necessary if the original token is emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">An ID of a Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <exception cref="InvalidAccessTokenException"></exception>
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
        /// <exception cref="InvalidAccessTokenException"></exception>
        public static void UpdateToken(string tokenValue, DateTime expirationDate)
        {
            Storage.UpdateAccessToken(tokenValue, expirationDate);
        }

        /// <summary>
        /// Deletes the specified token regardless of expiration date.
        /// </summary>
        /// <param name="tokenValue">The value of the original token.</param>
        public static void DeleteToken(string tokenValue)
        {
            Storage.DeleteAccessToken(tokenValue);
        }

        /// <summary>
        /// Deletes all tokens of the given user regardless of expiration date.
        /// </summary>
        /// <param name="userId">The token owner ID.</param>
        public static void DeleteTokensByUser(int userId)
        {
            Storage.DeleteAccessTokensByUser(userId);
        }

        /// <summary>
        /// Deletes the tokens associated by the specified contentId regardless of expiration date.
        /// </summary>
        /// <param name="contentId">The associated content id.</param>
        public static void DeleteTokensByContent(int contentId)
        {
            Storage.DeleteAccessTokensByContent(contentId);
        }

        /// <summary>
        /// Deletes all access tokens that expired a certain time ago.
        /// </summary>
        public static void Cleanup()
        {
            SnTrace.Database.Write("Cleanup access tokens.");
            Storage.CleanupAccessTokens();
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
