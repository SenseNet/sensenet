using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
// ReSharper disable CheckNamespace

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

        private static IAccessTokenDataProviderExtension Storage => DataStore.GetDataProviderExtension<IAccessTokenDataProviderExtension>();

        /// <summary>
        /// Deletes all AccessTokens even if they are still valid.
        /// </summary>
        public static void DeleteAllAccessTokens()
        {
            Storage.DeleteAllAccessTokensAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Deletes all AccessTokens even if they are still valid.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken)
        {
            return Storage.DeleteAllAccessTokensAsync(cancellationToken);
        }

        /// <summary>
        /// Creates a new token for the provided user with the specified timeout.
        /// The token can be bound to a content or a feature.
        /// </summary>
        /// <param name="userId">The id of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout of the token.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <returns>The new AccessToken instance.</returns>
        public static AccessToken CreateToken(int userId, TimeSpan timeout, int contentId = 0, string feature = null)
        {
            return CreateTokenAsync(userId, timeout, contentId, feature, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a new token for the provided user with the specified timeout.
        /// </summary>
        /// <param name="userId">The id of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout of the token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new AccessToken instance.</returns>
        public static Task<AccessToken> CreateTokenAsync(int userId, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            return CreateTokenAsync(userId, timeout, 0, null, cancellationToken);
        }
        /// <summary>
        /// Creates a new token for the provided user with the specified timeout.
        /// The token can be bound to a content.
        /// </summary>
        /// <param name="userId">The id of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout of the token.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new AccessToken instance.</returns>
        public static Task<AccessToken> CreateTokenAsync(int userId, TimeSpan timeout, int contentId,
            CancellationToken cancellationToken)
        {
            return CreateTokenAsync(userId, timeout, contentId, null, cancellationToken);
        }
        /// <summary>
        /// Creates a new token for the provided user with the specified timeout.
        /// The token can be bound to a content or a feature.
        /// </summary>
        /// <param name="userId">The id of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout of the token.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new AccessToken instance.</returns>
        public static async Task<AccessToken> CreateTokenAsync(int userId, TimeSpan timeout, int contentId, string feature,
            CancellationToken cancellationToken)
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

            await Storage.SaveAccessTokenAsync(token, cancellationToken).ConfigureAwait(false);
            return token;
        }

        /// <summary>
        /// Loads an existing token or creates a new one for the provided user with the specified timeout.
        /// If there is an existing token that expires in less then 5 minutes, this method issues a new one.
        /// The token can be bound to a content or a feature.
        /// </summary>
        /// <param name="userId">The id of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout of the token.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <returns>The existing or new AccessToken instance.</returns>
        public static AccessToken GetOrAddToken(int userId, TimeSpan timeout, int contentId = 0, string feature = null)
        {
            return GetOrAddTokenAsync(userId, timeout, contentId, feature, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Loads an existing token or creates a new one for the provided user with the specified timeout.
        /// If there is an existing token that expires in less then 5 minutes, this method issues a new one.
        /// </summary>
        /// <param name="userId">The id of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout of the token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps
        /// the existing or new AccessToken instance.</returns>
        public static Task<AccessToken> GetOrAddTokenAsync(int userId, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            return GetOrAddTokenAsync(userId, timeout, 0, cancellationToken);
        }
        /// <summary>
        /// Loads an existing token or creates a new one for the provided user with the specified timeout.
        /// If there is an existing token that expires in less then 5 minutes, this method issues a new one.
        /// The token can be bound to a content.
        /// </summary>
        /// <param name="userId">The id of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout of the token.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps
        /// the existing or new AccessToken instance.</returns>
        public static Task<AccessToken> GetOrAddTokenAsync(int userId, TimeSpan timeout, int contentId,
            CancellationToken cancellationToken)
        {
            return GetOrAddTokenAsync(userId, timeout, contentId, null, cancellationToken);
        }
        /// <summary>
        /// Loads an existing token or creates a new one for the provided user with the specified timeout.
        /// If there is an existing token that expires in less then 5 minutes, this method issues a new one.
        /// The token can be bound to a content or a feature.
        /// </summary>
        /// <param name="userId">The id of the User that is the owner of the token.</param>
        /// <param name="timeout">The timeout of the token.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps
        /// the existing or new AccessToken instance.</returns>
        public static async Task<AccessToken> GetOrAddTokenAsync(int userId, TimeSpan timeout, int contentId, string feature,
            CancellationToken cancellationToken)
        {
            var maxExpiration = DateTime.UtcNow.Add(timeout);
            var existingToken = (await Storage.LoadAccessTokensAsync(userId, cancellationToken).ConfigureAwait(false))
                .OrderBy(at => at.ExpirationDate)
                .LastOrDefault(at => at.ContentId == contentId &&
                                     at.Feature == feature &&
                                     at.ExpirationDate <= maxExpiration);

            // if the found token expires in less then a minimum expiration, we issue a new one
            return existingToken?.ExpirationDate > DateTime.UtcNow.AddMinutes(MinimumTokenExpirationMinutes)
                ? existingToken
                : await CreateTokenAsync(userId, timeout, contentId, feature, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Designed for test purposes.
        /// Returns the AccessToken by the provided Id.
        /// The token is null if it does not exist in the database.
        /// </summary>
        internal static AccessToken GetTokenById(int accessTokenId)
        {
            return Storage.LoadAccessTokenByIdAsync(accessTokenId, CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Designed for test purposes.
        /// Returns the AccessToken by the provided Id.
        /// The token is null if it does not exist in the database.
        /// </summary>
        internal static Task<AccessToken> GetTokenByIdAsync(int accessTokenId, 
            CancellationToken cancellationToken)
        {
            return Storage.LoadAccessTokenByIdAsync(accessTokenId, cancellationToken);
        }

        /// <summary>
        /// Gets the token by the specified value and the provided filters if there is any.
        /// The 'contentId' or 'feature' parameters are necessary if the original token was emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <returns>The existing AccessToken or null.</returns>
        public static AccessToken GetToken(string tokenValue, int contentId = 0, string feature = null)
        {
            return Storage.LoadAccessTokenAsync(tokenValue, contentId, feature, CancellationToken.None)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the token by the specified value.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the existing AccessToken or null.</returns>
        public static Task<AccessToken> GetTokenAsync(string tokenValue,
            CancellationToken cancellationToken)
        {
            return GetTokenAsync(tokenValue, 0, cancellationToken);
        }
        /// <summary>
        /// Gets the token by the specified value and the provided content id.
        /// The 'contentId' parameter is necessary if the original token was emitted by that.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the existing AccessToken or null.</returns>
        public static Task<AccessToken> GetTokenAsync(string tokenValue, int contentId,
            CancellationToken cancellationToken)
        {
            return GetTokenAsync(tokenValue, contentId, null, cancellationToken);
        }
        /// <summary>
        /// Gets the token by the specified value and the provided filters if there is any.
        /// The 'contentId' or 'feature' parameters are necessary if the original token was emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the existing AccessToken or null.</returns>
        public static Task<AccessToken> GetTokenAsync(string tokenValue, int contentId, string feature,
            CancellationToken cancellationToken)
        {
            return Storage.LoadAccessTokenAsync(tokenValue, contentId, feature, cancellationToken);
        }

        /// <summary>
        /// Gets all tokens of the provided User.
        /// </summary>
        /// <param name="userId">The token owner ID.</param>
        /// <returns>An AccessToken array.</returns>
        public static AccessToken[] GetAllTokens(int userId)
        {
            return Storage.LoadAccessTokensAsync(userId, CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Gets all tokens of the provided User.
        /// </summary>
        /// <param name="userId">The token owner ID.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps an AccessToken array.</returns>
        public static Task<AccessToken[]> GetAllTokensAsync(int userId, 
            CancellationToken cancellationToken)
        {
            return Storage.LoadAccessTokensAsync(userId, cancellationToken);
        }

        /// <summary>
        /// Checks whether the specified token value exists and has not yet expired.
        /// The 'contentId' or 'feature' parameters are necessary if the original token was emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <returns>True if the token exists and is valid.</returns>
        public static bool TokenExists(string tokenValue, int contentId = 0, string feature = null)
        {
            return TokenExistsAsync(tokenValue, contentId, feature, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Checks whether the specified token value exists and has not yet expired.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>True if the token exists and is valid.</returns>
        public static Task<bool> TokenExistsAsync(string tokenValue, CancellationToken cancellationToken)
        {
            return TokenExistsAsync(tokenValue, 0, null, cancellationToken);
        }
        /// <summary>
        /// checks whether the specified token value exists and has not yet expired.
        /// The 'contentId' parameter is necessary if the original token was emitted by that.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and
        /// wraps True if the token exists and is valid.</returns>
        public static Task<bool> TokenExistsAsync(string tokenValue, int contentId, CancellationToken cancellationToken)
        {
            return TokenExistsAsync(tokenValue, contentId, null, cancellationToken);
        }
        /// <summary>
        /// Checks whether the specified token value exists and has not yet expired.
        /// The 'contentId' or 'feature' parameters are necessary if the original token was emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and
        /// wraps True if the token exists and is valid.</returns>
        public static async Task<bool> TokenExistsAsync(string tokenValue, int contentId, string feature, CancellationToken cancellationToken)
        {
            return await GetTokenAsync(tokenValue, contentId, feature, cancellationToken).ConfigureAwait(false) != null;
        }

        /// <summary>
        /// Assumes the token value existence. Missing or expired token causes InvalidAccessTokenException.
        /// The 'contentId' or 'feature' parameters are necessary if the original token was emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <exception cref="InvalidAccessTokenException"></exception>
        public static void AssertTokenExists(string tokenValue, int contentId = 0, string feature = null)
        {
            AssertTokenExistsAsync(tokenValue, contentId, feature, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Assumes the token value existence. Missing or expired token causes InvalidAccessTokenException.
        /// The 'contentId' or 'feature' parameters are necessary if the original token was emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <exception cref="InvalidAccessTokenException"></exception>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task AssertTokenExistsAsync(string tokenValue, CancellationToken cancellationToken)
        {
            return AssertTokenExistsAsync(tokenValue, 0, cancellationToken);
        }
        /// <summary>
        /// Assumes the token value existence. Missing or expired token causes InvalidAccessTokenException.
        /// The 'contentId' or 'feature' parameters are necessary if the original token was emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <exception cref="InvalidAccessTokenException"></exception>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task AssertTokenExistsAsync(string tokenValue, int contentId,
            CancellationToken cancellationToken)
        {
            return AssertTokenExistsAsync(tokenValue, contentId, null, cancellationToken);
        }
        /// <summary>
        /// Assumes the token value existence. Missing or expired token causes InvalidAccessTokenException.
        /// The 'contentId' or 'feature' parameters are necessary if the original token was emitted by these.
        /// </summary>
        /// <param name="tokenValue">The token value.</param>
        /// <param name="contentId">The id of the Content that is associated with the token.</param>
        /// <param name="feature">Any word that identifies the feature.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <exception cref="InvalidAccessTokenException"></exception>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static async Task AssertTokenExistsAsync(string tokenValue, int contentId, string feature,
            CancellationToken cancellationToken)
        {
            if (!await TokenExistsAsync(tokenValue, contentId, feature, cancellationToken).ConfigureAwait(false))
                throw new InvalidAccessTokenException("Token not found or it is expired.");
        }

        /// <summary>
        /// Updates the expiration date of the specified token.
        /// Missing or expired token causes InvalidAccessTokenException.
        /// </summary>
        /// <param name="tokenValue">The value of the original token.</param>
        /// <param name="expirationDate">The new expiration date.</param>
        /// <exception cref="InvalidAccessTokenException"></exception>
        public static void UpdateToken(string tokenValue, DateTime expirationDate)
        {
            Storage.UpdateAccessTokenAsync(tokenValue, expirationDate, CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Updates the expiration date of the specified token.
        /// Missing or expired token causes InvalidAccessTokenException.
        /// </summary>
        /// <param name="tokenValue">The value of the original token.</param>
        /// <param name="expirationDate">The new expiration date.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <exception cref="InvalidAccessTokenException"></exception>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task UpdateTokenAsync(string tokenValue, DateTime expirationDate,
            CancellationToken cancellationToken)
        {
            return Storage.UpdateAccessTokenAsync(tokenValue, expirationDate, cancellationToken);
        }

        /// <summary>
        /// Deletes the specified token regardless of expiration date.
        /// </summary>
        /// <param name="tokenValue">The value of the original token.</param>
        public static void DeleteToken(string tokenValue)
        {
            Storage.DeleteAccessTokenAsync(tokenValue, CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Deletes the specified token regardless of expiration date.
        /// </summary>
        /// <param name="tokenValue">The value of the original token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task DeleteTokenAsync(string tokenValue, CancellationToken cancellationToken)
        {
            return Storage.DeleteAccessTokenAsync(tokenValue, cancellationToken);
        }

        /// <summary>
        /// Deletes all tokens of the provided user regardless of expiration date.
        /// </summary>
        /// <param name="userId">The token owner ID.</param>
        public static void DeleteTokensByUser(int userId)
        {
            Storage.DeleteAccessTokensByUserAsync(userId, CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Deletes all tokens of the provided user regardless of expiration date.
        /// </summary>
        /// <param name="userId">The token owner ID.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task DeleteTokensByUserAsync(int userId, CancellationToken cancellationToken)
        {
            return Storage.DeleteAccessTokensByUserAsync(userId, cancellationToken);
        }

        /// <summary>
        /// Deletes tokens associated with the specified contentId regardless of expiration date.
        /// </summary>
        /// <param name="contentId">The associated content id.</param>
        public static void DeleteTokensByContent(int contentId)
        {
            Storage.DeleteAccessTokensByContentAsync(contentId, CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Deletes the tokens associated with the specified contentId regardless of expiration date.
        /// </summary>
        /// <param name="contentId">The associated content id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task DeleteTokensByContentAsync(int contentId, CancellationToken cancellationToken)
        {
            return Storage.DeleteAccessTokensByContentAsync(contentId, cancellationToken);
        }

        /// <summary>
        /// Deletes all expired access tokens.
        /// </summary>
        public static void Cleanup()
        {
            SnTrace.Database.Write("Cleanup access tokens.");
            Storage.CleanupAccessTokensAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Deletes all expired access tokens.
        /// </summary>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task CleanupAsync(CancellationToken cancellationToken)
        {
            SnTrace.Database.Write("Cleanup access tokens.");
            return Storage.CleanupAccessTokensAsync(cancellationToken);
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
