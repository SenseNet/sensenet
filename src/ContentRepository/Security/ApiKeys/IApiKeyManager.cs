﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Security.ApiKeys
{
    /// <summary>
    /// Defines methods for managing api keys.
    /// </summary>
    public interface IApiKeyManager
    {
        /// <summary>
        /// Gets the user related to the provided api key or null.
        /// </summary>
        Task<User> GetUserByApiKeyAsync(string apiKey, CancellationToken cancel);
        /// <summary>
        /// Gets all api keys related to the provided user id.
        /// </summary>        
        Task<ApiKey[]> GetApiKeysByUserAsync(int userId, CancellationToken cancel);
        /// <summary>
        /// Creates a new api key for the provided user and expiration.
        /// </summary>
        Task<ApiKey> CreateApiKeyAsync(int userId, DateTime expiration, CancellationToken cancel);
        /// <summary>
        /// Deletes the provided api key.
        /// </summary>
        System.Threading.Tasks.Task DeleteApiKeyAsync(string apiKey, CancellationToken cancel);
        /// <summary>
        /// Deletes all api keys or only expired ones.
        /// </summary>
        System.Threading.Tasks.Task DeleteApiKeysAsync(bool expiredOnly, CancellationToken cancel);
    }
}
