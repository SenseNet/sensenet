using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Represents a content sharing record.
    /// </summary>
    public class SharingData
    {
        // The JsonProperty attribute is necessary here to let 
        // JSON.Net deserialize SharingData objects.

        [JsonProperty]
        public string Id { get; internal set; } = Guid.NewGuid().ToString();
        [JsonProperty]
        public string Token { get; internal set; }
        [JsonProperty]
        public int Identity { get; internal set; }
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty]
        public SharingMode Mode { get; internal set; }
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty]
        public SharingLevel Level { get; internal set; }
        [JsonProperty]
        public int CreatorId { get; internal set; }
        [JsonProperty]
        public DateTime ShareDate { get; internal set; }
    }

    /// <summary>
    /// A special sharing data where the identity properties (Identity and CreatorId)
    /// contain the original content id only if the current user has permission for them.
    /// If not, the value will contain the id of the Somebody user.
    /// </summary>
    internal class SafeSharingData : SharingData
    {
        private SafeSharingData() {}

        public static SafeSharingData Create(SharingData sharingData)
        {
            // clone the shared data, but hide inaccessible identities
            return sharingData == null ? null : new SafeSharingData
            {
                Id = sharingData.Id,
                Token = sharingData.Token,
                Identity = GetSafeIdentity(sharingData.Identity),
                Mode = sharingData.Mode,
                Level = sharingData.Level,
                CreatorId = GetSafeIdentity(sharingData.CreatorId),
                ShareDate = sharingData.ShareDate
            };
        }

        private static int GetSafeIdentity(int identity)
        {
            if (identity == 0)
                return identity;

            return SecurityHandler.HasPermission(identity, PermissionType.Open)
                ? identity
                : Configuration.Identifiers.SomebodyUserId;
        }
    }
}
