using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient.Security
{
    /// <summary>
    /// EF entity for the "EFEntities" table (security entities).
    /// Mirrors the EFEntity from EFCSecurityStore but without SQL Server dependencies.
    /// </summary>
    [Table("EFEntities")]
    public class PgSqlEFEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public int? OwnerId { get; set; }
        public int? ParentId { get; set; }
        public bool IsInherited { get; set; }

        [ForeignKey("ParentId")]
        public PgSqlEFEntity Parent { get; set; }

        public ICollection<PgSqlEFEntity> Children { get; set; }
        public ICollection<PgSqlEFEntry> EFEntries { get; set; }
    }

    /// <summary>
    /// EF entity for the "EFEntries" table (ACE entries / permission entries).
    /// </summary>
    [Table("EFEntries")]
    public class PgSqlEFEntry
    {
        public int EFEntityId { get; set; }
        public int EntryType { get; set; }
        public int IdentityId { get; set; }
        public bool LocalOnly { get; set; }
        public long AllowBits { get; set; }
        public long DenyBits { get; set; }

        [ForeignKey("EFEntityId")]
        public PgSqlEFEntity EFEntity { get; set; }
    }

    /// <summary>
    /// EF entity for the "EFMemberships" table (group memberships).
    /// </summary>
    [Table("EFMemberships")]
    public class PgSqlEFMembership
    {
        public int GroupId { get; set; }
        public int MemberId { get; set; }
        public bool IsUser { get; set; }
    }

    /// <summary>
    /// EF entity for the "EFMessages" table (security activity messages).
    /// </summary>
    [Table("EFMessages")]
    public class PgSqlEFMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(400)]
        public string SavedBy { get; set; }

        public System.DateTime SavedAt { get; set; }

        [MaxLength(20)]
        public string ExecutionState { get; set; }

        [MaxLength(400)]
        public string LockedBy { get; set; }

        public System.DateTime? LockedAt { get; set; }

        public byte[] Body { get; set; }
    }

    // Helper types for raw SQL queries

    [Keyless]
    public class PgSqlIntItem
    {
        public int Id { get; set; }
        public int Value { get; set; }
    }

    [Keyless]
    public class PgSqlStringItem
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Keyless]
    public class PgSqlStoredSecurityEntity
    {
        public int Id { get; set; }
        public int? nullableOwnerId { get; set; }
        public int? nullableParentId { get; set; }
        public bool IsInherited { get; set; }
        public bool HasExplicitEntry { get; set; }
    }
}
