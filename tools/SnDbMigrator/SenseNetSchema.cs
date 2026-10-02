namespace SnDbMigrator;

/// <summary>
/// Defines the sensenet database schema: table order, columns, identity columns,
/// blob columns, and FK relationships. Used by the migrator to process tables
/// in the correct order and handle special column types.
/// </summary>
public static class SenseNetSchema
{
    /// <summary>
    /// Tables in FK-dependency order (parents before children).
    /// Tables without FKs can appear in any order.
    /// </summary>
    public static readonly TableDef[] Tables =
    [
        // ── Schema & type definitions (no FKs) ─────────────────
        new("SchemaModification", "SchemaModificationId"),
        new("PropertyTypes", "PropertyTypeId"),
        new("ContentListTypes", "ContentListTypeId"),
        new("NodeTypes", "NodeTypeId"),

        // ── Core content tree ──────────────────────────────────
        new("Nodes", "NodeId"),
        new("Versions", "VersionId"),

        // ── Property storage ───────────────────────────────────
        new("LongTextProperties", "LongTextPropertyId"),
        new("ReferenceProperties", "ReferencePropertyId"),

        // ── Files & binaries (BLOB!) ───────────────────────────
        new("Files", "FileId", IsBlob: true, BlobColumns: ["Stream"]),
        new("BinaryProperties", "BinaryPropertyId"),

        // ── Operational tables ─────────────────────────────────
        new("TreeLocks", "TreeLockId"),
        new("LogEntries", "LogId"),
        new("IndexingActivities", "IndexingActivityId"),
        new("Packages", "Id"),
        new("AccessTokens", "AccessTokenId"),
        new("SharedLocks", "SharedLockId"),

        // ── Security tables ────────────────────────────────────
        new("EFEntities", IdentityColumn: null),  // PK is Id but NOT auto-increment
        new("EFEntries", IdentityColumn: null),    // Composite PK
        new("EFMemberships", IdentityColumn: null), // Composite PK
        new("EFMessages", "Id", IsBlob: true, BlobColumns: ["Body"]),

        // ── Statistics & locks ─────────────────────────────────
        new("StatisticalData", "Id"),
        new("StatisticalAggregations", IdentityColumn: null),  // Composite PK
        new("ExclusiveLocks", "Id"),

        // ── Client apps ────────────────────────────────────────
        new("ClientApps", IdentityColumn: null),    // PK is varchar
        new("ClientSecrets", IdentityColumn: null),  // PK is varchar

        // ── Journal & workflow (may not exist) ─────────────────
        new("JournalItems", "Id", Optional: true),
        new("WorkflowNotification", "NotificationId", Optional: true),
    ];

    /// <summary>
    /// FK constraints that must be disabled before migration
    /// and re-enabled after. Order: child → parent.
    /// </summary>
    public static readonly FkDef[] ForeignKeys =
    [
        new("NodeTypes", "FK_NodeTypes_NodeTypes"),
        new("Nodes", "FK_Nodes_NodeTypes"),
        new("Nodes", "FK_Nodes_Parent"),
        new("Nodes", "FK_Nodes_LockedBy"),
        new("Nodes", "FK_Nodes_Nodes_CreatedById"),
        new("Nodes", "FK_Nodes_Nodes_ModifiedById"),
        new("Nodes", "FK_Nodes_Nodes_ContentListId"),
        new("Versions", "FK_Versions_Nodes"),
        new("Versions", "FK_Versions_Nodes_CreatedBy"),
        new("Versions", "FK_Versions_Nodes_ModifiedBy"),
        new("BinaryProperties", "FK_BinaryProperties_PropertyTypes"),
        new("BinaryProperties", "FK_BinaryProperties_Versions"),
        new("BinaryProperties", "FK_BinaryProperties_Files"),
        new("ReferenceProperties", "FK_ReferenceProperties_PropertyTypes"),
        new("LongTextProperties", "FK_LongTextProperties_PropertyTypes"),
        new("LongTextProperties", "FK_LongTextProperties_Versions"),
        new("EFEntities", "FK_EFEntities_EFEntities_ParentId"),
        new("EFEntries", "FK_EFEntries_EFEntities_EFEntityId"),
    ];
}

/// <param name="Name">Table name (case-sensitive for PostgreSQL).</param>
/// <param name="IdentityColumn">IDENTITY/SERIAL column name, or null if no auto-increment PK.</param>
/// <param name="IsBlob">True if the table contains large binary columns.</param>
/// <param name="BlobColumns">Names of VARBINARY(MAX)/BYTEA columns.</param>
/// <param name="Optional">True if the table may not exist in older installations.</param>
public record TableDef(
    string Name,
    string? IdentityColumn = null,
    bool IsBlob = false,
    string[]? BlobColumns = null,
    bool Optional = false);

/// <param name="Table">Table that owns the FK constraint.</param>
/// <param name="ConstraintName">Name of the FK constraint.</param>
public record FkDef(string Table, string ConstraintName);
