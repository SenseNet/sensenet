using Microsoft.EntityFrameworkCore;

namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient.Security
{
    /// <summary>
    /// PostgreSQL-compatible DbContext for sensenet security data.
    /// Replaces the SQL Server-only SecurityStorage from EFCSecurityStore.
    /// </summary>
    internal class PgSqlSecurityStorage : DbContext
    {
        private readonly string _connectionString;
        private readonly int _commandTimeout;

        public PgSqlSecurityStorage(string connectionString, int commandTimeout = 120)
        {
            _connectionString = connectionString;
            _commandTimeout = commandTimeout;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_connectionString, options =>
            {
                options.CommandTimeout(_commandTimeout);
            });
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<PgSqlEFEntity> EFEntities { get; set; }
        public DbSet<PgSqlEFEntry> EFEntries { get; set; }
        public DbSet<PgSqlEFMembership> EFMemberships { get; set; }
        public DbSet<PgSqlEFMessage> EFMessages { get; set; }

        // Helper entity sets for raw SQL queries
        internal DbSet<PgSqlIntItem> IntSet { get; set; }
        internal DbSet<PgSqlStringItem> StringSet { get; set; }
        internal DbSet<PgSqlStoredSecurityEntity> StoredSecurityEntitySet { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PgSqlEFEntity>()
                .HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .IsRequired(false)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<PgSqlEFEntry>()
                .HasOne(e => e.EFEntity)
                .WithMany(f => f.EFEntries)
                .IsRequired()
                .HasForeignKey(e => e.EFEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<PgSqlEFMembership>()
                .HasKey(a => new { a.GroupId, a.MemberId });

            modelBuilder.Entity<PgSqlEFEntry>()
                .HasKey(a => new { a.EFEntityId, a.EntryType, a.IdentityId, a.LocalOnly });

            base.OnModelCreating(modelBuilder);
        }
    }
}
