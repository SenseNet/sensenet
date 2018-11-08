using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Sharing
{
    public enum SharingLevel
    {
        Open,
        Edit
    }
    public enum SharingMode
    {
        Public,
        Authenticated,
        Private
    }
    /// <summary>
    /// Storage model of content sharing information.
    /// </summary>
    public class SharingData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Token { get; set; }
        public int Identity { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SharingMode Mode { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SharingLevel Level { get; set; }
        public int CreatorId { get; set; }
        public DateTime ShareDate { get; set; }
    }

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
