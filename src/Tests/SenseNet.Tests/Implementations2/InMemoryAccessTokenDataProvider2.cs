using System;
using System.Linq;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Tests.Implementations2
{
    internal class InMemoryAccessTokenDataProvider2 : IAccessTokenDataProviderExtension
    {
        public DataCollection<AccessTokenDoc> GetAccessTokens()
        {
            return ((InMemoryDataProvider2)DataStore.DataProvider).DB.GetCollection<AccessTokenDoc>();
        }


        public void DeleteAllAccessTokens()
        {
            GetAccessTokens().Clear();
        }
        public void SaveAccessToken(AccessToken token)
        {
            var accessTokens = GetAccessTokens();

            AccessTokenDoc existing = null;
            if (token.Id != 0)
                existing = accessTokens.FirstOrDefault(x => x.AccessTokenRowId == token.Id);

            if (existing != null)
            {
                existing.ExpirationDate = token.ExpirationDate;
                return;
            }

            var newAccessTokenRowId = accessTokens.Count == 0 ? 1 : accessTokens.Max(t => t.AccessTokenRowId) + 1;
            accessTokens.Add(new AccessTokenDoc
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
        }

        public AccessToken LoadAccessTokenById(int accessTokenId)
        {
            var existing = GetAccessTokens().FirstOrDefault(x => x.AccessTokenRowId == accessTokenId);
            return existing == null ? null : CreateAccessTokenFromDoc(existing);
        }

        public AccessToken LoadAccessToken(string tokenValue, int contentId, string feature)
        {
            var contentIdValue = contentId == 0 ? (int?)null : contentId;
            var existing = GetAccessTokens().FirstOrDefault(x => x.Value == tokenValue &&
                                                           x.ContentId == contentIdValue &&
                                                           x.Feature == feature &&
                                                           x.ExpirationDate > DateTime.UtcNow);
            return existing == null ? null : CreateAccessTokenFromDoc(existing);
        }
        public AccessToken[] LoadAccessTokens(int userId)
        {
            return GetAccessTokens()
                .Where(x => x.UserId == userId && x.ExpirationDate > DateTime.UtcNow)
                .Select(CreateAccessTokenFromDoc)
                .ToArray();
        }

        public void UpdateAccessToken(string tokenValue, DateTime newExpirationDate)
        {
            var doc = GetAccessTokens().FirstOrDefault(x => x.Value == tokenValue && x.ExpirationDate > DateTime.UtcNow);
            if (doc == null)
                throw new InvalidAccessTokenException("Token not found or it is expired.");
            doc.ExpirationDate = newExpirationDate;
        }

        public void DeleteAccessToken(string tokenValue)
        {
            var accessTokens = GetAccessTokens();
            var docs = accessTokens.Where(x => x.Value == tokenValue).ToArray();
            foreach (var doc in docs)
                accessTokens.Remove(doc);
        }

        public void DeleteAccessTokensByUser(int userId)
        {
            var accessTokens = GetAccessTokens();
            var docs = accessTokens.Where(x => x.UserId == userId).ToArray();
            foreach (var doc in docs)
                accessTokens.Remove(doc);
        }

        public void DeleteAccessTokensByContent(int contentId)
        {
            var accessTokens = GetAccessTokens();
            var docs = accessTokens.Where(x => x.ContentId == contentId).ToArray();
            foreach (var doc in docs)
                accessTokens.Remove(doc);
        }

        public void CleanupAccessTokens()
        {
            // do nothing
        }

        private AccessToken CreateAccessTokenFromDoc(AccessTokenDoc doc)
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
