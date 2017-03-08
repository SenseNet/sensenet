using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    public enum DocumentOpenLevel { Denied, See, Preview, Open, OpenMinor }
    public class PermissionChecker
    {
        private IUser _user;
        private QueryFieldLevel _queryFieldLevel;
        private bool _allVersions;

        public PermissionChecker(IUser user, QueryFieldLevel queryFieldLevel, bool allVersions)
        {
            _user = user;
            _queryFieldLevel = queryFieldLevel;
            _allVersions = allVersions;
        }

        public bool IsPermitted(int nodeId, bool isLastPublic, bool isLastDraft)
        {
            var docLevel = GetDocumentLevel(nodeId);

            // pre-check: do not do any other operation
            if (docLevel == DocumentOpenLevel.Denied)
                return false;

            if (_allVersions)
            {
                var canAccesOldVersions = SecurityHandler.HasPermission(nodeId, PermissionType.RecallOldVersion);
                switch (docLevel)
                {
                    case DocumentOpenLevel.See:
                        return isLastPublic && canAccesOldVersions && _queryFieldLevel <= QueryFieldLevel.HeadOnly;
                    case DocumentOpenLevel.Preview:
                        return isLastPublic && canAccesOldVersions && _queryFieldLevel <= QueryFieldLevel.NoBinaryOrFullText;
                    case DocumentOpenLevel.Open:
                        return isLastPublic;
                    case DocumentOpenLevel.OpenMinor:
                        return canAccesOldVersions;
                    case DocumentOpenLevel.Denied:
                        return false;
                    default:
                        throw new SnNotSupportedException("##Unknown DocumentOpenLevel");
                }
            }
            else
            {
                switch (docLevel)
                {
                    case DocumentOpenLevel.See:
                        return isLastPublic && _queryFieldLevel <= QueryFieldLevel.HeadOnly;
                    case DocumentOpenLevel.Preview:
                        return isLastPublic && _queryFieldLevel <= QueryFieldLevel.NoBinaryOrFullText;
                    case DocumentOpenLevel.Open:
                        return isLastPublic;
                    case DocumentOpenLevel.OpenMinor:
                        return isLastDraft;
                    case DocumentOpenLevel.Denied:
                        return false;
                    default:
                        throw new SnNotSupportedException("##Unknown DocumentOpenLevel");
                }
            }
        }
        private DocumentOpenLevel GetDocumentLevel(int nodeId)
        {
            var userId = _user.Id;
            if (userId == -1)
                return DocumentOpenLevel.OpenMinor;
            if (userId < -1)
                return DocumentOpenLevel.Denied;

            List<int> identities = null;
            try
            {
                identities = SecurityHandler.GetIdentitiesByMembership(_user, nodeId);
            }
            catch (EntityNotFoundException)
            {
                return DocumentOpenLevel.Denied;
            }

            List<AceInfo> entries = null;
            try
            {
                using (new SystemAccount())
                    entries = SecurityHandler.GetEffectiveEntries(nodeId);
            }
            catch (Exception ex) // LOGGED
            {
                //TODO: collect aggregated errors per query instead of logging every error
                SnLog.WriteWarning($"GetEffectiveEntries threw an exception for id {nodeId}. Error: {ex}");
                return DocumentOpenLevel.Denied;
            }

            var allowBits = 0UL;
            var denyBits = 0UL;
            foreach (var entry in entries)
            {
                if (identities.Contains(entry.IdentityId))
                {
                    allowBits |= entry.AllowBits;
                    denyBits |= entry.DenyBits;
                }
            }
            allowBits = allowBits & ~denyBits;
            var docLevel = DocumentOpenLevel.Denied;
            if ((allowBits & PermissionType.See.Mask) > 0)
                docLevel = DocumentOpenLevel.See;
            if ((allowBits & PermissionType.Preview.Mask) > 0)
                docLevel = DocumentOpenLevel.Preview;
            if ((allowBits & PermissionType.PreviewWithoutRedaction.Mask) > 0)
                docLevel = DocumentOpenLevel.Open;
            if ((allowBits & PermissionType.OpenMinor.Mask) > 0)
                docLevel = DocumentOpenLevel.OpenMinor;
            return docLevel;
        }

    }
}
