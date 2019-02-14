using System;
using System.Collections.Generic;
using System.Linq;
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

        public DataProvider MainProvider { get; set; }
        public List<AccessTokenRow> AccessTokens { get; set; } = new List<AccessTokenRow>();

        public void DeleteAllAccessTokens()
        {
            AccessTokens.Clear();
        }
        public void SaveAccessToken(AccessToken token)
        {
            AccessTokenRow existing = null;
            if (token.Id != 0)
                existing = AccessTokens.FirstOrDefault(x => x.AccessTokenRowId == token.Id);

            if (existing != null)
            {
                existing.ExpirationDate = token.ExpirationDate;
                return;
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
        }

        public AccessToken LoadAccessTokenById(int accessTokenId)
        {
            var existing = AccessTokens.FirstOrDefault(x => x.AccessTokenRowId == accessTokenId);
            return existing == null ? null : CreateAccessTokenFromRow(existing);
        }

        public AccessToken LoadAccessToken(string tokenValue, int contentId, string feature)
        {
            var contentIdValue = contentId == 0 ? (int?)null : contentId;
            var existing = AccessTokens.FirstOrDefault(x => x.Value == tokenValue &&
                                                           x.ContentId == contentIdValue &&
                                                           x.Feature == feature &&
                                                           x.ExpirationDate > DateTime.UtcNow);
            return existing == null ? null : CreateAccessTokenFromRow(existing);
        }
        public AccessToken[] LoadAccessTokens(int userId)
        {
            return AccessTokens
                .Where(x => x.UserId == userId && x.ExpirationDate > DateTime.UtcNow)
                .Select(CreateAccessTokenFromRow)
                .ToArray();
        }
        public AccessToken[] LoadAccessTokens(int userId, int contentId, string feature)
        {
            var contentIdValue = contentId == 0 ? (int?)null : contentId;

            return AccessTokens
                .Where(x => x.UserId == userId && 
                        x.ExpirationDate > DateTime.UtcNow &&
                        x.ContentId == contentIdValue &&
                        x.Feature == feature)
                .Select(CreateAccessTokenFromRow)
                .ToArray();
        }

        public void UpdateAccessToken(string tokenValue, DateTime newExpirationDate)
        {
            var row = AccessTokens.FirstOrDefault(x => x.Value == tokenValue && x.ExpirationDate > DateTime.UtcNow);
            if (row == null)
                throw new InvalidAccessTokenException("Token not found or it is expired.");
            row.ExpirationDate = newExpirationDate;
        }

        public void DeleteAccessToken(string tokenValue)
        {
            var rows = AccessTokens.Where(x => x.Value == tokenValue).ToArray();
            foreach (var row in rows)
                AccessTokens.Remove(row);
        }

        public void DeleteAccessTokensByUser(int userId)
        {
            var rows = AccessTokens.Where(x => x.UserId == userId).ToArray();
            foreach (var row in rows)
                AccessTokens.Remove(row);
        }

        public void DeleteAccessTokensByContent(int contentId)
        {
            var rows = AccessTokens.Where(x => x.ContentId == contentId).ToArray();
            foreach (var row in rows)
                AccessTokens.Remove(row);
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
