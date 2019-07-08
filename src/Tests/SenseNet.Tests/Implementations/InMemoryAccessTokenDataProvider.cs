using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Tests.Implementations
{
    //UNDONE:DB: ASYNC API: CancellationToken is not used in this class.
    public class InMemoryAccessTokenDataProvider : IAccessTokenDataProviderExtension
    {
        public DataCollection<AccessTokenDoc> GetAccessTokens()
        {
            return ((InMemoryDataProvider)DataStore.DataProvider).DB.GetCollection<AccessTokenDoc>();
        }
        
        public Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            GetAccessTokens().Clear();
            return Task.CompletedTask;
        }
        public Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken = default(CancellationToken))
        {
            var accessTokens = GetAccessTokens();

            AccessTokenDoc existing = null;
            if (token.Id != 0)
                existing = accessTokens.FirstOrDefault(x => x.AccessTokenRowId == token.Id);

            if (existing != null)
            {
                existing.ExpirationDate = token.ExpirationDate;
                return Task.CompletedTask;
            }

            var newAccessTokenRowId = accessTokens.Count == 0 ? 1 : accessTokens.Max(t => t.AccessTokenRowId) + 1;
            accessTokens.Insert(new AccessTokenDoc
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
            var existing = GetAccessTokens().FirstOrDefault(x => x.AccessTokenRowId == accessTokenId);
            return Task.FromResult(existing == null ? null : CreateAccessTokenFromDoc(existing));
        }

        public Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature, CancellationToken cancellationToken = default(CancellationToken))
        {
            var contentIdValue = contentId == 0 ? (int?)null : contentId;
            var existing = GetAccessTokens().FirstOrDefault(x => x.Value == tokenValue &&
                                                           x.ContentId == contentIdValue &&
                                                           x.Feature == feature &&
                                                           x.ExpirationDate > DateTime.UtcNow);
            return Task.FromResult(existing == null ? null : CreateAccessTokenFromDoc(existing));
        }
        public Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(GetAccessTokens()
                .Where(x => x.UserId == userId && x.ExpirationDate > DateTime.UtcNow)
                .Select(CreateAccessTokenFromDoc)
                .ToArray());
        }

        public Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate, CancellationToken cancellationToken = default(CancellationToken))
        {
            var doc = GetAccessTokens().FirstOrDefault(x => x.Value == tokenValue && x.ExpirationDate > DateTime.UtcNow);
            if (doc == null)
                throw new InvalidAccessTokenException("Token not found or it is expired.");
            doc.ExpirationDate = newExpirationDate;

            return Task.CompletedTask;
        }

        public Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            var accessTokens = GetAccessTokens();
            var docs = accessTokens.Where(x => x.Value == tokenValue).ToArray();
            foreach (var doc in docs)
                accessTokens.Remove(doc);

            return Task.CompletedTask;
        }

        public Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var accessTokens = GetAccessTokens();
            var docs = accessTokens.Where(x => x.UserId == userId).ToArray();
            foreach (var doc in docs)
                accessTokens.Remove(doc);

            return Task.CompletedTask;
        }

        public Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var accessTokens = GetAccessTokens();
            var docs = accessTokens.Where(x => x.ContentId == contentId).ToArray();
            foreach (var doc in docs)
                accessTokens.Remove(doc);

            return Task.CompletedTask;
        }

        public Task CleanupAccessTokensAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // do nothing
            return Task.CompletedTask;
        }

        private static AccessToken CreateAccessTokenFromDoc(AccessTokenDoc doc)
        {
            return new AccessToken
            {
                Id = doc.AccessTokenRowId,
                Value = doc.Value,
                UserId = doc.UserId,
                ContentId = doc.ContentId ?? 0,
                Feature = doc.Feature,
                CreationDate = doc.CreationDate,
                ExpirationDate = doc.ExpirationDate
            };
        }
    }
}
