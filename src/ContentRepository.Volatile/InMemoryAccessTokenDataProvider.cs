using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Volatile
{
    /// <summary> 
    /// This is an in-memory implementation of the <see cref="IAccessTokenDataProviderExtension"/> interface.
    /// It requires the main data provider to be an <see cref="InMemoryDataProvider"/>.
    /// </summary>
    public class InMemoryAccessTokenDataProvider : IAccessTokenDataProviderExtension
    {
        public DataCollection<AccessTokenDoc> GetAccessTokens()
        {
            return ((InMemoryDataProvider)DataStore.DataProvider).DB.GetCollection<AccessTokenDoc>();
        }
        
        public System.Threading.Tasks.Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken)
        {
            GetAccessTokens().Clear();
            return System.Threading.Tasks.Task.CompletedTask;
        }
        public System.Threading.Tasks.Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken)
        {
            var accessTokens = GetAccessTokens();

            AccessTokenDoc existing = null;
            if (token.Id != 0)
                existing = accessTokens.FirstOrDefault(x => x.AccessTokenRowId == token.Id);

            if (existing != null)
            {
                existing.ExpirationDate = token.ExpirationDate;
                return System.Threading.Tasks.Task.CompletedTask;
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
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public Task<AccessToken> LoadAccessTokenByIdAsync(int accessTokenId, CancellationToken cancellationToken)
        {
            var existing = GetAccessTokens().FirstOrDefault(x => x.AccessTokenRowId == accessTokenId);
            return System.Threading.Tasks.Task.FromResult(existing == null ? null : CreateAccessTokenFromDoc(existing));
        }

        public Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature, CancellationToken cancellationToken)
        {
            var contentIdValue = contentId == 0 ? (int?)null : contentId;
            var existing = GetAccessTokens().FirstOrDefault(x => x.Value == tokenValue &&
                                                           x.ContentId == contentIdValue &&
                                                           x.Feature == feature &&
                                                           x.ExpirationDate > DateTime.UtcNow);
            return System.Threading.Tasks.Task.FromResult(existing == null ? null : CreateAccessTokenFromDoc(existing));
        }
        public Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult(GetAccessTokens()
                .Where(x => x.UserId == userId && x.ExpirationDate > DateTime.UtcNow)
                .Select(CreateAccessTokenFromDoc)
                .ToArray());
        }

        public System.Threading.Tasks.Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate, CancellationToken cancellationToken)
        {
            var doc = GetAccessTokens().FirstOrDefault(x => x.Value == tokenValue && x.ExpirationDate > DateTime.UtcNow);
            if (doc == null)
                throw new InvalidAccessTokenException("Token not found or it is expired.");
            doc.ExpirationDate = newExpirationDate;

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken)
        {
            var accessTokens = GetAccessTokens();
            var docs = accessTokens.Where(x => x.Value == tokenValue).ToArray();
            foreach (var doc in docs)
                accessTokens.Remove(doc);

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken)
        {
            var accessTokens = GetAccessTokens();
            var docs = accessTokens.Where(x => x.UserId == userId).ToArray();
            foreach (var doc in docs)
                accessTokens.Remove(doc);

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken)
        {
            var accessTokens = GetAccessTokens();
            var docs = accessTokens.Where(x => x.ContentId == contentId).ToArray();
            foreach (var doc in docs)
                accessTokens.Remove(doc);

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task CleanupAccessTokensAsync(CancellationToken cancellationToken)
        {
            // do nothing
            return System.Threading.Tasks.Task.CompletedTask;
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
