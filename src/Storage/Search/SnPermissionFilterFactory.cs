using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;
using SenseNet.Security;

namespace SenseNet.ContentRepository.Storage.Search
{
    internal class SnPermissionFilterFactory : IPermissionFilterFactory
    {
        public IPermissionFilter Create(int userId)
        {
            throw new NotImplementedException();
        }

        public IPermissionFilter Create(SnQuery query, IQueryContext context)
        {
            return new PermissionChecker(query, context);
        }
    }
    internal class PermissionChecker : IPermissionFilter
    {
        private enum DocumentOpenLevel { Denied, See, Preview, Open, OpenMinor }

        #region  private class FieldNameVisitor : SnQueryVisitor
        private class FieldNameVisitor : SnQueryVisitor
        {
            private List<string> _fieldNames = new List<string>();
            public IEnumerable<string> FieldNames { get { return _fieldNames; } }

            public override SnQueryPredicate VisitRangePredicate(RangePredicate range)
            {
                var visitedField = range.FieldName;
                if (!_fieldNames.Contains(visitedField))
                    _fieldNames.Add(visitedField);
                return base.VisitRangePredicate(range);
            }

            public override SnQueryPredicate VisitTextPredicate(TextPredicate text)
            {
                var visitedField = text.FieldName;
                if (!_fieldNames.Contains(visitedField))
                    _fieldNames.Add(visitedField);
                return base.VisitTextPredicate(text);
            }
        }
        #endregion

        private readonly int _userId;
        private IUser _user;
        private readonly QueryFieldLevel _queryFieldLevel;
        private readonly bool _allVersions;

        [Obsolete("", true)]
        public PermissionChecker(IUser user, QueryFieldLevel queryFieldLevel, bool allVersions)
        {
            _userId = user.Id;
            _user = user;
            _queryFieldLevel = queryFieldLevel;
            _allVersions = allVersions;
        }
        public PermissionChecker(SnQuery query, IQueryContext context)
        {
            _userId = context.UserId;
            _user = Node.LoadNode(_userId) as IUser;
            _queryFieldLevel = GetFieldLevel(query);
            _allVersions = context.AllVersions;
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
            var userId = _userId;
            if (userId == -1)
                return DocumentOpenLevel.OpenMinor;
            if (userId < -1)
                return DocumentOpenLevel.Denied;

            List<int> identities;
            try
            {
                identities = SecurityHandler.GetIdentitiesByMembership(_user, nodeId);
            }
            catch (EntityNotFoundException)
            {
                return DocumentOpenLevel.Denied;
            }

            List<AceInfo> entries;
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

        // -----------------------------------------------------------------------

        private static readonly string[] HeadOnlyFields = Node.GetHeadOnlyProperties();

        private QueryFieldLevel GetFieldLevel(SnQuery query)
        {
            var v = new FieldNameVisitor();
            v.Visit(query.QueryTree);
            return GetFieldLevel(v.FieldNames);
        }
        internal static QueryFieldLevel GetFieldLevel(IEnumerable<string> fieldNames)
        {
            var fieldLevel = QueryFieldLevel.NotDefined;
            foreach (var fieldName in fieldNames)
            {
                var indexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(fieldName);
                var level = GetFieldLevel(fieldName, indexingInfo);
                fieldLevel = level > fieldLevel ? level : fieldLevel;
            }
            return fieldLevel;
        }
        internal static QueryFieldLevel GetFieldLevel(string fieldName, IPerFieldIndexingInfo indexingInfo)
        {
            QueryFieldLevel level;

            if (fieldName == IndexFieldName.AllText)
                level = QueryFieldLevel.BinaryOrFullText;
            else if (indexingInfo == null)
                level = QueryFieldLevel.BinaryOrFullText;
            else if (indexingInfo.FieldDataType == typeof(BinaryData))
                level = QueryFieldLevel.BinaryOrFullText;
            else if (fieldName == IndexFieldName.InFolder || fieldName == IndexFieldName.InTree
                || fieldName == IndexFieldName.Type || fieldName == IndexFieldName.TypeIs
                || HeadOnlyFields.Contains(fieldName))
                level = QueryFieldLevel.HeadOnly;
            else
                level = QueryFieldLevel.NoBinaryOrFullText;

            return level;
        }

    }

}
