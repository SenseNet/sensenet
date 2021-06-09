using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines methods for managing access tokens in the database.
    /// </summary>
    public interface IAccessTokenDataProviderExtension : IDataProviderExtension
    {
        /// <summary>
        /// Deletes all AccessTokens even if they are still valid.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Saves an access token instance to the database.
        /// </summary>
        /// <param name="token">The access token to save.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken);

        /// <summary>
        /// Gets an access token by its id.
        /// </summary>
        /// <param name="accessTokenId">Access token identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps an access token instance or null.</returns>
        Task<AccessToken> LoadAccessTokenByIdAsync(int accessTokenId, CancellationToken cancellationToken);
        /// <summary>
        /// Gets a token by its value related to a content and a feature.
        /// The contentId or feature parameters are necessary if the original token was emitted by these.
        /// </summary>
        /// <param name="tokenValue">Token value.</param>
        /// <param name="contentId">Content id.</param>
        /// <param name="feature">Feature identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the found token instance.</returns>
        Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature, CancellationToken cancellationToken);
        /// <summary>
        /// Gets all tokens related to a user.
        /// </summary>
        /// <param name="userId">The token owner id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps an array of tokens.</returns>
        Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the expiration date of the specified token.
        /// </summary>
        /// <param name="tokenValue">Token value.</param>
        /// <param name="newExpirationDate">New expiration date.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes the specified token regardless of expiration date.
        /// </summary>
        /// <param name="tokenValue">Token value.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes all tokens of the provided user.
        /// </summary>
        /// <param name="userId">The token owner id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes all tokens related to a content.
        /// </summary>
        /// <param name="contentId">Content id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes all access tokens that expired a certain time ago.
        /// Expiration time is determined by the extension implementation.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CleanupAccessTokensAsync(CancellationToken cancellationToken);
    }
}
