# SnDbMigrator

A production-grade **MSSQL → PostgreSQL** database migrator for sensenet CMS.

Designed for large databases (300 GB+) with streaming blob support, checkpoint/resume, and a live progress dashboard.

## Features

| Feature | Description |
|---|---|
| **Streaming blobs** | `Files.Stream` and `EFMessages.Body` are read/written without loading entire BLOBs into memory |
| **COPY BINARY** | Non-blob tables use PostgreSQL `COPY BINARY` protocol for maximum throughput |
| **Checkpoint / resume** | Progress is saved after each table — a failed migration can be resumed |
| **FK handling** | Foreign key constraints and triggers are disabled during migration and re-enabled after |
| **Sequence fix** | PostgreSQL `SERIAL` sequences are reset to `MAX(id)` after bulk insert |
| **Verification** | Optional row-count comparison between source and target |
| **Live progress** | Spectre.Console progress bars with per-table rate tracking |
| **Graceful cancel** | `Ctrl+C` finishes the current batch and saves checkpoint |

## Prerequisites

- .NET 10 SDK
- Source: MSSQL database (SQL Server 2019+)
- Target: PostgreSQL database (16+) **with schema already created**

> **Important:** The target PostgreSQL database must already have all tables created (e.g., via sensenet's installation process). The migrator only copies **data**, not schema.

## Quick Start

```bash
# 1. Edit connection strings
cp appsettings.json appsettings.local.json
nano appsettings.local.json

# 2. Run
dotnet run
```

## Configuration

Edit `appsettings.json` (or create `appsettings.local.json` for local overrides):

```json
{
  "Migration": {
    "Source": {
      "Provider": "MsSql",
      "ConnectionString": "Server=localhost,1433;Database=sensenet;User Id=sa;Password=...;TrustServerCertificate=true"
    },
    "Target": {
      "Provider": "PostgreSql",
      "ConnectionString": "Host=localhost;Port=5432;Database=sensenet;Username=postgres;Password=..."
    },
    "BatchSize": 5000,
    "BlobBatchSize": 50,
    "CheckpointFile": "./migration-checkpoint.json",
    "SkipTables": [],
    "TruncateTarget": true,
    "DisableForeignKeys": true,
    "FixSequences": true,
    "Verify": true
  }
}
```

### Settings

| Setting | Default | Description |
|---|---|---|
| `BatchSize` | 5000 | Rows per batch for regular tables |
| `BlobBatchSize` | 50 | Rows per batch for blob tables (Files, EFMessages) |
| `CheckpointFile` | `./migration-checkpoint.json` | Path for checkpoint/resume file |
| `SkipTables` | `[]` | Tables to skip (e.g., `["LogEntries", "StatisticalData"]`) |
| `TruncateTarget` | `true` | Truncate target tables before migration |
| `DisableForeignKeys` | `true` | Disable FK constraints during migration |
| `FixSequences` | `true` | Reset SERIAL sequences to MAX(id) after migration |
| `Verify` | `true` | Verify row counts after migration |

Settings can also be overridden via:
- Environment variables: `SNMIGRATE_Migration__BatchSize=10000`
- Command line: `dotnet run -- --Migration:BatchSize=10000`

## Migration Pipeline

```
1. Pre-flight    → Test connectivity to both databases
2. Discover      → Determine which tables exist in source & target
3. Disable FKs   → ALTER TABLE ... DISABLE TRIGGER ALL
4. Truncate      → TRUNCATE TABLE ... CASCADE (if configured)
5. Migrate       → Table-by-table in FK-dependency order
   ├── Regular tables: COPY BINARY (batched by ID)
   └── Blob tables: Parameterized INSERT (streaming)
6. Fix sequences → SELECT setval(seq, MAX(id))
7. Enable FKs    → ALTER TABLE ... ENABLE TRIGGER ALL + VALIDATE
8. Verify        → Compare row counts source vs target
```

## Table Migration Order

Tables are processed in FK-dependency order (parents first):

1. `SchemaModification`, `PropertyTypes`, `ContentListTypes`, `NodeTypes`
2. `Nodes`, `Versions`
3. `LongTextProperties`, `ReferenceProperties`
4. `Files` *(blob)*, `BinaryProperties`
5. `TreeLocks`, `LogEntries`, `IndexingActivities`, `Packages`, `AccessTokens`, `SharedLocks`
6. `EFEntities`, `EFEntries`, `EFMemberships`, `EFMessages` *(blob)*
7. `StatisticalData`, `StatisticalAggregations`, `ExclusiveLocks`
8. `ClientApps`, `ClientSecrets`
9. `JournalItems`, `WorkflowNotification` *(optional)*

## Type Mapping

| MSSQL | PostgreSQL | Notes |
|---|---|---|
| `int IDENTITY` | `SERIAL` | Sequence reset after migration |
| `tinyint` | `SMALLINT` | |
| `bit` | `BOOLEAN` or `SMALLINT` | Depends on table (see code) |
| `datetime2` | `TIMESTAMP WITHOUT TIME ZONE` | |
| `timestamp` (rowversion) | `BIGINT` | Byte-reversed conversion |
| `uniqueidentifier` | `UUID` | |
| `nvarchar(N)` | `VARCHAR(N)` | |
| `nvarchar(MAX)` | `TEXT` | |
| `nvarchar(450) CI` | `CITEXT` | Only `Nodes.Path` |
| `varbinary(MAX)` | `BYTEA` | Streamed for blob tables |

## Resume After Failure

If the migration fails or is cancelled (`Ctrl+C`), a checkpoint file is saved. On next run, you'll be prompted to resume:

```
⚠ A checkpoint file exists. Resume previous migration? [Y/n]
```

The checkpoint tracks which tables have been completed and the last processed ID for the table that was in progress.

## Skipping Tables

For large databases, you may want to skip non-essential tables:

```json
{
  "Migration": {
    "SkipTables": ["LogEntries", "StatisticalData", "StatisticalAggregations", "IndexingActivities"]
  }
}
```

## File Overview

```
tools/SnDbMigrator/
├── SnDbMigrator.csproj      # .NET 10 project file
├── appsettings.json          # Default configuration
├── Program.cs                # Entry point, config loading, banner
├── MigrationOptions.cs       # Strongly-typed configuration
├── MigrationEngine.cs        # Main orchestrator (8-step pipeline)
├── TableMigrator.cs          # Per-table migration (COPY BINARY / INSERT)
├── SenseNetSchema.cs         # Table definitions, FK list, migration order
├── SequenceFixer.cs          # PostgreSQL SERIAL sequence reset
├── ForeignKeyManager.cs      # FK constraint disable/enable
├── Checkpoint.cs             # JSON checkpoint for resume support
└── README.md                 # This file
```
