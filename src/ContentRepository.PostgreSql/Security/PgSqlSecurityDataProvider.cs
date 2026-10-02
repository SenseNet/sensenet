using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.Diagnostics;
using SenseNet.Security;
using SenseNet.Security.Messaging;
using SenseNet.Security.Messaging.SecurityMessages;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient.Security
{
    /// <summary>
    /// PostgreSQL implementation of <see cref="ISecurityDataProvider"/>.
    /// Replaces the SQL Server-only EFCSecurityDataProvider.
    /// </summary>
    public class PgSqlSecurityDataProvider : ISecurityDataProvider
    {
        private readonly PgSqlSecurityDataOptions _options;
        private readonly ILogger<PgSqlSecurityDataProvider> _logger;
        private readonly IMessageSenderManager _messageSenderManager;
        private readonly IRetrier _retrier;

        public PgSqlSecurityDataProvider(
            IMessageSenderManager messageSenderManager,
            IRetrier retrier,
            IOptions<PgSqlSecurityDataOptions> options,
            ILogger<PgSqlSecurityDataProvider> logger)
        {
            _messageSenderManager = messageSenderManager;
            _retrier = retrier;
            _options = options?.Value ?? new PgSqlSecurityDataOptions();
            _logger = logger;

            if (string.IsNullOrEmpty(_options.ConnectionString))
                _logger.LogError("No connection string was configured for the PostgreSQL security database.");
        }

        internal PgSqlSecurityStorage Db()
        {
            return new PgSqlSecurityStorage(ConnectionString, CommandTimeout);
        }

        private int CommandTimeout => _options.SqlCommandTimeout;

        public string ConnectionString
        {
            get => _options.ConnectionString;
            set => _options.ConnectionString = value;
        }

        public IActivitySerializer ActivitySerializer { get; set; }

        // ===================================================================== Database Setup

        public void InstallDatabase()
        {
            // Tables are created by PgSqlDataInstaller via the schema SQL script.
            // No additional action needed here.
        }

        public async Task<bool> IsDatabaseReadyAsync(CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: IsDatabaseReady()");

            const string schemaCheckSql = @"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_name = 'EFEntries' AND table_schema = 'public'
) THEN TRUE ELSE FALSE END";

            var result = false;
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var conn = db.Database.GetDbConnection();
                try
                {
                    await conn.OpenAsync(cancel).ConfigureAwait(false);
                    var cmd = conn.CreateCommand();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = schemaCheckSql;
                    var dbResult = await cmd.ExecuteScalarAsync(cancel).ConfigureAwait(false);
                    result = Convert.ToBoolean(dbResult);
                }
                catch (Exception ex)
                {
                    _logger.LogTrace("Error when accessing the database: {Message}", ex.Message);
                }
                finally
                {
                    await conn.CloseAsync().ConfigureAwait(false);
                }
            }
            op.Successful = true;
            return result;
        }

        // ===================================================================== Security Entities

        [Obsolete("Use async version instead.")]
        public IEnumerable<StoredSecurityEntity> LoadSecurityEntities()
        {
            return LoadSecurityEntitiesAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<StoredSecurityEntity>> LoadSecurityEntitiesAsync(CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: LoadSecurityEntities()");

            IEnumerable<StoredSecurityEntity> result;
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                result = await db.EFEntities.AsNoTracking().Select(x => new StoredSecurityEntity
                {
                    Id = x.Id,
                    nullableOwnerId = x.OwnerId,
                    nullableParentId = x.ParentId,
                    IsInherited = x.IsInherited
                }).ToArrayAsync(cancel).ConfigureAwait(false);
            }
            op.Successful = true;
            return result;
        }

        public IEnumerable<int> LoadAffectedEntityIdsByEntriesAndBreaks()
        {
            return LoadAffectedEntityIdsByEntriesAndBreaksAsync(CancellationToken.None)
                .GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<int>> LoadAffectedEntityIdsByEntriesAndBreaksAsync(CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: LoadAffectedEntityIdsByEntriesAndBreaks()");

            IEnumerable<int> result;
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                // Get entity IDs that have explicit entries or broken inheritance
                var entryEntityIds = await db.EFEntries
                    .Select(e => e.EFEntityId).Distinct()
                    .ToArrayAsync(cancel).ConfigureAwait(false);

                var brokenInheritanceIds = await db.EFEntities
                    .Where(e => !e.IsInherited)
                    .Select(e => e.Id)
                    .ToArrayAsync(cancel).ConfigureAwait(false);

                result = entryEntityIds.Union(brokenInheritanceIds).Distinct().ToArray();
            }
            op.Successful = true;
            return result;
        }

        public IEnumerable<StoredAce> LoadAllAces()
        {
            using var db = Db();
            foreach (var entry in db.EFEntries.AsNoTracking())
            {
                yield return new StoredAce
                {
                    EntityId = entry.EFEntityId,
                    EntryType = (EntryType)entry.EntryType,
                    IdentityId = entry.IdentityId,
                    LocalOnly = entry.LocalOnly,
                    AllowBits = (ulong)entry.AllowBits,
                    DenyBits = (ulong)entry.DenyBits
                };
            }
        }

        [Obsolete("Use async version instead.")]
        public StoredSecurityEntity LoadStoredSecurityEntity(int entityId)
        {
            return LoadStoredSecurityEntityAsync(entityId, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<StoredSecurityEntity> LoadStoredSecurityEntityAsync(int entityId, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: LoadStoredSecurityEntityAsync(entityId: {0})", entityId);

            var result = await RetryAsync(async () =>
            {
                var db = Db();
                await using (db.ConfigureAwait(false))
                {
                    var entity = await db.EFEntities
                        .AsNoTracking()
                        .Where(e => e.Id == entityId)
                        .Select(e => new
                        {
                            e.Id,
                            e.OwnerId,
                            e.ParentId,
                            e.IsInherited,
                            HasExplicitEntry = db.EFEntries.Any(en => en.EFEntityId == e.Id)
                        })
                        .FirstOrDefaultAsync(cancel).ConfigureAwait(false);

                    if (entity == null) return null;

                    return new StoredSecurityEntity
                    {
                        Id = entity.Id,
                        nullableOwnerId = entity.OwnerId,
                        nullableParentId = entity.ParentId,
                        IsInherited = entity.IsInherited,
                        HasExplicitEntry = entity.HasExplicitEntry
                    };
                }
            }, cancel).ConfigureAwait(false);

            op.Successful = true;
            return result;
        }

        [Obsolete("Use async version instead.")]
        public void InsertSecurityEntity(StoredSecurityEntity entity)
        {
            InsertSecurityEntityAsync(entity, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task InsertSecurityEntityAsync(StoredSecurityEntity entity, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: InsertSecurityEntity. Id: {0}, ParentId: {1}",
                entity.Id, entity.ParentId);

            await RetryAsync(async () =>
            {
                var db = Db();
                await using (db.ConfigureAwait(false))
                {
                    var existing = await db.EFEntities
                        .FirstOrDefaultAsync(x => x.Id == entity.Id, cancel).ConfigureAwait(false);
                    if (existing != null) return;

                    db.EFEntities.Add(new PgSqlEFEntity
                    {
                        Id = entity.Id,
                        OwnerId = entity.nullableOwnerId,
                        ParentId = entity.nullableParentId,
                        IsInherited = entity.IsInherited
                    });

                    try
                    {
                        await db.SaveChangesAsync(cancel).ConfigureAwait(false);
                    }
                    catch (DbUpdateException)
                    {
                        // entity already exists, that's ok
                    }
                }
            }, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        [Obsolete("Use async version instead.")]
        public void UpdateSecurityEntity(StoredSecurityEntity entity)
        {
            UpdateSecurityEntityAsync(entity, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task UpdateSecurityEntityAsync(StoredSecurityEntity entity, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: UpdateSecurityEntity. Id: {0}, ParentId: {1}",
                entity.Id, entity.ParentId);

            var exceptions = new List<Exception>();
            for (var retry = 3; retry > 0; retry--)
            {
                try
                {
                    var db = Db();
                    await using (db.ConfigureAwait(false))
                    {
                        var oldEntity = await db.EFEntities
                            .FirstOrDefaultAsync(x => x.Id == entity.Id, cancel).ConfigureAwait(false);
                        if (oldEntity == null)
                            throw new EntityNotFoundException("Cannot update entity because it does not exist: " + entity.Id);

                        oldEntity.OwnerId = entity.nullableOwnerId;
                        oldEntity.ParentId = entity.nullableParentId;
                        oldEntity.IsInherited = entity.IsInherited;

                        await db.SaveChangesAsync(cancel).ConfigureAwait(false);
                        return;
                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    exceptions.Add(ex);
                    if (retry > 1) await System.Threading.Tasks.Task.Delay(10, cancel).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    break;
                }
            }

            if (exceptions.Count > 0)
                throw new SecurityStructureException(
                    "Cannot update entity because of concurrency: " + entity.Id,
                    new AggregateException(exceptions));

            op.Successful = true;
        }

        [Obsolete("Use async version instead.")]
        public void DeleteSecurityEntity(int entityId)
        {
            DeleteSecurityEntityAsync(entityId, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task DeleteSecurityEntityAsync(int entityId, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: DeleteSecurityEntity(entityId: {0})", entityId);

            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var oldEntity = await db.EFEntities
                    .FirstOrDefaultAsync(x => x.Id == entityId, cancel).ConfigureAwait(false);
                if (oldEntity == null) return;

                db.EFEntities.Remove(oldEntity);
                try
                {
                    await db.SaveChangesAsync(cancel).ConfigureAwait(false);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // already deleted
                }
                catch (Exception ex)
                {
                    throw new SecurityStructureException(
                        "Cannot delete entity because of a database error: " + entityId, ex);
                }
            }
            op.Successful = true;
        }

        [Obsolete("Use async version instead.")]
        public void MoveSecurityEntity(int sourceId, int targetId)
        {
            MoveSecurityEntityAsync(sourceId, targetId, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task MoveSecurityEntityAsync(int sourceId, int targetId, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: MoveSecurityEntity(sourceId: {0}, targetId: {1})", sourceId, targetId);

            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var source = await db.EFEntities
                    .FirstOrDefaultAsync(x => x.Id == sourceId, cancel).ConfigureAwait(false);
                if (source == null)
                    throw new EntityNotFoundException("Cannot execute the move operation because source does not exist: " + sourceId);

                var target = await db.EFEntities
                    .FirstOrDefaultAsync(x => x.Id == targetId, cancel).ConfigureAwait(false);
                if (target == null)
                    throw new EntityNotFoundException("Cannot execute the move operation because target does not exist: " + targetId);

                source.ParentId = target.Id;
                await db.SaveChangesAsync(cancel).ConfigureAwait(false);
            }
            op.Successful = true;
        }

        // ===================================================================== Permission Entries

        [Obsolete("Use async version instead.")]
        public IEnumerable<StoredAce> LoadAllPermissionEntries()
        {
            return LoadAllPermissionEntriesAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<StoredAce>> LoadAllPermissionEntriesAsync(CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: LoadAllPermissionEntries()");

            IEnumerable<StoredAce> result;
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var dbResult = await db.EFEntries.ToArrayAsync(cancel).ConfigureAwait(false);
                result = dbResult.Select(a => new StoredAce
                {
                    EntityId = a.EFEntityId,
                    EntryType = (EntryType)a.EntryType,
                    IdentityId = a.IdentityId,
                    LocalOnly = a.LocalOnly,
                    AllowBits = (ulong)a.AllowBits,
                    DenyBits = (ulong)a.DenyBits
                }).ToArray();
            }
            op.Successful = true;
            return result;
        }

        [Obsolete("Use async version instead.")]
        public IEnumerable<StoredAce> LoadPermissionEntries(IEnumerable<int> entityIds)
        {
            return LoadPermissionEntriesAsync(entityIds, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<StoredAce>> LoadPermissionEntriesAsync(
            IEnumerable<int> entityIds, CancellationToken cancel)
        {
            var entityIdArray = entityIds as int[] ?? entityIds.ToArray();

            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: LoadPermissionEntries(entityIds: {0})",
                string.Join(", ", entityIdArray.Select(x => x.ToString())));

            var result = await RetryAsync(async () =>
            {
                var db = Db();
                await using (db.ConfigureAwait(false))
                {
                    var dbResult = await db.EFEntries
                        .Where(x => entityIdArray.Contains(x.EFEntityId))
                        .ToArrayAsync(cancel).ConfigureAwait(false);

                    return dbResult.Select(a => new StoredAce
                    {
                        EntityId = a.EFEntityId,
                        EntryType = (EntryType)a.EntryType,
                        IdentityId = a.IdentityId,
                        LocalOnly = a.LocalOnly,
                        AllowBits = (ulong)a.AllowBits,
                        DenyBits = (ulong)a.DenyBits
                    }).ToArray();
                }
            }, cancel).ConfigureAwait(false);
            op.Successful = true;
            return result;
        }

        [Obsolete("Use async version instead.")]
        public int GetEstimatedEntityCount()
        {
            return GetEstimatedEntityCountAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<int> GetEstimatedEntityCountAsync(CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: GetEstimatedEntityCount()");

            int result;
            var db = Db();
            await using (db.ConfigureAwait(false))
                result = await db.EFEntities.CountAsync(cancel).ConfigureAwait(false);
            op.Successful = true;
            return result;
        }

        [Obsolete("Use async version instead.")]
        public void WritePermissionEntries(IEnumerable<StoredAce> aces)
        {
            WritePermissionEntriesAsync(aces, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task WritePermissionEntriesAsync(IEnumerable<StoredAce> aces, CancellationToken cancel)
        {
            var storedAces = aces as StoredAce[] ?? aces.ToArray();

            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: WritePermissionEntries. Count: {0}", storedAces.Length);

            try
            {
                var db = Db();
                await using (db.ConfigureAwait(false))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("BEGIN;");

                    foreach (var ace in storedAces)
                        sb.AppendFormat(
                            "DELETE FROM \"EFEntries\" WHERE \"EFEntityId\" = {0} AND \"EntryType\" = {1} AND \"IdentityId\" = {2} AND \"LocalOnly\" = {3};\n",
                            ace.EntityId, (int)ace.EntryType, ace.IdentityId, ace.LocalOnly ? "true" : "false");

                    foreach (var ace in storedAces)
                        sb.AppendFormat(
                            "INSERT INTO \"EFEntries\" (\"EFEntityId\", \"EntryType\", \"IdentityId\", \"LocalOnly\", \"AllowBits\", \"DenyBits\") VALUES ({0}, {1}, {2}, {3}, {4}, {5});\n",
                            ace.EntityId, (int)ace.EntryType, ace.IdentityId, ace.LocalOnly ? "true" : "false",
                            (long)ace.AllowBits, (long)ace.DenyBits);

                    sb.AppendLine("COMMIT;");

                    await db.Database.ExecuteSqlRawAsync(sb.ToString(), cancel).ConfigureAwait(false);
                }
            }
            catch (Npgsql.PostgresException ex)
            {
                var message = ex.Message.Contains("foreign key constraint")
                    ? "Cannot write permission entries because one of the entities is missing from the database. " +
                      string.Join(",", storedAces.Select(a => a.EntityId).Distinct().OrderBy(ei => ei))
                    : "Cannot write permission entries because of a database error.";

                throw new SecurityStructureException(message, ex);
            }

            op.Successful = true;
        }

        [Obsolete("Use async version instead.")]
        public void RemovePermissionEntries(IEnumerable<StoredAce> aces)
        {
            RemovePermissionEntriesAsync(aces, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task RemovePermissionEntriesAsync(IEnumerable<StoredAce> aces, CancellationToken cancel)
        {
            var storedAces = aces as StoredAce[] ?? aces.ToArray();
            if (storedAces.Length == 0)
                return;

            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: RemovePermissionEntries. Count: {0}", storedAces.Length);

            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var sb = new StringBuilder();
                if (storedAces.Length > 1)
                    sb.AppendLine("BEGIN;");

                foreach (var ace in storedAces)
                    sb.AppendFormat(
                        "DELETE FROM \"EFEntries\" WHERE \"EFEntityId\" = {0} AND \"EntryType\" = {1} AND \"IdentityId\" = {2} AND \"LocalOnly\" = {3};\n",
                        ace.EntityId, (int)ace.EntryType, ace.IdentityId, ace.LocalOnly ? "true" : "false");

                if (storedAces.Length > 1)
                    sb.AppendLine("COMMIT;");

                await db.Database.ExecuteSqlRawAsync(sb.ToString(), cancel).ConfigureAwait(false);
            }
            op.Successful = true;
        }

        [Obsolete("Use async version instead.")]
        public void RemovePermissionEntriesByEntity(int entityId)
        {
            RemovePermissionEntriesByEntityAsync(entityId, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task RemovePermissionEntriesByEntityAsync(int entityId, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: RemovePermissionEntriesByEntity(entityId: {0})", entityId);

            var db = Db();
            await using (db.ConfigureAwait(false))
                await db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM \"EFEntries\" WHERE \"EFEntityId\" = {0}", new object[] { entityId }, cancel)
                    .ConfigureAwait(false);
            op.Successful = true;
        }

        [Obsolete("Use async version instead.")]
        public void DeleteEntitiesAndEntries(int entityId)
        {
            DeleteEntitiesAndEntriesAsync(entityId, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task DeleteEntitiesAndEntriesAsync(int entityId, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: DeleteEntitiesAndEntries(entityId: {0})", entityId);

            const string script = @"
WITH RECURSIVE ""EntityCTE"" AS (
    SELECT ""Id"", ""ParentId"" FROM ""EFEntities"" WHERE ""Id"" = {0}
    UNION ALL
    SELECT E.""Id"", E.""ParentId"" FROM ""EFEntities"" E
        INNER JOIN ""EntityCTE"" ON E.""ParentId"" = ""EntityCTE"".""Id""
)
DELETE FROM ""EFEntries"" WHERE ""EFEntityId"" IN (SELECT ""Id"" FROM ""EntityCTE"");

WITH RECURSIVE ""EntityCTE"" AS (
    SELECT ""Id"", ""ParentId"" FROM ""EFEntities"" WHERE ""Id"" = {0}
    UNION ALL
    SELECT E.""Id"", E.""ParentId"" FROM ""EFEntities"" E
        INNER JOIN ""EntityCTE"" ON E.""ParentId"" = ""EntityCTE"".""Id""
)
DELETE FROM ""EFEntities"" WHERE ""Id"" IN (SELECT ""Id"" FROM ""EntityCTE"");
";
            var db = Db();
            await using (db.ConfigureAwait(false))
                await db.Database.ExecuteSqlRawAsync(script, new object[] { entityId }, cancel)
                    .ConfigureAwait(false);
            op.Successful = true;
        }

        // ===================================================================== Groups & Membership

        [Obsolete("Use async version instead.")]
        public IEnumerable<SecurityGroup> LoadAllGroups()
        {
            return LoadAllGroupsAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<SecurityGroup>> LoadAllGroupsAsync(CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: LoadAllGroupsAsync()");

            var groups = new Dictionary<int, SecurityGroup>();
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var memberships = await db.EFMemberships.AsNoTracking()
                    .ToArrayAsync(cancel).ConfigureAwait(false);

                foreach (var membership in memberships)
                {
                    var group = EnsureGroup(membership.GroupId, groups);
                    if (membership.IsUser)
                    {
                        group.UserMemberIds.Add(membership.MemberId);
                    }
                    else
                    {
                        var memberGroup = EnsureGroup(membership.MemberId, groups);
                        group.Groups.Add(memberGroup);
                        memberGroup.ParentGroups.Add(group);
                    }
                }
            }
            op.Successful = true;
            return groups.Values;
        }

        [Obsolete("Use async version instead.")]
        public SecurityGroup LoadSecurityGroup(int groupId)
        {
            return LoadSecurityGroupAsync(groupId, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<SecurityGroup> LoadSecurityGroupAsync(int groupId, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: LoadSecurityGroup(groupId: {0})", groupId);

            var group = new SecurityGroup(groupId);
            var groups = new Dictionary<int, SecurityGroup> { { group.Id, group } };
            var rows = 0;
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                foreach (var membership in await db.EFMemberships
                    .Where(x => x.GroupId == groupId)
                    .ToArrayAsync(cancel).ConfigureAwait(false))
                {
                    rows++;
                    if (membership.IsUser)
                    {
                        group.UserMemberIds.Add(membership.MemberId);
                    }
                    else
                    {
                        var memberGroup = EnsureGroup(membership.MemberId, groups);
                        group.Groups.Add(memberGroup);
                        memberGroup.ParentGroups.Add(group);
                    }
                }
            }
            op.Successful = true;
            return rows == 0 ? null : group;
        }

        public void QueryGroupRelatedEntities(int groupId, out IEnumerable<int> entityIds, out IEnumerable<int> exclusiveEntityIds)
        {
            var result = QueryGroupRelatedEntitiesAsync(groupId, CancellationToken.None).GetAwaiter().GetResult();
            entityIds = result.EntityIds;
            exclusiveEntityIds = result.ExclusiveEntityIds;
        }

        public async Task<GroupRelatedEntitiesQueryResult> QueryGroupRelatedEntitiesAsync(int groupId, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: QueryGroupRelatedEntities(groupId: {0})", groupId);

            var exclusiveEntityIds = new List<int>();
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var entityIds = await db.EFEntries
                    .Where(x => x.IdentityId == groupId)
                    .Select(x => x.EFEntityId).Distinct()
                    .ToArrayAsync(cancel).ConfigureAwait(false);

                foreach (var relatedEntityId in entityIds)
                {
                    var aces = await db.EFEntries
                        .Where(x => x.EFEntityId == relatedEntityId)
                        .ToArrayAsync(cancel).ConfigureAwait(false);
                    var groupRelatedCount = aces.Count(x => x.IdentityId == groupId);
                    if (aces.Length == groupRelatedCount)
                        exclusiveEntityIds.Add(relatedEntityId);
                }

                op.Successful = true;
                return new GroupRelatedEntitiesQueryResult
                {
                    EntityIds = entityIds,
                    ExclusiveEntityIds = exclusiveEntityIds
                };
            }
        }

        [Obsolete("Use async version instead.")]
        public void DeleteIdentityAndRelatedEntries(int identityId)
        {
            DeleteIdentityAndRelatedEntriesAsync(identityId, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task DeleteIdentityAndRelatedEntriesAsync(int identityId, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: DeleteIdentityAndRelatedEntries(identityId: {0})", identityId);

            const string script = @"
DELETE FROM ""EFMemberships"" WHERE ""GroupId"" = {0} OR ""MemberId"" = {0};
DELETE FROM ""EFEntries"" WHERE ""IdentityId"" = {0};";

            var db = Db();
            await using (db.ConfigureAwait(false))
                await db.Database.ExecuteSqlRawAsync(script, new object[] { identityId }, cancel)
                    .ConfigureAwait(false);
            op.Successful = true;
        }

        [Obsolete("Use async version instead.")]
        public void DeleteIdentitiesAndRelatedEntries(IEnumerable<int> ids)
        {
            DeleteIdentitiesAndRelatedEntriesAsync(ids, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task DeleteIdentitiesAndRelatedEntriesAsync(IEnumerable<int> ids, CancellationToken cancel)
        {
            var idArray = ids as int[] ?? ids.ToArray();

            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: DeleteIdentitiesAndRelatedEntries(ids: {0})",
                string.Join(", ", idArray.Select(x => x.ToString())));

            if (idArray.Length == 0) return;

            var idList = string.Join(", ", idArray);
            var script = $@"
BEGIN;
DELETE FROM ""EFEntries"" WHERE ""IdentityId"" IN ({idList});
DELETE FROM ""EFMemberships"" WHERE ""GroupId"" IN ({idList}) OR ""MemberId"" IN ({idList});
COMMIT;";

            var db = Db();
            await using (db.ConfigureAwait(false))
                await db.Database.ExecuteSqlRawAsync(script, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        [Obsolete("Use async version instead.")]
        public void AddMembers(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers)
        {
            AddMembersAsync(groupId, userMembers, groupMembers, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task AddMembersAsync(int groupId, IEnumerable<int> userMembers,
            IEnumerable<int> groupMembers, CancellationToken cancel)
        {
            groupMembers ??= Array.Empty<int>();
            userMembers ??= Array.Empty<int>();

            var groupArray = groupMembers as int[] ?? groupMembers.ToArray();
            var userArray = userMembers as int[] ?? userMembers.ToArray();

            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: AddMembers(groupId: {0}, userMembers: [{1}], groupMembers: [{2}])",
                groupId,
                string.Join(", ", userArray.Select(x => x.ToString())),
                string.Join(", ", groupArray.Select(x => x.ToString())));

            var allNewMembers = groupArray.Union(userArray);
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var origMemberIds = await db.EFMemberships
                    .Where(m => m.GroupId == groupId && allNewMembers.Contains(m.MemberId))
                    .Select(m => m.MemberId)
                    .ToArrayAsync(cancel).ConfigureAwait(false);

                var newGroupIds = groupArray.Except(origMemberIds).ToArray();
                var newUserIds = userArray.Except(origMemberIds).ToArray();

                db.EFMemberships.AddRange(
                    newGroupIds.Select(g => new PgSqlEFMembership { GroupId = groupId, MemberId = g, IsUser = false }));
                db.EFMemberships.AddRange(
                    newUserIds.Select(g => new PgSqlEFMembership { GroupId = groupId, MemberId = g, IsUser = true }));

                await db.SaveChangesAsync(cancel).ConfigureAwait(false);
            }
            op.Successful = true;
        }

        [Obsolete("Use async version instead.")]
        public void RemoveMembers(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers)
        {
            RemoveMembersAsync(groupId, userMembers, groupMembers, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task RemoveMembersAsync(int groupId, IEnumerable<int> userMembers,
            IEnumerable<int> groupMembers, CancellationToken cancel)
        {
            groupMembers ??= Array.Empty<int>();
            userMembers ??= Array.Empty<int>();

            var groupArray = groupMembers as int[] ?? groupMembers.ToArray();
            var userArray = userMembers as int[] ?? userMembers.ToArray();

            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: RemoveMembers(groupId: {0}, userMembers: [{1}], groupMembers: [{2}])",
                groupId,
                string.Join(", ", userArray.Select(x => x.ToString())),
                string.Join(", ", groupArray.Select(x => x.ToString())));

            var memberIds = string.Join(", ", groupArray.Union(userArray));
            if (string.IsNullOrEmpty(memberIds)) return;

            var db = Db();
            await using (db.ConfigureAwait(false))
                await db.Database.ExecuteSqlRawAsync(
                    $"DELETE FROM \"EFMemberships\" WHERE \"GroupId\" = {{0}} AND \"MemberId\" IN ({memberIds})",
                    new object[] { groupId }, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        // ===================================================================== Security Activities

        [Obsolete("Use async version instead.")]
        public int GetLastSecurityActivityId()
        {
            return GetLastSecurityActivityIdAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<int> GetLastSecurityActivityIdAsync(CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: GetLastSecurityActivityId()");

            int result;
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var lastMsg = await db.EFMessages
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefaultAsync(cancel).ConfigureAwait(false);
                result = lastMsg?.Id ?? 0;
            }
            op.Successful = true;
            return result;
        }

        [Obsolete("Use async version instead.")]
        public int[] GetUnprocessedActivityIds()
        {
            return GetUnprocessedActivityIdsAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<int[]> GetUnprocessedActivityIdsAsync(CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: GetUnprocessedActivityIds()");

            int[] result;
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                // Get unprocessed activity IDs + the current sequence value
                var unprocessed = await db.EFMessages
                    .Where(x => x.ExecutionState != "Done")
                    .Select(x => x.Id)
                    .ToArrayAsync(cancel).ConfigureAwait(false);

                var maxId = await db.EFMessages
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancel).ConfigureAwait(false);

                result = unprocessed.Append(maxId).OrderBy(x => x).ToArray();
            }
            op.Successful = true;
            return result;
        }

        [Obsolete("Use async version instead.")]
        public SecurityActivity[] LoadSecurityActivities(int from, int to, int count, bool executingUnprocessedActivities)
        {
            return LoadSecurityActivitiesAsync(from, to, count, executingUnprocessedActivities, CancellationToken.None)
                .GetAwaiter().GetResult();
        }

        public async Task<SecurityActivity[]> LoadSecurityActivitiesAsync(int from, int to, int count,
            bool executingUnprocessedActivities, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: LoadSecurityActivities(from: {0}, to: {1}, count: {2})", from, to, count);

            var result = await RetryAsync(async () =>
            {
                var activities = new List<SecurityActivity>();
                var db = Db();
                await using (db.ConfigureAwait(false))
                {
                    var items = await db.EFMessages
                        .Where(x => x.Id >= from && x.Id <= to)
                        .OrderBy(x => x.Id)
                        .Take(count)
                        .ToArrayAsync(cancel).ConfigureAwait(false);

                    foreach (var item in items)
                    {
                        var activity = ActivitySerializer.DeserializeActivity(item.Body);
                        if (activity == null) continue;
                        activity.Id = item.Id;
                        activity.FromDatabase = true;
                        activity.IsUnprocessedActivity = executingUnprocessedActivities;
                        activities.Add(activity);
                    }
                }
                return activities.ToArray();
            }, cancel).ConfigureAwait(false);
            op.Successful = true;
            return result;
        }

        [Obsolete("Use async version instead.")]
        public SecurityActivity[] LoadSecurityActivities(int[] gaps, bool executingUnprocessedActivities)
        {
            return LoadSecurityActivitiesAsync(gaps, executingUnprocessedActivities, CancellationToken.None)
                .GetAwaiter().GetResult();
        }

        public async Task<SecurityActivity[]> LoadSecurityActivitiesAsync(int[] gaps,
            bool executingUnprocessedActivities, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: LoadSecurityActivities(gaps: [{0}])",
                gaps.Length > 20 ? $"count: {gaps.Length}" : string.Join(", ", gaps.Select(x => x.ToString())));

            var result = await RetryAsync(async () =>
            {
                var activities = new List<SecurityActivity>();
                var db = Db();
                await using (db.ConfigureAwait(false))
                {
                    var items = await db.EFMessages
                        .Where(x => gaps.Contains(x.Id))
                        .OrderBy(x => x.Id)
                        .ToArrayAsync(cancel).ConfigureAwait(false);

                    foreach (var item in items)
                    {
                        var activity = ActivitySerializer.DeserializeActivity(item.Body);
                        if (activity == null) continue;
                        activity.Id = item.Id;
                        activity.FromDatabase = true;
                        activity.IsUnprocessedActivity = executingUnprocessedActivities;
                        activities.Add(activity);
                    }
                }
                return activities.ToArray();
            }, cancel).ConfigureAwait(false);
            op.Successful = true;
            return result;
        }

        [Obsolete("Use async version instead.")]
        public SecurityActivity LoadSecurityActivity(int id)
        {
            return LoadSecurityActivityAsync(id, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<SecurityActivity> LoadSecurityActivityAsync(int id, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: LoadSecurityActivity(id: {0})", id);

            SecurityActivity result = null;
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var msg = await db.EFMessages
                    .FirstOrDefaultAsync(x => x.Id == id, cancel).ConfigureAwait(false);

                if (msg != null)
                {
                    result = ActivitySerializer.DeserializeActivity(msg.Body);
                    result.Id = msg.Id;
                }
            }
            op.Successful = true;
            return result;
        }

        [Obsolete("Use async version instead.")]
        public int SaveSecurityActivity(SecurityActivity activity, out int bodySize)
        {
            var result = SaveSecurityActivityAsync(activity, CancellationToken.None).GetAwaiter().GetResult();
            bodySize = result.BodySize;
            return result.ActivityId;
        }

        public async Task<SaveSecurityActivityResult> SaveSecurityActivityAsync(SecurityActivity activity, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: SaveSecurityActivity. Id: {0}, TypeName: {1}",
                activity.Id, activity.TypeName);

            var body = ActivitySerializer.SerializeActivity(activity);

            var result = await RetryAsync(async () =>
            {
                var db = Db();
                await using (db.ConfigureAwait(false))
                {
                    var dbEntry = db.EFMessages.Add(new PgSqlEFMessage
                    {
                        ExecutionState = "Wait",
                        SavedBy = _messageSenderManager.InstanceId,
                        SavedAt = DateTime.UtcNow,
                        Body = body
                    });
                    await db.SaveChangesAsync(cancel).ConfigureAwait(false);
                    return new SaveSecurityActivityResult { ActivityId = dbEntry.Entity.Id, BodySize = body.Length };
                }
            }, cancel).ConfigureAwait(false);

            op.Successful = true;
            return result;
        }

        [Obsolete("Use async version instead.")]
        public void CleanupSecurityActivities(int timeLimitInMinutes)
        {
            CleanupSecurityActivitiesAsync(timeLimitInMinutes, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task CleanupSecurityActivitiesAsync(int timeLimitInMinutes, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: CleanupSecurityActivities(timeLimitInMinutes: {0})", timeLimitInMinutes);

            var db = Db();
            await using (db.ConfigureAwait(false))
                await db.Database.ExecuteSqlRawAsync(
                    $"DELETE FROM \"EFMessages\" WHERE \"SavedAt\" < NOW() AT TIME ZONE 'UTC' - ('{timeLimitInMinutes} minutes')::INTERVAL AND \"ExecutionState\" = 'Done'",
                    cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        // ===================================================================== Activity Execution Locks

        [Obsolete("Use async version instead.")]
        public SecurityActivityExecutionLock AcquireSecurityActivityExecutionLock(
            SecurityActivity securityActivity, int timeoutInSeconds)
        {
            return AcquireSecurityActivityExecutionLockAsync(securityActivity, timeoutInSeconds, CancellationToken.None)
                .GetAwaiter().GetResult();
        }

        public async Task<SecurityActivityExecutionLock> AcquireSecurityActivityExecutionLockAsync(
            SecurityActivity securityActivity, int timeoutInSeconds, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: AcquireSecurityActivityExecutionLock. Id: {0}, TypeName: {1}, timeoutInSeconds: {2}",
                securityActivity.Id, securityActivity.TypeName, timeoutInSeconds);

            var maxTime = timeoutInSeconds == int.MaxValue
                ? DateTime.MaxValue
                : DateTime.UtcNow.AddSeconds(timeoutInSeconds);

            while (DateTime.UtcNow < maxTime)
            {
                var lockResult = await RetryAsync(async () =>
                {
                    var db = Db();
                    await using (db.ConfigureAwait(false))
                    {
                        // Try to acquire the lock
                        var affected = await db.Database.ExecuteSqlRawAsync(
                            "UPDATE \"EFMessages\" SET \"ExecutionState\" = {0}, \"LockedBy\" = {1}, \"LockedAt\" = NOW() AT TIME ZONE 'UTC' " +
                            "WHERE \"Id\" = {2} AND (\"ExecutionState\" = 'Wait' OR (\"ExecutionState\" = 'Executing' AND \"LockedAt\" < NOW() AT TIME ZONE 'UTC' - ({3} || ' seconds')::INTERVAL))",
                            new object[] { "Executing", _messageSenderManager.InstanceId ?? "", securityActivity.Id, timeoutInSeconds.ToString() },
                            cancel).ConfigureAwait(false);

                        if (affected > 0)
                            return "LockedForYou";

                        // Check current state
                        var msg = await db.EFMessages
                            .Where(x => x.Id == securityActivity.Id)
                            .Select(x => x.ExecutionState)
                            .FirstOrDefaultAsync(cancel).ConfigureAwait(false);

                        return msg ?? "";
                    }
                }, cancel).ConfigureAwait(false);

                switch (lockResult)
                {
                    case "LockedForYou":
                        op.Successful = true;
                        return new SecurityActivityExecutionLock(securityActivity, this, true);
                    case "Executing":
                    case "Done":
                        op.Successful = true;
                        return new SecurityActivityExecutionLock(securityActivity, this, false);
                }
            }

            op.Successful = true;
            throw new SecurityActivityTimeoutException(
                $"Waiting for a SecurityActivityExecutionLock timed out: #{securityActivity.Id}/{securityActivity.TypeName}");
        }

        [Obsolete("Use async version instead.")]
        public void RefreshSecurityActivityExecutionLock(SecurityActivity securityActivity)
        {
            RefreshSecurityActivityExecutionLockAsync(securityActivity, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task RefreshSecurityActivityExecutionLockAsync(SecurityActivity securityActivity, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: RefreshSecurityActivityExecutionLock. Id: {0}", securityActivity.Id);

            var db = Db();
            await using (db.ConfigureAwait(false))
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE \"EFMessages\" SET \"LockedAt\" = NOW() AT TIME ZONE 'UTC' WHERE \"Id\" = {0}",
                    new object[] { securityActivity.Id }, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        [Obsolete("Use async version instead.")]
        public void ReleaseSecurityActivityExecutionLock(SecurityActivity securityActivity)
        {
            ReleaseSecurityActivityExecutionLockAsync(securityActivity, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async System.Threading.Tasks.Task ReleaseSecurityActivityExecutionLockAsync(SecurityActivity securityActivity, CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: ReleaseSecurityActivityExecutionLock. Id: {0}", securityActivity.Id);

            var db = Db();
            await using (db.ConfigureAwait(false))
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE \"EFMessages\" SET \"ExecutionState\" = 'Done' WHERE \"Id\" = {0}",
                    new object[] { securityActivity.Id }, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        // ===================================================================== Consistency Check

        [Obsolete("Use async version instead.")]
        public IEnumerable<long> GetMembershipForConsistencyCheck()
        {
            return GetMembershipForConsistencyCheckAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<long>> GetMembershipForConsistencyCheckAsync(CancellationToken cancel)
        {
            using var op = SnTrace.SecurityDatabase.StartOperation(
                "PgSqlSecurityDataProvider: GetMembershipForConsistencyCheck()");

            long[] result;
            var db = Db();
            await using (db.ConfigureAwait(false))
            {
                var dbResult = await db.EFMemberships.ToArrayAsync(cancel).ConfigureAwait(false);
                result = dbResult.Select(m => (Convert.ToInt64(m.GroupId) << 32) + m.MemberId).ToArray();
            }
            op.Successful = true;
            return result;
        }

        // ===================================================================== Tools

        private static SecurityGroup EnsureGroup(int groupId, Dictionary<int, SecurityGroup> groups)
        {
            if (groups.TryGetValue(groupId, out var group))
                return group;
            group = new SecurityGroup(groupId);
            groups.Add(group.Id, group);
            return group;
        }

        private static bool RetriableException(Exception ex)
        {
            return (ex is InvalidOperationException && ex.Message.Contains("connection from the pool")) ||
                   (ex is Npgsql.NpgsqlException && ex.Message.Contains("connection"));
        }

        internal System.Threading.Tasks.Task RetryAsync(Func<System.Threading.Tasks.Task> action, CancellationToken cancel)
        {
            return RetryAsync<object>(async () =>
            {
                await action().ConfigureAwait(false);
                return null;
            }, cancel);
        }

        internal Task<T> RetryAsync<T>(Func<Task<T>> action, CancellationToken cancel)
        {
            return _retrier.RetryAsync(action,
                shouldRetryOnError: (ex, _) => RetriableException(ex),
                onAfterLastIteration: (_, ex, i) =>
                {
                    SnTrace.Security.WriteError(
                        $"Security data layer error: {ex.Message}. Retry cycle ended after {i} iterations.");
                    throw new InvalidOperationException("Security data layer timeout occurred.", ex);
                },
                cancel: cancel);
        }
    }
}
