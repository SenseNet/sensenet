using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Tests.Implementations
{
    internal class InMemoryAccessTokenDataProvider : IAccessTokenDataProviderExtension
    {
        public class AccessTokenRow
        {
            public int AccessTokenRowId;
            public string Value;
            public int UserId;
            public int? ContentId;
            public string Feature;
            public DateTime CreationDate;
            public DateTime ExpirationDate;

            public AccessTokenRow Clone()
            {
                return new AccessTokenRow
                {
                    AccessTokenRowId = AccessTokenRowId,
                    Value = Value,
                    UserId = UserId,
                    ContentId = ContentId,
                    Feature = Feature,
                    CreationDate = CreationDate,
                    ExpirationDate = ExpirationDate
                };
            }
        }

        public List<AccessTokenRow> AccessTokens { get; set; } = new List<AccessTokenRow>();

        public Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            AccessTokens.Clear();
            return Task.CompletedTask;
        }
        public Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken = default(CancellationToken))
        {
            AccessTokenRow existing = null;
            if (token.Id != 0)
                existing = AccessTokens.FirstOrDefault(x => x.AccessTokenRowId == token.Id);

            if (existing != null)
            {
                existing.ExpirationDate = token.ExpirationDate;
                return Task.CompletedTask;
            }

            var newAccessTokenRowId = AccessTokens.Count == 0 ? 1 : AccessTokens.Max(t => t.AccessTokenRowId) + 1;
            AccessTokens.Add(new AccessTokenRow
            {
                AccessTokenRowId = newAccessTokenRowId,
                Value = token.Value,
                UserId = token.UserId,
                ContentId = token.ContentId == 0 ? (int?)null : token.ContentId,
                Feature = token.Feature,
                CreationDate = token.CreationDate,
                ExpirationDate = token.ExpirationDate
            });

            token.Id = newAccessTokenRowId;

            return Task.CompletedTask;
        }

        public Task<AccessToken> LoadAccessTokenByIdAsync(int accessTokenId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var existing = AccessTokens.FirstOrDefault(x => x.AccessTokenRowId == accessTokenId);
            return Task.FromResult(existing == null ? null : CreateAccessTokenFromRow(existing));
        }

        public Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature, CancellationToken cancellationToken = default(CancellationToken))
        {
            var contentIdValue = contentId == 0 ? (int?)null : contentId;
            var existing = AccessTokens.FirstOrDefault(x => x.Value == tokenValue &&
                                                           x.ContentId == contentIdValue &&
                                                           x.Feature == feature &&
                                                           x.ExpirationDate > DateTime.UtcNow);
            return Task.FromResult(existing == null ? null : CreateAccessTokenFromRow(existing));
        }
        public Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(AccessTokens
                .Where(x => x.UserId == userId && x.ExpirationDate > DateTime.UtcNow)
                .Select(CreateAccessTokenFromRow)
                .ToArray());
        }

        public Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate, CancellationToken cancellationToken = default(CancellationToken))
        {
            var row = AccessTokens.FirstOrDefault(x => x.Value == tokenValue && x.ExpirationDate > DateTime.UtcNow);
            if (row == null)
                throw new InvalidAccessTokenException("Token not found or it is expired.");
            row.ExpirationDate = newExpirationDate;

            return Task.CompletedTask;
        }

        public Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            var rows = AccessTokens.Where(x => x.Value == tokenValue).ToArray();
            foreach (var row in rows)
                AccessTokens.Remove(row);

            return Task.CompletedTask;
        }

        public Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var rows = AccessTokens.Where(x => x.UserId == userId).ToArray();
            foreach (var row in rows)
                AccessTokens.Remove(row);

            return Task.CompletedTask;
        }

        public Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var rows = AccessTokens.Where(x => x.ContentId == contentId).ToArray();
            foreach (var row in rows)
                AccessTokens.Remove(row);

            return Task.CompletedTask;
        }

        public Task CleanupAccessTokensAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // do nothing
            return Task.CompletedTask;
        }

        private AccessToken CreateAccessTokenFromRow(AccessTokenRow row)
        {
            return new AccessToken
            {
                Id = row.AccessTokenRowId,
                Value = row.Value,
                UserId = row.UserId,
                ContentId = row.ContentId ?? 0,
                Feature = row.Feature,
                CreationDate = row.CreationDate,
                ExpirationDate = row.ExpirationDate
            };
        }
    }
}
